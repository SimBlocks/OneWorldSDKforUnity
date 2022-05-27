//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Threading;
using OsmSharp;
using sbio.owsdk.Features;
using sbio.owsdk.Geodetic;

namespace sbio.owsdk.OSM
{
  public delegate FeatureRelation OSMRelationResolver(long relationID);

  public sealed class OSMFeatureContext : IFeatureContext
  {
    public IReadOnlyDictionary<ulong, FeatureNode> Nodes
    {
      get { return (IReadOnlyDictionary<ulong, FeatureNode>)m_Nodes; }
    }

    public IReadOnlyDictionary<ulong, FeatureArea> Areas
    {
      get { return (IReadOnlyDictionary<ulong, FeatureArea>)m_Areas; }
    }

    public IReadOnlyDictionary<ulong, FeatureWay> Ways
    {
      get { return (IReadOnlyDictionary<ulong, FeatureWay>)m_Ways; }
    }

    public IReadOnlyDictionary<ulong, FeatureRelation> Relations
    {
      get { return (IReadOnlyDictionary<ulong, FeatureRelation>)m_Relations; }
    }

    public OSMFeatureContext(Action<Feature> consumer, CancellationToken tok)
    {
      m_Consumer = consumer;
      m_CancellationToken = tok;
    }

    public void Add(IEnumerable<OsmGeo> source)
    {
      foreach (var elt in source)
      {
        m_CancellationToken.ThrowIfCancellationRequested();
        AddElement(elt);
      }
    }

    public void Complete()
    {
      var resolvedObjects = new Dictionary<ulong, Feature>(m_Nodes.Count + m_EncounteredWays.Count + m_EncounteredRelations.Count);

      foreach (var kvp in m_Nodes)
      {
        resolvedObjects.Add(kvp.Key, kvp.Value);
      }

      //Finalize any ways now that we've got all the nodes
      foreach (var way in m_EncounteredWays)
      {
        var len = way.Nodes.Length;
        var coordinates = new List<Geodetic2d>(len);

        foreach (var nodeID in way.Nodes)
        {
          FeatureNode node;
          if (m_Nodes.TryGetValue((ulong)nodeID, out node))
          {
            coordinates.Add(node.Coordinate);
          }
          else
          {
            //Could not resolve
            continue;
          }
        }

        var size = coordinates.Count;

        if (size > 3 && coordinates[0] == coordinates[size - 1])
        {
          if (coordinates[0] == coordinates[size - 1])
          {
            coordinates.RemoveAt(size - 1);
          }

          if (FeatureUtil.IsClockwise((IReadOnlyList<Geodetic2d>)coordinates))
          {
            //WARNING:
            //I am calling the List<T> reverse, which mutates the list in-place
            //Be careful not to call the IEnumerable<T> extension method which does not!!!!
            ((List<Geodetic2d>)coordinates).Reverse();
          }

          var featureArea = new FeatureArea((ulong)way.Id, OSMUtil.ConvertTags(way.Tags), coordinates.ToArray());
          m_Areas.Add(featureArea.ID, featureArea);
          resolvedObjects.Add(featureArea.ID, featureArea);
        }
        else
        {
          var featureWay = new FeatureWay((ulong)way.Id, OSMUtil.ConvertTags(way.Tags), coordinates.ToArray());
          m_Ways.Add(featureWay.ID, featureWay);
          resolvedObjects.Add(featureWay.ID, featureWay);
        }

        m_CancellationToken.ThrowIfCancellationRequested();
      }

      m_EncounteredWays.Clear();

      //Now resolve relations
      bool resolvedAnything;
      var relationsToResolve = new HashSet<Relation>(m_EncounteredRelations);
      do
      {
        resolvedAnything = false;

        foreach (var relation in m_EncounteredRelations)
        {
          if (relationsToResolve.Contains(relation))
          {
            //Try and resolve it
            var members = new List<FeatureRelationMember>(relation.Members.Length);
            foreach (var member in relation.Members)
            {
              Feature feature;
              if (resolvedObjects.TryGetValue((ulong)member.Id, out feature))
              {
                members.Add(new FeatureRelationMember(member.Role, feature));
              }
              else
              {
                break;
              }
            }

            if (members.Count == relation.Members.Length)
            {
              relationsToResolve.Remove(relation);

              string typeTag = null;
              relation.Tags?.TryGetValue("type", out typeTag);

              FeatureRelation featureRelation;
              switch (typeTag)
              {
                case "multipolygon":
                  featureRelation = MultipolygonProcessor.Process(this, relation);
                  break;
                default:
                  featureRelation = RelationProcessor.Process(this, relation);
                  break;
              }

              resolvedObjects.Add(featureRelation.ID, featureRelation);
              m_Relations.Add(featureRelation.ID, featureRelation);
              resolvedAnything = true;
            }
          }
        }
      } while (resolvedAnything);

      m_EncounteredRelations.Clear();

      //Remove any nodes, ways, and relations which are contained in other relations
      foreach (var relation in m_Relations.Values)
      {
        foreach (var member in relation.Elements)
        {
          resolvedObjects.Remove(member.Feature.ID);
        }

        m_CancellationToken.ThrowIfCancellationRequested();
      }

      //We're now left with top-level features (not inside relations)
      //produce those
      foreach (var kvp in resolvedObjects)
      {
        m_Consumer(kvp.Value);
        m_CancellationToken.ThrowIfCancellationRequested();
      }
    }

    private void AddElement(OsmGeo elt)
    {
      var id = elt.Id.Value;
      if (m_PendingFeatures.Add(id))
      {
        switch (elt.Type)
        {
          case OsmGeoType.Node:
          {
            //Nodes we can resolve immediately
            var node = (Node)elt;
            var tags = OSMUtil.ConvertTags(node.Tags);
            var coordinate = Geodetic2d.FromDegrees(node.Latitude ?? 0, node.Longitude ?? 0);
            var featureNode = new FeatureNode((ulong)id, tags, coordinate);
            m_Nodes.Add(featureNode.ID, featureNode);
          }
            break;
          case OsmGeoType.Way:
          {
            //Ways we need to leave for later
            var way = (Way)elt;
            m_EncounteredWays.Add(way);
          }
            break;
          case OsmGeoType.Relation:
          {
            //Likewise for relations
            var relation = (Relation)elt;
            m_EncounteredRelations.Add(relation);
          }
            break;
        }
      }
    }

    private readonly ISet<long> m_PendingFeatures = new HashSet<long>();
    private readonly IList<Way> m_EncounteredWays = new List<Way>();
    private readonly IList<Relation> m_EncounteredRelations = new List<Relation>();

    private readonly IDictionary<ulong, FeatureNode> m_Nodes = new Dictionary<ulong, FeatureNode>();
    private readonly IDictionary<ulong, FeatureWay> m_Ways = new Dictionary<ulong, FeatureWay>();
    private readonly IDictionary<ulong, FeatureArea> m_Areas = new Dictionary<ulong, FeatureArea>();
    private readonly IDictionary<ulong, FeatureRelation> m_Relations = new Dictionary<ulong, FeatureRelation>();
    private readonly Action<Feature> m_Consumer;
    private readonly CancellationToken m_CancellationToken;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

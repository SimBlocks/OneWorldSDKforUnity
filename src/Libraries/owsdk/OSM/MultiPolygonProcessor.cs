//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;
using System.Linq;
using OsmSharp;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Features;

namespace sbio.owsdk.OSM
{
  public sealed class MultipolygonProcessor
  {
    private sealed class CoordinateSequence
    {
      public ulong ID
      {
        get { return m_ID; }
      }

      public string Role
      {
        get { return m_Role; }
      }

      public List<Geodetic2d> Coordinates
      {
        get { return m_Coordinates; }
      }

      public CoordinateSequence(ulong id, string role, IEnumerable<Geodetic2d> coords)
      {
        m_ID = id;
        m_Role = role;
        m_Coordinates = coords.ToList();
      }

      /// Tries to add another sequence onto the start or end of this one.
      /// If it succeeds, the other sequence may also be modified and
      /// should be considered "spent".
      public bool TryAdd(CoordinateSequence other)
      {
        if (other.Role != m_Role)
        {
          return false;
        }

        //add the sequence at the end
        if (last() == other.first())
        {
          m_Coordinates.RemoveAt(m_Coordinates.Count - 1);
          m_Coordinates.AddRange(other.m_Coordinates);
          return true;
        }

        //add the sequence backwards at the end
        if (last() == other.last())
        {
          m_Coordinates.RemoveAt(m_Coordinates.Count - 1);
          m_Coordinates.AddRange(((IEnumerable<Geodetic2d>)other.m_Coordinates).Reverse());
          return true;
        }

        //add the sequence at the beginning
        if (first() == other.last())
        {
          m_Coordinates.RemoveAt(0);
          m_Coordinates.InsertRange(0, other.m_Coordinates);
          return true;
        }

        //add the sequence backwards at the beginning
        if (first() == other.first())
        {
          m_Coordinates.RemoveAt(0);
          m_Coordinates.InsertRange(0, ((IEnumerable<Geodetic2d>)other.m_Coordinates).Reverse());
          return true;
        }

        return false;
      }

      public bool isClosed()
      {
        return m_Coordinates.Count > 1 && m_Coordinates[0] == m_Coordinates[m_Coordinates.Count - 1];
      }

      public bool containsRing(IEnumerable<Geodetic2d> coords)
      {
        var mCoords = m_Coordinates;
        return coords.All(p => FeatureUtil.IsPointInPolygon(p, mCoords));
      }

      private Geodetic2d first()
      {
        return m_Coordinates[0];
      }

      private Geodetic2d last()
      {
        return m_Coordinates[m_Coordinates.Count - 1];
      }

      private readonly ulong m_ID;
      private readonly string m_Role;
      private readonly List<Geodetic2d> m_Coordinates;
    }

    private readonly List<FeatureTag> m_Tags = new List<FeatureTag>();


    /// For algorithm details, see http://wiki.openstreetmap.org/wiki/Relation:multipolygon/Algorithm
    public static FeatureRelation Process(IFeatureContext context, Relation relation)
    {
      var tags = new List<FeatureTag>();
      bool allClosed = true;
      // NOTE do not count multipolygon tag itself
      bool hasNoTags = relation.Tags.Count < 2;
      List<int> outerIndices = new List<int>();
      List<int> innerIndices = new List<int>();
      List<CoordinateSequence> sequences = new List<CoordinateSequence>();

      for (var i = 0; i < relation.Members.Length; ++i)
      {
        var member = relation.Members[i];
        if (member.Type != OsmGeoType.Way)
          continue;

        IEnumerable<Geodetic2d> coordinates;

        FeatureWay way;
        if (context.Ways.TryGetValue((ulong)member.Id, out way))
        {
          coordinates = way.Coordinates;
        }
        else
        {
          FeatureArea area;
          if (context.Areas.TryGetValue((ulong)member.Id, out area))
          {
            // NOTE make coordinates to be closed ring
            coordinates = area.Coordinates.Append(area.Coordinates.First());

            // NOTE merge tags to relation
            // hasNoTags prevents the case where relation has members with their own tags
            // which should be processed independently (geometry reuse)
            if (member.Role == "outer" && hasNoTags)
            {
              MergeTags(tags, area.Tags.Values);
            }
          }
          else
          {
            coordinates = Enumerable.Empty<Geodetic2d>();
          }
        }

        if (!coordinates.Any())
        {
          continue;
        }

        if (member.Role == "outer")
        {
          outerIndices.Add(sequences.Count);
        }
        else if (member.Role == "inner")
        {
          innerIndices.Add(sequences.Count);
        }
        else
        {
          continue;
        }

        var sequence = new CoordinateSequence((ulong)member.Id, member.Role, coordinates);
        if (!sequence.isClosed())
          allClosed = false;

        sequences.Add(sequence);
      }

      if (outerIndices.Count == 1 && allClosed)
      {
        return simpleCase(relation, tags, sequences, outerIndices, innerIndices);
      }
      else
      {
        return complexCase(relation, tags, sequences);
      }
    }

    private static FeatureRelation simpleCase(Relation relation, List<FeatureTag> tags, List<CoordinateSequence> sequences, List<int> outerIndices, List<int> innerIndices)
    {
      var features = new List<FeatureRelationMember>();
      // TODO set correct tags!
      {
        var outer = sequences[outerIndices[0]];
        var id = outer.ID;
        var coordinates = new List<Geodetic2d>();

        // outer
        FeatureUtil.InsertCoordinates(outer.Coordinates, coordinates, true);
        var outerArea = new FeatureArea(id,
          Enumerable.Empty<FeatureTag>(),
          coordinates);
        features.Add(new FeatureRelationMember(outer.Role, outerArea));
      }

      // inner
      foreach (var i in innerIndices)
      {
        var seq = sequences[i];

        var id = seq.ID;
        var coordinates = new List<Geodetic2d>();

        FeatureUtil.InsertCoordinates(seq.Coordinates, coordinates, false);
        var innerArea = new FeatureArea(id,
          Enumerable.Empty<FeatureTag>(),
          coordinates);

        features.Add(new FeatureRelationMember(seq.Role, innerArea));
      }

      return new FeatureRelation((ulong)relation.Id, tags, features);
    }

    private static FeatureRelation complexCase(Relation relation, List<FeatureTag> tags, List<CoordinateSequence> sequences)
    {
      var features = new List<FeatureRelationMember>();
      var rings = createRings(sequences);

      while (rings.Any())
      {
        // find an outer ring
        CoordinateSequence outer = null;
        foreach (var candidate in rings)
        {
          bool containedInOtherRings = false;
          foreach (var other in rings)
          {
            if (other != candidate && other.containsRing(candidate.Coordinates))
            {
              containedInOtherRings = true;
              break;
            }
          }

          if (containedInOtherRings)
          {
            continue;
          }

          outer = candidate;
          rings.Remove(candidate);
          break;
        }

        // find inner rings of that ring
        List<CoordinateSequence> inners = new List<CoordinateSequence>();
        for (var i = 0; i < rings.Count; ++i)
        {
          var ring = rings[i];
          if (outer.containsRing(ring.Coordinates))
          {
            bool containedInOthers = false;
            foreach (var other in rings)
            {
              if (other != ring && other.containsRing(ring.Coordinates))
              {
                containedInOthers = true;
                break;
              }
            }

            if (!containedInOthers)
            {
              inners.Add(ring);
              rings.RemoveAt(i);
              --i;
              continue;
            }
          }
        }

        {
          // outer
          var id = outer.ID;
          var coordinates = new List<Geodetic2d>();
          FeatureUtil.InsertCoordinates(outer.Coordinates, coordinates, true);

          var outerArea = new FeatureArea(id,
            Enumerable.Empty<FeatureTag>(),
            coordinates);

          features.Add(new FeatureRelationMember(outer.Role, outerArea));
        }

        // inner: create a new area and remove the used rings
        foreach (var innerRing in inners)
        {
          var id = innerRing.ID;
          var coordinates = new List<Geodetic2d>();
          FeatureUtil.InsertCoordinates(innerRing.Coordinates, coordinates, false);

          var innerArea = new FeatureArea(id,
            Enumerable.Empty<FeatureTag>(),
            coordinates);

          features.Add(new FeatureRelationMember(innerRing.Role, innerArea));
        }
      }

      return new FeatureRelation((ulong)relation.Id, tags, features);
    }

    private static List<CoordinateSequence> createRings(List<CoordinateSequence> sequences)
    {
      List<CoordinateSequence> closedRings = new List<CoordinateSequence>();
      CoordinateSequence currentRing = null;
      while (sequences.Any())
      {
        if (currentRing == null)
        {
          // start a new ring with any remaining node sequence
          var lastIndex = sequences.Count - 1;
          currentRing = sequences[lastIndex];
          sequences.RemoveAt(lastIndex);
        }
        else
        {
          // try to continue the ring by appending a node sequence
          bool isFound = false;
          foreach (var seq in sequences)
          {
            if (!currentRing.TryAdd(seq))
            {
              continue;
            }

            isFound = true;
            sequences.Remove(seq);
            break;
          }

          if (!isFound)
            return new List<CoordinateSequence>();
        }

        // check whether the ring under construction is closed
        if (currentRing != null && currentRing.isClosed())
        {
          // TODO check that it isn't self-intersecting!
          closedRings.Add(currentRing);
          currentRing = null;
        }
      }

      return closedRings;
    }

    private static void MergeTags(List<FeatureTag> tags, IEnumerable<FeatureTag> other)
    {
      var newTags = tags.Where(newTag => !tags.Any(oldTag => newTag.ID == oldTag.ID)).ToArray();
      tags.InsertRange(tags.Count - 1, newTags);
    }

    private void MergeTags(IEnumerable<FeatureTag> tags)
    {
      MergeTags(m_Tags, tags);
    }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

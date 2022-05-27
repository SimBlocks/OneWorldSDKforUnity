//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Linq;
using OsmSharp;
using OsmSharp.Tags;
using sbio.owsdk.Features;

namespace sbio.owsdk.OSM
{
  public static class OSMUtil
  {
    public static void VisitRelationMembers(IRelationMemberVisitor visitor, IFeatureContext featureContext, RelationMember[] relationMembers)
    {
      for (var i = 0; i < relationMembers.Length; ++i)
      {
        var member = relationMembers[i];
        switch (member.Type)
        {
          case OsmGeoType.Node:
          {
            FeatureNode node;
            if (featureContext.Nodes.TryGetValue((ulong)member.Id, out node))
            {
              visitor.Accept(Tuple.Create(node, member.Role));
            }
          }
            break;
          case OsmGeoType.Way:
          {
            FeatureArea area;
            if (featureContext.Areas.TryGetValue((ulong)member.Id, out area))
            {
              visitor.Accept(Tuple.Create(area, member.Role));
            }
            else
            {
              FeatureWay way;
              if (featureContext.Ways.TryGetValue((ulong)member.Id, out way))
              {
                visitor.Accept(Tuple.Create(way, member.Role));
              }
            }
          }
            break;
          case OsmGeoType.Relation:
          {
            FeatureRelation childRelation;
            if (featureContext.Relations.TryGetValue((ulong)member.Id, out childRelation))
            {
              visitor.Accept(Tuple.Create(childRelation, member.Role));
            }
          }
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      }
    }

    public static IEnumerable<FeatureTag> ConvertTags(IEnumerable<Tag> tags)
    {
      if (tags == null)
      {
        return Enumerable.Empty<FeatureTag>();
      }
      else
      {
        return tags.Select(t => new FeatureTag(t.Key, t.Value));
      }
    }
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

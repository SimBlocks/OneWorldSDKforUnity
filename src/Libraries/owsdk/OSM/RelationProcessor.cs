//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Linq;
using OsmSharp;
using sbio.owsdk.Features;

namespace sbio.owsdk.OSM
{
  public sealed class RelationProcessor
    : IRelationMemberVisitor
  {
    public static FeatureRelation Process(IFeatureContext context, Relation relation)
    {
      var processor = new RelationProcessor(relation);

      OSMUtil.VisitRelationMembers(processor, context, relation.Members);

      return new FeatureRelation((ulong)relation.Id,
        OSMUtil.ConvertTags(relation.Tags),
        processor.m_Features);
    }

    private RelationProcessor(Relation relation)
    {
      m_Relation = relation;
    }

    public void Accept(Tuple<FeatureNode, string> elt)
    {
      if (!isAlreadyProcessed(elt.Item1))
      {
        m_Features.Add(new FeatureRelationMember(elt.Item2, elt.Item1));
      }
    }

    public void Accept(Tuple<FeatureArea, string> elt)
    {
      if (!isAlreadyProcessed(elt.Item1))
      {
        m_Features.Add(new FeatureRelationMember(elt.Item2, elt.Item1));
      }
    }

    public void Accept(Tuple<FeatureWay, string> elt)
    {
      if (!isAlreadyProcessed(elt.Item1))
      {
        m_Features.Add(new FeatureRelationMember(elt.Item2, elt.Item1));
      }
    }

    public void Accept(Tuple<FeatureRelation, string> elt)
    {
      if (isAlreadyProcessed(elt.Item1) || hasReferenceToParent(elt.Item1))
      {
        return;
      }

      //TODO Might need to modify code to have resolving occur here instead,
      //because we might not want to resolve nodes that are referencing parents
      m_Features.Add(new FeatureRelationMember(elt.Item2, elt.Item1));
    }

    private bool isAlreadyProcessed(Feature feature)
    {
      return m_Features.Any(m => m.Feature == feature);
    }

    /// Checks whether relation has reference to current. If yes, it should not be
    /// processed because of recursion.
    /// NOTE This check finds only simple cases.
    private bool hasReferenceToParent(FeatureRelation rel)
    {
      return rel.Elements.Any(e => e.Feature.ID == (ulong)m_Relation.Id);
    }

    private readonly Relation m_Relation;
    private readonly IList<FeatureRelationMember> m_Features = new List<FeatureRelationMember>();
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

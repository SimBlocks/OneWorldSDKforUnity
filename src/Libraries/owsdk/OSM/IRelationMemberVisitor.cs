//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.owsdk.Features;


namespace sbio.owsdk.OSM
{
  public interface IRelationMemberVisitor
    : IVisitor<Tuple<FeatureNode, string>>
      , IVisitor<Tuple<FeatureArea, string>>
      , IVisitor<Tuple<FeatureWay, string>>
      , IVisitor<Tuple<FeatureRelation, string>>
  {
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

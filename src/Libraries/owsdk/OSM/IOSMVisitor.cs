//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using OsmSharp;

namespace sbio.owsdk.OSM
{
  public interface IOSMVisitor
    : IVisitor<Node>
      , IVisitor<Way>
      , IVisitor<Relation>
  {
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

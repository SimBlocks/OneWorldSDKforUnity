//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;
using OsmSharp;

namespace sbio.owsdk.OSM
{
  public interface IOSMContext
  {
    IReadOnlyDictionary<long, Node> Nodes { get; }
    IReadOnlyDictionary<long, Way> Ways { get; }
    IReadOnlyDictionary<long, Relation> Relations { get; }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

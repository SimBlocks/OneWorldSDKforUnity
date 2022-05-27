//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;

namespace sbio.owsdk.Features
{
  public interface IFeatureContext
  {
    IReadOnlyDictionary<ulong, FeatureNode> Nodes { get; }
    IReadOnlyDictionary<ulong, FeatureArea> Areas { get; }
    IReadOnlyDictionary<ulong, FeatureWay> Ways { get; }
    IReadOnlyDictionary<ulong, FeatureRelation> Relations { get; }
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

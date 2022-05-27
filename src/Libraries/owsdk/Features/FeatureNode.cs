//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;
using System.Linq;
using sbio.owsdk.Geodetic;

namespace sbio.owsdk.Features
{
  public sealed class FeatureNode : Feature
  {
    public Geodetic2d Coordinate
    {
      get { return m_Coordinate; }
    }

    public FeatureNode(ulong id, IEnumerable<FeatureTag> tags, Geodetic2d coordinate)
      : this(id, tags.ToArray(), coordinate)
    {
    }

    public FeatureNode(ulong id, FeatureTag[] tags, Geodetic2d coordinate)
      : base(id, tags)
    {
      m_Coordinate = coordinate;
    }

    private readonly Geodetic2d m_Coordinate;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

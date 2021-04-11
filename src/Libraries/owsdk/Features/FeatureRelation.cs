//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;
using System.Linq;

namespace sbio.owsdk.Features
{
  public sealed class FeatureRelation : Feature
  {
    public FeatureRelationMember[] Elements
    {
      get { return m_Elements; }
    }

    public FeatureRelation(ulong id, IEnumerable<FeatureTag> tags, IEnumerable<Feature> elements, IEnumerable<string> roles)
      : this(id, tags.ToArray(), elements.Zip(roles, (e, r) => new FeatureRelationMember(r, e)))
    {
    }

    public FeatureRelation(ulong id, IEnumerable<FeatureTag> tags, IEnumerable<FeatureRelationMember> elements)
      : base(id, tags)
    {
      m_Elements = elements.ToArray();
    }

    private readonly FeatureRelationMember[] m_Elements;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

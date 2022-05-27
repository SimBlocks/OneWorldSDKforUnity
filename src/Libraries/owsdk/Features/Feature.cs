//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sbio.owsdk.Features
{
  public abstract class Feature
  {
    public ulong ID
    {
      get { return m_ID; }
    }

    public IDictionary<string, FeatureTag> Tags
    {
      get { return m_Tags; }
    }

    public override string ToString()
    {
      var builder = new StringBuilder();

      builder.AppendFormat("[{0}]{{", ID);
      bool first = true;
      foreach (var tag in Tags.Values)
      {
        if (!first)
        {
          builder.Append(',');
        }

        builder.AppendFormat("{0}={1}", tag.ID, tag.Value);
        first = false;
      }

      builder.Append('}');
      return builder.ToString();
    }

    protected Feature(ulong id, IEnumerable<FeatureTag> tags)
      : this(id, tags.ToDictionary(t => t.ID))
    {
    }

    protected Feature(ulong id, IDictionary<string, FeatureTag> tags)
    {
      m_ID = id;
      m_Tags = tags;
    }

    private readonly ulong m_ID;
    private readonly IDictionary<string, FeatureTag> m_Tags;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

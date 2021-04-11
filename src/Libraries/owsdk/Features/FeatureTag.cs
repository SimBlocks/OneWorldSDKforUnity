//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Concurrent;

namespace sbio.owsdk.Features
{
  public struct FeatureTag : IEquatable<FeatureTag>
  {
    private static readonly ConcurrentDictionary<string, string> sc_TagStringPool = new ConcurrentDictionary<string, string>();

    public string ID
    {
      get { return m_ID; }
    }

    public string Value
    {
      get { return m_Value; }
    }

    public bool Equals(FeatureTag other)
    {
      return Object.ReferenceEquals(m_ID, other.m_ID)
             && Object.ReferenceEquals(m_Value, other.m_Value);
    }

    public override bool Equals(object obj)
    {
      if (obj is FeatureTag)
      {
        return Equals((FeatureTag)obj);
      }

      return base.Equals(obj);
    }

    public override int GetHashCode()
    {
      return m_ID.GetHashCode() ^ m_Value.GetHashCode();
    }

    public FeatureTag(string id, string value)
    {
      m_ID = GetPooledString(id);
      m_Value = GetPooledString(value);
    }

    private static string GetPooledString(string str)
    {
      return sc_TagStringPool.GetOrAdd(str, val => val);
    }

    private readonly string m_ID;
    private readonly string m_Value;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

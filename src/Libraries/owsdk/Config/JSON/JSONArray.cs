//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace sbio.owsdk.Config.JSON
{
  class JSONArray : IConfigArray
  {
    public IConfigValue this[int index]
    {
      get { return GetValue(index, ConfigNull.Null); }
    }

    public int Length
    {
      get { return m_Array.Count; }
    }

    public IConfigValue GetValue(int index, IConfigValue defaultValue)
    {
      return new JSONValue(m_Array[index]);
    }

    public IConfigArray GetArray(int index, IConfigArray defaultValue)
    {
      var tok = m_Array[index];
      if (tok.Type == JTokenType.Object)
      {
        return new JSONArray(tok.Value<JArray>());
      }

      return defaultValue;
    }

    public bool GetBool(int index, bool defaultValue)
    {
      var tok = m_Array[index];
      if (tok.Type == JTokenType.Boolean)
      {
        return tok.Value<bool>();
      }

      return defaultValue;
    }

    public int GetInt(int index, int defaultValue)
    {
      var tok = m_Array[index];
      if (tok.Type == JTokenType.Integer)
      {
        return tok.Value<int>();
      }

      return defaultValue;
    }

    public double GetNumber(int index, double defaultValue)
    {
      var tok = m_Array[index];
      if (tok.Type == JTokenType.Float)
      {
        return tok.Value<double>();
      }

      return defaultValue;
    }

    public IConfigObject GetObject(int index, IConfigObject defaultValue)
    {
      var tok = m_Array[index];
      if (tok.Type == JTokenType.Object)
      {
        return new JSONObject(tok.Value<JObject>());
      }

      return defaultValue;
    }

    public string GetString(int index, string defaultValue)
    {
      var tok = m_Array[index];
      if (tok.Type == JTokenType.String)
      {
        return tok.Value<string>();
      }

      return defaultValue;
    }

    public IEnumerator GetEnumerator()
    {
      foreach (var tok in MeaningfulValues())
      {
        yield return new JSONValue(tok);
      }
    }

    IEnumerator<IConfigValue> IEnumerable<IConfigValue>.GetEnumerator()
    {
      foreach (var tok in MeaningfulValues())
      {
        yield return new JSONValue(tok);
      }
    }

    public JSONArray(JArray array)
    {
      m_Array = array;
    }

    private IEnumerable<JToken> MeaningfulValues()
    {
      foreach (var val in m_Array)
      {
        switch (val.Type)
        {
          case JTokenType.Null:
          case JTokenType.Boolean:
          case JTokenType.Integer:
          case JTokenType.Float:
          case JTokenType.String:
          case JTokenType.Object:
          case JTokenType.Array:
            yield return val;
            break;
          default:
            continue;
        }
      }
    }

    private readonly JArray m_Array;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

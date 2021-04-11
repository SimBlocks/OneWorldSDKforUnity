//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using Newtonsoft.Json.Linq;

namespace sbio.owsdk.Config.JSON
{
  public class JSONObject : IConfigObject
  {
    public IConfigValue this[string path]
    {
      get { return this.GetValue(path, ConfigNull.Null); }
    }

    public bool HasValue(string path)
    {
      JToken tmp;
      return m_Object.TryGetPathedValue(path, out tmp);
    }

    public bool TryGetValue(string path, out IConfigValue value)
    {
      JToken val;
      if (m_Object.TryGetPathedValue(path, out val))
      {
        value = new JSONValue(val);
        return true;
      }
      else
      {
        value = null;
        return false;
      }
    }

    public bool TryGetBool(string path, out bool value)
    {
      JToken val;
      if (m_Object.TryGetPathedValue(path, out val))
      {
        value = val.Value<bool>();
        return true;
      }
      else
      {
        value = false;
        return false;
      }
    }

    public bool TryGetInt(string path, out int value)
    {
      JToken val;
      if (m_Object.TryGetPathedValue(path, out val))
      {
        value = val.Value<int>();
        return true;
      }
      else
      {
        value = 0;
        return false;
      }
    }

    public bool TryGetNumber(string path, out double value)
    {
      JToken val;
      if (m_Object.TryGetPathedValue(path, out val))
      {
        value = val.Value<double>();
        return true;
      }
      else
      {
        value = 0.0;
        return false;
      }
    }

    public bool TryGetString(string path, out string value)
    {
      JToken val;
      if (m_Object.TryGetPathedValue(path, out val))
      {
        value = val.Value<string>();
        return true;
      }
      else
      {
        value = string.Empty;
        return false;
      }
    }

    public bool TryGetObject(string path, out IConfigObject value)
    {
      JToken val;
      if (m_Object.TryGetPathedValue(path, out val))
      {
        if (val.Type == JTokenType.Object)
        {
          value = new JSONObject(val.Value<JObject>());
          return true;
        }
      }

      value = null;
      return false;
    }

    public bool TryGetArray(string path, out IConfigArray value)
    {
      JToken val;
      if (m_Object.TryGetPathedValue(path, out val))
      {
        if (val.Type == JTokenType.Array)
        {
          value = new JSONArray(val.Value<JArray>());
          return true;
        }
      }

      value = null;
      return false;
    }

    public JSONObject(JObject obj)
    {
      m_Object = obj;
    }

    private readonly JObject m_Object;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

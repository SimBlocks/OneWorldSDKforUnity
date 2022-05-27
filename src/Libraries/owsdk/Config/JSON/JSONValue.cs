//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using Newtonsoft.Json.Linq;

namespace sbio.owsdk.Config.JSON
{
  public struct JSONValue : IConfigValue
  {
    public bool BoolValue
    {
      get { return m_Token.Value<bool>(); }
    }

    public IConfigArray ArrayValue
    {
      get
      {
        if (m_Token.Type != JTokenType.Null)
        {
          return new JSONArray(m_Token.Value<JArray>());
        }
        else
        {
          return ConfigNull.Null;
        }
      }
    }

    public int IntValue
    {
      get { return m_Token.Value<int>(); }
    }

    public double NumberValue
    {
      get { return m_Token.Value<double>(); }
    }

    public IConfigObject ObjectValue
    {
      get
      {
        if (m_Token.Type != JTokenType.Null)
        {
          return new JSONObject(m_Token.Value<JObject>());
        }
        else
        {
          return ConfigNull.Null;
        }
      }
    }

    public string StringValue
    {
      get { return m_Token.Value<string>(); }
    }

    public ConfigValueType ValueType
    {
      get
      {
        switch (m_Token.Type)
        {
          case JTokenType.Integer:
            return ConfigValueType.Integer;
          case JTokenType.Boolean:
            return ConfigValueType.Boolean;
          case JTokenType.Float:
            return ConfigValueType.Number;
          case JTokenType.String:
            return ConfigValueType.String;
          case JTokenType.Object:
            return ConfigValueType.Object;
          case JTokenType.Array:
            return ConfigValueType.Array;
          case JTokenType.Null:
            return ConfigValueType.Null;
          default:
            throw new Exception();
        }
      }
    }

    public JSONValue(JToken token)
    {
      m_Token = token;
    }

    private readonly JToken m_Token;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

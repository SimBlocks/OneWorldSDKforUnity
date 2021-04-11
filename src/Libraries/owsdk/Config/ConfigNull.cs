//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace sbio.owsdk.Config
{
  public class ConfigNull : IConfigValue
    , IConfigObject
    , IConfigArray
  {
    public static ConfigNull Null
    {
      get { return new ConfigNull(); }
    }

    public ConfigValueType ValueType { get; }

    public bool BoolValue
    {
      get { return false; }
    }

    public int IntValue
    {
      get { return 0; }
    }

    public double NumberValue
    {
      get { return 0; }
    }

    public string StringValue
    {
      get { return "null"; }
    }

    public IConfigObject ObjectValue { get; }
    public IConfigArray ArrayValue { get; }

    public int Length
    {
      get { return 0; }
    }

    public IConfigValue this[int index]
    {
      get { return this; }
    }

    public IConfigValue this[string path]
    {
      get { return this; }
    }

    private ConfigNull()
    {
    }

    public bool HasValue(string path)
    {
      return false;
    }

    public bool TryGetValue(string path, out IConfigValue value)
    {
      value = this;
      return false;
    }

    public bool TryGetBool(string path, out bool value)
    {
      value = BoolValue;
      return false;
    }

    public bool TryGetInt(string path, out int value)
    {
      value = IntValue;
      return false;
    }

    public bool TryGetNumber(string path, out double value)
    {
      value = NumberValue;
      return false;
    }

    public bool TryGetString(string path, out string value)
    {
      value = StringValue;
      return false;
    }

    public bool TryGetObject(string path, out IConfigObject value)
    {
      value = this;
      return false;
    }

    public bool TryGetArray(string path, out IConfigArray value)
    {
      value = this;
      return false;
    }

    public IConfigValue GetValue(int index, IConfigValue defaultValue)
    {
      return defaultValue;
    }

    public bool GetBool(int index, bool defaultValue)
    {
      return defaultValue;
    }

    public int GetInt(int index, int defaultValue)
    {
      return defaultValue;
    }

    public double GetNumber(int index, double defaultValue)
    {
      return defaultValue;
    }

    public string GetString(int index, string defaultValue)
    {
      return defaultValue;
    }

    public IConfigObject GetObject(int index, IConfigObject defaultValue)
    {
      return defaultValue;
    }

    public IConfigArray GetArray(int index, IConfigArray defaultValue)
    {
      return defaultValue;
    }

    public IEnumerator<IConfigValue> GetEnumerator()
    {
      return Enumerable.Empty<IConfigValue>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return Enumerable.Empty<IConfigValue>().GetEnumerator();
    }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

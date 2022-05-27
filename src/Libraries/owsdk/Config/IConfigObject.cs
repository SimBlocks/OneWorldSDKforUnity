//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

namespace sbio.owsdk.Config
{
  public interface IConfigObject
  {
    IConfigValue this[string path] { get; }
    bool HasValue(string path);

    bool TryGetValue(string path, out IConfigValue value);
    bool TryGetBool(string path, out bool value);
    bool TryGetInt(string path, out int value);
    bool TryGetNumber(string path, out double value);
    bool TryGetString(string path, out string value);
    bool TryGetObject(string path, out IConfigObject value);
    bool TryGetArray(string path, out IConfigArray value);
  }

  public static class IConfigObjectExtensions
  {
    public static IConfigValue GetValue(this IConfigObject obj, string path)
    {
      return obj.GetValue(path, ConfigNull.Null);
    }

    public static IConfigValue GetValue(this IConfigObject obj, string path, IConfigValue defaultValue)
    {
      IConfigValue ret;
      if (obj.TryGetValue(path, out ret))
      {
        return ret;
      }

      return defaultValue;
    }

    public static bool GetBool(this IConfigObject obj, string path, bool defaultValue = default(bool))
    {
      bool ret;
      if (obj.TryGetBool(path, out ret))
      {
        return ret;
      }

      return defaultValue;
    }

    public static int GetInt(this IConfigObject obj, string path, int defaultValue = default(int))
    {
      int ret;
      if (obj.TryGetInt(path, out ret))
      {
        return ret;
      }

      return defaultValue;
    }

    public static double GetNumber(this IConfigObject obj, string path, double defaultValue = default(double))
    {
      double ret;
      if (obj.TryGetNumber(path, out ret))
      {
        return ret;
      }
      else
      {
        return defaultValue;
      }
    }

    public static string GetString(this IConfigObject obj, string path)
    {
      return obj.GetString(path, ConfigNull.Null.StringValue);
    }

    public static string GetString(this IConfigObject obj, string path, string defaultValue)
    {
      string ret;
      if (obj.TryGetString(path, out ret))
      {
        return ret;
      }

      return defaultValue;
    }

    public static IConfigObject GetObject(this IConfigObject obj, string path)
    {
      return obj.GetObject(path, ConfigNull.Null);
    }

    public static IConfigObject GetObject(this IConfigObject obj, string path, IConfigObject defaultValue)
    {
      IConfigObject ret;
      if (obj.TryGetObject(path, out ret))
      {
        return ret;
      }

      return defaultValue;
    }

    public static IConfigArray GetArray(this IConfigObject obj, string path)
    {
      return obj.GetArray(path, ConfigNull.Null);
    }

    public static IConfigArray GetArray(this IConfigObject obj, string path, IConfigArray defaultValue)
    {
      IConfigArray ret;
      if (obj.TryGetArray(path, out ret))
      {
        return ret;
      }

      return defaultValue;
    }
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

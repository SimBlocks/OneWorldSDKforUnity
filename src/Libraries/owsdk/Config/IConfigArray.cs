//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;

namespace sbio.owsdk.Config
{
  public interface IConfigArray :
    IEnumerable<IConfigValue>
  {
    IConfigValue this[int index] { get; }

    int Length { get; }

    IConfigValue GetValue(int index, IConfigValue defaultValue);
    bool GetBool(int index, bool defaultValue = default(bool));
    int GetInt(int index, int defaultValue = default(int));
    double GetNumber(int index, double defaultValue = default(double));
    string GetString(int index, string defaultValue);
    IConfigObject GetObject(int index, IConfigObject defaultValue);
    IConfigArray GetArray(int index, IConfigArray defaultValue);
  }

  public static class IConfigArrayExtensions
  {
    public static string GetString(this IConfigArray ary, int index)
    {
      return ary.GetString(index, ConfigNull.Null.StringValue);
    }

    public static IConfigValue GetValue(this IConfigArray ary, int index)
    {
      return ary.GetValue(index, ConfigNull.Null);
    }

    public static IConfigObject GetObject(this IConfigArray ary, int index)
    {
      return ary.GetObject(index, ConfigNull.Null);
    }

    public static IConfigArray GetArray(this IConfigArray ary, int index)
    {
      return ary.GetArray(index, ConfigNull.Null);
    }
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using Newtonsoft.Json.Linq;

namespace sbio.owsdk.Config.JSON
{
  public static class JSONExtensions
  {
    public static string[] SplitPath(string path)
    {
      return path.Split(new char[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool TryGetPathedValue(this JObject obj, string path, out JToken val)
    {
      JContainer currObj = obj;
      JToken tmp = null;
      foreach (var propName in SplitPath(path))
      {
        int intVal;
        var parsedInt = int.TryParse(propName, out intVal);
        if (currObj == null)
        {
          tmp = null;
          break;
        }
        else if (currObj is JObject && ((JObject)currObj).TryGetValue(propName, StringComparison.InvariantCulture, out tmp))
        {
          currObj = tmp as JContainer;
          continue;
        }
        else if (currObj is JArray && parsedInt && ((JArray)currObj).Count < intVal)
        {
          tmp = ((JArray)currObj)[intVal];
          currObj = tmp as JContainer;
        }
        else
        {
          break;
        }
      }

      if (tmp == null
          || tmp.Type == JTokenType.Null)
      {
        val = null;
        return false;
      }
      else
      {
        val = tmp;
        return true;
      }
    }
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

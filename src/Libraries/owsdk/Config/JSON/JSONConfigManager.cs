//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sbio.owsdk.Config.JSON
{
  public sealed class JSONConfigManager : IConfigManager
  {
    public DirectoryInfo SystemAppDataDir { get; private set; }
    public DirectoryInfo SystemProductDataDir { get; private set; }
    public DirectoryInfo UserAppDataDir { get; private set; }
    public DirectoryInfo UserProductDataDir { get; private set; }

    public IConfigValue this[string path]
    {
      get
      {
        IConfigValue ret;
        if (TryGetValue(path, out ret))
        {
          return ret;
        }
        else
        {
          return ConfigNull.Null;
        }
      }
    }

    public bool HasValue(string path)
    {
      JToken tmp;
      return m_CachedFile.TryGetPathedValue(path, out tmp);
    }

    public bool TryGetValue(string path, out IConfigValue value)
    {
      JToken val;
      if (m_CachedFile.TryGetPathedValue(path, out val))
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
      if (m_CachedFile.TryGetPathedValue(path, out val))
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
      if (m_CachedFile.TryGetPathedValue(path, out val))
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
      if (m_CachedFile.TryGetPathedValue(path, out val))
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
      if (m_CachedFile.TryGetPathedValue(path, out val))
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
      if (m_CachedFile.TryGetPathedValue(path, out val))
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
      if (m_CachedFile.TryGetPathedValue(path, out val))
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

    public DirectoryInfo InstallDir { get; private set; }
    public DirectoryInfo DataDir { get; private set; }
    public FileInfo SystemConfigFile { get; private set; }
    public FileInfo LocalConfigFile { get; private set; }
    public FileInfo UserConfigFile { get; private set; }
    public string AppName { get; private set; }
    public string CompanyName { get; private set; }
    public string ProductName { get; private set; }

    public JSONConfigManager(DirectoryInfo installDir, string app, string company = null, string product = null)
    {
      if (installDir == null)
      {
        throw new ArgumentNullException(nameof(installDir));
      }

      if (string.IsNullOrEmpty(app))
      {
        throw new ArgumentException("cannot be null or empty", nameof(app));
      }


      InstallDir = new DirectoryInfo(Path.GetFullPath(installDir.FullName));
      DataDir = new DirectoryInfo(CombinePath(InstallDir.FullName, sc_DataDirName));
      AppName = FixAndCheckName(app, "app");
      CompanyName = FixAndCheckName(company, "company");
      ProductName = FixAndCheckName(product, "product");

      var appConfigFileNameAndExt = AppName + sc_ConfigFileExt;
      var localConfigFileNameAndExt = AppName + sc_LocalConfigFileExt;

      var systemDataDir = DataDir;
      var userDataDir = new DirectoryInfo(CombinePath(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        , CompanyName
        , ProductName));

      SystemAppDataDir = new DirectoryInfo(CombinePath(
        systemDataDir.FullName
        , sc_AppDirName
        , AppName));

      SystemProductDataDir = new DirectoryInfo(CombinePath(
        systemDataDir.FullName
        , sc_SharedDirName));

      UserAppDataDir = new DirectoryInfo(CombinePath(
        userDataDir.FullName
        , sc_AppDirName
        , AppName));

      UserProductDataDir = new DirectoryInfo(CombinePath(
        userDataDir.FullName
        , sc_SharedDirName));

      SystemConfigFile = new FileInfo(CombinePath(
        systemDataDir.FullName
        , appConfigFileNameAndExt));

      LocalConfigFile = new FileInfo(CombinePath(
        systemDataDir.FullName
        , localConfigFileNameAndExt));

      UserConfigFile = new FileInfo(CombinePath(
        userDataDir.FullName
        , appConfigFileNameAndExt));

      m_CachedFile = new JObject();

      var mergeHandling = new JsonMergeSettings {MergeNullValueHandling = MergeNullValueHandling.Merge, MergeArrayHandling = MergeArrayHandling.Concat};

      if (SystemConfigFile.Exists)
      {
        using (var sr = SystemConfigFile.OpenText())
        using (var jsonReader = new JsonTextReader(sr))
        {
          try
          {
            m_CachedFile.Merge(JToken.ReadFrom(jsonReader), mergeHandling);
          }
          catch (JsonReaderException)
          {
          }
        }
      }

      if (LocalConfigFile.Exists)
      {
        using (var sr = LocalConfigFile.OpenText())
        using (var jsonReader = new JsonTextReader(sr))
        {
          try
          {
            m_CachedFile.Merge(JToken.ReadFrom(jsonReader), mergeHandling);
          }
          catch (JsonReaderException)
          {
          }
        }
      }

      if (UserConfigFile.Exists)
      {
        using (var sr = UserConfigFile.OpenText())
        using (var jsonReader = new JsonTextReader(sr))
        {
          try
          {
            m_CachedFile.Merge(JToken.ReadFrom(jsonReader), mergeHandling);
          }
          catch (JsonReaderException)
          {
          }
        }
      }
    }

    private static string FixAndCheckName(string name, string argName)
    {
      if (name == null)
      {
        name = string.Empty;
      }

      if (name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
      {
        throw new ArgumentException("argument cannot contain file name characters", argName);
      }

      return name;
    }

    private static string CombinePath(string path1, string path2, params string[] restPaths)
    {
      //Not at all efficient
      var ret = Path.Combine(path1, path2);

      foreach (var p in restPaths)
      {
        ret = Path.Combine(ret, p);
      }

      return ret;
    }

    private const string sc_DataDirName = "data";
    private const string sc_SharedDirName = "shared";
    private const string sc_AppDirName = "app";
    private const string sc_ConfigFileExt = ".config.json";
    private const string sc_LocalConfigFileExt = ".local.config.json";

    private JObject m_CachedFile;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using sbio.owsdk.Async;
using sbio.owsdk.Config;
using sbio.owsdk.Config.JSON;
using sbio.owsdk.Services;
using sbio.owsdk.Providers;
using sbio.owsdk.Providers.Bing;
using sbio.owsdk.Providers.OSM;
using sbio.owsdk.Providers.SQL;
using sbio.owsdk.Providers.WebServices;
using sbio.owsdk.Utilities;
using sbio.owsdk.WMS;
using UnityEngine;
using sbio.owsdk.Unity;

namespace sbio.OneWorldSDKViewer
{
  /// <summary>
  /// Sets up configuration options for the OneWorldSDKViewerContext and starts loading
  /// </summary>
  public sealed class OneWorldSDKViewer : MonoBehaviour
  {
    #region MonoBehaviour

    public OneWorldSDKViewerContext OneWorldSDKViewerContext;

    public Texture2D TestPatternTexture;

    public string AppName = "OneWorldSDK_Viewer";
    public string CompanyName = "SimBlocks LLC";

    public string ProductName = "OneWorldSDK Viewer";

    private void Awake()
    {
      //Overwrite configs from ones from JSON
      DirectoryInfo projectRootDir;
      if (Application.isEditor)
      {
        projectRootDir = new DirectoryInfo(Path.Combine(Application.dataPath, "../../../"));
      }
      else
      {
        projectRootDir = new DirectoryInfo(Path.Combine(Application.dataPath, "../../../"));
      }

      m_Config = new JSONConfigManager(projectRootDir
        , app: AppName
        , company: CompanyName
        , product: ProductName);

      m_Disposables = new Stack<IDisposable>();

      LoadConfiguration();
    }

    private void Start()
    {
      StartCoroutine(Load());
    }

    private void OnApplicationQuit()
    {
      if (m_LoadingTask != null)
      {
        m_LoadingTokenSource.Cancel();
        try
        {
          m_LoadingTask.Wait();
        }
        catch
        {
        }

        m_LoadingTask = null;
        m_LoadingTokenSource = null;
      }

      OneWorldSDKViewerContext.Shutdown.Raise();

      if (m_Disposables != null)
      {
        var exceptions = default(List<Exception>);
        while (m_Disposables.Any())
        {
          var disp = m_Disposables.Pop();
          try
          {
            disp.Dispose();
          }
          catch (Exception e)
          {
            if (exceptions == null)
            {
              exceptions = new List<Exception>();
            }

            exceptions.Add(e);
          }
        }

        m_Disposables = null;

        if (exceptions != null)
        {
          throw new AggregateException(exceptions);
        }
      }
    }

    private void Update()
    {
    }

    #endregion //MonoBehaviour

    private static bool AllCancellable(List<AsyncTask> loaders)
    {
      for (var i = 0; i < loaders.Count; ++i)
      {
        if (!loaders[i].Current)
        {
          return false;
        }
      }

      return true;
    }

    private void LoadConfiguration()
    {
      var config = OneWorldSDKViewerContext.Config;
      SerializedGeodetic3d defaultVals = new SerializedGeodetic3d(0.0, 0.0, 4000000);
      config.StartPosition = m_Config.GetGeodetic3d("startingPosition", defaultVals);

      {
        //Destinations
        IConfigArray destinationPoints;
        if (m_Config.TryGetArray("destinationPoints", out destinationPoints))
        {
          config.DestinationPoints = destinationPoints
            .Select(v => (SerializedGeodetic3d)v.Geodetic3dVal())
            .ToArray();
        }
      }

      {
        //Terrain Tiles
        IEnumerable<IConfigValue> terrainProviderTypes;
        {
          var providersVal = m_Config.GetValue("terrainTileProviders");
          {
            switch (providersVal.ValueType)
            {
              case ConfigValueType.String:
              case ConfigValueType.Object:
              {
                terrainProviderTypes = new IConfigValue[] {providersVal};
              }
                break;
              case ConfigValueType.Array:
              {
                terrainProviderTypes = providersVal.ArrayValue;
              }
                break;
              default:
                terrainProviderTypes = Enumerable.Empty<IConfigValue>();
                break;
            }
          }
        }

        Debug.Log("The first BuildTerrainProvider()");

        var providers = terrainProviderTypes
          .Select(val => BuildTerrainProvider(val))
          .Where(p => p != null).ToArray();

        if (providers.Length != 0)
        {
          //Default if none configured
          Debug.Log(providers.Length + " providers");
          config.ImageryProviders = providers;
          OneWorldSDKViewerContext.InitImagery();
        }
        else
        {
          Debug.Log("No providers!");
        }
      }

      {
        //Tile attributes
        var tileAttributesProviderType = m_Config.GetString("tileAttributesProviderType", "sql");
        switch (tileAttributesProviderType.ToLowerInvariant())
        {
          case "sql":
          {
            var tileAttributesDB = RelFile(m_Config.GetString("tileAttributesSettings.sql.databaseFile", Path.Combine(SystemAppDataDir, "tile_attributes.db")));

            var settings = SQLiteTileAttributesProvider.Settings.Default;
            settings.BaseLOD = m_Config.GetInt("tileAttributesSettings.sql.baseLOD", settings.BaseLOD);

            var provider = new SQLiteTileAttributesProvider(tileAttributesDB, settings);
            m_Disposables.Push(provider);
            config.TileAttributesProvider = provider;
          }
            break;
        }
      }

      {
        //Elevations
        var elevationProviderType = m_Config.GetString("elevationProviderType", "sql");

        switch (elevationProviderType.ToLowerInvariant())
        {
          case "file":
            OneWorldSDKViewerContext.WorldContext.ElevationProvider = BuildElevationProvider("file", m_Config.GetObject("elevationProviderSettings.file"));
            break;
          case "bing":
            OneWorldSDKViewerContext.WorldContext.ElevationProvider = BuildElevationProvider("bing", m_Config.GetObject("elevationProviderSettings.bing"));
            break;
          case "sql":
            OneWorldSDKViewerContext.WorldContext.ElevationProvider = BuildElevationProvider("sql", m_Config.GetObject("elevationProviderSettings.sql"));
            break;
          case "gpkg":
            OneWorldSDKViewerContext.WorldContext.ElevationProvider = BuildElevationProvider("gpkg", m_Config.GetObject("elevationProviderSettings.gpkg"));
            break;
        }
      }

      {
        //Locations
        var locationsDatabaseFile = RelFile(m_Config.GetString("locationsDatabaseFile", Path.Combine(SystemAppDataDir, "locations.db")));
        var locationProvider = new SQLiteLocationProvider(locationsDatabaseFile);
        m_Disposables.Push(locationProvider);
        config.LocationProvider = locationProvider;
      }

      {
        //Features
        var featureProviderType = m_Config.GetString("featureProviderType", "osm");
        switch (featureProviderType.ToLowerInvariant())
        { 
          case "osm":
          default:
          {
            var featureTileDir = RelDir(m_Config.GetString("featureProviderSettings.osm.featureTileDir", Path.Combine(SystemAppDataDir, "featureTiles")));
            var settings = OSMFileFeatureProvider.Settings.Default;

            settings.LOD = m_Config.GetInt("featureProviderSettings.osm.lod", settings.LOD);

            var provider = new OSMFileFeatureProvider(settings, featureTileDir);
            OneWorldSDKViewerContext.WorldContext.FeatureProvider = provider;
          }
            break;
        }
      }

      {
        //Mesh provider
        var meshProviderType = m_Config.GetString("tileMeshProviderType", string.Empty);
        config.TileMeshProvider = BuildMeshProvider(meshProviderType, m_Config.GetObject("tileMeshProviderSettings"));
      }

      {
        //Terrain chunker settings
        var chunkerSettings = config.ChunkerSettings;
        chunkerSettings.MaxNumTiles = m_Config.GetInt("tileChunkerSettings.numTiles", chunkerSettings.MaxNumTiles);
        chunkerSettings.PreloadPercent = m_Config.GetNumber("tileChunkerSettings.preloadPercent", chunkerSettings.PreloadPercent);
        chunkerSettings.DisablePhysics = m_Config.GetBool("tileChunkerSettings.disablePhysics", chunkerSettings.DisablePhysics);
        chunkerSettings.MaxConcurrentLoad = m_Config.GetInt("tileChunkerSettings.maxConcurrentLoad", chunkerSettings.MaxConcurrentLoad);
        chunkerSettings.MaxTileLOD = m_Config.GetInt("tileChunkerSettings.maxTileLOD", chunkerSettings.MaxTileLOD);
        chunkerSettings.MaxPhysicsLOD = m_Config.GetInt("tileChunkerSettings.maxPhysicsLOD", chunkerSettings.MaxPhysicsLOD);
        chunkerSettings.LoadFrameBudget = m_Config.GetInt("tileChunkerSettings.loadFrameBudget", chunkerSettings.LoadFrameBudget);
        chunkerSettings.AtlasTileSize = m_Config.GetInt("tileChunkerSettings.atlasTileSize", chunkerSettings.AtlasTileSize);
        chunkerSettings.CompressTextures = m_Config.GetBool("tileChunkerSettings.compressTextures", chunkerSettings.CompressTextures);
      }

      config.SkyboxSwitchoutDistance = m_Config.GetNumber("skyboxSwitchoutDistance", config.SkyboxSwitchoutDistance);
      config.FogDensity = (float)m_Config.GetNumber("fogDensity", config.FogDensity);

      var defaultScreenshotsDir = string.IsNullOrEmpty(config.ScreenshotDirectory) ? Path.Combine(SystemAppDataDir, "screenshots") : config.ScreenshotDirectory;
      config.ScreenshotDirectory = RelString(m_Config.GetString("screenshotsDir", defaultScreenshotsDir));
      config.GridSize = m_Config.GetInt("gridSize", config.GridSize);

      //Camera switch-out distances
      {
        IConfigArray distances;
        if (m_Config.TryGetArray("nearFarPlaneSwitchouts", out distances))
        {
          config.NearFarSwitchouts = distances
            .Where(v => v.ValueType == ConfigValueType.Object)
            .Select(v => v.ObjectValue)
            .Select(
              o =>
                new NearFarplaneSwitchoutInfo
                {
                  Distance = o.GetNumber("distance"),
                  Near = o.GetNumber("near"),
                  Far = o.GetNumber("far")
                })
            .OrderBy(info => info.Distance)
            .ToArray();
        }

        if (config.NearFarSwitchouts == null || config.NearFarSwitchouts.Length == 0)
        {
          config.NearFarSwitchouts = new NearFarplaneSwitchoutInfo[]
          {
            new NearFarplaneSwitchoutInfo
            {
              Distance = 2000,
              Near = 0.1,
              Far = 700000
            },
            new NearFarplaneSwitchoutInfo
            {
              Distance = 5000,
              Near = 10,
              Far = 2000000
            },
            new NearFarplaneSwitchoutInfo
            {
              Distance = 40000000,
              Near = 100,
              Far = 100000000
            }
          };
        }
      }
    }

    private ITerrainTileProvider BuildTerrainProvider(IConfigValue config)
    {
      Debug.Log("Build Terrain Provider");
      string typeName;
      IConfigObject options;

      switch (config.ValueType)
      {
        case ConfigValueType.String:
        {
          typeName = config.StringValue;
          options = ConfigNull.Null;
        }
          break;
        case ConfigValueType.Object:
        {
          typeName = config.ObjectValue.GetString("type", "web");
          options = config.ObjectValue.GetObject("options", ConfigNull.Null.ObjectValue);
        }
          break;
        default:
          return null;
      }

      Debug.Log("after first switch");
      switch (typeName.ToLowerInvariant())
      { 
        case "sql":
        {
          Debug.Log("SQL");
          var settings = SQLiteTerrainTileProvider.Settings.Default;
          settings.Fallback = BuildTerrainProvider(options.GetValue("fallback", ConfigNull.Null));
          settings.ReadOnly = options.GetBool("readOnly", settings.ReadOnly);

          var terrainTileDatabaseFile = RelFile(options.GetString("databaseFile", Path.Combine(SystemAppDataDir, "terrain_images.db")));

          var provider = new SQLiteTerrainTileProvider(terrainTileDatabaseFile, settings);
          m_Disposables.Push(provider);

          Action<ITerrainTileProvider> imageryProviderChanged =
            newProvider =>
            {
              if (newProvider == provider)
              {
                OneWorldSDKViewerContext.TileContext.ActiveTileMapper = WMSTileMapper.Instance;
              }
            };
          OneWorldSDKViewerContext.ActiveImageryProviderChanged += imageryProviderChanged;
          m_Disposables.Push(new DisposableAction(() => OneWorldSDKViewerContext.ActiveImageryProviderChanged -= imageryProviderChanged));

          return provider;
        }
        case "bing":
        {
          Debug.Log("Bing");
          var settings = WebTerrainTileProvider.Settings.Default;

          settings.Source = WebTileSource.Bing;

          var provider = new WebTerrainTileProvider(settings);
          m_Disposables.Push(provider);

          Action<ITerrainTileProvider> imageryProviderChanged =
            newProvider =>
            {
              if (newProvider == provider)
              {
                OneWorldSDKViewerContext.TileContext.ActiveTileMapper = WMSTileMapper.Instance;
              }
            };
          OneWorldSDKViewerContext.ActiveImageryProviderChanged += imageryProviderChanged;
          m_Disposables.Push(new DisposableAction(() => OneWorldSDKViewerContext.ActiveImageryProviderChanged -= imageryProviderChanged));

          return provider;
        }
        case "bingRoads":
        {
          Debug.Log("Bing Roads");
          var settings = WebTerrainTileProvider.Settings.Default;

          settings.Source = WebTileSource.BingRoads;

          var provider = new WebTerrainTileProvider(settings);
          m_Disposables.Push(provider);

          Action<ITerrainTileProvider> imageryProviderChanged =
            newProvider =>
            {
              if (newProvider == provider)
              {
                OneWorldSDKViewerContext.TileContext.ActiveTileMapper = WMSTileMapper.Instance;
              }
            };
          OneWorldSDKViewerContext.ActiveImageryProviderChanged += imageryProviderChanged;
          m_Disposables.Push(new DisposableAction(() => OneWorldSDKViewerContext.ActiveImageryProviderChanged -= imageryProviderChanged));

          return provider;
        }
        case "osm":
        {
          Debug.Log("OSM");
          var settings = WebTerrainTileProvider.Settings.Default;

          settings.Source = WebTileSource.OpenStreetMap;

          var provider = new WebTerrainTileProvider(settings);
          m_Disposables.Push(provider);

          Action<ITerrainTileProvider> imageryProviderChanged =
            newProvider =>
            {
              if (newProvider == provider)
              {
                OneWorldSDKViewerContext.TileContext.ActiveTileMapper = WMSTileMapper.Instance;
              }
            };
          OneWorldSDKViewerContext.ActiveImageryProviderChanged += imageryProviderChanged;
          m_Disposables.Push(new DisposableAction(() => OneWorldSDKViewerContext.ActiveImageryProviderChanged -= imageryProviderChanged));

          return provider;
        }
        case "web":
        {
          Debug.Log("Web");
          var settings = WebTerrainTileProvider.Settings.Default;

          var sourceStr = options.GetString("source", "bing");

          switch (sourceStr.ToLowerInvariant())
          {
            case "bing":
            case "bingaerial":
              settings.Source = WebTileSource.Bing;
              break;
            case "binghybrid":
              settings.Source = WebTileSource.BingHybrid;
              break;
            case "bingroads":
              settings.Source = WebTileSource.BingRoads;
              break;
            case "osm":
            case "openstreetmap":
              settings.Source = WebTileSource.OpenStreetMap;
              break;
          }

          var provider = new WebTerrainTileProvider(settings);
          m_Disposables.Push(provider);

          Action<ITerrainTileProvider> imageryProviderChanged =
            newProvider =>
            {
              if (newProvider == provider)
              {
                OneWorldSDKViewerContext.TileContext.ActiveTileMapper = WMSTileMapper.Instance;
              }
            };
          OneWorldSDKViewerContext.ActiveImageryProviderChanged += imageryProviderChanged;
          m_Disposables.Push(new DisposableAction(() => OneWorldSDKViewerContext.ActiveImageryProviderChanged -= imageryProviderChanged));

          return provider;
        }
        case "wms":
        {
          Debug.Log("WMS tile provider");
          var settings = WMSTileProvider.Settings.Default;

          settings.Username = options.GetString("username", settings.Username);
          settings.Password = options.GetString("password", settings.Password);
          settings.Format = options.GetString("format", settings.Format);
          settings.Srs = options.GetString("srs", settings.Srs);

          {
            //base URL
            string baseUrl;
            if (options.TryGetString("baseUrl", out baseUrl))
            {
              settings.BaseURL = new Uri(baseUrl);
            }
          }

          {
            //layers, can be single str or array of strings
            IConfigValue val;
            if (options.TryGetValue("layers", out val))
            {
              switch (val.ValueType)
              {
                case ConfigValueType.Array:
                {
                  var ary = val.ArrayValue;
                  settings.Layers = ary.Select(v => v.StringValue).ToList();
                }
                  break;
                case ConfigValueType.String:
                  settings.Layers = new[] {val.StringValue};
                  break;
              }
            }
          }
          Debug.Log("Username: " + settings.Username + " Password: " + settings.Password + " Format: " + " BaseURL: " + settings.BaseURL + " Layers: " + settings.Layers);
          var provider = new WMSTileProvider(settings);
          m_Disposables.Push(provider);

          Action mapperChangedAction = () => { provider.TileMapper = OneWorldSDKViewerContext.TileContext.ActiveTileMapper; };
          mapperChangedAction();
          OneWorldSDKViewerContext.TileContext.TileMapperChanged += mapperChangedAction;
          m_Disposables.Push(new DisposableAction(() => OneWorldSDKViewerContext.TileContext.TileMapperChanged -= mapperChangedAction));

          return provider;
        }
        case "birdseye":
        {
          Debug.Log("Birdseye tile provider");

          var settings = BirdseyeTileProvider.Settings.Default;

          {
            //base URL
            string baseUrl;
            if (options.TryGetString("baseUrl", out baseUrl))
            {
              settings.BaseURL = new Uri(baseUrl);
            }
          }
          settings.CenterPoint = options.GetString("centerPoint", settings.CenterPoint);
          settings.Include = options.GetBool("include", settings.Include);
          settings.Key = options.GetString("key", settings.Key);
          settings.Orientation = options.GetNumber("orientation", settings.Orientation);
          settings.UriScheme = options.GetString("uriScheme", settings.UriScheme);
          settings.ZoomLevel = options.GetInt("zoomLevel", settings.ZoomLevel);

          var provider = new BirdseyeTileProvider(settings);
          m_Disposables.Push(provider);

          Action<ITerrainTileProvider> imageryProviderChanged =
            newProvider =>
            {
              if (newProvider == provider)
              {
                OneWorldSDKViewerContext.TileContext.ActiveTileMapper = WMSTileMapper.Instance;
              }
            };

          OneWorldSDKViewerContext.ActiveImageryProviderChanged += imageryProviderChanged;
          m_Disposables.Push(new DisposableAction(() => OneWorldSDKViewerContext.ActiveImageryProviderChanged -= imageryProviderChanged));

          Debug.Log(provider.m_BaseURLStr);
          return provider;
        }
        case "wmts":
        {
          var capabilitiesUrl = options.GetString("capabilitiesUrl");
          var matrixSet = options.GetString("layer", null);
          var provider = new WMTSTileprovider(capabilitiesUrl, matrixSet);
          m_Disposables.Push(provider);

          Action<ITerrainTileProvider> imageryProviderChanged =
            newProvider =>
            {
              if (newProvider == provider)
              {
                OneWorldSDKViewerContext.TileContext.ActiveTileMapper = provider.TileMapper;
              }
            };
          OneWorldSDKViewerContext.ActiveImageryProviderChanged += imageryProviderChanged;
          m_Disposables.Push(new DisposableAction(() => OneWorldSDKViewerContext.ActiveImageryProviderChanged -= imageryProviderChanged));

          return provider;
        }
        default:
          Debug.Log("Default");
          return null;
      }
    }

    private IElevationProvider BuildElevationProvider(string typeName, IConfigObject options)
    {
      switch (typeName.ToLowerInvariant())
      { 
        case "bing":
        {
          var settings = BingElevationProvider.Settings.Default;
          settings.APIKey = options.GetString("apiKey", settings.APIKey);
          return new BingElevationProvider(settings);
        }
        case "sql":
        default:
        {
          var databaseFile = RelFile(options.GetString("databaseFile", Path.Combine(SystemAppDataDir, "elevations.db")));
          var settings = SQLiteElevationProvider.Settings.Default;
          settings.BaseLOD = options.GetInt("baseLOD", settings.BaseLOD);
          settings.UseDownsamples = options.GetBool("useDownsamples", settings.UseDownsamples);
          settings.Fallback = BuildElevationProvider(options.GetValue("fallback", ConfigNull.Null));
          var provider = new SQLiteElevationProvider(databaseFile, settings);
          m_Disposables.Push(provider);
          return provider;
        }
      }
    }

    private IElevationProvider BuildElevationProvider(IConfigValue config)
    {
      string typeName;
      IConfigObject options;

      switch (config.ValueType)
      {
        case ConfigValueType.String:
        {
          typeName = config.StringValue;
          options = ConfigNull.Null;
        }
          break;
        case ConfigValueType.Object:
        {
          typeName = config.ObjectValue.GetString("type", string.Empty);
          options = config.ObjectValue.GetObject("options", ConfigNull.Null.ObjectValue);
        }
          break;
        default:
          return null;
      }

      return BuildElevationProvider(typeName, options);
    }

    private ITileMeshProvider BuildMeshProvider(string type, IConfigObject config)
    {
      switch (type.ToLowerInvariant())
      {
        case "empty":
        {
          return new EmptyTileMeshProvider();
        }
        case "sqlcache":
        {
          var meshDatabaseFile = RelFile(config.GetString("sqlCache.databaseFile", Path.Combine(SystemAppDataDir, "meshes.db")));

          var settings = SQLiteCacheTileMeshProvider.Settings.Default;
          settings.ReadOnly = config.GetBool("sqlCache.readOnly", settings.ReadOnly);

          var baseProviderType = config.GetString("sqlCache.baseProviderType", "empty");
          var baseProvider = BuildMeshProvider(baseProviderType, config);

          var provider = new SQLiteCacheTileMeshProvider(meshDatabaseFile, settings, baseProvider);
          m_Disposables.Push(provider);
          return provider;
        }
        case "elevation":
        default:
        {
          var settings = ElevationTileMeshProvider.Settings.Default;
          settings.NumSamples = config.GetInt("elevation.numSamples", settings.NumSamples);
          settings.MaxParallelRequests = config.GetInt("elevation.maxParallelRequests", settings.MaxParallelRequests);
          settings.SkirtHeight = config.GetNumber("elevation.skirtHeight", settings.SkirtHeight);
          settings.WaterDepth = config.GetNumber("elevation.waterDepth", settings.WaterDepth);

          var provider = new ElevationTileMeshProvider(OneWorldSDKViewerContext.WorldContext.Ellipsoid, OneWorldSDKViewerContext.WorldContext.ElevationProvider, OneWorldSDKViewerContext.Config.TileAttributesProvider, settings);

          Action mapperChangedAction = () => { provider.TileMapper = OneWorldSDKViewerContext.TileContext.ActiveTileMapper; };
          mapperChangedAction();
          OneWorldSDKViewerContext.TileContext.TileMapperChanged += mapperChangedAction;
          m_Disposables.Push(new DisposableAction(() => OneWorldSDKViewerContext.TileContext.TileMapperChanged -= mapperChangedAction));

          return provider;
        }
      }
    }

    private IEnumerator Load()
    {
      if (m_LoadingTask != null && !m_LoadingTask.IsCompleted)
      {
        throw new InvalidOperationException("OneWorldSDKViewer is already loading");
      }

      m_LoadingTokenSource = new AsyncCancellationTokenSource();
      m_LoadingTask = new AsyncTask(LoadImpl(m_LoadingTokenSource), m_LoadingTokenSource.Token);

      while (m_LoadingTask.MoveNext())
      {
        yield return new WaitForFixedUpdate();
      }

      if (m_LoadingTask.IsFaulted)
      {
        //Cleanup best we can
        throw m_LoadingTask.Exception;
      }

      OneWorldSDKViewerContext.FinishedLoading.Raise();
    }

    private IEnumerator<bool> LoadImpl(AsyncCancellationTokenSource tok)
    {
      List<AsyncTask> loadTasks;
      {
        var loadRoutines = new List<IEnumerator<bool>>();
        OneWorldSDKViewerContext.BeginLoading.Raise(loadRoutines, tok.Token);
        loadTasks = new List<AsyncTask>(loadRoutines.Count);

        for (var i = 0; i < loadRoutines.Count; ++i)
        {
          loadTasks.Add(new AsyncTask(loadRoutines[i], tok.Token));
        }
      }

      var exceptions = new List<Exception>();
      while (loadTasks.Count > 0)
      {
        for (var i = 0; i < loadTasks.Count; ++i)
        {
          var task = loadTasks[i];
          if (task.MoveNext())
          {
            yield return AllCancellable(loadTasks);
          }
          else
          {
            if (task.IsFaulted)
            {
              exceptions.Add(task.Exception);
              tok.Cancel();
            }

            loadTasks.RemoveAt(i);
            --i;
          }
        }
      }

      if (exceptions.Any())
      {
        throw new AggregateException(exceptions);
      }
    }

    private string RelString(string path)
    {
      return Path.Combine(RelPathBase, path);
    }

    private FileInfo RelFile(string path)
    {
      return new FileInfo(RelString(path));
    }

    private DirectoryInfo RelDir(string path)
    {
      return new DirectoryInfo(RelString(path));
    }

    private string SystemAppDataDir
    {
      get { return m_Config.SystemAppDataDir.FullName; }
    }

    private string RelPathBase
    {
      get { return m_Config.SystemConfigFile.Directory.FullName; }
    }

    private AsyncTask m_LoadingTask;
    private AsyncCancellationTokenSource m_LoadingTokenSource;

    private Stack<IDisposable> m_Disposables;
    private JSONConfigManager m_Config;
  }
}




//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.owsdk.Services;
using UnityEngine;
using sbio.owsdk.Unity;
using sbio.owsdk.Unity.Events;
using sbio.OneWorldSDKViewer.Config;

namespace sbio.OneWorldSDKViewer
{
  [CreateAssetMenu(menuName = "OneWorldSDK Viewer/Context")]
  public sealed class OneWorldSDKViewerContext : ScriptableObject
  {
    #region ScriptableObject

    public WorldContext WorldContext;
    public TileLoadContext TileContext;

    /// <summary>
    /// Event triggered when the viewer begins loading
    /// </summary>
    public BeginAsyncOpEvent BeginLoading;

    /// <summary>
    /// Event triggered when the viewer finishes loading successfully
    /// </summary>
    public GameEvent FinishedLoading;

    /// <summary>
    /// Event triggered when the viewer is disabled
    /// </summary>
    public GameEvent Shutdown;

    public OneWorldSDKViewerConfig DefaultConfig;

    private void OnEnable()
    {
      m_ActiveProviderIndex = 0;
      Config = DefaultConfig ? Instantiate(DefaultConfig) : CreateInstance<OneWorldSDKViewerConfig>();
    }

    private void OnDisable()
    {
      Config = null;
    }

    #endregion

    public event Action<ITerrainTileProvider> ActiveImageryProviderChanged;
    public event Action<Camera> CameraChanged;

    public OneWorldSDKViewerConfig Config { get; private set; }

    /// <summary>
    /// The camera that is viewing the world
    /// This may be different from 'CameraBase'
    /// </summary>
    public Camera Camera
    {
      get { return m_Camera; }
      set
      {
        if (value != m_Camera)
        {
          m_Camera = value;
          CameraChanged?.Invoke(value);
        }
      }
    }

    public ITerrainTileProvider ActiveImageryProvider
    {
      get
      {
        if (Config.ImageryProviders.Length == 0)
        {
          return null;
        }

        return Config.ImageryProviders[m_ActiveProviderIndex];
      }
    }

    public bool ShowWireframe { get; set; }
    public bool UseTestPattern { get; set; }

    public void InitImagery()
    {
      ActiveImageryProviderChanged?.Invoke(ActiveImageryProvider);
    }

    public void CycleNextImageryProvider()
    {
      if (Config.ImageryProviders.Length <= 1)
      {
        return;
      }

      m_ActiveProviderIndex = (m_ActiveProviderIndex + 1) % Config.ImageryProviders.Length;

      ActiveImageryProviderChanged?.Invoke(ActiveImageryProvider);
    }

    public void CyclePreviousImageryProvider()
    {
      if (Config.ImageryProviders.Length <= 1)
      {
        return;
      }

      if (m_ActiveProviderIndex > 0)
      {
        --m_ActiveProviderIndex;
      }
      else
      {
        //Wrap to end
        m_ActiveProviderIndex = Config.ImageryProviders.Length - 1;
      }

      ActiveImageryProviderChanged?.Invoke(ActiveImageryProvider);
    }

    private Camera m_Camera;
    private int m_ActiveProviderIndex;
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

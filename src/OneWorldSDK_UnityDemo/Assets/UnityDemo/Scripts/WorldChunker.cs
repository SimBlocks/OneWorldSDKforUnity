//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;
using sbio.owsdk.Async;
using sbio.owsdk.Services;
using UnityEngine;
using sbio.owsdk.Unity;

namespace sbio.OneWorldSDKViewer
{
  /// <summary>
  /// Encapsulates a TerrainTileChunker on the globe
  /// </summary>
  public sealed class WorldChunker : MonoBehaviour
  {
    #region MonoBehaviour

    public OneWorldSDKViewerContext OneWorldSDKViewerContext;
    public Shader TileShader;
    public Material TileMaterial;
    public Material WireframeMaterial;

    private void Start()
    {
      OneWorldSDKViewerContext.BeginLoading.Event += OnBeginLoading;
      OneWorldSDKViewerContext.FinishedLoading.Event += OnFinishedLoading;

      var settings = OneWorldSDKViewerContext.Config.ChunkerSettings.Settings;
      var meshProvider = OneWorldSDKViewerContext.Config.TileMeshProvider;
      m_TileChunker = new TerrainTileChunker(settings, TileShader, TileShader, OneWorldSDKViewerContext.WorldContext,
        OneWorldSDKViewerContext.TileContext, meshProvider)
      {
        TerrainTileProvider = OneWorldSDKViewerContext.ActiveImageryProvider
      };

      m_StandardMaterials = new Material[]
      {
      };

      m_WireframeMaterials = new Material[]
      {
        WireframeMaterial
      };

      enabled = false;
    }

    private void Update()
    {
      if (OneWorldSDKViewerContext.ShowWireframe)
      {
        m_TileChunker.SetAdditionalTileMaterials(m_WireframeMaterials);
      }
      else
      {
        m_TileChunker.SetAdditionalTileMaterials(m_StandardMaterials);
      }

      m_TileChunker.Update();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
      if (m_TileChunker != null)
      {
        m_TileChunker.OnDrawGizmos();
      }
    }
#endif

    private void OnApplicationQuit()
    {
      if (m_HaveLoaded)
      {
        OneWorldSDKViewerContext.ActiveImageryProviderChanged -= OnImageryProviderChanged;
        OneWorldSDKViewerContext.CameraChanged -= OnCameraChanged;
      }

      OneWorldSDKViewerContext.FinishedLoading.Event -= OnFinishedLoading;
      OneWorldSDKViewerContext.BeginLoading.Event -= OnBeginLoading;

      m_TileChunker.Dispose();
      m_TileChunker = null;
    }

    #endregion

    private void OnBeginLoading(IList<IEnumerator<bool>> routines, AsyncCancellationToken tok)
    {
      routines.Add(m_TileChunker.PreloadTiles(tok));
    }

    private void OnFinishedLoading()
    {
      m_HaveLoaded = true;
      enabled = true;

      OnCameraChanged(OneWorldSDKViewerContext.Camera);
      OneWorldSDKViewerContext.CameraChanged += OnCameraChanged;

      OnImageryProviderChanged(OneWorldSDKViewerContext.ActiveImageryProvider);
      OneWorldSDKViewerContext.ActiveImageryProviderChanged += OnImageryProviderChanged;
    }

    private void OnCameraChanged(Camera newCamera)
    {
      m_TileChunker.Camera = OneWorldSDKViewerContext.Camera;
    }

    private void OnImageryProviderChanged(ITerrainTileProvider newProvider)
    {
      m_TileChunker.TerrainTileProvider = newProvider;
    }

    private TerrainTileChunker m_TileChunker;
    private bool m_HaveLoaded;
    private Material[] m_StandardMaterials;
    private Material[] m_WireframeMaterials;
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

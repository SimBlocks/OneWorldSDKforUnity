//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.owsdk.Services;
using UnityEngine;
using sbio.owsdk.Unity;
using sbio.owsdk.Unity.Config;

namespace sbio.OneWorldSDKViewer.Config
{
  [CreateAssetMenu(menuName = "OneWorldSDK Viewer/Config")]
  public sealed class OneWorldSDKViewerConfig : ScriptableObject
  {
    #region ScriptableObject

    public SerializedGeodetic3d StartPosition;
    public SerializedGeodetic3d[] DestinationPoints;
    public ITerrainTileProvider[] ImageryProviders;
    public ITileAttributesProvider TileAttributesProvider;
    public ILocationProvider LocationProvider;
    public ITileMeshProvider TileMeshProvider;
    public double SkyboxSwitchoutDistance;
    public float FogDensity = 1.2e-06f;
    public int GridSize = 4000;
    public string ScreenshotDirectory;
    public NearFarplaneSwitchoutInfo[] NearFarSwitchouts;
    public TileChunkerConfig DefaultChunkerSettings;

    private void OnEnable()
    {
      ChunkerSettings = DefaultChunkerSettings ? Instantiate(DefaultChunkerSettings) : CreateInstance<TileChunkerConfig>();
    }

    #endregion

    public TileChunkerConfig ChunkerSettings { get; set; }
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

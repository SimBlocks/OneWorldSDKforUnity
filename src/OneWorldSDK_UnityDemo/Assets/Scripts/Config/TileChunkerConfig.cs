//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using UnityEngine;

namespace sbio.owsdk.Unity.Config
{
  [CreateAssetMenu(menuName = "OWSDK/Config")]
  public sealed class TileChunkerConfig : ScriptableObject
  {
    #region ScriptableObject

    public int MaxNumTiles = 4095;
    public double PreloadPercent = 5;
    public bool DisablePhysics = false;
    public int MaxConcurrentLoad = Environment.ProcessorCount;
    public int MaxTileLOD = 20;
    public int MaxPhysicsLOD = 13;
    public int LoadFrameBudget = 3;
    public int AtlasTileSize = 16;
    public float ResolutionDistanceBias = 1.0f;
    public bool CompressTextures = true;

    #endregion

    public TerrainTileChunker.Settings Settings => new TerrainTileChunker.Settings
    {
      MaxNumTiles = MaxNumTiles,
      PreloadPercent = PreloadPercent,
      DisablePhysics = DisablePhysics,
      MaxConcurrentLoad = MaxConcurrentLoad,
      MaxTileLOD = MaxTileLOD,
      MaxPhysicsLOD = MaxPhysicsLOD,
      LoadFrameBudget = LoadFrameBudget,
      AtlasTileSize = AtlasTileSize,
      CompressTextures = CompressTextures,
      ResolutionDistanceBias = ResolutionDistanceBias
    };
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

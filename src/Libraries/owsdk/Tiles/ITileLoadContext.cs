//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;

namespace sbio.owsdk.Tiles
{
  public interface ITileLoadContext
  {
    /// <summary>
    /// Called when the given tile is activated, either after finishing to load or if it is deactivated and subsequently activated again.
    /// </summary>
    event Action<TerrainTileIndex> Activated;

    /// <summary>
    /// Called when the given tile is determined to be visible
    /// </summary>
    event Action<TerrainTileIndex> IsVisible;

    /// <summary>
    /// Called when the given tile is determined to not be visible
    /// </summary>
    event Action<TerrainTileIndex> IsInvisible;

    /// <summary>
    /// Called when the given tile is deactivated. A deactivated tile is available to unload or to activate again
    /// </summary>
    event Action<TerrainTileIndex> Deactivated;

    /// <summary>
    /// Called when the given tile is unloaded. Any remaining data for the tile should be cleared.
    /// This will never be called unless BeginLoading was called, but may be invoked even if loading was never completed (partial load)
    /// </summary>
    event Action<TerrainTileIndex> Unloaded;

    IDisposable RegisterTileLoader(ILoadTile loader);
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

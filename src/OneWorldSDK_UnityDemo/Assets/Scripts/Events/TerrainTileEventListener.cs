//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using UnityEngine.Events;
using sbio.owsdk.Tiles;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Concrete implementation of a GameEventListener receiving a string argument
  /// </summary>
  public sealed class TerrainTileEventListener : GameEventListener<TerrainTileIndex>
  {
    public TerrainTileEvent EventSource;
    public TerrainTileUnityEvent EventResponse;

    protected override GameEvent<TerrainTileIndex> Source
    {
      get { return EventSource; }
    }

    protected override UnityEvent<TerrainTileIndex> Response
    {
      get { return EventResponse; }
    }
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

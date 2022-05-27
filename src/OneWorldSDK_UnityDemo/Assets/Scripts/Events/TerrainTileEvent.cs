//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using UnityEngine;
using sbio.owsdk.Tiles;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Concrete implementation of GameEvent receiving a bool argument
  /// </summary>
  [CreateAssetMenu(menuName = "OWSDK/Events/Terrain Tile Event", order = 170)]
  public sealed class TerrainTileEvent : GameEvent<TerrainTileIndex>
  {
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

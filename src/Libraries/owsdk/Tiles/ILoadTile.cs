//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;
using sbio.owsdk.Async;

namespace sbio.owsdk.Tiles
{
  public interface ILoadTile
  {
    /// <summary>
    /// Called when the given tile begins loading
    /// </summary>
    /// <param name="idx">The tile that has begun loading</param>
    /// <param name="tok">Cancellation token for the operation</param>
    /// <returns>A coroutine that performs the loading operation</returns>
    IEnumerator<bool> BeginLoading(TerrainTileIndex idx, AsyncCancellationToken tok);
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

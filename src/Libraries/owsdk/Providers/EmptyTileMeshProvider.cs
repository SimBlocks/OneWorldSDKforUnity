//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Threading;
using System.Threading.Tasks;
using sbio.owsdk.Services;
using sbio.owsdk.Tiles;

namespace sbio.owsdk.Providers
{
  public sealed class EmptyTileMeshProvider : ITileMeshProvider
  {
    public Task<TileMesh> QueryTileMeshAsync(TerrainTileIndex idx, CancellationToken tok)
    {
      throw new Exception();
    }
  }
}



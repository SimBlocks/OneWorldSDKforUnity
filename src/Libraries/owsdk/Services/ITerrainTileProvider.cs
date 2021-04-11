//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Threading;
using System.Threading.Tasks;
using sbio.owsdk.Tiles;

namespace sbio.owsdk.Services
{
  //Interface to query terrain terrain tile data
  public interface ITerrainTileProvider
  {
    Task<int> LoadTerrainTileAsyncInto(TerrainTileIndex id, ArraySegment<byte> buffer, CancellationToken tok);
  }

  public static class ITerrainTileProviderExtensions
  {
    public static ArraySegment<byte> LoadTerrainTile(this ITerrainTileProvider provider, TerrainTileIndex id)
    {
      return provider.LoadTerrainTileAsync(id).Result;
    }

    public static int LoadTerrainTileInto(this ITerrainTileProvider provider, TerrainTileIndex id, byte[] target)
    {
      return provider.LoadTerrainTileAsyncInto(id, target).Result;
    }

    public static int LoadTerrainTileInto(this ITerrainTileProvider provider, TerrainTileIndex id, ArraySegment<byte> target)
    {
      return provider.LoadTerrainTileAsyncInto(id, target).Result;
    }

    public static async Task<ArraySegment<byte>> LoadTerrainTileAsync(this ITerrainTileProvider provider, TerrainTileIndex id)
    {
      var result = new byte[256 * 256 * 4];
      var count = await provider.LoadTerrainTileAsyncInto(id, result).ConfigureAwait(false);
      return new ArraySegment<byte>(result, 0, count);
    }

    public static Task<int> LoadTerrainTileAsyncInto(this ITerrainTileProvider provider, TerrainTileIndex id, byte[] target)
    {
      return provider.LoadTerrainTileAsyncInto(id, target, CancellationToken.None);
    }

    public static Task<int> LoadTerrainTileAsyncInto(this ITerrainTileProvider provider, TerrainTileIndex id, byte[] target, CancellationToken tok)
    {
      return provider.LoadTerrainTileAsyncInto(id, new ArraySegment<byte>(target), tok);
    }

    public static Task<int> LoadTerrainTileAsyncInto(this ITerrainTileProvider provider, TerrainTileIndex id, ArraySegment<byte> target)
    {
      return provider.LoadTerrainTileAsyncInto(id, target, CancellationToken.None);
    }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

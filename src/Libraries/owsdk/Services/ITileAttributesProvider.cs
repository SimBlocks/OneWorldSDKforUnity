//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Threading;
using System.Threading.Tasks;
using sbio.owsdk.Tiles;

namespace sbio.owsdk.Services
{
  public interface ITileAttributesProvider
  {
    Task<ITileAttributeMask> QueryTileAttributesAsync(TerrainTileIndex idx, CancellationToken tok);
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using BruTile;
using BruTile.Wmts;
using DotSpatial.Projections;
using sbio.owsdk.Services;
using sbio.owsdk.Tiles;
using sbio.owsdk.WMS;

namespace sbio.owsdk.Providers.WebServices
{
  public sealed class WMTSTileprovider : ITerrainTileProvider, IDisposable
  {
    public Task<int> LoadTerrainTileAsyncInto(TerrainTileIndex id, ArraySegment<byte> buffer, CancellationToken tok)
    {
      return Task.Run(() =>
      {
        var tileData = m_TileSource.GetTile(new TileInfo() {Index = new TileIndex(id.Column, id.Row, m_LODS[id.Level])});

        if (buffer.Count < tileData.Length)
        {
          throw new ArgumentException("The buffer is too small to hold the image. (Buffer size: " + buffer.Count + ", image size: " + tileData.Length + ")", nameof(buffer));
        }

        var count = tileData.Length;
        Array.Copy(tileData, 0, buffer.Array, buffer.Offset, tileData.Length);
        return count;
      }, tok);
    }

    public void Dispose()
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      m_Disposed = true;
    }

    public WMTSTileMapper TileMapper
    {
      get { return m_TileMapper; }
    }

    public WMTSTileprovider(string capabilitiesUrl, string layer = null)
    {
      using (var client = new HttpClient())
      {
        var capabilities = WmtsParser.Parse(client.GetStreamAsync(new Uri(capabilitiesUrl)).Result);
        m_TileSource = capabilities
          .Where(s => layer == null || ((WmtsTileSchema)s.Schema).Layer == layer)
          .First();

        var schema = (WmtsTileSchema)m_TileSource.Schema;
        var projection = ProjectionInfo.FromAuthorityCode(schema.SupportedSRS.Authority, int.Parse(schema.SupportedSRS.Identifier));
        var orderedResolutions = schema.Resolutions.OrderByDescending(r => r.Value.ScaleDenominator).ToList();

        m_TileMapper = new WMTSTileMapper(projection, orderedResolutions
          .Select(kvp => kvp.Value)
          .Select(r =>
            new WMTSTileMatrix
            {
              UnitsPerPixel = r.UnitsPerPixel,
              ScaleDenominator = r.ScaleDenominator,
              Top = r.Top,
              Left = r.Left,
              MatrixWidth = (int)r.MatrixWidth,
              MatrixHeight = (int)r.MatrixHeight,
              TileWidth = r.TileWidth,
              TileHeight = r.TileHeight
            }));

        m_LODS = orderedResolutions.Select(kvp => kvp.Key).ToArray();
      }
    }


    private bool m_Disposed;

    private readonly ITileSource m_TileSource;
    private readonly WMTSTileMapper m_TileMapper;
    private readonly string[] m_LODS;
  }
}



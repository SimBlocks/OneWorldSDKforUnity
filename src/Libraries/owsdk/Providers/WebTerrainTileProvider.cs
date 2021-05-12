//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BruTile;
using BruTile.Predefined;
using BruTile.Web;
using sbio.owsdk.Services;
using sbio.owsdk.Tiles;

namespace sbio.owsdk.Providers
{
  public enum WebTileSource
  {
    Bing,
    BingHybrid,
    BingRoads,
    OpenStreetMap
  }

  public sealed class WebTerrainTileProvider : ITerrainTileProvider,
    IDisposable
  {
    public sealed class Settings
    {
      public static Settings Default
      {
        get { return new Settings(); }
      }

      public Settings()
      {
        Source = WebTileSource.Bing;
      }

      public WebTileSource Source { get; set; }

      public string APIKey { get; set; }
    }

    public void Dispose()
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      m_Disposed = true;
    }

    public Task<int> LoadTerrainTileAsyncInto(TerrainTileIndex id, ArraySegment<byte> buffer, CancellationToken tok)
    {
      return Task.Run(() =>
      {
        TileInfo tileInfo = new TileInfo() {Index = new TileIndex(id.Column, id.Row, id.Level.ToString())};
        byte[] tileData = null;

        try
        {
          tileData = m_Tilesource.GetTile(tileInfo);
        }
        catch (Exception ex)
        {
          System.Console.WriteLine(ex.ToString());
        }

        if(tileData == null)
        {
          throw new ArgumentException("Tile is null");
        }

        //Compare the image against the Bing 'tile not available' image
        //TODO: This won't work on other providers, naturally
        var sha = new SHA256Managed();
        var checksum = sha.ComputeHash(tileData);
        var checksumStr = BitConverter.ToString(checksum).Replace("-", string.Empty);

        if (checksumStr == sc_FailureImageHash)
        {
          throw new Exception();
        }

        if (buffer.Count < tileData.Length)
        {
          throw new ArgumentException("The buffer is too small to hold the image. (Buffer size: " + buffer.Count + ", image size: " + tileData.Length + ")", nameof(buffer));
        }

        var count = tileData.Length;
        Array.Copy(tileData, 0, buffer.Array, buffer.Offset, tileData.Length);
        return count;
      }, tok);
    }

    public WebTerrainTileProvider(Settings settings)
    {
      switch (settings.Source)
      {
        case WebTileSource.Bing:
          m_Tilesource = KnownTileSources.Create(KnownTileSource.BingAerial, settings.APIKey);
          break;
        case WebTileSource.BingHybrid:
          m_Tilesource = KnownTileSources.Create(KnownTileSource.BingHybrid, settings.APIKey);
          break;
        case WebTileSource.BingRoads:
          m_Tilesource = KnownTileSources.Create(KnownTileSource.BingHybrid, settings.APIKey);
          break;
        case WebTileSource.OpenStreetMap:
          m_Tilesource = KnownTileSources.Create(KnownTileSource.OpenStreetMap, settings.APIKey);
          break;
        default:
          throw new NotImplementedException(string.Format("The source '{0}' is not implemented", settings.Source));
      }
    }

    private static readonly string sc_FailureImageHash = "45D35034A62D30443E492851752F67F439B95A908FCE54DE601F7373FCB7AB05";

    private readonly HttpTileSource m_Tilesource;

    private bool m_Disposed;
  }
}



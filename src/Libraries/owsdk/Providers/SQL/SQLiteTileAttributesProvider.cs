//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using sbio.owsdk.Services;
using sbio.owsdk.OSM;
using sbio.owsdk.Tiles;
using sbio.owsdk.WMS;
using Hjg.Pngcs;
using System.Data.SQLite;

namespace sbio.owsdk.Providers.SQL
{
  public sealed class SQLiteTileAttributesProvider : ITileAttributesProvider
    , IDisposable
  {
    public class Settings
    {
      public static Settings Default
      {
        get { return new Settings(); }
      }

      public Settings()
      {
        BaseLOD = 12;
      }

      /// <summary>
      /// The base LOD to use when upsampling
      /// </summary>
      public int BaseLOD { get; set; }
    }

    public void Dispose()
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      m_Disposed = true;

      if (m_Connection != null)
      {
        m_Connection.Dispose();
      }
    }

    public Task<ITileAttributeMask> QueryTileAttributesAsync(TerrainTileIndex idx, CancellationToken tok)
    {
      return Task.Run(() => QueryTileAttributes(idx), tok);
    }

    public SQLiteTileAttributesProvider(FileInfo databaseFile)
      : this(databaseFile, Settings.Default)
    {
    }

    public SQLiteTileAttributesProvider(FileInfo databaseFile, Settings settings)
    {
      m_AllWaterMask = new TileAttributeMask(Enumerable.Repeat((byte)128, 256 * 256).ToArray());

      if (databaseFile.Exists)
      {
        var connectionString = new SQLiteConnectionStringBuilder() {DataSource = databaseFile.FullName}.ConnectionString;
        m_Connection = new SQLiteConnection(connectionString);
        m_Connection.Open();
      }
      else
      {
        m_Connection = null;
      }

      m_BaseLOD = settings.BaseLOD;
    }

    private ITileAttributeMask QueryTileAttributes(TerrainTileIndex idx)
    {
      if (m_Connection == null)
      {
        return m_AllWaterMask;
      }

      if (idx.Level > m_BaseLOD)
      {
        var parentTile = WMSConversions.ParentTile(idx);
        var parentMask = QueryTileAttributes(parentTile);

        var xOffset = (idx.Column % 2) * 128;
        var yOffset = (idx.Row % 2) * 128;

        return UpsampleAttributes(parentMask, 1, xOffset, yOffset);
      }

      var tileID = WMSConversions.TileToQuadKey(idx);
      using (var cmd = m_Connection.CreateCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = "SELECT `mask` FROM `attribute_masks` WHERE `tileID` = @id";

        cmd.Parameters.AddWithValue("id", tileID);

        using (var reader = cmd.ExecuteReader(CommandBehavior.SingleResult))
        {
          if (reader.Read())
          {
            var len = reader.GetBytes(0, 0, null, 0, 0);
            var buf = new byte[len];
            reader.GetBytes(0, 0, buf, 0, buf.Length);
            using (var stream = new MemoryStream(buf))
            {
              var pngReader = new PngReader(stream);

              return new TileAttributeMask2d(pngReader.ReadRowsByte().ScanlinesB);
            }
          }
          else
          {
            return m_AllWaterMask;
          }
        }
      }
    }

    private ITileAttributeMask UpsampleAttributes(ITileAttributeMask mask, int levels, int xOffset, int yOffset)
    {
      var result = new byte[256 * 256];

      var scale = 1.0 / (1 << levels);

      for (int j = 0; j < 256; ++j)
      {
        var yPx = yOffset + (j * scale);
        for (int i = 0; i < 256; ++i)
        {
          var xPx = xOffset + (i * scale);

          if (mask.IsWater(xPx, yPx))
          {
            result[j * 256 + i] = 128;
          }
        }
      }

      return new TileAttributeMask(result);
    }

    /// <summary>
    /// Database connection
    /// null if not available.
    /// </summary>
    private readonly SQLiteConnection m_Connection;

    private readonly ITileAttributeMask m_AllWaterMask;
    private readonly int m_BaseLOD;
    private bool m_Disposed;
  }
}



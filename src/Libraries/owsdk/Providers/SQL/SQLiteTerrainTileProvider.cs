//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SQLite;
using sbio.owsdk.Services;
using sbio.owsdk.Tiles;
using sbio.owsdk.WMS;

namespace sbio.owsdk.Providers.SQL
{
  public sealed class SQLiteTerrainTileProvider : ITerrainTileProvider
    , IDisposable
  {
    public sealed class Settings
    {
      public static Settings Default
      {
        get { return new Settings(); }
      }

      public Settings()
      {
        Fallback = null;
        ReadOnly = false;
      }

      /// <summary>
      /// Terrain tile provider to query if the database doesn't contain a requested image.
      /// </summary>
      public ITerrainTileProvider Fallback { get; set; }

      /// <summary>
      /// When true, images that are fetched from the fallback (if present) are not saved back to the database
      /// </summary>
      public bool ReadOnly { get; set; }
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

    public SQLiteTerrainTileProvider(FileInfo databaseFile)
      : this(databaseFile, Settings.Default)
    {
    }

    public SQLiteTerrainTileProvider(FileInfo databaseFile, Settings settings)
    {
      m_Fallback = settings.Fallback;
      m_ReadOnly = settings.ReadOnly;

      if (m_ReadOnly)
      {
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
      }
      else
      {
        //Not read-only. Ensure directory exists so that SQL lib can create if necessary
        {
          //NOTE: On Unity's runtime, attempting to create certain directories eg 'C:\' will fail even if they already exist
          var dir = databaseFile.Directory;
          if (!dir.Exists)
          {
            dir.Create();
          }
        }

        var connectionString = new SQLiteConnectionStringBuilder() {DataSource = databaseFile.FullName}.ConnectionString;
        m_Connection = new SQLiteConnection(connectionString);
        m_Connection.Open();

        //Make sure the images table exists
        using (var cmd = m_Connection.CreateCommand())
        {
          cmd.CommandType = CommandType.Text;
          cmd.CommandText = "CREATE TABLE IF NOT EXISTS `tile_images`(`id` TEXT NOT NULL, `image` BLOB NOT NULL, PRIMARY KEY(`id`));";
          cmd.ExecuteNonQuery();
        }
      }
    }

    public Task<int> LoadTerrainTileAsyncInto(TerrainTileIndex id, ArraySegment<byte> buffer, CancellationToken tok)
    {
      return Task.Run(
        async () =>
        {
          int count;
          var tileID = WMSConversions.TileToQuadKey(id);
          if (TryGetImage(tileID, buffer, out count, tok))
          {
            return count;
          }
          else if (m_Fallback != null)
          {
            count = await m_Fallback.LoadTerrainTileAsyncInto(id, buffer, tok).ConfigureAwait(false);

            if (!m_ReadOnly)
            {
              var copy = new byte[count];
              Array.Copy(buffer.Array, buffer.Offset, copy, 0, copy.Length);
              WriteImage(tileID, copy);
            }

            return count;
          }
          else
          {
            throw new ArgumentOutOfRangeException();
          }
        },
        tok);
    }

    private bool TryGetImage(string tileID, ArraySegment<byte> buffer, out int count, CancellationToken tok)
    {
      if (m_Connection == null)
      {
        count = 0;
        return false;
      }

      using (var cmd = m_Connection.CreateCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = "SELECT `image` FROM `tile_images` WHERE `id` = @id";

        cmd.Parameters.AddWithValue("id", tileID);

        using (var reader = cmd.ExecuteReader(CommandBehavior.SingleResult))
        {
          tok.ThrowIfCancellationRequested();

          if (reader.Read())
          {
            var len = reader.GetBytes(0, 0, null, 0, 0);

            if (buffer.Count < len)
            {
              throw new ArgumentException("The buffer is too small to hold the image. (Buffer size: " + buffer.Count + ", image size: " + len + ")", nameof(buffer));
            }

            count = (int)len;
            reader.GetBytes(0, 0, buffer.Array, buffer.Offset, (int)len);
            return true;
          }
          else
          {
            count = 0;
            return false;
          }
        }
      }
    }

    private void WriteImage(string tileID, byte[] buffer)
    {
      using (var cmd = m_Connection.CreateCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = "INSERT OR IGNORE INTO `tile_images` (`id`, `image`) VALUES (@id, @image)";

        cmd.Parameters.AddWithValue("id", tileID);
        cmd.Parameters.AddWithValue("image", buffer);

        cmd.ExecuteNonQuery();
      }
    }

    /// <summary>
    /// Database connection.
    /// null if database is not available (read-only and does not already exist)
    /// </summary>
    private readonly SQLiteConnection m_Connection;

    /// <summary>
    /// Fallback provider
    /// null if not available
    /// </summary>
    private readonly ITerrainTileProvider m_Fallback;

    /// <summary>
    /// If true, database is not to be modified
    /// </summary>
    private readonly bool m_ReadOnly;

    private bool m_Disposed;
  }
}



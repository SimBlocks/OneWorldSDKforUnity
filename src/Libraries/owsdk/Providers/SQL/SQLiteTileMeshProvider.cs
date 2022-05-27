//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using sbio.Core.Math;
using sbio.owsdk.Extensions;
using sbio.owsdk.Services;
using sbio.owsdk.Tiles;
using System.Data.SQLite;
using sbio.owsdk.WMS;

namespace sbio.owsdk.Providers.SQL
{
  public sealed class SQLiteCacheTileMeshProvider : ITileMeshProvider
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
        ReadOnly = false;
      }

      public bool ReadOnly { get; set; }
    }

    public Task<TileMesh> QueryTileMeshAsync(TerrainTileIndex idx, CancellationToken tok)
    {
      return Task.Run(async () =>
      {
        var tileID = WMSConversions.TileToQuadKey(idx);

        TileMesh ret;

        if (!TryQueryTileMesh(tileID, out ret, tok))
        {
          ret = await m_MeshProvider.QueryTileMeshAsync(idx, tok);
          if (!m_Settings.ReadOnly)
          {
            InsertTileMesh(tileID, ret, tok);
          }
        }

        return ret;
      }, tok);
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

    public SQLiteCacheTileMeshProvider(FileInfo databaseFile, ITileMeshProvider meshProvider)
      : this(databaseFile, Settings.Default, meshProvider)
    {
    }

    public SQLiteCacheTileMeshProvider(FileInfo databaseFile, Settings settings, ITileMeshProvider meshProvider)
    {
      m_Settings = settings;
      m_MeshProvider = meshProvider;

      if (m_Settings.ReadOnly)
      {
        if (databaseFile.Exists)
        {
          m_Connection = new SQLiteConnection(new SQLiteConnectionStringBuilder() {DataSource = databaseFile.FullName}.ConnectionString);
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

        m_Connection = new SQLiteConnection(new SQLiteConnectionStringBuilder() {DataSource = databaseFile.FullName}.ConnectionString);
        m_Connection.Open();

        //Make sure the table exists
        using (var cmd = m_Connection.CreateCommand())
        {
          cmd.CommandType = CommandType.Text;
          cmd.CommandText = "CREATE TABLE IF NOT EXISTS `meshes`(`tileID` TEXT NOT NULL, `blob` BLOB NOT NULL, PRIMARY KEY (`tileID`));";
          cmd.ExecuteNonQuery();
        }
      }

      m_FormatBuffer = new ThreadLocal<byte[]>(() => new byte[(1 << 16) * 8 * 4 * 2 + ((1 << 16) * 4 * 3)]);
    }

    private static void WriteMesh(BinaryWriter writer, DBMesh mesh)
    {
      writer.Write(mesh.Center.ToVector3d());
      writer.Write(mesh.Extents);
      writer.Write(mesh.Rotation);
      writer.Write(mesh.Positions);
      writer.Write(mesh.Normals);
      writer.Write(mesh.Uvs);
      writer.WriteAsUShortArray(mesh.Triangles);
    }

    private static DBMesh ReadMesh(BinaryReader reader)
    {
      Vec3LeftHandedGeocentric center = new Vec3LeftHandedGeocentric(reader.ReadV3d());
      var extents = reader.ReadV3f();
      var rotation = reader.ReadQuatf();
      var positions = reader.ReadV3fArray();
      var normals = reader.ReadV3fArray();
      var uvs = reader.ReadV2fArray();
      var triangles = reader.ReadUShortArrayAsInt();

      return new DBMesh(center, extents, rotation, positions, normals, uvs, triangles);
    }

    private bool TryQueryTileMesh(string tileID, out TileMesh tileMesh, CancellationToken tok)
    {
      if (m_Connection == null)
      {
        tileMesh = default(TileMesh);
        return false;
      }

      using (var cmd = m_Connection.CreateCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = "SELECT `blob` FROM `meshes` WHERE `tileID` = @tileID";

        cmd.Parameters.AddWithValue("tileID", tileID);

        using (var reader = cmd.ExecuteReader())
        {
          if (reader.Read())
          {
            var formatBuf = m_FormatBuffer.Value;
            reader.GetBytes(0, 0, formatBuf, 0, formatBuf.Length);

            var memStream = new MemoryStream(formatBuf);
            var binReader = new BinaryReader(memStream);
            var dbMesh = ReadMesh(binReader);
            tileMesh = new TileMesh(dbMesh.Center, dbMesh.Extents, dbMesh.Rotation, dbMesh.Positions, dbMesh.Uvs, dbMesh.Normals, dbMesh.Triangles);
            return true;
          }
          else
          {
            tileMesh = default(TileMesh);
            return false;
          }
        }
      }
    }

    private void InsertTileMesh(string tileID, TileMesh mesh, CancellationToken tok)
    {
      var tileIDParam = new SQLiteParameter("tileID", DbType.String);
      tileIDParam.Value = tileID;

      using (var cmd = m_Connection.CreateCommand())
      {
        cmd.CommandText = "INSERT INTO `meshes` (`tileID`, `blob`) VALUES (@tileID, @blob)";

        var blobParam = new SQLiteParameter("blob", DbType.Binary);

        cmd.Parameters.Add(tileIDParam);
        cmd.Parameters.Add(blobParam);

        var dbMesh = new DBMesh(mesh.Center, mesh.Extents, mesh.Rotation, mesh.Vertices, mesh.Normals, mesh.Uvs, mesh.Triangles);

        var formatBuf = m_FormatBuffer.Value;
        var memStream = new MemoryStream(formatBuf);
        var writer = new BinaryWriter(memStream);
        WriteMesh(writer, dbMesh);

        blobParam.Value = formatBuf.CopySegment((int)memStream.Position);

        cmd.ExecuteNonQuery();
      }
    }

    private struct DBMesh
    {
      public Vec3LeftHandedGeocentric Center { get; set; }
      public Vector3f Extents { get; set; }
      public QuaternionLeftHandedGeocentric Rotation { get; set; }
      public Vector3f[] Positions { get; set; }
      public Vector3f[] Normals { get; set; }
      public Vector2f[] Uvs { get; set; }
      public int[] Triangles { get; set; }

      public DBMesh(Vec3LeftHandedGeocentric center, Vector3f extents, QuaternionLeftHandedGeocentric rotation, Vector3f[] positions, Vector3f[] normals, Vector2f[] uvs, int[] triangles)
      {
        Center = center;
        Extents = extents;
        Rotation = rotation;
        Positions = positions;
        Normals = normals;
        Uvs = uvs;
        Triangles = triangles;
      }
    }

    private readonly Settings m_Settings;
    private readonly ITileMeshProvider m_MeshProvider;

    private readonly ThreadLocal<byte[]> m_FormatBuffer;
    private readonly SQLiteConnection m_Connection;
    private bool m_Disposed;
  }
}



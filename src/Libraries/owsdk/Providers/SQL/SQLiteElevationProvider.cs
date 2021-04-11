//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using sbio.owsdk.Services;
using sbio.owsdk.Tiles;
using sbio.owsdk.WMS;
using Hjg.Pngcs;
using System.Data.SQLite;

namespace sbio.owsdk.Providers.SQL
{
  public sealed class SQLiteElevationProvider : IElevationProvider
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
        BaseLOD = 14;
        UseDownsamples = false;
        Fallback = null;
      }

      public int BaseLOD { get; set; }
      public bool UseDownsamples { get; set; }

      /// <summary>
      /// Elevation provider to query if the database doesn't contain a requested heightmap.
      /// </summary>
      public IElevationProvider Fallback { get; set; }
    }

    public Task QueryPointSamplesAsyncInto(ArraySegment<ElevationPointSample> points, CancellationToken tok)
    {
      return Task.Run(
        () =>
        {
          List<KeyValuePair<TerrainTileIndex, List<int>>> groups;

          if (m_UseDownsamples)
          {
            groups = GroupByLeastTiles(points, m_BaseLOD, tok);
          }
          else
          {
            groups = GroupByTiles(points, m_BaseLOD, tok);
          }

          var groupCount = groups.Count;
          for (var i = 0; i < groupCount; ++i)
          {
            tok.ThrowIfCancellationRequested();
            var g = groups[i];
            LoadGroupPoints(g.Key, g.Value, points, tok);
          }
        });
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

    public SQLiteElevationProvider(FileInfo databaseFile)
      : this(databaseFile, Settings.Default)
    {
    }

    public SQLiteElevationProvider(FileInfo databaseFile, Settings settings)
    {
      m_DatabaseFile = databaseFile;

      m_BaseLOD = settings.BaseLOD;
      m_UseDownsamples = settings.UseDownsamples;
      m_Fallback = settings.Fallback;

      if (databaseFile.Exists)
      {
        var connectionString = new SQLiteConnectionStringBuilder() {DataSource = m_DatabaseFile.FullName}.ConnectionString;

        m_Connection = new SQLiteConnection(connectionString);
        m_Connection.Open();
      }
      else
      {
        m_Connection = null;
      }

      m_HeightmapRowBuffer = new ThreadLocal<int[]>(() => new int[256]);
      m_HeightmapBuffer = new ThreadLocal<float[]>(() => new float[256 * 256]);
      m_Stream = new ThreadLocal<MemoryStream>(
        () =>
        {
          var buf = new byte[256 * 256 * 4];
          return new MemoryStream(buf, 0, buf.Length, true, true);
        });
    }

    private struct PointIndex
    {
      public int Row
      {
        get { return m_Row; }
      }

      public int Column
      {
        get { return m_Column; }
      }

      public PointIndex(int row, int column)
      {
        m_Row = row;
        m_Column = column;
      }

      private readonly int m_Row;
      private readonly int m_Column;
    }

    private static double Interpolate(double row, double col, float[] ary, int aryHeight, int aryWidth)
    {
      //Clamp values
      row = Math.Min(Math.Max(row, 0), aryHeight - 1);
      col = Math.Min(Math.Max(col, 0), aryWidth - 1);

      var rowTruncate = (int)row;
      var rowFraction = row - rowTruncate;

      if (rowTruncate == aryHeight - 1)
      {
        rowTruncate = rowTruncate - 1;
        rowFraction = 1;
      }

      var colTruncate = (int)col;
      var colFraction = col - colTruncate;
      if (colTruncate == aryWidth - 1)
      {
        colTruncate = colTruncate - 1;
        colFraction = 1;
      }

      return
        (1 - rowFraction) * ((1 - colFraction) * ary[rowTruncate * aryWidth + colTruncate] + colFraction * ary[rowTruncate * aryWidth + colTruncate + 1])
        + rowFraction * ((1 - colFraction) * ary[(rowTruncate + 1) * aryWidth + colTruncate] + colFraction * ary[(rowTruncate + 1) * aryWidth + colTruncate + 1]);
    }

    private bool TryGetHeightmap(TerrainTileIndex idx, float[] heightmap, CancellationToken tok)
    {
      if (m_Connection == null)
      {
        return false;
      }

      try
      {
        using (var cmd = m_Connection.CreateCommand())
        {
          cmd.CommandType = CommandType.Text;
          cmd.CommandText = "SELECT `baseElevation`, `elevationDelta`, `heightMap` FROM `heightmaps` WHERE `tileID` = @tileID";

          cmd.Parameters.AddWithValue("tileID", WMSConversions.TileToQuadKey(idx));

          using (var reader = cmd.ExecuteReader())
          {
            tok.ThrowIfCancellationRequested();
            if (reader.Read())
            {
              var rowBuffer = m_HeightmapRowBuffer.Value;
              var memStream = m_Stream.Value;
              var buffer = memStream.GetBuffer();

              var baseElevation = reader.GetFloat(0);
              var elevationDelta = reader.GetFloat(1);
              //Read the heightmap png
              reader.GetBytes(2, 0, buffer, 0, buffer.Length);
              reader.Close();

              memStream.Seek(0, SeekOrigin.Begin);

              var pngReader = new PngReader(memStream);
              pngReader.ShouldCloseStream = false;

              var imgInfo = pngReader.ImgInfo;
              var multiplier = elevationDelta / ((1 << imgInfo.BitspPixel) - 1);

              for (var j = 0; j < 256; ++j)
              {
                var jOff = j * 256;
                pngReader.ReadRowInt(rowBuffer, j);
                for (var i = 0; i < 256; ++i)
                {
                  heightmap[jOff + i] = rowBuffer[i] * multiplier + baseElevation;
                }
              }

              return true;
            }
            else
            {
              return false;
            }
          }
        }
      }
      catch
      {
        return false;
      }
    }

    private void LoadGroupPoints(TerrainTileIndex idx, IList<int> indices, ArraySegment<ElevationPointSample> points, CancellationToken tok)
    {
      var heightmap = m_HeightmapBuffer.Value;

      if (TryGetHeightmap(idx, heightmap, tok))
      {
        int tilePixelX, tilePixelY;
        WMSConversions.TileToPixelXY(idx, out tilePixelX, out tilePixelY);

        var indicesCount = indices.Count;
        for (var i = 0; i < indicesCount; ++i)
        {
          tok.ThrowIfCancellationRequested();
          var pointIndex = indices[i];
          var geoPoint = points.Array[pointIndex].Position;

          double pixelX, pixelY;
          WMSConversions.GeoToPixelXY(geoPoint, idx.Level, out pixelX, out pixelY);

          pixelX -= tilePixelX;
          pixelY -= tilePixelY;

          var elevation = Interpolate(pixelY, pixelX, heightmap, 256, 256);
          points.Array[pointIndex] = new ElevationPointSample(geoPoint, elevation);
        }
      }
      else if (m_Fallback != null)
      {
        var indicesCount = indices.Count;
        var geoPoints = new ElevationPointSample[indicesCount];
        for (var i = 0; i < indicesCount; ++i)
        {
          tok.ThrowIfCancellationRequested();
          geoPoints[i] = points.Array[indices[i]];
        }

        m_Fallback.QueryPointSamplesInto(geoPoints);

        for (var i = 0; i < indicesCount; ++i)
        {
          tok.ThrowIfCancellationRequested();
          points.Array[indices[i]] = geoPoints[i];
        }
      }
    }

    private List<KeyValuePair<TerrainTileIndex, List<int>>> GroupByLeastTiles(ArraySegment<ElevationPointSample> points, int baseLod, CancellationToken tok)
    {
      var groupCount = 0;
      var groupings = new List<KeyValuePair<TerrainTileIndex, List<int>>>();

      var startIdx = points.Offset;
      var endIdx = startIdx + points.Count;
      for (var i = startIdx; i < endIdx; ++i)
      {
        tok.ThrowIfCancellationRequested();
        var geoPoint = points.Array[i].Position;

        int tileX, tileY;
        WMSConversions.GeoToTileXY(geoPoint, baseLod, out tileX, out tileY);
        var tileIndex = new TerrainTileIndex(baseLod, tileY, tileX);

        var tileGroup = default(List<int>);
        for (var gidx = 0; gidx < groupCount; ++gidx)
        {
          var kvp = groupings[gidx];
          if (kvp.Key.Equals(tileIndex))
          {
            tileGroup = kvp.Value;
          }
        }

        if (tileGroup == null)
        {
          ++groupCount;

          if (baseLod > 1 && groupCount > 4)
          {
            //Crossing to another tile. Try a lower LOD
            groupings.Clear();
            return GroupByLeastTiles(points, baseLod - 1, tok);
          }
          else
          {
            tileGroup = new List<int>();
            groupings.Add(new KeyValuePair<TerrainTileIndex, List<int>>(tileIndex, tileGroup));
          }
        }

        tileGroup.Add(i);
      }

      return groupings;
    }

    private List<KeyValuePair<TerrainTileIndex, List<int>>> GroupByTiles(ArraySegment<ElevationPointSample> points, int lod, CancellationToken tok)
    {
      var groupCount = 0;
      var groupings = new List<KeyValuePair<TerrainTileIndex, List<int>>>();
      var startIdx = points.Offset;
      var endIdx = startIdx + points.Count;
      for (var i = startIdx; i < endIdx; ++i)
      {
        tok.ThrowIfCancellationRequested();
        var geoPoint = points.Array[i].Position;

        int tileX, tileY;
        WMSConversions.GeoToTileXY(geoPoint, lod, out tileX, out tileY);
        var tileIndex = new TerrainTileIndex(lod, tileY, tileX);

        var tileGroup = default(List<int>);
        for (var gidx = 0; gidx < groupCount; ++gidx)
        {
          var kvp = groupings[gidx];
          if (kvp.Key.Equals(tileIndex))
          {
            tileGroup = kvp.Value;
          }
        }

        if (tileGroup == null)
        {
          ++groupCount;
          tileGroup = new List<int>();
          groupings.Add(new KeyValuePair<TerrainTileIndex, List<int>>(tileIndex, tileGroup));
        }

        tileGroup.Add(i);
      }

      return groupings;
    }

    private readonly FileInfo m_DatabaseFile;
    private readonly int m_BaseLOD;
    private readonly bool m_UseDownsamples;
    private readonly IElevationProvider m_Fallback;

    private readonly SQLiteConnection m_Connection;
    private readonly ThreadLocal<int[]> m_HeightmapRowBuffer;
    private readonly ThreadLocal<float[]> m_HeightmapBuffer;
    private readonly ThreadLocal<MemoryStream> m_Stream;

    private bool m_Disposed;
  }
}



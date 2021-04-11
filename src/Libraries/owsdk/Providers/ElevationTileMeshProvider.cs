//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using sbio.Core.Math;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Services;
using sbio.owsdk.Tiles;
using sbio.owsdk.WMS;

namespace sbio.owsdk.Providers
{
  public sealed class ElevationTileMeshProvider : ITileMeshProvider
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
        NumSamples = 20;
        MaxParallelRequests = Environment.ProcessorCount;
        SkirtHeight = 1000;
        WaterDepth = 50;
      }

      /// <summary>
      /// The number of horizontal and vertical samples to generate for each tile (NxN)
      /// </summary>
      public int NumSamples { get; set; }

      /// <summary>
      /// The maximum number of requests to allow to happen concurrently
      /// </summary>
      public int MaxParallelRequests { get; set; }

      /// <summary>
      /// The height of the skirt surrounding the tiles
      /// </summary>
      public double SkirtHeight { get; set; }

      /// <summary>
      /// How much to offset every generated vertex that is considered 'water'
      /// </summary>
      public double WaterDepth { get; set; }
    }

    public ITileMapper TileMapper { get; set; }

    public Task<TileMesh> QueryTileMeshAsync(TerrainTileIndex idx, CancellationToken tok)
    {
      return Task.Run(async () =>
      {
        ElevationPointSample[] sampleBuf;
        while (!m_SamplesBuffers.TryPop(out sampleBuf))
        {
          tok.ThrowIfCancellationRequested();
          await Task.Yield();
        }

        try
        {
          GenerateSamplePoints(idx, sampleBuf, tok);
          if (m_ElevationProvider != null)
          {
            await m_ElevationProvider.QueryPointSamplesAsyncInto(sampleBuf, tok);
          }

          ITileAttributeMask mask;
          if (m_TileAttributesProvider != null)
          {
            try
            {
              mask = await m_TileAttributesProvider.QueryTileAttributesAsync(idx, tok);
            }
            catch
            {
              mask = null;
            }
          }
          else
          {
            mask = null;
          }

          return GenerateMesh(idx, sampleBuf, mask, tok);
        }
        finally
        {
          m_SamplesBuffers.Push(sampleBuf);
        }
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

    public ElevationTileMeshProvider(Ellipsoid ellipsoid, Settings settings)
      : this(ellipsoid, null, null, settings)
    {
    }

    public ElevationTileMeshProvider(Ellipsoid ellipsoid, IElevationProvider elevationProvider, Settings settings)
      : this(ellipsoid, elevationProvider, null, settings)
    {
    }

    public ElevationTileMeshProvider(Ellipsoid ellipsoid, ITileAttributesProvider attributesProvider, Settings settings)
      : this(ellipsoid, null, attributesProvider, settings)
    {
    }

    public ElevationTileMeshProvider(Ellipsoid ellipsoid, IElevationProvider elevationProvider, ITileAttributesProvider attributesProvider, Settings settings)
    {
      m_Ellipsoid = ellipsoid;
      m_ElevationProvider = elevationProvider;
      m_TileAttributesProvider = attributesProvider;
      m_SamplesBuffers = new ConcurrentStack<ElevationPointSample[]>(Enumerable.Repeat(0, settings.MaxParallelRequests).Select(_ => new ElevationPointSample[settings.NumSamples * settings.NumSamples]));
      m_NumSamples = settings.NumSamples;
      m_SkirtHeight = settings.SkirtHeight;
      m_WaterDepth = settings.WaterDepth;
      TileMapper = WMSTileMapper.Instance;
    }

    private static Bounds3d SampleBounds(Ellipsoid ellipsoid, IEnumerable<ElevationPointSample> samples)
    {
      return Bounds3d.FromPoints(samples.Select(s => ellipsoid.ToVec3LeftHandedGeocentric(s.Position, s.Elevation).ToVector3d()));
    }

    /// <summary>
    /// Generates the edge indices of a [rowsxcolumns] 2d matrix, in clockwise order
    /// the order starting at [0, 0]
    /// eg for a 3x3 matrix:
    /// [0,0], [0,1], [0,2], [1,2], [2,2], [2,1], [2,0], [1,0]
    /// </summary>
    /// <param name="rows"></param>
    /// <param name="columns"></param>
    /// <returns></returns>
    private static IEnumerable<Tuple<int, int>> EdgeIndices(int rows, int columns)
    {
      //Top
      for (int i = 0; i < columns - 1; ++i)
      {
        yield return Tuple.Create(0, i);
      }

      //Right
      for (int i = 0; i < rows - 1; ++i)
      {
        yield return Tuple.Create(i, columns - 1);
      }

      //Bottom
      for (int i = columns - 1; i > 0; --i)
      {
        yield return Tuple.Create(rows - 1, i);
      }

      //Left
      for (int i = rows - 1; i > 0; --i)
      {
        yield return Tuple.Create(i, 0);
      }
    }

    private void GenerateSamplePoints(TerrainTileIndex tileIndex, ElevationPointSample[] samples, CancellationToken tok)
    {
      var numSamplesY = m_NumSamples;
      var numSamplesX = m_NumSamples;

      var mapPixelWidth = TileMapper.MapPixelWidth(tileIndex.Level);
      int txPx, tyPx;
      TileMapper.TileToPixelXY(tileIndex, out txPx, out tyPx);
      var tilePixelWidth = TileMapper.TilePixelHeight(tileIndex.Level);
      var tilePixelHeight = TileMapper.TilePixelWidth(tileIndex.Level);
      var xDeltaPx = (double)tilePixelHeight / (numSamplesY - 1);
      var yDeltaPx = (double)tilePixelWidth / (numSamplesX - 1);

      for (var y = 0; y < numSamplesY; ++y)
      {
        var yOff = y * numSamplesX;
        var yPx = (int)((tyPx + yDeltaPx * y));
        for (var x = 0; x < numSamplesX; ++x)
        {
          var xOff = x;
          var xPx = (int)((txPx + xDeltaPx * x)) % mapPixelWidth; //wrap around longitude

          var geoPoint = TileMapper.PixelXYToGeo(tileIndex.Level, xPx, yPx);
          samples[yOff + xOff] = new ElevationPointSample(geoPoint);
        }
      }
    }

    private TileMesh GenerateMesh(TerrainTileIndex idx, ElevationPointSample[] elevationSamples, ITileAttributeMask mask, CancellationToken tok)
    {
      var tileIndex = idx;
      var ellipsoid = m_Ellipsoid;
      Vector3d vCenter = SampleBounds(m_Ellipsoid, elevationSamples).Center;
      Vec3LeftHandedGeocentric meshOrigin3d = new Vec3LeftHandedGeocentric(vCenter.x, vCenter.y, vCenter.z);

      //The length of the skirt
      var skirtHeightMeters = m_SkirtHeight;
      var numLatVertices = m_NumSamples;
      var numLonVertices = m_NumSamples;
      var numLatLonVertices = numLatVertices * numLonVertices;
      var skirtSampleIndices = MathUtilities.IsEqual(m_SkirtHeight, 0.0) ? sc_NoSkirtIndices : EdgeIndices(numLatVertices, numLonVertices).ToArray();
      var numSkirtVertices = skirtSampleIndices.Length;

      var numVertices = numLatLonVertices + numSkirtVertices;
      var vertices = new Vector3f[numVertices];
      var normals = new Vector3f[numVertices];

      QuaternionLeftHandedGeocentric rotation;
      {
        Vec3LeftHandedGeocentric normal;
        var north = ellipsoid.NorthDirection(ellipsoid.ToGeodetic2d(meshOrigin3d), out normal);
        rotation = MathUtilities.CreateGeocentricRotation(north, normal);
      }
      var rotationInv = rotation.Inverse();

      //Get the top left pixel to calculate UV's
      int xOriginPx, yOriginPx;
      TileMapper.TileToPixelXY(tileIndex, out xOriginPx, out yOriginPx);

      for (var vertIndex = 0; vertIndex < numVertices; ++vertIndex)
      {
        Geodetic2d coordGeo;
        double elevation;
        if (vertIndex < numLatLonVertices)
        {
          //Regular vertex
          var sample = elevationSamples[vertIndex];
          coordGeo = sample.Position;
          elevation = sample.Elevation;
        }
        else
        {
          //Skirt vertex
          var skirtIndex = vertIndex - numLatLonVertices;
          var sample = elevationSamples[skirtSampleIndices[skirtIndex].Item1 * numLonVertices + skirtSampleIndices[skirtIndex].Item2];
          coordGeo = sample.Position;
          elevation = sample.Elevation - skirtHeightMeters;
        }

        {
          double xPx, yPx;
          TileMapper.GeoToPixelXY(coordGeo, idx.Level, out xPx, out yPx);

          if (mask != null && mask.IsWater(xPx - xOriginPx, yPx - yOriginPx))
          {
            elevation -= m_WaterDepth;
          }
        }

        //Vertex position in 3d world
        Vec3LeftHandedGeocentric normal;
        var point3d = ellipsoid.ToVec3LeftHandedGeocentric(coordGeo, elevation, out normal);

        normals[vertIndex] = rotationInv.Multiply(normal).ToVector3f();
        vertices[vertIndex] = rotationInv.Multiply(point3d - meshOrigin3d).ToVector3f();
      }

      var extents = (Vector3f)Bounds3d.FromPoints(vertices.Take(numLatLonVertices).Select(v => (Vector3d)v)).Extents;

      var triangles = new int[(numLatVertices - 1) * (numLonVertices - 1) * 2 * 3 + (numSkirtVertices * 2 * 3)];

      {
        var latLonTriangleVerticesCount = (numLatVertices - 1) * (numLonVertices - 1);
        var skirtVertIndexBase = numLatLonVertices;
        var triangleIndexBase = (numLatVertices - 1) * (numLonVertices - 1) * 2 * 3;
        var triangleCount = latLonTriangleVerticesCount + numSkirtVertices;

        //Calculate triangles
        for (var index = 0; index < triangleCount; ++index)
        {
          if (index < latLonTriangleVerticesCount)
          {
            //Non-skirt triangle
            int x = 0;
            int y = Math.DivRem(index, numLatVertices - 1, out x);

            var topLeftVert = (y * numLonVertices + x);
            var topRightVert = (y * numLonVertices + (x + 1));
            var bottomLeftVert = ((y + 1) * numLonVertices + x);
            var bottomRightVert = ((y + 1) * numLonVertices + (x + 1));

            var triangleIndex = (y * (numLonVertices - 1) + x) * 6;

            triangles[triangleIndex + 0] = topLeftVert;
            triangles[triangleIndex + 1] = topRightVert;
            triangles[triangleIndex + 2] = bottomRightVert;

            triangles[triangleIndex + 3] = topLeftVert;
            triangles[triangleIndex + 4] = bottomRightVert;
            triangles[triangleIndex + 5] = bottomLeftVert;
          }
          else
          {
            //Skirt triangle
            var i = index - latLonTriangleVerticesCount;
            var y1 = skirtSampleIndices[i].Item1;
            var x1 = skirtSampleIndices[i].Item2;
            var y2 = skirtSampleIndices[(i + 1) % numSkirtVertices].Item1;
            var x2 = skirtSampleIndices[(i + 1) % numSkirtVertices].Item2;

            var sampleRefIdx1 = (y1 * numLonVertices + x1);
            var sampleRefIdx2 = (y2 * numLonVertices + x2);
            var skirtIdx1 = (skirtVertIndexBase + i);
            var skirtIdx2 = (skirtVertIndexBase + ((i + 1) % numSkirtVertices));

            int triangleIndex = triangleIndexBase + i * 6;

            triangles[triangleIndex + 0] = skirtIdx1;
            triangles[triangleIndex + 1] = sampleRefIdx2;
            triangles[triangleIndex + 2] = sampleRefIdx1;

            triangles[triangleIndex + 3] = skirtIdx1;
            triangles[triangleIndex + 4] = skirtIdx2;
            triangles[triangleIndex + 5] = sampleRefIdx2;
          }
        }
      }

      var uvs = TileMeshUtil.CalculateUVs(idx, TileMapper, meshOrigin3d, rotation, vertices, m_Ellipsoid, tok);

      return new TileMesh(meshOrigin3d, extents, rotation, vertices, uvs, normals, triangles);
    }

    private static readonly Tuple<int, int>[] sc_NoSkirtIndices = new Tuple<int, int>[0];

    private readonly ConcurrentStack<ElevationPointSample[]> m_SamplesBuffers;
    private readonly Ellipsoid m_Ellipsoid;
    private readonly IElevationProvider m_ElevationProvider;
    private readonly ITileAttributesProvider m_TileAttributesProvider;
    private readonly int m_NumSamples;
    private readonly double m_SkirtHeight;
    private readonly double m_WaterDepth;
    private bool m_Disposed;
  }
}



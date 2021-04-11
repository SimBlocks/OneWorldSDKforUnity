//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Linq;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Tiles;
using DotSpatial.Projections;

namespace sbio.owsdk.WMS
{
  public struct WMTSTileMatrix
  {
    public double UnitsPerPixel { get; set; }
    public double ScaleDenominator { get; set; }
    public double Top { get; set; }
    public double Left { get; set; }
    public int MatrixWidth { get; set; }
    public int MatrixHeight { get; set; }
    public int TileWidth { get; set; }
    public int TileHeight { get; set; }
  }

  public sealed class WMTSTileMapper : ITileMapper
  {
    public const double StandardPixelSize = 0.00028d;

    public int TilePixelWidth(int lod)
    {
      var tm = LODMatrix(lod);
      return tm.TileWidth;
    }

    public int TilePixelHeight(int lod)
    {
      var tm = LODMatrix(lod);
      return tm.TileHeight;
    }

    public int NumTilesX(int lod)
    {
      var tm = LODMatrix(lod);
      return tm.MatrixWidth;
    }

    public int NumTilesY(int lod)
    {
      var tm = LODMatrix(lod);
      return tm.MatrixHeight;
    }

    public void GeoToPixelXY(Geodetic2d geo, int lod, out double pixelX, out double pixelY)
    {
      var tm = LODMatrix(lod);

      var xy = new double[] {geo.LongitudeDegrees, geo.LatitudeDegrees};

      Reproject.ReprojectPoints(xy, null, KnownCoordinateSystems.Geographic.World.WGS1984, m_SrsProjection, 0, 1);

      var srsX = xy[0];
      var srsY = xy[1];

      pixelX = ((srsX - tm.Left) / tm.UnitsPerPixel) + 0.5;
      pixelY = ((tm.Top - srsY) / tm.UnitsPerPixel) + 0.5;
    }

    public Geodetic2d PixelXYToGeo(int lod, double pixelX, double pixelY)
    {
      var tm = LODMatrix(lod);

      var srsX = tm.Left + (pixelX * tm.UnitsPerPixel);
      var srsY = tm.Top - (pixelY * tm.UnitsPerPixel);

      var xy = new double[] {srsX, srsY};
      Reproject.ReprojectPoints(xy, null, m_SrsProjection, KnownCoordinateSystems.Geographic.World.WGS1984, 0, 1);

      return Geodetic2d.FromDegrees(xy[1], xy[0]);
    }

    public WMTSTileMapper(ProjectionInfo srs, IEnumerable<WMTSTileMatrix> tileMatrices, int startLOD = 0)
    {
      m_SrsProjection = srs;
      m_StartLOD = startLOD;
      m_TileMatrices = tileMatrices.ToArray();
    }

    private WMTSTileMatrix LODMatrix(int lod)
    {
      var idx = lod - m_StartLOD;

      if (idx < 0 || idx >= m_TileMatrices.Length)
      {
        throw new ArgumentOutOfRangeException(nameof(lod));
      }

      return m_TileMatrices[idx];
    }

    private readonly ProjectionInfo m_SrsProjection;
    private readonly int m_StartLOD;
    private readonly WMTSTileMatrix[] m_TileMatrices;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

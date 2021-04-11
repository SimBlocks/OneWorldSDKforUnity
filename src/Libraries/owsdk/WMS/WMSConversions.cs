//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Text;
using System.Collections.Generic;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Tiles;

namespace sbio.owsdk.WMS
{
  public static class WMSConversions
  {
    private const double EarthRadius = 6378137;
    private const double MinLatitudeDegrees = -85.05112878;
    private const double MaxLatitudeDegrees = 85.05112878;
    private const double MinLongitudeDegrees = -180;
    private const double MaxLongitudeDegrees = 180;
    private const double MinLongitudeRadians = -Math.PI;
    private const double MaxLongitudeRadians = Math.PI;
    private const double MinLatitudeRadians = -1.48442222974871;
    private const double MaxLatitudeRadians = 1.48442222974871;

    /// <summary>
    /// Clips a number to the specified minimum and maximum values.
    /// </summary>
    /// <param name="n">The number to clip.</param>
    /// <param name="minValue">Minimum allowable value.</param>
    /// <param name="maxValue">Maximum allowable value.</param>
    /// <returns>The clipped value.</returns>
    private static double Clip(double n, double minValue, double maxValue)
    {
      return Math.Min(Math.Max(n, minValue), maxValue);
    }

    /// <summary>
    /// Determines the map width and height (in pixels) at a specified level
    /// of detail.
    /// </summary>
    /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
    /// to 23 (highest detail).</param>
    /// <returns>The map width and height in pixels.</returns>
    public static uint MapSize(int levelOfDetail)
    {
      return (uint)256 << levelOfDetail;
    }

    /// <summary>
    /// Determines the map height and width in tiles at a specified level
    /// of detail.
    /// </summary>
    /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
    /// to 23 (highest detail).</param>
    /// <returns>The map width and height in tiles.</returns>
    public static uint MapTileSize(int levelOfDetail)
    {
      return (uint)1 << levelOfDetail;
    }

    /// <summary>
    /// Determines the ground resolution (in meters per pixel) at a specified
    /// latitude and level of detail.
    /// </summary>
    /// <param name="latitudeDegrees">Latitude (in degrees) at which to measure the
    /// ground resolution.</param>
    /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
    /// to 23 (highest detail).</param>
    /// <returns>The ground resolution, in meters per pixel.</returns>
    public static double GroundResolution(double latitudeDegrees, int levelOfDetail)
    {
      latitudeDegrees = Clip(latitudeDegrees, MinLatitudeDegrees, MaxLatitudeDegrees);
      return Math.Cos(latitudeDegrees * Math.PI / 180) * 2 * Math.PI * EarthRadius / MapSize(levelOfDetail);
    }

    public static double GroundResolutionRadians(double latitudeRadians, int levelOfDetail)
    {
      latitudeRadians = Clip(latitudeRadians, MinLatitudeRadians, MaxLatitudeRadians);
      return Math.Cos(latitudeRadians) * 2 * Math.PI * EarthRadius / MapSize(levelOfDetail);
    }

    public static double GroundResolution(Geodetic2d geoCoord, int levelOfDetail)
    {
      return GroundResolutionRadians(geoCoord.LatitudeRadians, levelOfDetail);
    }

    /// <summary>
    /// Determines the map scale at a specified latitude, level of detail,
    /// and screen resolution.
    /// </summary>
    /// <param name="latitudeDegrees">Latitude (in degrees) at which to measure the
    /// map scale.</param>
    /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
    /// to 23 (highest detail).</param>
    /// <param name="screenDpi">Resolution of the screen, in dots per inch.</param>
    /// <returns>The map scale, expressed as the denominator N of the ratio 1 : N.</returns>
    public static double MapScale(double latitudeDegrees, int levelOfDetail, int screenDpi)
    {
      return GroundResolution(latitudeDegrees, levelOfDetail) * screenDpi / 0.0254;
    }

    /// <summary>
    /// Converts a point from latitude/longitude WGS-84 coordinates (in degrees)
    /// into pixel XY coordinates at a specified level of detail.
    /// </summary>
    /// <param name="latitudeDegrees">Latitude of the point, in degrees.</param>
    /// <param name="longitudeDegrees">Longitude of the point, in degrees.</param>
    /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
    /// to 23 (highest detail).</param>
    /// <param name="pixelX">Output parameter receiving the X coordinate in pixels.</param>
    /// <param name="pixelY">Output parameter receiving the Y coordinate in pixels.</param>
    public static void LatLongToPixelXY(double latitudeDegrees, double longitudeDegrees, int levelOfDetail, out int pixelX, out int pixelY)
    {
      latitudeDegrees = Clip(latitudeDegrees, MinLatitudeDegrees, MaxLatitudeDegrees);
      longitudeDegrees = Clip(longitudeDegrees, MinLongitudeDegrees, MaxLongitudeDegrees);

      double x = (longitudeDegrees + 180) / 360;
      double sinLatitude = Math.Sin(latitudeDegrees * Math.PI / 180);
      double y = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI);

      uint mapSize = MapSize(levelOfDetail);
      pixelX = (int)Clip(x * mapSize + 0.5, 0, mapSize - 1);
      pixelY = (int)Clip(y * mapSize + 0.5, 0, mapSize - 1);
    }

    public static void LatLongRadiansToPixelXY(double latitudeRadians, double longitudeRadians, int levelOfDetail, out int pixelX, out int pixelY)
    {
      latitudeRadians = Clip(latitudeRadians, MinLatitudeRadians, MaxLatitudeRadians);
      longitudeRadians = Clip(longitudeRadians, MinLongitudeRadians, MaxLongitudeRadians);

      var x = (longitudeRadians + Math.PI) / (2 * Math.PI);
      var sinLatitude = Math.Sin(latitudeRadians);
      var y = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI);

      var mapSize = MapSize(levelOfDetail);
      pixelX = (int)Clip(x * mapSize + 0.5, 0, mapSize - 1);
      pixelY = (int)Clip(y * mapSize + 0.5, 0, mapSize - 1);
    }

    public static void GeoToPixelXY(Geodetic2d geo, int levelOfDetail, out int pixelX, out int pixelY)
    {
      LatLongRadiansToPixelXY(geo.LatitudeRadians, geo.LongitudeRadians, levelOfDetail, out pixelX, out pixelY);
    }

    public static void LatLongToPixelXY(double latitudeDegrees, double longitudeDegrees, int levelOfDetail, out double pixelX, out double pixelY)
    {
      latitudeDegrees = Clip(latitudeDegrees, MinLatitudeDegrees, MaxLatitudeDegrees);
      longitudeDegrees = Clip(longitudeDegrees, MinLongitudeDegrees, MaxLongitudeDegrees);

      double x = (longitudeDegrees + 180) / 360;
      double sinLatitude = Math.Sin(latitudeDegrees * Math.PI / 180);
      double y = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI);

      uint mapSize = MapSize(levelOfDetail);
      pixelX = Clip(x * mapSize + 0.5, 0, mapSize - 1);
      pixelY = Clip(y * mapSize + 0.5, 0, mapSize - 1);
    }

    public static void LatLongRadiansToPixelXY(double latitudeRadians, double longitudeRadians, int levelOfDetail, out double pixelX, out double pixelY)
    {
      latitudeRadians = Clip(latitudeRadians, MinLatitudeRadians, MaxLatitudeRadians);
      longitudeRadians = Clip(longitudeRadians, MinLongitudeRadians, MaxLongitudeRadians);

      var x = (longitudeRadians + Math.PI) / (2 * Math.PI);
      var sinLatitude = Math.Sin(latitudeRadians);
      var y = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI);

      var mapSize = MapSize(levelOfDetail);
      pixelX = Clip(x * mapSize + 0.5, 0, mapSize - 1);
      pixelY = Clip(y * mapSize + 0.5, 0, mapSize - 1);
    }

    public static void GeoToPixelXY(Geodetic2d geo, int levelOfDetail, out double pixelX, out double pixelY)
    {
      LatLongRadiansToPixelXY(geo.LatitudeRadians, geo.LongitudeRadians, levelOfDetail, out pixelX, out pixelY);
    }

    /// <summary>
    /// Converts a pixel from pixel XY coordinates at a specified level of detail
    /// into latitude/longitude WGS-84 coordinates (in degrees).
    /// </summary>
    /// <param name="pixelX">X coordinate of the point, in pixels.</param>
    /// <param name="pixelY">Y coordinates of the point, in pixels.</param>
    /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
    /// to 23 (highest detail).</param>
    /// <param name="latitudeDegrees">Output parameter receiving the latitude in degrees.</param>
    /// <param name="longitudeDegrees">Output parameter receiving the longitude in degrees.</param>
    public static void PixelXYToLatLong(int pixelX, int pixelY, int levelOfDetail, out double latitudeDegrees, out double longitudeDegrees)
    {
      double mapSize = MapSize(levelOfDetail);
      double x = (Clip(pixelX, 0, mapSize - 1) / mapSize) - 0.5;
      double y = 0.5 - (Clip(pixelY, 0, mapSize - 1) / mapSize);

      latitudeDegrees = 90 - 360 * Math.Atan(Math.Exp(-y * 2 * Math.PI)) / Math.PI;
      longitudeDegrees = 360 * x;
    }

    public static double PixelXToLongitudeRadians(int pixelX, int levelOfDetail)
    {
      double mapSize = MapSize(levelOfDetail);
      double x = (Clip(pixelX, 0, mapSize - 1) / mapSize) - 0.5;
      return 2 * Math.PI * x;
    }

    public static double PixelYToLatitudeRadians(int pixelY, int levelOfDetail)
    {
      double mapSize = MapSize(levelOfDetail);
      double y = 0.5 - (Clip(pixelY, 0, mapSize - 1) / mapSize);

      return Math.PI * 0.5 - 2 * Math.Atan(Math.Exp(-y * 2 * Math.PI));
    }

    /// <summary>
    /// Converts a pixel from pixel XY coordinates at a specified level of detail
    /// into latitude/longitude WGS-84 coordinates (in degrees).
    /// </summary>
    /// <param name="pixelX">X coordinate of the point, in pixels.</param>
    /// <param name="pixelY">Y coordinates of the point, in pixels.</param>
    /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
    /// to 23 (highest detail).</param>
    /// <param name="latitudeRadians">Output parameter receiving the latitude in degrees.</param>
    /// <param name="longitudeRadians">Output parameter receiving the longitude in degrees.</param>
    public static void PixelXYToLatLongRadians(int pixelX, int pixelY, int levelOfDetail, out double latitudeRadians, out double longitudeRadians)
    {
      double mapSize = MapSize(levelOfDetail);
      double x = (Clip(pixelX, 0, mapSize - 1) / mapSize) - 0.5;
      double y = 0.5 - (Clip(pixelY, 0, mapSize - 1) / mapSize);

      latitudeRadians = Math.PI * 0.5 - 2 * Math.Atan(Math.Exp(-y * 2 * Math.PI));
      longitudeRadians = 2 * Math.PI * x;
    }

    public static Geodetic2d PixelXYToGeo(int pixelX, int pixelY, int levelOfDetail)
    {
      double mapSize = MapSize(levelOfDetail);
      double x = (Clip(pixelX, 0, mapSize - 1) / mapSize) - 0.5;
      double y = 0.5 - (Clip(pixelY, 0, mapSize - 1) / mapSize);

      var latitudeRadians = Math.PI * 0.5 - 2 * Math.Atan(Math.Exp(-y * 2 * Math.PI));
      var longitudeRadians = 2 * Math.PI * x;

      return Geodetic2d.FromRadians(latitudeRadians, longitudeRadians);
    }

    public static Geodetic2d PixelXYToGeo(double pixelX, double pixelY, int levelOfDetail)
    {
      double mapSize = MapSize(levelOfDetail);
      double x = (Clip(pixelX, 0, mapSize - 1) / mapSize) - 0.5;
      double y = 0.5 - (Clip(pixelY, 0, mapSize - 1) / mapSize);

      var latitudeRadians = Math.PI * 0.5 - 2 * Math.Atan(Math.Exp(-y * 2 * Math.PI));
      var longitudeRadians = 2 * Math.PI * x;

      return Geodetic2d.FromRadians(latitudeRadians, longitudeRadians);
    }

    /// <summary>
    /// Converts pixel XY coordinates into tile XY coordinates of the tile containing
    /// the specified pixel.
    /// </summary>
    /// <param name="pixelX">Pixel X coordinate.</param>
    /// <param name="pixelY">Pixel Y coordinate.</param>
    /// <param name="tileX">Output parameter receiving the tile X coordinate.</param>
    /// <param name="tileY">Output parameter receiving the tile Y coordinate.</param>
    public static void PixelXYToTileXY(int pixelX, int pixelY, out int tileX, out int tileY)
    {
      tileX = pixelX / 256;
      tileY = pixelY / 256;
    }

    public static int PixelXToTileX(int pixelX)
    {
      return pixelX / 256;
    }

    public static int PixelYToTileY(int pixelY)
    {
      return pixelY / 256;
    }

    public static TerrainTileIndex PixelXYToTile(int pixelX, int pixelY, int lod)
    {
      return new TerrainTileIndex(lod, pixelY / 256, pixelX / 256);
    }

    public static void LatLongToTileXY(double latitude, double longitude, int levelOfDetail, out int tileX, out int tileY)
    {
      int pixelX, pixelY;
      LatLongToPixelXY(latitude, longitude, levelOfDetail, out pixelX, out pixelY);

      PixelXYToTileXY(pixelX, pixelY, out tileX, out tileY);
    }

    public static void LatLongRadiansToTileXY(double latitudeRadians, double longitudeRadians, int levelOfDetail, out int tileX, out int tileY)
    {
      int pixelX, pixelY;
      LatLongRadiansToPixelXY(latitudeRadians, longitudeRadians, levelOfDetail, out pixelX, out pixelY);

      PixelXYToTileXY(pixelX, pixelY, out tileX, out tileY);
    }

    public static void GeoToTileXY(Geodetic2d geo, int levelOfDetail, out int tileX, out int tileY)
    {
      LatLongRadiansToTileXY(geo.LatitudeRadians, geo.LongitudeRadians, levelOfDetail, out tileX, out tileY);
    }

    public static TerrainTileIndex GeoToTile(Geodetic2d geo, int levelOfDetail)
    {
      int tileX, tileY;
      LatLongRadiansToTileXY(geo.LatitudeRadians, geo.LongitudeRadians, levelOfDetail, out tileX, out tileY);

      return new TerrainTileIndex(levelOfDetail, tileY, tileX);
    }

    public static IEnumerable<TerrainTileIndex> TilesInExtent(GeoBoundingBox area, int lod)
    {
      int leftCol, topRow;
      GeoToTileXY(area.NorthWest, lod, out leftCol, out topRow);
      int rightCol, bottomRow;
      GeoToTileXY(area.SouthEast, lod, out rightCol, out bottomRow);

      for (var j = topRow; j <= bottomRow; ++j)
      {
        for (var i = leftCol; i <= rightCol; ++i)
        {
          yield return new TerrainTileIndex(lod, j, i);
        }
      }
    }

    public static int ExtentTileWidth(GeoBoundingBox area, int lod)
    {
      int leftCol, topRow;
      GeoToTileXY(area.NorthWest, lod, out leftCol, out topRow);
      int rightCol, bottomRow;
      GeoToTileXY(area.SouthEast, lod, out rightCol, out bottomRow);

      return (rightCol - leftCol) + 1;
    }

    public static int ExtentTileHeight(GeoBoundingBox area, int lod)
    {
      int leftCol, topRow;
      GeoToTileXY(area.NorthWest, lod, out leftCol, out topRow);
      int rightCol, bottomRow;
      GeoToTileXY(area.SouthEast, lod, out rightCol, out bottomRow);

      return (bottomRow - topRow) + 1;
    }

    public static void ExtentPixelSize(GeoBoundingBox area, int lod, out int pixelX, out int pixelY, out int pixelWidth, out int pixelHeight)
    {
      int leftCol, topRow;
      GeoToPixelXY(area.NorthWest, lod, out leftCol, out topRow);
      int rightCol, bottomRow;
      GeoToPixelXY(area.SouthEast, lod, out rightCol, out bottomRow);

      pixelX = leftCol;
      pixelY = topRow;
      pixelWidth = (rightCol - leftCol) + 1;
      pixelHeight = (bottomRow - topRow) + 1;
    }

    public static int ExtentPixelWidth(GeoBoundingBox area, int lod)
    {
      int leftCol, topRow;
      GeoToPixelXY(area.NorthWest, lod, out leftCol, out topRow);
      int rightCol, bottomRow;
      GeoToPixelXY(area.SouthEast, lod, out rightCol, out bottomRow);

      return (rightCol - leftCol) + 1;
    }

    public static int ExtentPixelHeight(GeoBoundingBox area, int lod)
    {
      int leftCol, topRow;
      GeoToPixelXY(area.NorthWest, lod, out leftCol, out topRow);
      int rightCol, bottomRow;
      GeoToPixelXY(area.SouthEast, lod, out rightCol, out bottomRow);

      return (bottomRow - topRow) + 1;
    }

    /// <summary>
    /// Converts tile XY coordinates into pixel XY coordinates of the upper-left pixel
    /// of the specified tile.
    /// </summary>
    /// <param name="tileX">Tile X coordinate.</param>
    /// <param name="tileY">Tile Y coordinate.</param>
    /// <param name="pixelX">Output parameter receiving the pixel X coordinate.</param>
    /// <param name="pixelY">Output parameter receiving the pixel Y coordinate.</param>
    public static void TileXYToPixelXY(int tileX, int tileY, out int pixelX, out int pixelY)
    {
      pixelX = tileX * 256;
      pixelY = tileY * 256;
    }

    public static int TileXToPixelX(int tileX)
    {
      return tileX * 256;
    }

    public static int TileYToPixelY(int tileY)
    {
      return tileY * 256;
    }

    public static double TileXToLongitudeRadians(int tileX, int levelOfDetail)
    {
      return PixelXToLongitudeRadians(TileXToPixelX(tileX), levelOfDetail);
    }

    public static double TileYToLatitudeRadians(int tileY, int levelOfDetail)
    {
      return PixelYToLatitudeRadians(TileYToPixelY(tileY), levelOfDetail);
    }

    public static void TileXYToLatLong(int tileX, int tileY, int levelOfDetail, out double latitudeDegrees, out double longitudeDegrees)
    {
      int pixelX, pixelY;
      TileXYToPixelXY(tileX, tileY, out pixelX, out pixelY);
      PixelXYToLatLong(pixelX, pixelY, levelOfDetail, out latitudeDegrees, out longitudeDegrees);
    }

    public static void TileXYToLatLongRadians(int tileX, int tileY, int levelOfDetail, out double latitudeRadians, out double longitudeRadians)
    {
      int pixelX, pixelY;
      TileXYToPixelXY(tileX, tileY, out pixelX, out pixelY);
      PixelXYToLatLongRadians(pixelX, pixelY, levelOfDetail, out latitudeRadians, out longitudeRadians);
    }

    public static Geodetic2d TileXYToGeo(int tileX, int tileY, int levelOfDetail)
    {
      double latitudeRadians, longitudeRadians;
      TileXYToLatLongRadians(tileX, tileY, levelOfDetail, out latitudeRadians, out longitudeRadians);
      return Geodetic2d.FromRadians(latitudeRadians, longitudeRadians);
    }

    public static void TileToPixelXY(TerrainTileIndex idx, out int pixelX, out int pixelY)
    {
      TileXYToPixelXY(idx.Column, idx.Row, out pixelX, out pixelY);
    }

    public static Geodetic2d TileToGeo(TerrainTileIndex idx)
    {
      return TileXYToGeo(idx.Column, idx.Row, idx.Level);
    }

    public static GeoBoundingBox TileXYToBounds(int tileX, int tileY, int levelOfDetail)
    {
      int pixelX, pixelY;
      TileXYToPixelXY(tileX, tileY, out pixelX, out pixelY);

      return new GeoBoundingBox(PixelXYToGeo(pixelX, pixelY, levelOfDetail), PixelXYToGeo(pixelX + 255, pixelY + 255, levelOfDetail));
    }

    public static GeoBoundingBox TileToBounds(TerrainTileIndex idx)
    {
      return TileXYToBounds(idx.Column, idx.Row, idx.Level);
    }

    public static TerrainTileIndex ParentTile(TerrainTileIndex idx)
    {
      var lod = idx.Level - 1;
      var row = idx.Row / 2;
      var col = idx.Column / 2;

      return new TerrainTileIndex(lod, row, col);
    }

    public static bool IsParent(TerrainTileIndex a, TerrainTileIndex b)
    {
      if (a.Level >= b.Level)
        return false;
      else
      {
        var lodDiff = b.Level - a.Level;
        var lod = b.Level - lodDiff;
        var row = b.Row >> lodDiff;
        var col = b.Column >> lodDiff;
        return new TerrainTileIndex(lod, row, col) == a;
      }
    }

    public static void Subtiles(TerrainTileIndex idx, out TerrainTileIndex topLeft, out TerrainTileIndex bottomLeft, out TerrainTileIndex topRight, out TerrainTileIndex bottomRight)
    {
      var lod = idx.Level + 1;
      var topRow = idx.Row * 2;
      var bottomRow = topRow + 1;

      var leftColumn = idx.Column * 2;
      var rightColumn = leftColumn + 1;

      topLeft = new TerrainTileIndex(lod, topRow, leftColumn);
      bottomLeft = new TerrainTileIndex(lod, bottomRow, leftColumn);
      topRight = new TerrainTileIndex(lod, topRow, rightColumn);
      bottomRight = new TerrainTileIndex(lod, bottomRow, rightColumn);
    }

    public static IEnumerable<TerrainTileIndex> Subtiles(TerrainTileIndex idx)
    {
      var lod = idx.Level + 1;
      var topRow = idx.Row * 2;
      var bottomRow = topRow + 1;

      var leftColumn = idx.Column * 2;
      var rightColumn = leftColumn + 1;

      yield return new TerrainTileIndex(lod, topRow, leftColumn);
      yield return new TerrainTileIndex(lod, bottomRow, leftColumn);
      yield return new TerrainTileIndex(lod, topRow, rightColumn);
      yield return new TerrainTileIndex(lod, bottomRow, rightColumn);
    }

    /// <summary>
    /// Converts tile XY coordinates into a QuadKey at a specified level of detail.
    /// </summary>
    /// <param name="tileX">Tile X coordinate.</param>
    /// <param name="tileY">Tile Y coordinate.</param>
    /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
    /// to 23 (highest detail).</param>
    /// <returns>A string containing the QuadKey.</returns>
    public static string TileXYToQuadKey(int tileX, int tileY, int levelOfDetail)
    {
      StringBuilder quadKey = new StringBuilder();
      for (int i = levelOfDetail; i > 0; i--)
      {
        char digit = '0';
        int mask = 1 << (i - 1);
        if ((tileX & mask) != 0)
        {
          digit++;
        }

        if ((tileY & mask) != 0)
        {
          digit++;
          digit++;
        }

        quadKey.Append(digit);
      }

      return quadKey.ToString();
    }

    /// <summary>
    /// Converts a QuadKey into tile XY coordinates.
    /// </summary>
    /// <param name="quadKey">QuadKey of the tile.</param>
    /// <param name="tileX">Output parameter receiving the tile X coordinate.</param>
    /// <param name="tileY">Output parameter receiving the tile Y coordinate.</param>
    /// <param name="levelOfDetail">Output parameter receiving the level of detail.</param>
    public static void QuadKeyToTileXY(string quadKey, out int tileX, out int tileY, out int levelOfDetail)
    {
      tileX = tileY = 0;
      levelOfDetail = quadKey.Length;
      for (int i = levelOfDetail; i > 0; i--)
      {
        int mask = 1 << (i - 1);
        switch (quadKey[levelOfDetail - i])
        {
          case '0':
            break;

          case '1':
            tileX |= mask;
            break;

          case '2':
            tileY |= mask;
            break;

          case '3':
            tileX |= mask;
            tileY |= mask;
            break;

          default:
            throw new ArgumentException("Invalid QuadKey digit sequence.");
        }
      }
    }

    public static string TileToQuadKey(TerrainTileIndex idx)
    {
      return TileXYToQuadKey(idx.Column, idx.Row, idx.Level);
    }

    public static void QuadKeyToTile(string quadKey, out TerrainTileIndex idx)
    {
      int x, y, level;
      QuadKeyToTileXY(quadKey, out x, out y, out level);
      idx = new TerrainTileIndex(level, y, x);
    }

    public static TerrainTileIndex QuadKeyToTile(string quadKey)
    {
      TerrainTileIndex ret;
      QuadKeyToTile(quadKey, out ret);
      return ret;
    }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

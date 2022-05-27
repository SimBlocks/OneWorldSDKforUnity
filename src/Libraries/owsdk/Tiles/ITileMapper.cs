//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.owsdk.Geodetic;

namespace sbio.owsdk.Tiles
{
  public interface ITileMapper
  {
    int TilePixelWidth(int lod);
    int TilePixelHeight(int lod);
    int NumTilesX(int lod);
    int NumTilesY(int lod);

    void GeoToPixelXY(Geodetic2d geo, int lod, out double pixelX, out double pixelY);
    Geodetic2d PixelXYToGeo(int lod, double pixelX, double pixelY);
  }

  public static class ITileMapperExtensions
  {
    public static int MapPixelWidth(this ITileMapper tileMap, int lod)
    {
      return tileMap.TilePixelWidth(lod) * tileMap.NumTilesX(lod);
    }

    public static int MapPixelHeight(this ITileMapper tileMap, int lod)
    {
      return tileMap.TilePixelHeight(lod) * tileMap.NumTilesY(lod);
    }

    public static void GeoToPixelXY(this ITileMapper tileMap, Geodetic2d geo, int lod, out int pixelX, out int pixelY)
    {
      double x, y;
      tileMap.GeoToPixelXY(geo, lod, out x, out y);
      pixelX = (int)(x);
      pixelY = (int)(y);
    }

    public static TerrainTileIndex PixelXYToTile(this ITileMapper tileMap, int lod, double pixelX, double pixelY)
    {
      var row = (int)(pixelY) / tileMap.TilePixelHeight(lod);
      var col = (int)(pixelX) / tileMap.TilePixelWidth(lod);

      return new TerrainTileIndex(lod, row, col);
    }

    public static TerrainTileIndex GeoToTile(this ITileMapper tileMap, int lod, Geodetic2d geo)
    {
      double px, py;
      tileMap.GeoToPixelXY(geo, lod, out px, out py);
      return tileMap.PixelXYToTile(lod, px, py);
    }

    public static Geodetic2d TileToGeo(this ITileMapper tileMap, TerrainTileIndex idx)
    {
      return tileMap.PixelXYToGeo(idx.Level, idx.Column * tileMap.TilePixelWidth(idx.Level), idx.Row * tileMap.TilePixelHeight(idx.Level));
    }

    public static void TileToPixelXY(this ITileMapper tileMap, TerrainTileIndex idx, out int pxX, out int pxY)
    {
      pxX = idx.Column * tileMap.TilePixelWidth(idx.Level);
      pxY = idx.Row * tileMap.TilePixelHeight(idx.Level);
    }

    public static GeoBoundingBox TileToBounds(this ITileMapper tileMap, TerrainTileIndex idx)
    {
      var tileWidth = tileMap.TilePixelWidth(idx.Level);
      var tileHeight = tileMap.TilePixelHeight(idx.Level);

      var northWest = tileMap.PixelXYToGeo(idx.Level, (idx.Column) * tileWidth, (idx.Row) * tileHeight);
      var southEast = tileMap.PixelXYToGeo(idx.Level, (idx.Column + 1) * tileHeight, (idx.Row + 1) * tileHeight);

      return new GeoBoundingBox(northWest, southEast);
    }
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

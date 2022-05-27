//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.owsdk.Geodetic;
using sbio.owsdk.Tiles;

namespace sbio.owsdk.WMS
{
  public sealed class WMSTileMapper : ITileMapper
  {
    public static WMSTileMapper Instance
    {
      get { return sc_Instance; }
    }

    public void GeoToPixelXY(Geodetic2d geo, int lod, out double pixelX, out double pixelY)
    {
      WMSConversions.GeoToPixelXY(geo, lod, out pixelX, out pixelY);
    }

    public int NumTilesX(int lod)
    {
      return (int)WMSConversions.MapTileSize(lod);
    }

    public int NumTilesY(int lod)
    {
      return (int)WMSConversions.MapTileSize(lod);
    }

    public Geodetic2d PixelXYToGeo(int lod, double pixelX, double pixelY)
    {
      return WMSConversions.PixelXYToGeo(pixelX, pixelY, lod);
    }

    public int TilePixelHeight(int lod)
    {
      return 256;
    }

    public int TilePixelWidth(int lod)
    {
      return 256;
    }

    private static readonly WMSTileMapper sc_Instance = new WMSTileMapper();
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

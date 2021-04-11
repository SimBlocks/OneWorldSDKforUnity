//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Threading;
using sbio.Core.Math;
using sbio.owsdk.Geodetic;

namespace sbio.owsdk.Tiles
{
  public static class TileMeshUtil
  {
    public static Vector2f[] CalculateUVs(TerrainTileIndex idx, ITileMapper tileMapper, Vec3LeftHandedGeocentric center, QuaternionLeftHandedGeocentric rot, Vector3f[] vertices, Ellipsoid ellipsoid, CancellationToken tok)
    {
      var verticesLen = vertices.Length;
      var uvs = new Vector2f[verticesLen];

      //Get the top left pixel to calculate UV's 
      int xOriginPx, yOriginPx;
      tileMapper.TileToPixelXY(idx, out xOriginPx, out yOriginPx);

      var tilePixelWidth = tileMapper.TilePixelWidth(idx.Level);
      var tilePixelHeight = tileMapper.TilePixelHeight(idx.Level);

      var idlTile = idx.Column == 0 || idx.Column == (tileMapper.NumTilesX(idx.Level) - 1);
      if (!idlTile)
      {
        for (var i = 0; i < verticesLen; ++i)
        {
          var coordGeo = ellipsoid.ToGeodetic2d(rot.Multiply(new Vec3LeftHandedGeocentric(vertices[i])) + center);
          int samplePixelX, samplePixelY;
          tileMapper.GeoToPixelXY(coordGeo, idx.Level, out samplePixelX, out samplePixelY);
          var uvX = ClampTexelCoord(samplePixelX - xOriginPx, tilePixelWidth);
          var uvY = 1 - ClampTexelCoord(samplePixelY - yOriginPx, tilePixelHeight);
          uvs[i] = new Vector2f(uvX, uvY);
        }
      }
      else if (idx.Column == 0)
      {
        //Is touching IDL on left
        var mapWidthOverTwo = tileMapper.MapPixelWidth(idx.Level) / 2;
        for (var i = 0; i < verticesLen; ++i)
        {
          var coordGeo = ellipsoid.ToGeodetic2d(rot.Multiply(new Vec3LeftHandedGeocentric(vertices[i])) + center);
          int samplePixelX, samplePixelY;
          tileMapper.GeoToPixelXY(coordGeo, idx.Level, out samplePixelX, out samplePixelY);

          float uvX;
          if (samplePixelX > mapWidthOverTwo)
          {
            uvX = 0;
          }
          else
          {
            uvX = ClampTexelCoord(samplePixelX - xOriginPx, tilePixelWidth);
          }

          var uvY = 1 - ClampTexelCoord(samplePixelY - yOriginPx, tilePixelHeight);
          uvs[i] = new Vector2f(uvX, uvY);
        }
      }
      else
      {
        //Is touching IDL on right
        var mapSizeOverTwo = tileMapper.MapPixelWidth(idx.Level) / 2;
        for (var i = 0; i < verticesLen; ++i)
        {
          var coordGeo = ellipsoid.ToGeodetic2d(rot.Multiply(new Vec3LeftHandedGeocentric(vertices[i])) + center);
          int samplePixelX, samplePixelY;
          tileMapper.GeoToPixelXY(coordGeo, idx.Level, out samplePixelX, out samplePixelY);

          float uvX;
          if (samplePixelX < mapSizeOverTwo)
          {
            uvX = 1;
          }
          else
          {
            uvX = ClampTexelCoord(samplePixelX - xOriginPx, tilePixelWidth);
          }

          var uvY = 1 - ClampTexelCoord(samplePixelY - yOriginPx, tilePixelHeight);
          uvs[i] = new Vector2f(uvX, uvY);
        }
      }

      return uvs;
    }

    private static float ClampTexelCoord(int pixel, int dimSize)
    {
      return (float)(Math.Min(Math.Max(pixel, 0), (double)dimSize) / (double)dimSize);
    }

    private static float ClampTexelCoord(double pixel, int dimSize)
    {
      return (float)(Math.Min(Math.Max(pixel, 0), (double)dimSize) / (double)dimSize);
    }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

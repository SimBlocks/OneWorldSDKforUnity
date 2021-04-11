//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using sbio.Core.Math;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Tiles;
using UnityEngine;

namespace sbio.owsdk.Unity
{
  public static class UnityUtil
  {
    public static Vector2 Rotate(this Vector2 v, float degrees)
    {
      var radians = degrees * Mathf.Deg2Rad;
      var sin = Mathf.Sin(radians);
      var cos = Mathf.Cos(radians);
      var tx = v.x;
      var ty = v.y;

      return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
    }

    public static Vector2d RotateRadians(this Vector2d v, double radians)
    {
      var sin = Math.Sin(radians);
      var cos = Math.Cos(radians);
      var tx = v.x;
      var ty = v.y;

      return new Vector2d(cos * tx - sin * ty, sin * tx + cos * ty);
    }

    public static IEnumerable<TerrainTileIndex> Subtiles(this TerrainTileIndex idx)
    {
      int lod = idx.Level + 1;
      int leftRow = idx.Row * 2;
      int rightRow = leftRow + 1;

      int leftColumn = idx.Column * 2;
      int rightColumn = leftColumn + 1;

      for (var j = leftRow; j <= rightRow; ++j)
      {
        for (var i = leftColumn; i <= rightColumn; ++i)
        {
          yield return new TerrainTileIndex(lod, j, i);
        }
      }
    }

    public static void SubtilesInto(this TerrainTileIndex idx, TerrainTileIndex[] dst)
    {
      int lod = idx.Level + 1;
      int top = idx.Row * 2;
      int bottom = top + 1;

      int left = idx.Column * 2;
      int right = left + 1;

      dst[0] = new TerrainTileIndex(lod, top, left);
      dst[1] = new TerrainTileIndex(lod, bottom, left);
      dst[2] = new TerrainTileIndex(lod, top, right);
      dst[3] = new TerrainTileIndex(lod, bottom, right);
    }

    public static void SubtilesInto(this TerrainTileIndex idx, TerrainTileIndex?[] dst)
    {
      int lod = idx.Level + 1;
      int top = idx.Row * 2;
      int bottom = top + 1;

      int left = idx.Column * 2;
      int right = left + 1;

      dst[0] = new TerrainTileIndex(lod, top, left);
      dst[1] = new TerrainTileIndex(lod, bottom, left);
      dst[2] = new TerrainTileIndex(lod, top, right);
      dst[3] = new TerrainTileIndex(lod, bottom, right);
    }

    /// <summary>
    /// Constructs Geodetic3d points from a list of tripples in the format {longDegrees, latDegrees, heightMeters}
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
    public static IEnumerable<Geodetic3d> GeopointsFromTriples(IEnumerable<double> vertices)
    {
      int idx = 0;
      double[] curr = new double[3];
      foreach (var coord in vertices)
      {
        curr[idx++] = coord;
        if (idx == 3)
        {
          yield return Geodetic3d.FromDegrees(curr[1], curr[0], curr[2]);
          idx = 0;
        }
      }
    }

    /// <summary>
    /// Constructs Vector2 points from a list of pairs in the format {x, y}
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
    public static IEnumerable<Vector2> VectorsFromDoubles(IEnumerable<double> vertices)
    {
      int idx = 0;
      Vector2 curr = new Vector2();
      foreach (var coord in vertices)
      {
        curr[idx++] = (float)coord;
        if (idx == 2)
        {
          yield return curr;
          idx = 0;
        }
      }
    }

    /// <summary>
    /// Constructs a Color32 from an rgba packed color
    /// </summary>
    /// <param name="rgba"></param>
    /// <returns></returns>
    public static Color32 ColorFromRGBA(int rgba)
    {
      return new Color32(
        (byte)((rgba >> 24) & 0xFF),
        (byte)((rgba >> 16) & 0xFF),
        (byte)((rgba >> 8) & 0xFF),
        (byte)((rgba >> 0) & 0xFF));
    }


    public static Color32 ColorBlerp(Color32 a, Color32 b, Color32 c, Color32 d)
    {
      //Blerp each component
      var red = NumUtil.ColorBlerp(a.r, b.r, b.r, d.r, .5, .5);
      var green = NumUtil.ColorBlerp(a.g, b.g, b.g, d.g, .5, .5);
      var blue = NumUtil.ColorBlerp(a.b, b.b, b.b, d.b, .5, .5);
      var alpha = NumUtil.ColorBlerp(a.a, b.a, b.a, d.a, .5, .5);

      //Compose back together
      return new Color32(red, green, blue, alpha);
    }

    public static Bounds BoundsFromPoints(params Vector3[] points)
    {
      if (points.Length == 0)
      {
        return new Bounds(new Vector3(float.NaN, float.NaN, float.NaN), new Vector3(float.NaN, float.NaN, float.NaN));
      }
      else
      {
        var min = points[0];
        var max = points[0];
        for (var i = 1; i < points.Length; ++i)
        {
          var p = points[i];
          min = Vector3.Min(min, p);
          max = Vector3.Max(max, p);
        }

        var maxMinusMin = max - min;
        return new Bounds((maxMinusMin * 0.5f) + min, maxMinusMin);
      }
    }

    public static Bounds BoundsFromPoints(IEnumerable<Vector3> points)
    {
      using (var enumerator = points.GetEnumerator())
      {
        if (!enumerator.MoveNext())
        {
          return new Bounds(new Vector3(float.NaN, float.NaN, float.NaN), new Vector3(float.NaN, float.NaN, float.NaN));
        }
        else
        {
          var min = enumerator.Current;
          var max = enumerator.Current;
          while (enumerator.MoveNext())
          {
            var p = enumerator.Current;
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
          }

          var maxMinusMin = max - min;
          return new Bounds((maxMinusMin * 0.5f) + min, maxMinusMin);
        }
      }
    }
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

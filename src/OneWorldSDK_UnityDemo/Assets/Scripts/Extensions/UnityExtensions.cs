//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.Core.Math;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Services;
using UnityEngine;
using UnityMesh = UnityEngine.Mesh;

namespace sbio.owsdk.Unity.Extensions
{
  public static class UnityExtensions
  {
    public static Vector3 ToVector3(this Vec3LeftHandedGeocentric v)
    {
      return new Vector3((float)v.X, (float)v.Y, (float)v.Z);
    }

    public static Vector3 ToVector3(this Vector3d v3d)
    {
      return new Vector3((float)v3d.X, (float)v3d.Y, (float)v3d.Z);
    }

    public static Vec3LeftHandedGeocentric ToVec3LeftHandedGeocentric(this Vector3 v3)
    {
      return new Vec3LeftHandedGeocentric(v3.x, v3.y, v3.z);
    }

    public static UnityEngine.Quaternion ToUnityQuaternion(QuaternionLeftHandedGeocentric q)
    {
      return new UnityEngine.Quaternion((float)q.X, (float)q.Y, (float)q.Z, (float)q.W);
    }

    public static Vector3 ToVector3(this Vector3f v3f)
    {
      return new Vector3(v3f.X, v3f.Y, v3f.Z);
    }

    public static Vector3f ToVector3f(this Vector3 v3)
    {
      return new Vector3f(v3.x, v3.y, v3.z);
    }

    public static Vector2 ToVector2(this Vector2d v2d)
    {
      return new Vector2((float)v2d.X, (float)v2d.Y);
    }

    public static Vector2d ToVector2d(this Vector2 v2)
    {
      return new Vector2d(v2.x, v2.y);
    }

    public static Vector2 ToVector2(this Vector2f v2d)
    {
      return new Vector2(v2d.X, v2d.Y);
    }

    public static Vector2f ToVector2f(this Vector2 v2)
    {
      return new Vector2f(v2.x, v2.y);
    }

    /*public static Quaternion ToQuat(this Quaternion qd)
    {
      return new Quaternion((float)qd.X, (float)qd.Y, (float)qd.Z, (float)qd.W);
    }*/

    public static Quaternion ToQuatd(this Quaternion q)
    {
      return new Quaternion(q.x, q.y, q.z, q.w);
    }

    public static Quaternion ToQuat(this Quaternion qd)
    {
      return new Quaternion(qd.x, qd.y, qd.z, qd.w);
    }

    public static Quaternion ToQuatf(this Quaternion q)
    {
      return new Quaternion(q.x, q.y, q.z, q.w);
    }

    /// <summary>
    /// Calculate the 'NED' rotation at the specified geopos
    /// This is a rotation with 'Up' being the normal and 'Forward' being north (thus 'East' is 'Right')
    /// </summary>
    /// <param name="ellipsoid">The ellipsoid to use for reference</param>
    /// <param name="geoPos">The geo position to calculate rotation for</param>
    /// <returns></returns>
    public static Quaternion NEDRotation(this Ellipsoid ellipsoid, Geodetic2d geoPos)
    {
      Vec3LeftHandedGeocentric normal;
      var north1 = ellipsoid.NorthDirection(geoPos, out normal);
      Quaternion q = Quaternion.LookRotation(north1.ToVector3(), normal.ToVector3());
      return q;
    }

    public static Geodetic3d RTOToGeo(this IWorldContext context, Vector3 rtoPos)
    {
      return context.Ellipsoid.ToGeodetic3d(rtoPos.ToVec3LeftHandedGeocentric() + context.WorldOrigin);
    }

    public static UnityMesh GeoMesh(this IWorldContext context, Geodetic2d p1, Geodetic2d p2, int granularity, out Vec3LeftHandedGeocentric center, out Quaternion rotation)
    {
      var latDelta = NumUtil.AngleDiffRadians(p1.LatitudeRadians, p2.LatitudeRadians) / (granularity - 1);
      var lonDelta = NumUtil.AngleDiffRadians(p1.LongitudeRadians, p2.LongitudeRadians) / (granularity - 1);

      var flipped = latDelta < 0 ^ lonDelta < 0;

      var geoSamples = new ElevationPointSample[granularity * granularity];

      for (int j = 0; j < granularity; ++j)
      {
        for (int i = 0; i < granularity; ++i)
        {
          var lat = p1.LatitudeRadians + j * latDelta;
          var lon = p1.LongitudeRadians + i * lonDelta;
          geoSamples[j * granularity + i] = new ElevationPointSample(Geodetic2d.FromRadians(lat, lon));
        }
      }

      context.ElevationProvider.QueryPointSamplesInto(geoSamples);

      Geodetic2d centerGeo;
      {
        var centerLat = Math.Abs(p1.LatitudeRadians - p2.LatitudeRadians);
        var centerLon = Math.Abs(p1.LongitudeRadians - p2.LongitudeRadians);
        centerGeo = Geodetic2d.FromRadians(centerLat, centerLon);
      }

      center = context.Ellipsoid.ToVec3LeftHandedGeocentric(centerGeo);
      rotation = context.Ellipsoid.NEDRotation(centerGeo);
      var vertices = new Vector3[geoSamples.Length];

      var rotInv = Quaternion.Inverse(rotation);
      for (var i = 0; i < geoSamples.Length; ++i)
      {
        var sample = geoSamples[i];
        var p3d = context.Ellipsoid.ToVec3LeftHandedGeocentric(sample.Position, sample.Elevation);
        vertices[i] = rotInv * (p3d - center).ToVector3();
      }

      int t0, t2;
      int t3, t5;

      if (flipped)
      {
        t0 = 0;
        t2 = 2;
        t3 = 3;
        t5 = 5;
      }
      else
      {
        t0 = 2;
        t2 = 0;
        t3 = 5;
        t5 = 3;
      }

      var triangles = new int[(granularity - 1) * (granularity - 1) * 6];
      //Calculate triangles
      for (int j = 0; j < granularity - 1; ++j)
      {
        for (int i = 0; i < granularity - 1; ++i)
        {
          var topLeftVert = (j * granularity + i);
          var topRightVert = (j * granularity + (i + 1));
          var bottomLeftVert = ((j + 1) * granularity + i);
          var bottomRightVert = ((j + 1) * granularity + (i + 1));

          var triangleIndex = (j * (granularity - 1) + i) * 6;

          triangles[triangleIndex + t0] = topLeftVert;
          triangles[triangleIndex + 1] = topRightVert;
          triangles[triangleIndex + t2] = bottomRightVert;

          triangles[triangleIndex + t3] = topLeftVert;
          triangles[triangleIndex + 4] = bottomRightVert;
          triangles[triangleIndex + t5] = bottomLeftVert;
        }
      }

      var ret = new UnityMesh()
      {
        vertices = vertices,
        triangles = triangles
      };

      return ret;
    }
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

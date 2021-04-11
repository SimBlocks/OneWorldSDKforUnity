//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Runtime.InteropServices;

namespace sbio.Core.Math
{
  public static class MathUtilities
  {
    public static bool IsEqual(float a, float b, float epsilon = 0.0001f)
    {
      return System.Math.Abs(a - b) < epsilon;
    }

    public static bool IsEqual(double a, double b, double epsilon = 0.0001)
    {
      return System.Math.Abs(a - b) < epsilon;
    }

    public static Vec3LeftHandedGeocentric GeocentricToLeftHandedGeocentric(Vec3Geocentric v)
    {
      return new Vec3LeftHandedGeocentric(v.X, v.Z, v.Y);
    }

    public static Vec3Geocentric LeftHandedGeocentricToGeocentric(Vec3LeftHandedGeocentric v)
    {
      return new Vec3Geocentric(v.X, v.Z, v.Y);    
    }

    /*
        public static void GetNorthEastDown(double lat, double lon, ref Vec3Geocentric north, ref Vec3Geocentric east, ref Vec3Geocentric down)
        {
          double[] m = new double[9];
          GetNorthEastDown(lat, lon, ref m[0], ref m[1], ref m[2], ref m[3], ref m[4], ref m[5], ref m[6], ref m[7], ref m[8]);
          north.Set(m[0], m[1], m[2]);
          east.Set(m[3], m[4], m[5]);
          down.Set(m[6], m[7], m[8]);
        }
        [DllImport("sbioMathNativePlugin")]
        public static extern void GetNorthEastDown(double lat, double lon,
          ref double northX, ref double northY, ref double northZ,
          ref double eastX, ref double eastY, ref double eastZ,
          ref double downX, ref double downY, ref double downZ);

        public static void ConvertGeocentricToGeodeticCoordinates(Vec3Geocentric position, ref double lat, ref double lon, ref double altitude)
        {
          ConvertGeocentricToGeodeticCoordinates(position.X, position.Y, position.Z, ref lat, ref lon, ref altitude);
        }

        [DllImport("sbioMathNativePlugin")]
        public static extern void ConvertGeocentricToGeodeticCoordinates(double x, double y, double z,
          ref double latitude, ref double longitude, ref double altitude);
    */
    public static QuaternionLeftHandedGeocentric CreateGeocentricRotation(Vec3LeftHandedGeocentric north, Vec3LeftHandedGeocentric up)
    {
      // Modified version of this:
      // https://answers.unity.com/questions/467614/what-is-the-source-code-of-quaternionlookrotation.html
      //north.Normalize();

      Vector3d vector = north.ToVector3d();
      Vector3d vector2 = Vector3d.Cross(up.ToVector3d(), vector).Normalize();
      Vector3d vector3 = Vector3d.Cross(vector, vector2);
      var m00 = vector2.x;
      var m01 = vector2.y;
      var m02 = vector2.z;
      var m10 = vector3.x;
      var m11 = vector3.y;
      var m12 = vector3.z;
      var m20 = vector.x;
      var m21 = vector.y;
      var m22 = vector.z;


      float num8 = (float)((m00 + m11) + m22);
      var quaternion = new QuaternionLeftHandedGeocentric();
      if (num8 > 0f)
      {
        var num = (float)System.Math.Sqrt(num8 + 1f);
        quaternion.W = num * 0.5f;
        num = 0.5f / num;
        quaternion.X = (float)((m12 - m21) * num);
        quaternion.Y = (float)((m20 - m02) * num);
        quaternion.Z = (float)((m01 - m10) * num);
        return quaternion;
      }

      if ((m00 >= m11) && (m00 >= m22))
      {
        var num7 = (float)System.Math.Sqrt(((1f + m00) - m11) - m22);
        var num4 = 0.5f / num7;
        quaternion.X = 0.5f * num7;
        quaternion.Y = (float)((m01 + m10) * num4);
        quaternion.Z = (float)((m02 + m20) * num4);
        quaternion.W = (float)((m12 - m21) * num4);
        return quaternion;
      }

      if (m11 > m22)
      {
        var num6 = (float)System.Math.Sqrt(((1f + m11) - m00) - m22);
        var num3 = 0.5f / num6;
        quaternion.X = (float)((m10 + m01) * num3);
        quaternion.Y = 0.5f * num6;
        quaternion.Z = (float)((m21 + m12) * num3);
        quaternion.W = (float)((m20 - m02) * num3);
        return quaternion;
      }

      var num5 = (float)System.Math.Sqrt(((1f + m22) - m00) - m11);
      var num2 = 0.5f / num5;
      quaternion.X = (float)((m20 + m02) * num2);
      quaternion.Y = (float)((m21 + m12) * num2);
      quaternion.Z = 0.5f * num5;
      quaternion.W = (float)((m01 - m10) * num2);
      return quaternion;
    }
  }
}



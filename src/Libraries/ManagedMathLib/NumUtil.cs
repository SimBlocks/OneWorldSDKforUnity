//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Globalization;

namespace sbio.Core.Math
{
  using Math = System.Math;

  public static class NumUtil
  {
    public static bool EqualsEpsilon(this double a, double b, double epsilon = double.Epsilon)
    {
      return Math.Abs(a - b) <= epsilon;
    }

    public static bool EqualsEpsilon(this float a, float b, float epsilon = float.Epsilon)
    {
      return Math.Abs(a - b) <= epsilon;
    }

    public static double RadiansToDegrees(double rads)
    {
      return rads * c_DegreesPerRadd;
    }

    public static double DegreesToRadians(double degrees)
    {
      return degrees * c_RadsPerDegreed;
    }

    public static float RadiansToDegrees(float rads)
    {
      return rads * c_DegreesPerRadf;
    }

    public static float DegreesToRadians(float degrees)
    {
      return degrees * c_RadsPerDegreef;
    }

    public static double Wrap(double value, double max, double min = 0)
    {
      value -= min;
      max -= min;
      if (MathUtilities.IsEqual(max, 0))
        return min;

      value = value % max;
      value += min;
      while (value < min)
      {
        value += max;
      }

      return value;
    }

    /// <summary>
    /// Wraps a latitude/longitude pair to fall in the range
    /// [-π/2, π/2] for latitude
    /// [-π, π) for longitude
    /// </summary>
    /// <param name="latitudeRadians"></param>
    /// <param name="longitudeRadians"></param>
    public static void WrapLatLongRadians(ref double latitudeRadians, ref double longitudeRadians)
    {
      if (latitudeRadians < -c_PiOverTwod)
      {
        if (latitudeRadians < -(2 * Math.PI))
          latitudeRadians = latitudeRadians % (2 * Math.PI);

        if (latitudeRadians < -Math.PI)
        {
          latitudeRadians = latitudeRadians % Math.PI;
          longitudeRadians += (2 * Math.PI);
        }
        else
        {
          latitudeRadians = -Math.PI - latitudeRadians;
          longitudeRadians += Math.PI;
        }
      }

      if (latitudeRadians > c_PiOverTwod)
      {
        if (latitudeRadians > (2 * Math.PI))
          latitudeRadians = latitudeRadians % (2 * Math.PI);

        if (latitudeRadians > Math.PI)
        {
          latitudeRadians = latitudeRadians % Math.PI;
          longitudeRadians += (2 * Math.PI);
        }
        else
        {
          latitudeRadians = Math.PI - latitudeRadians;
          longitudeRadians += Math.PI;
        }
      }

      if (longitudeRadians < -Math.PI)
      {
        if (longitudeRadians < -(2 * Math.PI))
          longitudeRadians = longitudeRadians % (2 * Math.PI);

        if (longitudeRadians < -Math.PI)
          longitudeRadians += (2 * Math.PI);
      }


      if (longitudeRadians >= Math.PI)
      {
        if (longitudeRadians > (2 * Math.PI))
          longitudeRadians = longitudeRadians % (2 * Math.PI);

        if (longitudeRadians >= Math.PI)
          longitudeRadians -= (2 * Math.PI);
       }
    }

    /// <summary>
    /// Wraps a latitude/longitude pair to fall in the range
    /// [-90, 90] for latitude
    /// [-180, 180) for longitude
    /// </summary>
    /// <param name="latitudeDegrees"></param>
    /// <param name="longitudeDegrees"></param>
    public static void WrapLatLongDegrees(ref double latitudeDegrees, ref double longitudeDegrees)
    {

      if (latitudeDegrees < -90)
      {
        if (latitudeDegrees < -360)
          latitudeDegrees = latitudeDegrees % 360;

        if (latitudeDegrees < -180)
        {
          latitudeDegrees = latitudeDegrees % 180;
          longitudeDegrees += 360;
        }
        else
        {
          latitudeDegrees = -180 - latitudeDegrees;
          longitudeDegrees += 180;
        }
      }


      if (latitudeDegrees > 90)
      {
        if (latitudeDegrees > 360)
          latitudeDegrees = latitudeDegrees % 360;

        if (latitudeDegrees > 180)
        {
          latitudeDegrees = latitudeDegrees % 180;
          longitudeDegrees += 360;
        }
        else
        {
          latitudeDegrees = 180 - latitudeDegrees;
          longitudeDegrees += 180;
        }
      }

      if (longitudeDegrees < -180)
      {
        if(longitudeDegrees < -360)
          longitudeDegrees = longitudeDegrees % 360;

        if (longitudeDegrees < -180)
          longitudeDegrees += 360;
      }


      if (longitudeDegrees >= 180)
      {
        if (longitudeDegrees > 360)
          longitudeDegrees = longitudeDegrees % 360;

        if (longitudeDegrees >= 180)
          longitudeDegrees -= 360;
      }
    }

    public static void DegreesToDMS(double degrees, out int d, out int m, out int s)
    {
      double ds;
      DegreesToDMS(degrees, out d, out m, out ds);
      s = (int)ds;
    }

    public static void DegreesToDMS(double degrees, out int d, out int m, out double s)
    {
      //DD: dd.ff
      //DMS: dd mm ss
      //
      //dd = dd
      //mm.gg = 60 * ff
      //ss = 60 * gg

      var dd = (int)degrees;
      var ff = Math.Abs(degrees - Math.Truncate(degrees));

      double gg;

      {
        var tmp = 60 * ff;
        gg = tmp - Math.Truncate(tmp);
      }

      d = dd;
      m = (int)(60 * ff);
      s = (60 * gg);
    }

    public static double DMSToDegrees(int d, int m, int s)
    {
      //DD: dd.ff
      //DMS: dd mm ss
      //
      //dd.ff = dd + mm / 60 + ss / 3600

      return d + (m / 60.0) + (s / 3600.0);
    }

    public static string LatitudeDegreesToDmsString(double latitudeDegrees)
    {
      return LatitudeDegreesToDmsString(CultureInfo.CurrentCulture, latitudeDegrees);
    }

    public static string LatitudeDegreesToDmsString(double latitudeDegrees, string numberFormat)
    {
      return LatitudeDegreesToDmsString(CultureInfo.CurrentCulture, latitudeDegrees, numberFormat);
    }

    public static string LatitudeDegreesToDmsString(CultureInfo culture, double latitudeDegrees)
    {
      int d, m, s;
      DegreesToDMS(latitudeDegrees, out d, out m, out s);
      return string.Format(culture, "{0}° {1}′ {2}″ {3}", Math.Abs(d), m, s, latitudeDegrees < 0 ? 'S' : 'N');
    }

    public static string LatitudeDegreesToDmsString(CultureInfo culture, double latitudeDegrees, string numberFormat)
    {
      int d, m, s;
      DegreesToDMS(latitudeDegrees, out d, out m, out s);
      return string.Format(culture, "{0}° {1}′ {2}″ {3}", Math.Abs(d).ToString(numberFormat), m.ToString(numberFormat), s.ToString(numberFormat), latitudeDegrees < 0 ? 'S' : 'N');
    }

    public static string LongitudeDegreesToDmsString(double longitudeDegrees)
    {
      return LongitudeDegreesToDmsString(CultureInfo.CurrentCulture, longitudeDegrees);
    }

    public static string LongitudeDegreesToDmsString(double longitudeDegrees, string numberFormat)
    {
      return LongitudeDegreesToDmsString(CultureInfo.CurrentCulture, longitudeDegrees, numberFormat);
    }

    public static string LongitudeDegreesToDmsString(CultureInfo culture, double longitudeDegrees)
    {
      int d, m, s;
      DegreesToDMS(longitudeDegrees, out d, out m, out s);
      return string.Format(culture, "{0}° {1}′ {2}″ {3}", Math.Abs(d), m, s, longitudeDegrees < 0 ? 'W' : 'E');
    }

    public static string LongitudeDegreesToDmsString(CultureInfo culture, double longitudeDegrees, string numberFormat)
    {
      int d, m, s;
      DegreesToDMS(longitudeDegrees, out d, out m, out s);
      return string.Format(culture, "{0}° {1}′ {2}″ {3}", Math.Abs(d).ToString(numberFormat), m.ToString(numberFormat), s.ToString(numberFormat), longitudeDegrees < 0 ? 'W' : 'E');
    }

    public static double AngleDiffRadians(double a, double b)
    {
      var d = b - a;
      return Math.Atan2(Math.Sin(d), Math.Cos(d));
    }

    public static double AngleDiffDegrees(double a, double b)
    {
      var d = DegreesToRadians(b - a);
      return RadiansToDegrees(Math.Atan2(Math.Sin(d), Math.Cos(d)));
    }

    #region Interpolate

    public static float Interpolate(float row, float col, float[] values, int height, int width)
    {
      return Interpolate(row, col, new ArraySegment<float>(values), height, width);
    }

    public static float Interpolate(float row, float col, ArraySegment<float> values, int height, int width)
    {
      int rowTruncate, colTruncate;
      double rowFraction, colFraction;
      ClampAndTruncate(row, height, out rowTruncate, out rowFraction);
      ClampAndTruncate(col, width, out colTruncate, out colFraction);

      var offset = values.Offset;
      var ary = values.Array;

      var c00 = ary[rowTruncate * height + colTruncate + offset];
      var c01 = ary[rowTruncate * height + (colTruncate + 1) + offset];
      var c10 = ary[(rowTruncate + 1) * height + colTruncate + offset];
      var c11 = ary[(rowTruncate + 1) * height + (colTruncate + 1) + offset];
      return Blerp(c00, c01, c10, c11, rowFraction, colFraction);
    }

    public static float Interpolate(double row, double col, float[] values, int height, int width)
    {
      return Interpolate(row, col, new ArraySegment<float>(values), height, width);
    }

    public static float Interpolate(double row, double col, ArraySegment<float> values, int height, int width)
    {
      int rowTruncate, colTruncate;
      double rowFraction, colFraction;
      ClampAndTruncate(row, height, out rowTruncate, out rowFraction);
      ClampAndTruncate(col, width, out colTruncate, out colFraction);

      var offset = values.Offset;
      var ary = values.Array;
      var c00 = ary[rowTruncate * height + colTruncate + offset];
      var c01 = ary[rowTruncate * height + (colTruncate + 1) + offset];
      var c10 = ary[(rowTruncate + 1) * height + colTruncate + offset];
      var c11 = ary[(rowTruncate + 1) * height + (colTruncate + 1) + offset];
      return Blerp(c00, c01, c10, c11, rowFraction, colFraction);
    }

    public static double Interpolate(double row, double col, double[] values, int height, int width)
    {
      return Interpolate(row, col, new ArraySegment<double>(values), height, width);
    }

    public static double Interpolate(double row, double col, ArraySegment<double> values, int height, int width)
    {
      int rowTruncate, colTruncate;
      double rowFraction, colFraction;
      ClampAndTruncate(row, height, out rowTruncate, out rowFraction);
      ClampAndTruncate(col, width, out colTruncate, out colFraction);

      var offset = values.Offset;
      var ary = values.Array;
      var c00 = ary[rowTruncate * height + colTruncate + offset];
      var c01 = ary[rowTruncate * height + (colTruncate + 1) + offset];
      var c10 = ary[(rowTruncate + 1) * height + colTruncate + offset];
      var c11 = ary[(rowTruncate + 1) * height + (colTruncate + 1) + offset];
      return Blerp(c00, c01, c10, c11, rowFraction, colFraction);
    }

    #region Greyscale Interpolation

    /// <summary>
    /// Interpolates from an greyscale pixel array to a final color.
    /// </summary>
    /// <returns>8-bit greyscale interpolated value</returns>
    public static byte InterpolateColor(double row, double col, byte[] values, int height, int width)
    {
      return InterpolateColor(row, col, new ArraySegment<byte>(values), height, width);
    }

    /// <summary>
    /// Interpolates from an greyscale pixel array to a final color.
    /// </summary>
    /// <returns>8-bit greyscale interpolated value</returns>
    public static byte InterpolateColor(double row, double col, ArraySegment<byte> values, int height, int width)
    {
      int rowTruncate, colTruncate;
      double rowFraction, colFraction;
      ClampAndTruncate(row, height, out rowTruncate, out rowFraction);
      ClampAndTruncate(col, width, out colTruncate, out colFraction);

      var offset = values.Offset;
      var ary = values.Array;

      var c00 = ary[rowTruncate * height + colTruncate + offset];
      var c01 = ary[rowTruncate * height + colTruncate + 1 + offset];
      var c10 = ary[(rowTruncate + 1) * height + colTruncate + offset];
      var c11 = ary[(rowTruncate + 1) * height + colTruncate + 1 + offset];

      //Blerp each component
      return ColorBlerp(c00, c01, c10, c11, rowFraction, colFraction);
    }

    /// <summary>
    /// Interpolates from an greyscale pixel array to a final color.
    /// </summary>
    /// <returns>16-bit greyscale interpolated value</returns>
    public static ushort InterpolateColor(double row, double col, ushort[] values, int height, int width)
    {
      return InterpolateColor(row, col, new ArraySegment<ushort>(values), height, width);
    }

    /// <summary>
    /// Interpolates from an greyscale pixel array to a final color.
    /// </summary>
    /// <returns>16-bit greyscale interpolated value</returns>
    public static ushort InterpolateColor(double row, double col, ArraySegment<ushort> values, int height, int width)
    {
      int rowTruncate, colTruncate;
      double rowFraction, colFraction;
      ClampAndTruncate(row, height, out rowTruncate, out rowFraction);
      ClampAndTruncate(col, width, out colTruncate, out colFraction);

      var offset = values.Offset;
      var ary = values.Array;

      var c00 = ary[rowTruncate * height + colTruncate + offset];
      var c01 = ary[rowTruncate * height + colTruncate + 1 + offset];
      var c10 = ary[(rowTruncate + 1) * height + colTruncate + offset];
      var c11 = ary[(rowTruncate + 1) * height + colTruncate + 1 + offset];

      //Blerp each component
      return ColorBlerp(c00, c01, c10, c11, rowFraction, colFraction);
    }

    #endregion

    #region Color Interpolation

    /// <summary>
    /// Interpolates from a pixel array to a final argb color using a 3 or 4 channel byte rgba byte array
    /// Each row in rgbas is a channel (color)
    /// 0 = r
    /// 1 = g
    /// 2 = b
    /// 3 = a (if used)
    /// 
    /// The alpha channel is only used if `alpha' is true
    /// </summary>
    /// <returns>32-bit argb interpolated color value</returns>
    public static uint InterpolateColor(double row, double col, byte[][] rgbas, int height, int width, bool alpha = false)
    {
      int rowTruncate, colTruncate;
      double rowFraction, colFraction;
      ClampAndTruncate(row, height, out rowTruncate, out rowFraction);
      ClampAndTruncate(col, width, out colTruncate, out colFraction);

      //blerp each component
      byte r;
      {
        var c00 = rgbas[0][rowTruncate * height + colTruncate];
        var c01 = rgbas[0][rowTruncate * height + colTruncate + 1];
        var c10 = rgbas[0][(rowTruncate + 1) * height + colTruncate];
        var c11 = rgbas[0][(rowTruncate + 1) * height + colTruncate + 1];
        r = ColorBlerp(c00, c01, c10, c11, rowFraction, colFraction);
      }

      byte g;
      {
        var c00 = rgbas[1][rowTruncate * height + colTruncate];
        var c01 = rgbas[1][rowTruncate * height + colTruncate + 1];
        var c10 = rgbas[1][(rowTruncate + 1) * height + colTruncate];
        var c11 = rgbas[1][(rowTruncate + 1) * height + colTruncate + 1];
        g = ColorBlerp(c00, c01, c10, c11, rowFraction, colFraction);
      }

      byte b;
      {
        var c00 = rgbas[2][rowTruncate * height + colTruncate];
        var c01 = rgbas[2][rowTruncate * height + colTruncate + 1];
        var c10 = rgbas[2][(rowTruncate + 1) * height + colTruncate];
        var c11 = rgbas[2][(rowTruncate + 1) * height + colTruncate + 1];
        b = ColorBlerp(c00, c01, c10, c11, rowFraction, colFraction);
      }

      byte a;
      if (alpha)
      {
        var c00 = rgbas[3][rowTruncate * height + colTruncate];
        var c01 = rgbas[3][rowTruncate * height + colTruncate + 1];
        var c10 = rgbas[3][(rowTruncate + 1) * height + colTruncate];
        var c11 = rgbas[3][(rowTruncate + 1) * height + colTruncate + 1];
        a = ColorBlerp(c00, c01, c10, c11, rowFraction, colFraction);
      }
      else
      {
        a = 0xFF;
      }

      //Compose back together
      return (uint)((a << 24) | (r << 16) | (g << 8) | b);
    }

    /// <summary>
    /// Interpolates from an argb pixel array to a final argb color.
    /// The alpha channel is only used if `alpha' is true
    /// </summary>
    /// <returns>32-bit argb interpolated color value</returns>
    public static int InterpolateColor(double row, double col, int[] values, int height, int width, bool alpha = false)
    {
      return InterpolateColor(row, col, new ArraySegment<int>(values), height, width, alpha);
    }

    /// <summary>
    /// Interpolates from an argb pixel array to a final argb color.
    /// The alpha channel is only used if `alpha' is true
    /// </summary>
    /// <returns>32-bit argb interpolated color value</returns>
    public static int InterpolateColor(double row, double col, ArraySegment<int> values, int height, int width, bool alpha = false)
    {
      int rowTruncate, colTruncate;
      double rowFraction, colFraction;
      ClampAndTruncate(row, height, out rowTruncate, out rowFraction);
      ClampAndTruncate(col, width, out colTruncate, out colFraction);

      var offset = values.Offset;
      var ary = values.Array;

      var c00 = ary[rowTruncate * height + colTruncate + offset];
      var c01 = ary[rowTruncate * height + colTruncate + 1 + offset];
      var c10 = ary[(rowTruncate + 1) * height + colTruncate + offset];
      var c11 = ary[(rowTruncate + 1) * height + colTruncate + 1 + offset];

      //Blerp each component
      var r = ColorBlerp((byte)((c00 >> 16) & 0xFF), (byte)((c01 >> 16) & 0xFF), (byte)((c10 >> 16) & 0xFF), (byte)((c11 >> 16) & 0xFF), rowFraction, colFraction);
      var g = ColorBlerp((byte)((c00 >> 08) & 0xFF), (byte)((c01 >> 08) & 0xFF), (byte)((c10 >> 08) & 0xFF), (byte)((c11 >> 08) & 0xFF), rowFraction, colFraction);
      var b = ColorBlerp((byte)((c00 >> 00) & 0xFF), (byte)((c01 >> 00) & 0xFF), (byte)((c10 >> 00) & 0xFF), (byte)((c11 >> 00) & 0xFF), rowFraction, colFraction);

      byte a;
      if (alpha)
      {
        a = ColorBlerp((byte)((c00 >> 24) & 0xFF), (byte)((c01 >> 24) & 0xFF), (byte)((c10 >> 24) & 0xFF), (byte)((c11 >> 24) & 0xFF), rowFraction, colFraction);
      }
      else
      {
        a = 0xFF;
      }

      //Compose back together
      return ((a << 24) | (r << 16) | (g << 8) | b);
    }

    #endregion Color Interpolation

    #endregion //Interpolate

    #region Lerp

    public static float Lerp(float a, float b, float t)
    {
      return a + (b - a) * t;
    }

    public static float Lerp(float a, float b, double t)
    {
      return a + (float)((b - a) * t);
    }

    public static double Lerp(double a, double b, double t)
    {
      return a + (b - a) * t;
    }

    public static byte Lerp(byte a, byte b, float t)
    {
      return (byte)(a + (b - a) * t);
    }

    public static byte Lerp(byte a, byte b, double t)
    {
      return (byte)(a + (b - a) * t);
    }

    //Color correct lerp
    public static byte ColorLerp(byte a, byte b, float t)
    {
      var a2 = a * a;
      return (byte)Math.Sqrt(a2 + ((b * b) - a2) * t);
    }

    //Color correct lerp
    public static byte ColorLerp(byte a, byte b, double t)
    {
      var a2 = a * a;
      return (byte)Math.Sqrt(a2 + ((b * b) - a2) * t);
    }

    #endregion //Lerp

    #region Blerp

    public static float Blerp(float c00, float c01, float c10, float c11, float ty, float tx)
    {
      //lerp on y
      var ly0 = (c00 + (c10 - c00) * ty);
      var ly1 = (c01 + (c11 - c01) * ty);

      //lerp on x
      return (ly0 + (ly1 - ly0) * tx);
    }

    public static float Blerp(float c00, float c01, float c10, float c11, double ty, double tx)
    {
      //lerp on y
      var ly0 = (float)(c00 + (c10 - c00) * ty);
      var ly1 = (float)(c01 + (c11 - c01) * ty);

      //lerp on x
      return (float)(ly0 + (ly1 - ly0) * tx);
    }

    public static double Blerp(double c00, double c01, double c10, double c11, double ty, double tx)
    {
      //lerp on y
      var ly0 = (c00 + (c10 - c00) * ty);
      var ly1 = (c01 + (c11 - c01) * ty);

      //lerp on x
      return (ly0 + (ly1 - ly0) * tx);
    }

    public static byte Blerp(byte c00, byte c01, byte c10, byte c11, float ty, float tx)
    {
      //lerp on y
      var ly0 = (byte)(c00 + (c10 - c00) * ty);
      var ly1 = (byte)(c01 + (c11 - c01) * ty);

      //lerp on x
      return (byte)(ly0 + (ly1 - ly0) * tx);
    }

    //Color correct Blerp
    public static byte ColorBlerp(byte c00, byte c01, byte c10, byte c11, float ty, float tx)
    {
      //Expand bytes to real color
      var rc00 = c00 * c00;
      var rc01 = c01 * c01;
      var rc10 = c10 * c10;
      var rc11 = c11 * c11;

      //lerp on y
      var ly0 = (rc00 + (rc10 - rc00) * ty);
      var ly1 = (rc01 + (rc11 - rc01) * ty);

      //lerp on x and sqrt
      return (byte)Math.Sqrt((ly0 + (ly1 - ly0) * tx));
    }

    //Color correct Blerp
    public static byte ColorBlerp(byte c00, byte c01, byte c10, byte c11, double ty, double tx)
    {
      //Expand bytes to real color
      var rc00 = c00 * c00;
      var rc01 = c01 * c01;
      var rc10 = c10 * c10;
      var rc11 = c11 * c11;

      //lerp on y
      var ly0 = (rc00 + (rc10 - rc00) * ty);
      var ly1 = (rc01 + (rc11 - rc01) * ty);

      //lerp on x and sqrt
      return (byte)Math.Sqrt((ly0 + (ly1 - ly0) * tx));
    }

    //Color correct Blerp
    public static ushort ColorBlerp(ushort c00, ushort c01, ushort c10, ushort c11, float ty, float tx)
    {
      //Expand words to real color
      var rc00 = c00 * c00;
      var rc01 = c01 * c01;
      var rc10 = c10 * c10;
      var rc11 = c11 * c11;

      //lerp on y
      var ly0 = (rc00 + (rc10 - rc00) * ty);
      var ly1 = (rc01 + (rc11 - rc01) * ty);

      //lerp on x and sqrt
      return (ushort)Math.Sqrt((ly0 + (ly1 - ly0) * tx));
    }

    //Color correct Blerp
    public static ushort ColorBlerp(ushort c00, ushort c01, ushort c10, ushort c11, double ty, double tx)
    {
      //Expand words to real color
      var rc00 = c00 * c00;
      var rc01 = c01 * c01;
      var rc10 = c10 * c10;
      var rc11 = c11 * c11;

      //lerp on y
      var ly0 = (rc00 + (rc10 - rc00) * ty);
      var ly1 = (rc01 + (rc11 - rc01) * ty);

      //lerp on x and sqrt
      return (ushort)Math.Sqrt((ly0 + (ly1 - ly0) * tx));
    }

    #endregion //Blerp

    private static void ClampAndTruncate(double x, int size, out int truncate, out double fraction)
    {
      var max = size - 1;

      //Clamp values 
      x = Math.Min(Math.Max(x, 0), max);
      truncate = (int)x;
      fraction = (x - truncate);
      if (truncate == max)
      {
        truncate = truncate - 1;
        fraction = 1;
      }
    }

    private const double c_DegreesPerRadd = (180 / Math.PI);
    private const double c_RadsPerDegreed = (Math.PI / 180);
    private const double c_PiOverTwod = (Math.PI / 2);

    private const float c_DegreesPerRadf = (float)(180 / Math.PI);
    private const float c_RadsPerDegreef = (float)(Math.PI / 180);
    private const float c_PiOverTwof = (float)(Math.PI / 2);
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

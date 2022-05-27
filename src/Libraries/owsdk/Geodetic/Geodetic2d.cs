//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Globalization;
using sbio.Core.Math;

namespace sbio.owsdk.Geodetic
{
  public struct Geodetic2d : IEquatable<Geodetic2d>
  {
    public static Geodetic2d Average(IEnumerable<Geodetic2d> points)
    {
      Vector2d lat2d, lon2d;
      var n = 1;

      using (var enumerator = points.GetEnumerator())
      {
        if (!enumerator.MoveNext())
        {
          throw new ArgumentException();
        }

        {
          var geo = enumerator.Current;
          lat2d = new Vector2d(Math.Sin(geo.LatitudeRadians), Math.Cos(geo.LatitudeRadians));
          lon2d = new Vector2d(Math.Sin(geo.LongitudeRadians), Math.Cos(geo.LongitudeRadians));
        }

        while (enumerator.MoveNext())
        {
          var geo = enumerator.Current;
          lat2d += new Vector2d(Math.Sin(geo.LatitudeRadians), Math.Cos(geo.LatitudeRadians));
          lon2d += new Vector2d(Math.Sin(geo.LongitudeRadians), Math.Cos(geo.LongitudeRadians));
          ++n;
        }
      }

      lat2d /= n;
      lon2d /= n;

      var latRadians = Math.Atan2(lat2d.x, lat2d.y);
      var lonRadians = Math.Atan2(lon2d.x, lon2d.y);

      return FromRadians(latRadians, lonRadians);
    }

    public double LatitudeRadians
    {
      get { return m_LatitudeRadians; }
    }

    public double LatitudeDegrees
    {
      get { return NumUtil.RadiansToDegrees(m_LatitudeRadians); }
    }

    public double LongitudeRadians
    {
      get { return m_LongitudeRadians; }
    }

    public double LongitudeDegrees
    {
      get { return NumUtil.RadiansToDegrees(m_LongitudeRadians); }
    }

    public static implicit operator Geodetic3d(Geodetic2d g2d)
    {
      return Geodetic3d.FromRadians(g2d.LatitudeRadians, g2d.LongitudeRadians, 0);
    }

    public static Geodetic2d FromDegrees(double latitudeDegrees, double longitudeDegrees)
    {
      return new Geodetic2d(NumUtil.DegreesToRadians(latitudeDegrees), NumUtil.DegreesToRadians(longitudeDegrees));
    }

    public static Geodetic2d FromRadians(double latitudeRadians, double longitudeRadians)
    {
      return new Geodetic2d(latitudeRadians, longitudeRadians);
    }

    public static bool operator ==(Geodetic2d lhs, Geodetic2d rhs)
    {
      return lhs.Equals(rhs);
    }

    public static bool operator !=(Geodetic2d lhs, Geodetic2d rhs)
    {
      return !lhs.Equals(rhs);
    }

    public bool Equals(Geodetic2d other)
    {
      return MathUtilities.IsEqual(m_LatitudeRadians, other.m_LatitudeRadians)
             && MathUtilities.IsEqual(m_LongitudeRadians, other.m_LongitudeRadians);
    }

    public bool EqualsEpsilon(Geodetic2d other, double epsilon = double.Epsilon)
    {
      return m_LatitudeRadians.EqualsEpsilon(other.m_LatitudeRadians, epsilon)
             && m_LongitudeRadians.EqualsEpsilon(other.m_LongitudeRadians, epsilon);
    }

    public override bool Equals(object obj)
    {
      if (obj is Geodetic2d)
      {
        return Equals((Geodetic2d)obj);
      }

      return false;
    }

    public override int GetHashCode()
    {
      return m_LatitudeRadians.GetHashCode() ^ m_LongitudeRadians.GetHashCode();
    }

    public override string ToString()
    {
      return string.Format(CultureInfo.CurrentCulture, "{0}, {1}",
        NumUtil.LatitudeDegreesToDmsString(LatitudeDegrees),
        NumUtil.LongitudeDegreesToDmsString(LongitudeDegrees));
    }

    private Geodetic2d(double latitudeRadians, double longitudeRadians)
    {
      NumUtil.WrapLatLongRadians(ref latitudeRadians, ref longitudeRadians);
      m_LatitudeRadians = latitudeRadians;
      m_LongitudeRadians = longitudeRadians;
    }

    private readonly double m_LatitudeRadians;
    private readonly double m_LongitudeRadians;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

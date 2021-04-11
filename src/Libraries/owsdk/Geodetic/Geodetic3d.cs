//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Globalization;
using sbio.Core.Math;

namespace sbio.owsdk.Geodetic
{
  public struct Geodetic3d : IEquatable<Geodetic3d>
  {
    public static Geodetic3d Average(IEnumerable<Geodetic3d> points)
    {
      Vector2d lat2d, lon2d;
      double elev;
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
          elev = geo.HeightMeters;
        }

        while (enumerator.MoveNext())
        {
          var geo = enumerator.Current;
          lat2d += new Vector2d(Math.Sin(geo.LatitudeRadians), Math.Cos(geo.LatitudeRadians));
          lon2d += new Vector2d(Math.Sin(geo.LongitudeRadians), Math.Cos(geo.LongitudeRadians));
          elev += geo.HeightMeters;
          ++n;
        }
      }

      lat2d /= n;
      lon2d /= n;
      elev /= n;

      var latRadians = Math.Atan2(lat2d.x, lat2d.y);
      var lonRadians = Math.Atan2(lon2d.x, lon2d.y);

      return FromRadians(latRadians, lonRadians, elev);
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

    public double HeightMeters
    {
      get { return m_HeightMeters; }
    }

    public static explicit operator Geodetic2d(Geodetic3d g3d)
    {
      return Geodetic2d.FromRadians(g3d.LatitudeRadians, g3d.LongitudeRadians);
    }

    public static Geodetic3d FromDegrees(double latitudeDegrees, double longitudeDegrees, double heightMeters)
    {
      return new Geodetic3d(NumUtil.DegreesToRadians(latitudeDegrees), NumUtil.DegreesToRadians(longitudeDegrees), heightMeters);
    }

    public static Geodetic3d FromRadians(double latitudeRadians, double longitudeRadians, double heightMeters)
    {
      return new Geodetic3d(latitudeRadians, longitudeRadians, heightMeters);
    }

    public static bool operator ==(Geodetic3d lhs, Geodetic3d rhs)
    {
      return lhs.Equals(rhs);
    }

    public static bool operator !=(Geodetic3d lhs, Geodetic3d rhs)
    {
      return !lhs.Equals(rhs);
    }

    public bool Equals(Geodetic3d other)
    {
      return MathUtilities.IsEqual(m_LatitudeRadians, other.m_LatitudeRadians)
             && MathUtilities.IsEqual(m_LongitudeRadians, other.m_LongitudeRadians)
             && MathUtilities.IsEqual(m_HeightMeters, other.m_HeightMeters);
    }

    public bool EqualsEpsilon(Geodetic3d other, double epsilon)
    {
      return m_LatitudeRadians.EqualsEpsilon(other.m_LatitudeRadians, epsilon)
             && m_LongitudeRadians.EqualsEpsilon(other.m_LongitudeRadians, epsilon)
             && m_HeightMeters.EqualsEpsilon(other.m_HeightMeters, epsilon);
    }

    public override bool Equals(object other)
    {
      if (other is Geodetic3d)
      {
        return Equals((Geodetic3d)other);
      }

      return false;
    }

    public override int GetHashCode()
    {
      return m_LatitudeRadians.GetHashCode()
             ^ m_LongitudeRadians.GetHashCode()
             ^ m_HeightMeters.GetHashCode();
    }

    public override string ToString()
    {
      return string.Format(CultureInfo.CurrentCulture, "{0:F}m ({1}, {2})",
        HeightMeters,
        NumUtil.LatitudeDegreesToDmsString(LatitudeDegrees),
        NumUtil.LongitudeDegreesToDmsString(LongitudeDegrees));
    }

    public string ToString(string numberFormat)
    {
      return string.Format(CultureInfo.CurrentCulture, "{0}m ({1}, {2})",
        HeightMeters.ToString(numberFormat),
        NumUtil.LatitudeDegreesToDmsString(LatitudeDegrees, numberFormat),
        NumUtil.LongitudeDegreesToDmsString(LongitudeDegrees, numberFormat));
    }

    public Geodetic3d(Geodetic2d geo2d, double heightMeters)
      : this(geo2d.LatitudeRadians, geo2d.LongitudeRadians, heightMeters)
    {
    }

    private Geodetic3d(double latitudeRadians, double longitudeRadians, double heightMeters)
    {
      NumUtil.WrapLatLongRadians(ref latitudeRadians, ref longitudeRadians);
      m_LatitudeRadians = latitudeRadians;
      m_LongitudeRadians = longitudeRadians;
      m_HeightMeters = heightMeters;
    }

    private readonly double m_LatitudeRadians;
    private readonly double m_LongitudeRadians;
    private readonly double m_HeightMeters;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

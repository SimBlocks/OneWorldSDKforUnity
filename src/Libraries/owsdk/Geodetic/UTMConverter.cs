//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.Core.Math;

namespace sbio.owsdk.Geodetic
{
  public class UTMConverter
  {
    public static UTMConverter WGS84
    {
      get { return sc_WGS84; }
    }

    public UTMConverter(string datumName)
    {
      m_DatumName = datumName;
      switch (m_DatumName)
      {
        case "Airy":
          m_A = 6377563;
          m_EccSquared = 0.00667054;
          break;
        case "Australian National":
          m_A = 6378160;
          m_EccSquared = 0.006694542;
          break;
        case "Bessel 1841":
          m_A = 6377397;
          m_EccSquared = 0.006674372;
          break;
        case "Bessel 1841 Nambia":
          m_A = 6377484;
          m_EccSquared = 0.006674372;
          break;
        case "Clarke 1866":
          m_A = 6378206;
          m_EccSquared = 0.006768658;
          break;
        case "Clarke 1880":
          m_A = 6378249;
          m_EccSquared = 0.006803511;
          break;
        case "Everest":
          m_A = 6377276;
          m_EccSquared = 0.006637847;
          break;
        case "Fischer 1960 Mercury":
          m_A = 6378166;
          m_EccSquared = 0.006693422;
          break;
        case "Fischer 1968":
          m_A = 6378150;
          m_EccSquared = 0.006693422;
          break;
        case "GRS 1967":
          m_A = 6378160;
          m_EccSquared = 0.006694605;
          break;
        case "GRS 1980":
          m_A = 6378137;
          m_EccSquared = 0.00669438;
          break;
        case "Helmert 1906":
          m_A = 6378200;
          m_EccSquared = 0.006693422;
          break;
        case "Hough":
          m_A = 6378270;
          m_EccSquared = 0.00672267;
          break;
        case "International":
          m_A = 6378388;
          m_EccSquared = 0.00672267;
          break;
        case "Krassovsky":
          m_A = 6378245;
          m_EccSquared = 0.006693422;
          break;
        case "Modified Airy":
          m_A = 6377340;
          m_EccSquared = 0.00667054;
          break;
        case "Modified Everest":
          m_A = 6377304;
          m_EccSquared = 0.006637847;
          break;
        case "Modified Fischer 1960":
          m_A = 6378155;
          m_EccSquared = 0.006693422;
          break;
        case "South American 1969":
          m_A = 6378160;
          m_EccSquared = 0.006694542;
          break;
        case "WGS 60":
          m_A = 6378165;
          m_EccSquared = 0.006693422;
          break;
        case "WGS 66":
          m_A = 6378145;
          m_EccSquared = 0.006694542;
          break;
        case "WGS 72":
          m_A = 6378135;
          m_EccSquared = 0.006694318;
          break;
        case "ED50":
          m_A = 6378388;
          m_EccSquared = 0.00672267;
          break; // International Ellipsoid
        case "WGS 84":
        case "EUREF89": // Max deviation from WGS 84 is 40 cm/km see http://ocq.dk/euref89 (in danish)
        case "ETRS89": // Same as EUREF89 
          m_A = 6378137;
          m_EccSquared = 0.00669438;
          break;
        default:
          throw new ArgumentException("The datum is unrecognized", nameof(datumName));
      }
    }

    public static void GeoToUTMZone(Geodetic2d geo, out int zoneNumber, out char zoneLetter)
    {
      LatLonDegreesToUTMZone(geo.LatitudeDegrees, geo.LongitudeDegrees, out zoneNumber, out zoneLetter);
    }

    public static void LatLonDegreesToUTMZone(double latitudeDegrees, double longitudeDegrees, out int lonZone, out char latZone)
    {
      var LongTemp = longitudeDegrees;
      if (LongTemp >= 8 && LongTemp <= 13 && latitudeDegrees > 54.5 && latitudeDegrees < 58)
      {
        lonZone = 32;
      }
      else if (latitudeDegrees >= 56.0 && latitudeDegrees < 64.0 && LongTemp >= 3.0 && LongTemp < 12.0)
      {
        lonZone = 32;
      }
      else
      {
        lonZone = (int)((LongTemp + 180) / 6) + 1;

        if (latitudeDegrees >= 72.0 && latitudeDegrees < 84.0)
        {
          if (LongTemp >= 0.0 && LongTemp < 9.0)
          {
            lonZone = 31;
          }
          else if (LongTemp >= 9.0 && LongTemp < 21.0)
          {
            lonZone = 33;
          }
          else if (LongTemp >= 21.0 && LongTemp < 33.0)
          {
            lonZone = 35;
          }
          else if (LongTemp >= 33.0 && LongTemp < 42.0)
          {
            lonZone = 37;
          }
        }
        else if (latitudeDegrees < -80 || 84 <= latitudeDegrees)
        {
          //Special zones for left and right of the mercator when out of bounds of normal zones
          if (longitudeDegrees < 0)
          {
            lonZone = 1;
          }
          else
          {
            lonZone = 2;
          }
        }
      }

      if (-80 <= latitudeDegrees && latitudeDegrees < 84)
      {
        int latIndex = -1;

        for (int i = c_Letters.Length - 1; i >= 0; --i)
        {
          if (c_Degrees[i] <= latitudeDegrees)
          {
            latIndex = i;
            break;
          }
        }

        if (latIndex == -1)
        {
          latIndex = c_Letters.Length - 1;
        }

        latZone = c_Letters[latIndex];
      }
      else if (latitudeDegrees < -80)
      {
        if (longitudeDegrees < 0)
        {
          //left of mercator
          latZone = 'A';
        }
        else
        {
          latZone = 'B';
        }
      }
      else //if(84 <= latitudeDegrees)
      {
        if (longitudeDegrees < 0)
        {
          //left of mercator
          latZone = 'Y';
        }
        else
        {
          latZone = 'Z';
        }
      }
    }

    public static UTMCoordinate WGS84GeoToUTM(Geodetic2d geo)
    {
      return sc_WGS84.GeoToUTM(geo);
    }

    public static Geodetic2d UTMToWGS84Geo(UTMCoordinate utm)
    {
      return sc_WGS84.UTMToGeo(utm);
    }

    public UTMCoordinate GeoToUTM(Geodetic2d geo)
    {
      int zoneNumber;
      char zoneLetter;

      var latRadians = geo.LatitudeRadians;
      var lonRadians = geo.LongitudeRadians;

      GeoToUTMZone(geo, out zoneNumber, out zoneLetter);

      var lonOriginRadians = NumUtil.DegreesToRadians((zoneNumber - 1) * 6 - 180 + 3); //+3 puts origin in middle of zone

      var eccPrimeSquared = (m_EccSquared) / (1 - m_EccSquared);

      var N = m_A / Math.Sqrt(1 - m_EccSquared * Math.Sin(latRadians) * Math.Sin(latRadians));
      var T = Math.Tan(latRadians) * Math.Tan(latRadians);
      var C = eccPrimeSquared * Math.Cos(latRadians) * Math.Cos(latRadians);
      var A = Math.Cos(latRadians) * (lonRadians - lonOriginRadians);

      var M = m_A * ((1 - m_EccSquared / 4 - 3 * m_EccSquared * m_EccSquared / 64 - 5 * m_EccSquared * m_EccSquared * m_EccSquared / 256) * latRadians
                     - (3 * m_EccSquared / 8 + 3 * m_EccSquared * m_EccSquared / 32 + 45 * m_EccSquared * m_EccSquared * m_EccSquared / 1024) * Math.Sin(2 * latRadians)
                     + (15 * m_EccSquared * m_EccSquared / 256 + 45 * m_EccSquared * m_EccSquared * m_EccSquared / 1024) * Math.Sin(4 * latRadians)
                     - (35 * m_EccSquared * m_EccSquared * m_EccSquared / 3072) * Math.Sin(6 * latRadians));

      var easting = 0.9996 * N * (A + (1 - T + C) * A * A * A / 6
                                    + (5 - 18 * T + T * T + 72 * C - 58 * eccPrimeSquared) * A * A * A * A * A / 120)
                    + 500000.0;

      var northing = 0.9996 * (M + N * Math.Tan(latRadians) * (A * A / 2 + (5 - T + 9 * C + 4 * C * C) * A * A * A * A / 24
                                                                         + (61 - 58 * T + T * T + 600 * C - 330 * eccPrimeSquared) * A * A * A * A * A * A / 720));

      if (latRadians < 0)
      {
        northing += 10000000.0;
      }

      return new UTMCoordinate(zoneNumber, zoneLetter, easting, northing);
    }

    public Geodetic2d UTMToGeo(UTMCoordinate utm)
    {
      var e1 = (1 - Math.Sqrt(1 - m_EccSquared)) / (1 + Math.Sqrt(1 - m_EccSquared));
      var x = utm.EastingMeters - 500000.0; //remove 500,000 meter offset for longitude
      var y = utm.NorthingMeters;
      var ZoneNumber = utm.ZoneNumber;
      var ZoneLetter = utm.ZoneLetter;

      if (ZoneLetter != 'N')
      {
        y -= 10000000.0;
      }

      var lonOriginRadians = NumUtil.DegreesToRadians((ZoneNumber - 1) * 6 - 180 + 3);

      var eccPrimeSquared = (m_EccSquared) / (1 - m_EccSquared);

      double M = y / 0.9996;
      var mu = M / (m_A * (1 - m_EccSquared / 4 - 3 * m_EccSquared * m_EccSquared / 64 - 5 * m_EccSquared * m_EccSquared * m_EccSquared / 256));

      var phi1Rad = mu + (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu)
                       + (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu)
                       + (151 * e1 * e1 * e1 / 96) * Math.Sin(6 * mu);

      var N1 = m_A / Math.Sqrt(1 - m_EccSquared * Math.Sin(phi1Rad) * Math.Sin(phi1Rad));
      var T1 = Math.Tan(phi1Rad) * Math.Tan(phi1Rad);
      var C1 = eccPrimeSquared * Math.Cos(phi1Rad) * Math.Cos(phi1Rad);
      var R1 = m_A * (1 - m_EccSquared) / Math.Pow(1 - m_EccSquared * Math.Sin(phi1Rad) * Math.Sin(phi1Rad), 1.5);
      var D = x / (N1 * 0.9996);

      var latRadians = phi1Rad - (N1 * Math.Tan(phi1Rad) / R1) * (D * D / 2 - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * eccPrimeSquared) * D * D * D * D / 24
                                                                  + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * eccPrimeSquared - 3 * C1 * C1) * D * D * D * D * D * D / 720);

      var lonRadians = lonOriginRadians + (D - (1 + 2 * T1 + C1) * D * D * D / 6 + (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * eccPrimeSquared + 24 * T1 * T1) * D * D * D * D * D / 120) / Math.Cos(phi1Rad);

      return Geodetic2d.FromRadians(latRadians, lonRadians);
    }

    private static readonly UTMConverter sc_WGS84 = new UTMConverter("WGS 84");

    //Latitude zone letters and the latitude value the line at the -bottom- of each zone
    //Note that this does not include the 'special' zones 'A', 'B', 'X', or 'Z'.
    private static readonly char[] c_Letters = new char[] {'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X'};
    private static readonly int[] c_Degrees = new int[] {-80, -72, -64, -56, -48, -40, -32, -24, -16, -8, 0, 8, 16, 24, 32, 40, 48, 56, 64, 72};

    private readonly string m_DatumName;
    private readonly double m_A;
    private readonly double m_EccSquared;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

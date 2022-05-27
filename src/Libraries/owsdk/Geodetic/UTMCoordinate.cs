//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Globalization;
using sbio.Core.Math;

namespace sbio.owsdk.Geodetic
{
  public struct UTMCoordinate : IEquatable<UTMCoordinate>
  {
    public int ZoneNumber
    {
      get { return m_ZoneNumber; }
    }

    public char ZoneLetter
    {
      get { return m_ZoneLetter; }
    }

    public double EastingMeters
    {
      get { return m_EastingMeters; }
    }

    public double NorthingMeters
    {
      get { return m_NorthingMeters; }
    }

    public static bool operator ==(UTMCoordinate lhs, UTMCoordinate rhs)
    {
      return lhs.Equals(rhs);
    }

    public static bool operator !=(UTMCoordinate lhs, UTMCoordinate rhs)
    {
      return !lhs.Equals(rhs);
    }

    public static bool TryParse(string utmString, out UTMCoordinate utm)
    {
      int index = 0;
      var split = utmString.Split(' ');

      if (split.Length <= index)
      {
        goto FailedParse;
      }

      int zoneNumber;
      {
        if (!int.TryParse(split[index], out zoneNumber))
        {
          goto FailedParse;
        }

        ++index;
      }

      if (split.Length <= index)
      {
        goto FailedParse;
      }

      if (split[index].Length != 1)
      {
        goto FailedParse;
      }

      char zoneLetter = split[index][0];
      {
        if (!('A' <= zoneLetter && zoneLetter <= 'Z'))
        {
          goto FailedParse;
        }

        ++index;
      }

      if (split.Length <= index)
      {
        goto FailedParse;
      }

      double easting;
      {
        //Easting & Northing might have a units designator at the tail
        var eastingString = split[index];
        var eastingUnitSubstringIndex = eastingString.LastIndexOfAny("0123456789".ToCharArray());


        if (eastingUnitSubstringIndex == eastingString.Length - 1)
        {
          if (!double.TryParse(eastingString, out easting))
          {
            goto FailedParse;
          }
        }
        else
        {
          var eastingSubstring = eastingString.Substring(0, eastingUnitSubstringIndex + 1);
          var unitSubstring = eastingString.Substring(eastingUnitSubstringIndex + 1);

          if (!double.TryParse(eastingSubstring, out easting))
          {
            goto FailedParse;
          }

          double unitConversion;
          //See if we recognize the units
          switch (unitSubstring)
          {
            case "m":
              unitConversion = 1;
              break;
            case "km":
              unitConversion = 1.0 / 1000.0;
              break;
            default:
              goto FailedParse;
          }

          easting *= unitConversion;
        }

        ++index;
      }

      if (split.Length <= index)
      {
        goto FailedParse;
      }

      {
        //See if there's an E or W for 'Easting' or 'Westing'
        var str = split[index];

        bool isEastWest = false;
        if (str.Length == 1)
        {
          switch (str[0])
          {
            case 'E':
              isEastWest = true;
              break;
            case 'W':
              isEastWest = true;
              throw new NotImplementedException();
            default:
              isEastWest = false;
              break;
          }
        }
        else
        {
          isEastWest = false;
        }

        if (isEastWest)
        {
          ++index;
        }
      }

      if (split.Length <= index)
      {
        goto FailedParse;
      }

      double northing;
      {
        var northingString = split[index];
        var northingUnitSubstringIndex = northingString.LastIndexOfAny("0123456789".ToCharArray());

        if (northingUnitSubstringIndex == northingString.Length - 1)
        {
          if (!double.TryParse(northingString, out northing))
          {
            goto FailedParse;
          }
        }
        else
        {
          var northingSubstring = northingString.Substring(0, northingUnitSubstringIndex + 1);
          var unitSubstring = northingString.Substring(northingUnitSubstringIndex + 1);

          if (!double.TryParse(northingSubstring, out northing))
          {
            goto FailedParse;
          }

          double unitConversion;
          //See if we recognize the units
          switch (unitSubstring)
          {
            case "m":
              unitConversion = 1;
              break;
            case "km":
              unitConversion = 1 / 1000;
              break;
            default:
              goto FailedParse;
          }

          northing *= unitConversion;
        }

        ++index;
      }

      if (split.Length > index)
      {
        //See if there's an N or S for 'Northing' or 'Southing'
        var str = split[index];

        bool isNorthSouth = false;
        if (str.Length == 1)
        {
          switch (str[0])
          {
            case 'N':
              isNorthSouth = true;
              break;
            case 'S':
              isNorthSouth = true;
              throw new NotImplementedException();
            default:
              isNorthSouth = false;
              break;
          }
        }
        else
        {
          isNorthSouth = false;
        }

        if (!isNorthSouth)
        {
          //Garbage at the end of the UTM string
          goto FailedParse;
        }
      }

      utm = new UTMCoordinate(zoneNumber, zoneLetter, easting, northing);
      return true;

      FailedParse:
      utm = new UTMCoordinate();
      return false;
    }

    public static UTMCoordinate Parse(string utmString)
    {
      UTMCoordinate ret;
      if (!TryParse(utmString, out ret))
      {
        throw new FormatException(string.Format("Could not parse an UTM coordinate from '{0}'", utmString));
      }

      return ret;
    }

    public bool Equals(UTMCoordinate other)
    {
      return m_ZoneNumber == other.ZoneNumber
             && m_ZoneLetter == other.ZoneLetter
             && MathUtilities.IsEqual(m_EastingMeters, other.m_EastingMeters)
             && MathUtilities.IsEqual(m_NorthingMeters, other.m_NorthingMeters);
    }

    public override bool Equals(object obj)
    {
      if (obj is UTMCoordinate)
      {
        return Equals((UTMCoordinate)obj);
      }

      return false;
    }

    public override int GetHashCode()
    {
      return m_EastingMeters.GetHashCode() ^ m_NorthingMeters.GetHashCode();
    }

    public override string ToString()
    {
      return string.Format(CultureInfo.CurrentCulture,
        "{0:00} {1} {2:0.##}m E {3:0.##}m N", m_ZoneNumber, m_ZoneLetter, m_EastingMeters, m_NorthingMeters);
    }

    public UTMCoordinate(int zoneNumber, char zoneLetter, double eastingMeters, double northingMeters)
    {
      m_ZoneNumber = zoneNumber;
      m_ZoneLetter = zoneLetter;
      m_EastingMeters = eastingMeters;
      m_NorthingMeters = northingMeters;
    }

    private readonly int m_ZoneNumber;
    private readonly char m_ZoneLetter;
    private readonly double m_EastingMeters;
    private readonly double m_NorthingMeters;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

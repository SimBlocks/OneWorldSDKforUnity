//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;

namespace sbio.owsdk.Geodetic
{
  /// Represents geo bounding box
  public struct GeoBoundingBox : IEquatable<GeoBoundingBox>
  {
    public double WestRadians
    {
      get { return m_NorthWest.LongitudeRadians; }
    }

    public double WestDegrees
    {
      get { return m_NorthWest.LongitudeDegrees; }
    }

    public double NorthRadians
    {
      get { return m_NorthWest.LatitudeRadians; }
    }

    public double NorthDegrees
    {
      get { return m_NorthWest.LatitudeDegrees; }
    }

    public double EastRadians
    {
      get { return m_SouthEast.LongitudeRadians; }
    }

    public double EastDegrees
    {
      get { return m_SouthEast.LongitudeDegrees; }
    }

    public double SouthRadians
    {
      get { return m_SouthEast.LatitudeRadians; }
    }

    public double SouthDegrees
    {
      get { return m_SouthEast.LatitudeDegrees; }
    }

    public Geodetic2d NorthWest
    {
      get { return m_NorthWest; }
    }

    public Geodetic2d NorthEast
    {
      get { return Geodetic2d.FromRadians(m_NorthWest.LatitudeRadians, m_SouthEast.LongitudeRadians); }
    }

    public Geodetic2d SouthWest
    {
      get { return Geodetic2d.FromRadians(m_SouthEast.LatitudeRadians, m_NorthWest.LongitudeRadians); }
    }

    public Geodetic2d SouthEast
    {
      get { return m_SouthEast; }
    }

    public Geodetic2d Center
    {
      get { return Geodetic2d.FromRadians(CenterLatRadians, CenterLonRadians); }
    }

    public Geodetic2d North
    {
      get { return Geodetic2d.FromRadians(m_NorthWest.LatitudeRadians, CenterLonRadians); }
    }

    public Geodetic2d South
    {
      get { return Geodetic2d.FromRadians(m_SouthEast.LatitudeRadians, CenterLonRadians); }
    }

    public Geodetic2d East
    {
      get { return Geodetic2d.FromRadians(CenterLatRadians, m_SouthEast.LongitudeRadians); }
    }

    public Geodetic2d West
    {
      get { return Geodetic2d.FromRadians(CenterLatRadians, m_NorthWest.LongitudeRadians); }
    }

    public double WidthRadians
    {
      get { return Math.Abs(m_NorthWest.LongitudeRadians - m_SouthEast.LongitudeRadians); }
    }

    public double WidthDegrees
    {
      get { return Math.Abs(m_NorthWest.LongitudeDegrees - m_SouthEast.LongitudeDegrees); }
    }

    public double HeightDegrees
    {
      get { return Math.Abs(m_NorthWest.LatitudeDegrees - m_SouthEast.LatitudeDegrees); }
    }

    public double HeightRadians
    {
      get { return Math.Abs(m_NorthWest.LatitudeRadians - m_SouthEast.LatitudeRadians); }
    }

    public bool Contains(Geodetic2d point)
    {
      var y1 = m_SouthEast.LatitudeRadians;
      var py = point.LatitudeRadians;
      var y2 = m_NorthWest.LatitudeRadians;

      var x1 = m_NorthWest.LongitudeRadians;
      var px = point.LongitudeRadians;
      var x2 = m_SouthEast.LongitudeRadians;

      bool containsLat;
      if (y1 <= y2)
      {
        //Not crossing a pole
        containsLat = y1 <= py && py <= y2;
      }
      else
      {
        throw new NotImplementedException("Don't know how to handle crossing polar boundaries.");
      }

      bool containsLon;
      if (x1 <= x2)
      {
        //Not crossing IDL
        containsLon = x1 <= px && px <= x2;
      }
      else
      {
        //Crossing IDL
        containsLon = x1 <= px || px <= x2;
      }

      return containsLat && containsLon;
    }

    public bool Contains(GeoBoundingBox other)
    {
      return Contains(other.m_NorthWest) && Contains(other.m_SouthEast);
    }

    public bool Intersects(GeoBoundingBox other)
    {
      return WestRadians < other.EastRadians
             && EastRadians > other.WestRadians
             && NorthRadians > other.SouthRadians
             && SouthRadians < other.NorthRadians;
    }

    public bool Equals(GeoBoundingBox other)
    {
      return m_NorthWest.Equals(other.m_NorthWest)
             && m_SouthEast.Equals(other.m_SouthEast);
    }

    public override bool Equals(object obj)
    {
      if (obj is GeoBoundingBox)
      {
        return Equals((GeoBoundingBox)obj);
      }

      return false;
    }

    public override int GetHashCode()
    {
      return m_NorthWest.GetHashCode()
             ^ m_SouthEast.GetHashCode();
    }

    public override string ToString()
    {
      return string.Format("[{0:F2}, {1:F2}] [{2:F2}, {3:F2}]", NorthDegrees, WestDegrees, SouthDegrees, EastDegrees);
    }

    public static GeoBoundingBox FromDegrees(double north, double west, double south, double east)
    {
      return new GeoBoundingBox(Geodetic2d.FromDegrees(north, west), Geodetic2d.FromDegrees(south, east));
    }

    public static GeoBoundingBox FromRadians(double north, double west, double south, double east)
    {
      return new GeoBoundingBox(Geodetic2d.FromRadians(north, west), Geodetic2d.FromRadians(south, east));
    }

    public GeoBoundingBox(Geodetic2d northWest, Geodetic2d southEast)
    {
      m_NorthWest = northWest;
      m_SouthEast = southEast;
    }

    private double CenterLatRadians
    {
      get { return (NorthWest.LatitudeRadians + SouthEast.LatitudeRadians) / 2; }
    }

    private double CenterLonRadians
    {
      get { return (NorthWest.LongitudeRadians + SouthEast.LongitudeRadians) / 2; }
    }

    private readonly Geodetic2d m_NorthWest;
    private readonly Geodetic2d m_SouthEast;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.Core.Math;

namespace sbio.owsdk.Geodetic
{
  public static class GeoUtils
  {
    /// Gets longitudinal offset in degrees
    public static double GetOffsetDegrees(Geodetic2d point, double offsetInMeters)
    {
      return NumUtil.RadiansToDegrees(offsetInMeters / WGS84EarthRadius(point.LatitudeRadians));
    }

    /// Earth longitudinal radius at a given latitude, according to the WGS-84 ellipsoid [m].
    public static double WGS84EarthRadius(double latitudeRadians)
    {
      // Semi-axes of WGS-84 geoidal reference
      const double WGS84_a = 6378137.0; // Major semiaxis [m]
      const double WGS84_b = 6356752.314245; // Minor semiaxis [m]

      // http://en.wikipedia.org/wiki/Earth_radius
      var an = WGS84_a * WGS84_a * Math.Cos(latitudeRadians);
      var bn = WGS84_b * WGS84_b * Math.Sin(latitudeRadians);
      var ad = WGS84_a * Math.Cos(latitudeRadians);
      var bd = WGS84_b * Math.Sin(latitudeRadians);
      return Math.Sqrt((an * an + bn * bn) / (ad * ad + bd * bd));
    }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

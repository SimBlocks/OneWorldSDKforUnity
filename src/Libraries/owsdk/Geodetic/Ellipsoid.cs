//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.Core.Math;

namespace sbio.owsdk.Geodetic
{
  public sealed class Ellipsoid
  {
    public Vec3LeftHandedGeocentric RadiiMeters
    {
      get { return m_RadiiMeters; }
      set
      {
        if (!m_RadiiMeters.EqualsEpsilon(value))
        {
          m_RadiiMeters = value;
          UpdateRadii();
        }
      }
    }

    public Vec3LeftHandedGeocentric RadiiSquared
    {
      get { return m_RadiiSquared; }
    }

    public Vec3LeftHandedGeocentric OneOverRadiiSquared
    {
      get { return m_OneOverRadiiSquared; }
    }

    public double MinimumRadius
    {
      get { return Math.Min(m_RadiiMeters.X, Math.Min(m_RadiiMeters.Y, m_RadiiMeters.Z)); }
    }

    public double MaximumRadius
    {
      get { return Math.Max(m_RadiiMeters.X, Math.Max(m_RadiiMeters.Y, m_RadiiMeters.Z)); }
    }

    /// <summary>
    /// Retrieves the direction of 'north' in world space relative to the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate north from</param>
    /// <returns>A vector in world space indicating the direction of 'north' relative to the given geoposition</returns>
    public Vec3LeftHandedGeocentric NorthDirection(Geodetic2d geodetic)
    {
      Vec3LeftHandedGeocentric normal;
      return NorthDirection(geodetic, out normal);
    }

    /// <summary>
    /// Retrieves the direction of 'north' and normal in world space relative to the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate north and normal from</param>
    /// <param name="normal">An output parameter receiving the normal position</param>
    /// <returns>A vector in world space indicating the direction of 'north' relative to the given geoposition</returns>
    public Vec3LeftHandedGeocentric NorthDirection(Geodetic2d geodetic, out Vec3LeftHandedGeocentric normal)
    {
      normal = GeodeticSurfaceNormal(geodetic);
      return ToNorth(normal);
    }

    /// <summary>
    /// Retrieves the direction of 'south' in world space relative to the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate south from</param>
    /// <returns>A vector in world space indicating the direction of 'south' relative to the given geoposition</returns>
    public Vec3LeftHandedGeocentric SouthDirection(Geodetic2d geodetic)
    {
      Vec3LeftHandedGeocentric normal;
      return SouthDirection(geodetic, out normal);
    }

    /// <summary>
    /// Retrieves the direction of 'south' and normal in world space relative to the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate south and normal from</param>
    /// <param name="normal">An output parameter receiving the normal position</param>
    /// <returns>A vector in world space indicating the direction of 'south' relative to the given geoposition</returns>
    public Vec3LeftHandedGeocentric SouthDirection(Geodetic2d geodetic, out Vec3LeftHandedGeocentric normal)
    {
      normal = GeodeticSurfaceNormal(geodetic);
      return ToSouth(normal);
    }

    /// <summary>
    /// Retrieves the direction of 'east' in world space relative to the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate east from</param>
    /// <returns>A vector in world space indicating the direction of 'east' relative to the given geoposition</returns>
    public Vec3LeftHandedGeocentric EastDirection(Geodetic2d geodetic)
    {
      Vec3LeftHandedGeocentric normal;
      return EastDirection(geodetic, out normal);
    }

    /// <summary>
    /// Retrieves the direction of 'east' and normal in world space relative to the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate south and normal from</param>
    /// <param name="normal">An output parameter receiving the normal position</param>
    /// <returns>A vector in world space indicating the direction of 'south' relative to the given geoposition</returns>
    public Vec3LeftHandedGeocentric EastDirection(Geodetic2d geodetic, out Vec3LeftHandedGeocentric normal)
    {
      Vec3LeftHandedGeocentric northDir = NorthDirection(geodetic, out normal);
      return normal.Cross(northDir).Normalized();
    }

    /// <summary>
    /// Retrieves the direction of 'west' in world space relative to the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate west from</param>
    /// <returns>A vector in world space indicating the direction of 'west' relative to the given geoposition</returns>
    public Vec3LeftHandedGeocentric WestDirection(Geodetic2d geodetic)
    {
      Vec3LeftHandedGeocentric normal;
      return WestDirection(geodetic, out normal);
    }

    /// <summary>
    /// Retrieves the direction of 'west' and normal in world space relative to the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate west and normal from</param>
    /// <param name="normal">An output parameter receiving the normal position</param>
    /// <returns>A vector in world space indicating the direction of 'west' relative to the given geoposition</returns>
    public Vec3LeftHandedGeocentric WestDirection(Geodetic2d geodetic, out Vec3LeftHandedGeocentric normal)
    {
      Vec3LeftHandedGeocentric northDir = NorthDirection(geodetic, out normal);
      return northDir.Cross(normal).Normalized();
    }

    /// <summary>
    /// Calculates all four cardinal directions at the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate cardinal directions from</param>
    /// <param name="north">An output parameter receiving the direction of 'north'</param>
    /// <param name="south">An output parameter receiving the direction of 'south'</param>
    /// <param name="east">An output parameter receiving the direction of 'east'</param>
    /// <param name="west">An output parameter receiving the direction of 'west'</param>
    public void CardinalDirections(Geodetic2d geodetic, out Vec3LeftHandedGeocentric north, out Vec3LeftHandedGeocentric south, out Vec3LeftHandedGeocentric east, out Vec3LeftHandedGeocentric west)
    {
      Vec3LeftHandedGeocentric normal;
      CardinalDirections(geodetic, out north, out south, out east, out west, out normal);
    }

    /// <summary>
    /// Calculates all four cardinal directions at the given geoposition, as well as the normal
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate cardinal directions from</param>
    /// <param name="north">An output parameter receiving the direction of 'north'</param>
    /// <param name="south">An output parameter receiving the direction of 'south'</param>
    /// <param name="east">An output parameter receiving the direction of 'east'</param>
    /// <param name="west">An output parameter receiving the direction of 'west'</param>
    /// /// <param name="normal">An output parameter receiving the direction of the normal at the given location</param>
    public void CardinalDirections(Geodetic2d geodetic, out Vec3LeftHandedGeocentric north, out Vec3LeftHandedGeocentric south, out Vec3LeftHandedGeocentric east, out Vec3LeftHandedGeocentric west, out Vec3LeftHandedGeocentric normal)
    {
      normal = GeodeticSurfaceNormal(geodetic);
      north = ToNorth(normal);
      south = -north;
      east = normal.Cross(north).Normalized();
      west = -east;
    }

    /// <summary>
    /// Calculates the surface normal at the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate the normal for</param>
    /// <returns>The normal direction vector at the given geoposition</returns>
    public Vec3LeftHandedGeocentric GeodeticSurfaceNormal(Geodetic2d geodetic)
    {
      var cosLatitude = Math.Cos(geodetic.LatitudeRadians);
      return
        ToWorld
        (new Vec3LeftHandedGeocentric(
          cosLatitude * Math.Cos(geodetic.LongitudeRadians),
          Math.Sin(geodetic.LatitudeRadians),
          cosLatitude * Math.Sin(geodetic.LongitudeRadians))).Normalized();
    }

    /// <summary>
    /// Calculates the surface normal at the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate the normal for</param>
    /// <returns>The normal direction vector at the given geoposition</returns>
    public Vec3LeftHandedGeocentric GeodeticSurfaceNormal(Geodetic3d geodetic)
    {
      var cosLatitude = Math.Cos(geodetic.LatitudeRadians);
      return
        ToWorld
        (new Vec3LeftHandedGeocentric(
          cosLatitude * Math.Cos(geodetic.LongitudeRadians),
          Math.Sin(geodetic.LatitudeRadians),
          cosLatitude * Math.Sin(geodetic.LongitudeRadians))).Normalized();
    }

    /// <summary>
    /// Calculates the surface normal at the given global position
    /// </summary>
    /// <param name="position">The global position to calculate the normal for</param>
    /// <returns>The normal direction vector at the given position</returns>
    public Vec3LeftHandedGeocentric GeodeticSurfaceNormal(Vec3LeftHandedGeocentric position)
    {
      return SurfacePointGeodeticSurfaceNormal(ScaleToGeodeticSurface(position));
    }

    /// <summary>
    /// Calculates the global surface position of the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate the surface position</param>
    /// <returns>The global position of the given geoposition</returns>
    public Vec3LeftHandedGeocentric ToVec3LeftHandedGeocentric(Geodetic2d geodetic)
    {
      Vec3LeftHandedGeocentric normal;
      return ToVec3LeftHandedGeocentric(geodetic, out normal);
    }

    /// <summary>
    /// Calculates the global surface position and normal of the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate the surface position</param>
    /// <param name="normal">Output parameter receiving the surface normal at the given position</param>
    /// <returns>The global position of the given geoposition</returns>
    public Vec3LeftHandedGeocentric ToVec3LeftHandedGeocentric(Geodetic2d geodetic, out Vec3LeftHandedGeocentric normal)
    {
      normal = GeodeticSurfaceNormal(geodetic);
      return ToVec3LeftHandedGeocentric(normal);
    }

    /// <summary>
    /// Calculates the global surface position, normal, and north direction of the given geoposition
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate the surface position</param>
    /// <param name="normal">Output parameter receiving the surface normal at the given position</param>
    /// <param name="north">Output parameter receiving the north direction at the given position</param>
    /// <returns>The global position of the given geoposition</returns>
    public Vec3LeftHandedGeocentric ToVec3LeftHandedGeocentric(Geodetic2d geodetic, out Vec3LeftHandedGeocentric normal, out Vec3LeftHandedGeocentric north)
    {
      normal = GeodeticSurfaceNormal(geodetic);
      north = ToNorth(normal);
      return ToVec3LeftHandedGeocentric(normal);
    }

    /// <summary>
    /// Calculates the global position of the given geoposition, offset by the given amount of meters
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate the surface position</param>
    /// <param name="heightMeters">Offset to apply from the surface when calculating position</param>
    /// <returns>The global position of the given geoposition modified by the given offset</returns>
    public Vec3LeftHandedGeocentric ToVec3LeftHandedGeocentric(Geodetic2d geodetic, double heightMeters)
    {
      Vec3LeftHandedGeocentric normal;
      return ToVec3LeftHandedGeocentric(geodetic, heightMeters, out normal);
    }

    /// <summary>
    /// Calculates the global position and normal of the given geoposition, offset by the given amount of meters
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate the surface position</param>
    /// <param name="heightMeters">Offset to apply from the surface when calculating position</param>
    /// <param name="normal">Output parameter receiving the normal at the given geoposition</param>
    /// <returns>The global position of the given geoposition modified by the given offset</returns>
    public Vec3LeftHandedGeocentric ToVec3LeftHandedGeocentric(Geodetic2d geodetic, double heightMeters, out Vec3LeftHandedGeocentric normal)
    {
      normal = GeodeticSurfaceNormal(geodetic);
      return ToVec3LeftHandedGeocentric(normal) + (heightMeters * normal);
    }

    /// <summary>
    /// Calculates the global position and normal of the given geoposition, offset by the given amount of meters
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate the surface position</param>
    /// <param name="heightMeters">Offset to apply from the surface when calculating position</param>
    /// <param name="normal">Output parameter receiving the normal at the given geoposition</param>
    /// <returns>The global position of the given geoposition modified by the given offset</returns>
    public Vec3LeftHandedGeocentric ToVec3LeftHandedGeocentric(Geodetic2d geodetic, double heightMeters, out Vec3LeftHandedGeocentric normal, out Vec3LeftHandedGeocentric north)
    {
      normal = GeodeticSurfaceNormal(geodetic);
      north = ToNorth(normal);
      return ToVec3LeftHandedGeocentric(normal) + (heightMeters * normal);
    }

    /// <summary>
    /// Calculates the global position of the given geoposition, offset by the given amount of meters
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate the surface position</param>
    public Vec3LeftHandedGeocentric ToVec3LeftHandedGeocentric(Geodetic3d geodetic)
    {
      Vec3LeftHandedGeocentric normal;
      return ToVec3LeftHandedGeocentric(geodetic, out normal);
    }

    /// <summary>
    /// Calculates the global position and normal of the given geoposition, offset by the given amount of meters
    /// </summary>
    /// <param name="geodetic">The geoposition to calculate the surface position</param>
    /// <param name="normal">Output parameter receiving the normal at the given geoposition</param>
    /// <returns>The global position of the given geoposition modified by the given offset</returns>
    public Vec3LeftHandedGeocentric ToVec3LeftHandedGeocentric(Geodetic3d geodetic, out Vec3LeftHandedGeocentric normal)
    {
      normal = GeodeticSurfaceNormal(geodetic);
      return ToVec3LeftHandedGeocentric(normal) + (geodetic.HeightMeters * normal);
    }

    /// <summary>
    /// Calculates the surface geoposition of the given global position
    /// </summary>
    /// <param name="position">The global position to calculate the geoposition for</param>
    /// <returns>The geoposition of the given global position</returns>
    public Geodetic2d ToGeodetic2d(Vec3LeftHandedGeocentric position)
    {
      Vec3LeftHandedGeocentric normal;
      return ToGeodetic2d(position, out normal);
    }

    /// <summary>
    /// Calculates the surface geoposition and normal of the given global position
    /// </summary>
    /// <param name="position">The global position to calculate the geoposition for</param>
    /// <param name="normal">Output parameter receiving the surface normal at the given geoposition</param>
    /// <returns>The geoposition of the given global position</returns>
    public Geodetic2d ToGeodetic2d(Vec3LeftHandedGeocentric position, out Vec3LeftHandedGeocentric normal)
    {
      normal = GeodeticSurfaceNormal(position);
      var localNormal = ToLocal(normal);
      return Geodetic2d.FromRadians(
        Math.Asin(localNormal.Y / localNormal.Magnitude),
        Math.Atan2(localNormal.Z, localNormal.X));
    }

    /// <summary>
    /// Calculates the surface geoposition and height offset of the given global position
    /// </summary>
    /// <param name="position">The global position to calculate the geoposition for</param>
    /// <returns>The geoposition and height offset of the given global position</returns>
    public Geodetic3d ToGeodetic3d(Vec3LeftHandedGeocentric position)
    {
      double X = position.X;
      double Y = position.Z;
      double Z = position.Y;

      // step 0: calculate convenience constants.
      const double a = 6_378_137.0;
      const double c = 6_356_752.314245;

      double ε2 = (Math.Pow(a, 2) - Math.Pow(c, 2)) / Math.Pow(a, 2);
      double έ2 = (Math.Pow(a, 2) - Math.Pow(c, 2)) / Math.Pow(c, 2);

      const double f = (a - c) / a;

      // step 1: compute W = (X^2 + Y^2) ^ (1/2)
      double W = Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2));

      // step 2: determine aD/c... Going to assume entity's altitude is less than 2000 kilometers above sea level.
      double aDc = 1.0026000;

      // step 3: compute T0 = Z(aD/c)
      double T0 = Z * aDc;

      // step 4: compute S0 = [Z(aD/c)^2 + W^2] ^ (1/2) 
      double S0 = Math.Sqrt(Math.Pow(T0, 2) + Math.Pow(W, 2));

      // step 5: compute sinß0 = T0 / S0 and cosß0 = W / S0
      double sinß0 = T0 / S0;
      double cosß0 = W / S0;

      // step 6: compute T1 = Z + ce'^2 sinß0^3
      double T1 = Z + c * έ2 * Math.Pow(sinß0, 3);

      // step 7: compute S1^2 = T1^2 + (W - ae^2 cosß0^3)^2
      double S1_2 = Math.Pow(T1, 2) + Math.Pow(W - a * ε2 * Math.Pow(cosß0, 3), 2);

      // step 8: square both sides of 23 to get sin^2ϕ1 = T1^2 / S1^2
      double sin_2ϕ1 = Math.Pow(T1, 2) / S1_2;

      // step 9: ready to get h. RN = a / (1 - e^2sin^2ϕ1)^ (1/2)
      double RN = a / Math.Sqrt(1 - ε2 * sin_2ϕ1);

      double sinϕ1 = Math.Sqrt(sin_2ϕ1);
      double cosϕ1 = (W - a * ε2 * Math.Pow(cosß0, 3)) / Math.Sqrt(S1_2);


      // compute second iteration:
      double tanϕ1 = sinϕ1 / cosϕ1;

      double tanß1 = (1 - f) * tanϕ1;
      double ß1 = Math.Atan(tanß1);
      double sinß1 = Math.Sin(ß1);
      double cosß1 = Math.Cos(ß1);

      double T2 = Z + c * έ2 * Math.Pow(sinß1, 3);
      double S2_2 = Math.Pow(T2, 2) + Math.Pow(W - a * ε2 * Math.Pow(cosß1, 3), 2);
      double sin_2ϕ2 = Math.Pow(T2, 2) / S2_2;
      RN = a / Math.Sqrt(1 - ε2 * sin_2ϕ2);

      double sinϕ2 = Math.Sqrt(sin_2ϕ2);
      double cosϕ2 = (W - a * ε2 * Math.Pow(cosß1, 3)) / Math.Sqrt(S2_2);

      double h;
      // step 10: if sin_2ϕ1 >= sin^2(67.5 degrees) then...
      if (sin_2ϕ2 >= Math.Pow(Math.Sin(NumUtil.DegreesToRadians(67.5)), 2))
      {
        // sinϕ1 = sin_2ϕ1 ^ (1/2) and h = Z / sinϕ1 + RN(e^2 - 1)
        h = Z / sinϕ2 + RN * (ε2 - 1);
      }
      else
      {
        // step 11: cosϕ1 = (W - a e^2 cos^3ß0) / S1 and h = W / cosϕ1 - RN
        h = W / cosϕ2 - RN;
      }

      // step 12: compute ϕ, and λ from tan-1(Y/X)
      double ϕ = Math.Atan2(sinϕ2, cosϕ2);
      double λ = Math.Atan2(Y, X);

      return Geodetic3d.FromRadians(ϕ, λ, h);
    }

    /// <summary>
    /// Calculates the surface geoposition, height offset, and normal of the given global position
    /// </summary>
    /// <param name="position">The global position to calculate the geoposition for</param>
    /// <param name="normal">Output parameter receiving the surface normal</param>
    /// <returns>The geoposition and height offset of the given global position</returns>
    public Geodetic3d ToGeodetic3d(Vec3LeftHandedGeocentric position, out Vec3LeftHandedGeocentric normal)
    {
      var p = ScaleToGeodeticSurface(position);
      var h = position - p;
      var height = Math.Sign(h.Dot(position)) * h.Magnitude;

      normal = SurfacePointGeodeticSurfaceNormal(p);
      var localNormal = ToLocal(normal);
      return ToGeodetic3d(position);
    }

    /// <summary>
    /// Calculates the surface position of the given point on the ellipsoid.
    /// This differs from ScaleToGeocentricSurface in that it projects on the surface, not towards the center of the ellipsoid.
    /// </summary>
    /// <param name="position">The global position for which to calculate the surface position</param>
    /// <returns>The projected surface position</returns>
    public Vec3LeftHandedGeocentric ScaleToGeodeticSurface(Vec3LeftHandedGeocentric position)
    {
      var localPos = ToLocal(position);

      var beta = 1.0 / Math.Sqrt(
        (localPos.X * localPos.X) * m_OneOverRadiiSquared.X +
        (localPos.Y * localPos.Y) * m_OneOverRadiiSquared.Y +
        (localPos.Z * localPos.Z) * m_OneOverRadiiSquared.Z);
      var n = new Vec3LeftHandedGeocentric(
        beta * localPos.X * m_OneOverRadiiSquared.X,
        beta * localPos.Y * m_OneOverRadiiSquared.Y,
        beta * localPos.Z * m_OneOverRadiiSquared.Z).Magnitude;
      var alpha = (1.0 - beta) * (localPos.Magnitude / n);

      var x2 = localPos.X * localPos.X;
      var y2 = localPos.Y * localPos.Y;
      var z2 = localPos.Z * localPos.Z;

      var da = 0.0;
      var db = 0.0;
      var dc = 0.0;
      var s = 0.0;
      var dSdA = 1.0;

      do
      {
        alpha -= (s / dSdA);

        da = 1.0 + (alpha * m_OneOverRadiiSquared.X);
        db = 1.0 + (alpha * m_OneOverRadiiSquared.Y);
        dc = 1.0 + (alpha * m_OneOverRadiiSquared.Z);

        var da2 = da * da;
        var db2 = db * db;
        var dc2 = dc * dc;

        var da3 = da * da2;
        var db3 = db * db2;
        var dc3 = dc * dc2;

        s = x2 / (m_RadiiSquared.X * da2) +
          y2 / (m_RadiiSquared.Y * db2) +
          z2 / (m_RadiiSquared.Z * dc2) - 1.0;

        dSdA = -2.0 *
               (x2 / (m_RadiiToTheFourth.X * da3) +
                y2 / (m_RadiiToTheFourth.Y * db3) +
                z2 / (m_RadiiToTheFourth.Z * dc3));
      } while (Math.Abs(s) > 1e10);

      return
        ToWorld(
          new Vec3LeftHandedGeocentric(
            localPos.X / da,
            localPos.Y / db,
            localPos.Z / dc));
    }

    public Ellipsoid()
      : this(1.0, 1.0, 1.0)
    {
    }

    public Ellipsoid(double radiusXMeters, double radiusYMeters, double radiusZMeters)
      : this(new Vec3LeftHandedGeocentric(radiusXMeters, radiusYMeters, radiusZMeters))
    {
    }

    public Ellipsoid(Vec3LeftHandedGeocentric radiiMeters)
    {
      m_RadiiMeters = radiiMeters;
      UpdateRadii();
    }

    private void UpdateRadii()
    {
      m_RadiiSquared = new Vec3LeftHandedGeocentric(
        (m_RadiiMeters.X * m_RadiiMeters.X),
        (m_RadiiMeters.Y * m_RadiiMeters.Y),
        (m_RadiiMeters.Z * m_RadiiMeters.Z));

      m_OneOverRadiiSquared = new Vec3LeftHandedGeocentric(
        1.0 / m_RadiiSquared.X,
        1.0 / m_RadiiSquared.Y,
        1.0 / m_RadiiSquared.Z);
      m_RadiiToTheFourth = new Vec3LeftHandedGeocentric(
        m_RadiiSquared.X * m_RadiiSquared.X,
        m_RadiiSquared.Y * m_RadiiSquared.Y,
        m_RadiiSquared.Z * m_RadiiSquared.Z);
    }

    private Vec3LeftHandedGeocentric SurfacePointGeodeticSurfaceNormal(Vec3LeftHandedGeocentric p)
    {
      var localPos = ToLocal(p);
      return
        ToWorld(
          new Vec3LeftHandedGeocentric(
            localPos.X * m_OneOverRadiiSquared.X,
            localPos.Y * m_OneOverRadiiSquared.Y,
            localPos.Z * m_OneOverRadiiSquared.Z).Normalized());
    }

    /// <summary>
    /// Calculates the 3d surface position from the given normal
    /// </summary>
    /// <param name="normal">The normal to compute the surface position of</param>
    /// <returns>A position on the surface of the ellipsoid with the given normal</returns>
    private Vec3LeftHandedGeocentric ToVec3LeftHandedGeocentric(Vec3LeftHandedGeocentric normal)
    {
      var localNormal = ToLocal(normal);
      var k =
        new Vec3LeftHandedGeocentric(
          m_RadiiSquared.X * localNormal.X,
          m_RadiiSquared.Y * localNormal.Y,
          m_RadiiSquared.Z * localNormal.Z);
      var gamma = Math.Sqrt(
        k.X * localNormal.X +
        k.Y * localNormal.Y +
        k.Z * localNormal.Z);

      return ToWorld(k / gamma);
    }

    /// <summary>
    /// Calculates the 'north' direction from the given normal
    /// </summary>
    /// <param name="normal">The normal for which to retrieve the 'north' direction</param>
    /// <returns>A vector that is orthogonal to 'normal' indicating the direction of 'north'</returns>
    private Vec3LeftHandedGeocentric ToNorth(Vec3LeftHandedGeocentric normal)
    {
      var upDir = ToWorld(new Vec3LeftHandedGeocentric(0, 1, 0));
      var projection = normal.Dot(upDir);
      return (upDir - normal * projection).Normalized();
    }

    /// <summary>
    /// Calculates the 'south' direction from the given normal
    /// </summary>
    /// <param name="normal">The normal for which to retrieve the 'south' direction</param>
    /// <returns>A vector that is orthogonal to 'normal' indicating the direction of 'south'</returns>
    private Vec3LeftHandedGeocentric ToSouth(Vec3LeftHandedGeocentric normal)
    {
      var downDir = ToWorld(new Vec3LeftHandedGeocentric(0, -1, 0));
      var projection = normal.Dot(downDir);
      return (downDir - normal * projection).Normalized();
    }

    private Vec3LeftHandedGeocentric ToWorld(Vec3LeftHandedGeocentric localPos)
    {
      //return m_Rotation.Multiply(localPos);
      return localPos;
    }

    private Vec3LeftHandedGeocentric ToLocal(Vec3LeftHandedGeocentric worldPos)
    {
      //return m_InverseRotation.Multiply(worldPos);
      return worldPos;
    }

    private Vec3LeftHandedGeocentric m_RadiiMeters;
    private Vec3LeftHandedGeocentric m_RadiiSquared;
    private Vec3LeftHandedGeocentric m_OneOverRadiiSquared;
    private Vec3LeftHandedGeocentric m_RadiiToTheFourth;
  }
}



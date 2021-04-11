//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.Core.Math;
using sbio.owsdk.Geodetic;

namespace sbio.owsdk.Services
{
  public struct ElevationPointSample : IEquatable<ElevationPointSample>
  {
    public Geodetic2d Position
    {
      get { return m_Position; }
    }

    public double Elevation
    {
      get { return m_Elevation; }
    }

    public bool Equals(ElevationPointSample other)
    {
      return Position == other.Position
             && MathUtilities.IsEqual(Elevation, other.Elevation);
    }

    public override bool Equals(object obj)
    {
      if (obj is ElevationPointSample)
      {
        return Equals((ElevationPointSample)obj);
      }

      return false;
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public ElevationPointSample(Geodetic2d position)
    {
      m_Position = position;
      m_Elevation = 0;
    }

    public ElevationPointSample(Geodetic2d position, double elevation)
    {
      m_Position = position;
      m_Elevation = elevation;
    }

    public ElevationPointSample(ElevationPointSample other)
      : this(other.Position, other.Elevation)
    {
    }

    private readonly Geodetic2d m_Position;
    private readonly double m_Elevation;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

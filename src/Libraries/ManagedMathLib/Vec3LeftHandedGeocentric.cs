//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
﻿using System;
using System.Globalization;

namespace sbio.Core.Math
{
  //Earth Centered Earth Fixed (ECEF) origin at Earth's center of mass. left-handed. 
  //X towards intersection of prime meridian and equator. 
  //Y towards north pole.
  public struct Vec3LeftHandedGeocentric : IEquatable<Vec3LeftHandedGeocentric>
  {
    public double X;
    public double Y;
    public double Z;

    public Vec3LeftHandedGeocentric(double x, double y, double z)
    {
      X = x;
      Y = y;
      Z = z;
    }

    public Vec3LeftHandedGeocentric(Vector3d v)
    {
      X = v.x;
      Y = v.y;
      Z = v.z;
    }

    public Vec3LeftHandedGeocentric Cross(Vec3LeftHandedGeocentric rhs)
    {
      return Cross(this, rhs);
    }

    public static Vec3LeftHandedGeocentric Cross(Vec3LeftHandedGeocentric a, Vec3LeftHandedGeocentric b)
    {
      return new Vec3LeftHandedGeocentric(a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);
    }

    public double Dot(Vec3LeftHandedGeocentric other)
    {
      return X * other.X + Y * other.Y + Z * other.Z;
    }

    public static double Dot(Vec3LeftHandedGeocentric a, Vec3LeftHandedGeocentric b)
    {
      return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    public bool EqualsEpsilon(Vec3LeftHandedGeocentric other, double epsilon = double.Epsilon)
    {
      return
        X.EqualsEpsilon(other.X, epsilon)
        && Y.EqualsEpsilon(other.Y, epsilon)
        && Z.EqualsEpsilon(other.Z, epsilon);
    }

    public double MagnitudeSquared
    {
      get { return X * X + Y * Y + Z * Z; }
    }

    public double Magnitude
    {
      get { return System.Math.Sqrt(MagnitudeSquared); }
    }

    public void Negate()
    {
      X = -X;
      Y = -Y;
      Z = -Z;
    }

    public Vec3LeftHandedGeocentric Negated()
    {
      return new Vec3LeftHandedGeocentric(-X, -Y, -Z);
    }

    public void Normalize()
    {
      double magnitude = Magnitude;
      X /= magnitude;
      Y /= magnitude;
      Z /= magnitude;
    }

    public Vec3LeftHandedGeocentric Normalized()
    {
      Vec3LeftHandedGeocentric v = new Vec3LeftHandedGeocentric(X, Y, Z);
      v.Normalize();
      return v;
    }

    public void Set(double x, double y, double z)
    {
      X = x;
      Y = y;
      Z = z;
    }

    public override string ToString()
    {
      return string.Format(CultureInfo.CurrentCulture, "({0}, {1}, {2})", X, Y, Z);
    }

    public string ToString(string numberFormat)
    {
      return string.Format(CultureInfo.CurrentCulture, "({0}, {1}, {2})", X.ToString(numberFormat), Y.ToString(numberFormat), Z.ToString(numberFormat));
    }

    public Vector3d ToVector3d()
    {
      return new Vector3d(X, Y, Z);
    }

    public Vector3f ToVector3f()
    {
      return new Vector3f((float)X, (float)Y, (float)Z);
    }

    public static Vec3LeftHandedGeocentric Zero
    {
      get { return new Vec3LeftHandedGeocentric(0.0, 0.0, 0.0); }
    }

    public static Vec3LeftHandedGeocentric NaN
    {
      get { return new Vec3LeftHandedGeocentric(double.NaN, double.NaN, double.NaN); }
    }

    public static Vec3LeftHandedGeocentric operator -(Vec3LeftHandedGeocentric vector)
    {
      return new Vec3LeftHandedGeocentric(-vector.X, -vector.Y, -vector.Z);
    }

    public static Vec3LeftHandedGeocentric operator +(Vec3LeftHandedGeocentric left, Vec3LeftHandedGeocentric right)
    {
      return new Vec3LeftHandedGeocentric(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    public static Vec3LeftHandedGeocentric operator -(Vec3LeftHandedGeocentric left, Vec3LeftHandedGeocentric right)
    {
      return new Vec3LeftHandedGeocentric(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static Vec3LeftHandedGeocentric operator *(Vec3LeftHandedGeocentric left, double right)
    {
      return new Vec3LeftHandedGeocentric(left.X * right, left.Y * right, left.Z * right);
    }

    public static Vec3LeftHandedGeocentric operator *(double left, Vec3LeftHandedGeocentric right)
    {
      return right * left;
    }

    public static Vec3LeftHandedGeocentric operator /(Vec3LeftHandedGeocentric left, double right)
    {
      return new Vec3LeftHandedGeocentric(left.X / right, left.Y / right, left.Z / right);
    }

    public static bool operator >(Vec3LeftHandedGeocentric left, Vec3LeftHandedGeocentric right)
    {
      return (left.X > right.X) && (left.Y > right.Y) && (left.Z > right.Z);
    }

    public static bool operator >=(Vec3LeftHandedGeocentric left, Vec3LeftHandedGeocentric right)
    {
      return (left.X >= right.X) && (left.Y >= right.Y) && (left.Z >= right.Z);
    }

    public static bool operator <(Vec3LeftHandedGeocentric left, Vec3LeftHandedGeocentric right)
    {
      return (left.X < right.X) && (left.Y < right.Y) && (left.Z < right.Z);
    }

    public static bool operator <=(Vec3LeftHandedGeocentric left, Vec3LeftHandedGeocentric right)
    {
      return (left.X <= right.X) && (left.Y <= right.Y) && (left.Z <= right.Z);
    }

    public bool Equals(Vec3LeftHandedGeocentric other)
    {
      return MathUtilities.IsEqual(X, other.X) && MathUtilities.IsEqual(Y, other.Y) && MathUtilities.IsEqual(Z, other.Z);
    }

    public override bool Equals(object obj)
    {
      if (obj is Vec3LeftHandedGeocentric)
      {
        return Equals((Vec3LeftHandedGeocentric)obj);
      }

      return false;
    }

    public override int GetHashCode()
    {
      return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
    }

    public static bool operator ==(Vec3LeftHandedGeocentric left, Vec3LeftHandedGeocentric right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(Vec3LeftHandedGeocentric left, Vec3LeftHandedGeocentric right)
    {
      return !left.Equals(right);
    }
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

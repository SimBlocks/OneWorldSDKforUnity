//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace sbio.Core.Math
{
  using Math = System.Math;

  [Serializable]
  [StructLayout(LayoutKind.Sequential)]
  public struct Vector3d : IEquatable<Vector3d>
  {
    private double m_X;
    private double m_Y;
    private double m_Z;

    public void Set(double tx, double ty, double tz)
    {
      m_X = tx;
      m_Y = ty;
      m_Z = tz;
    }

    public static Vector3d Zero
    {
      get { return new Vector3d(0.0, 0.0, 0.0); }
    }

    public static Vector3d NaN
    {
      get { return new Vector3d(double.NaN, double.NaN, double.NaN); }
    }

    public static Vector3d PositiveInfinity
    {
      get { return new Vector3d(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity); }
    }

    public static Vector3d NegativeInfinity
    {
      get { return new Vector3d(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity); }
    }

    public static bool IsPositiveInfinity(Vector3d v3d)
    {
      return
        double.IsPositiveInfinity(v3d.m_X)
        && double.IsPositiveInfinity(v3d.m_Y)
        && double.IsPositiveInfinity(v3d.m_Z);
    }

    public static Vector3d Cross(Vector3d a, Vector3d b)
    {
      return new Vector3d(a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);
    }

    public static double Dot(Vector3d a, Vector3d b)
    {
      return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    public static Vector3d Project(Vector3d vector, Vector3d normal)
    {
      return normal * Dot(vector, normal) / Dot(normal, normal);
    }

    public static Vector3d ProjectOnPlane(Vector3d vector, Vector3d planeNormal)
    {
      return vector - Project(vector, planeNormal);
    }

    public static double Angle(Vector3d from, Vector3d to)
    {
      return Math.Acos(from.Normalize().Dot(to.Normalize()));
    }

    public static double SignedAngle(Vector3d from, Vector3d to, Vector3d axis)
    {
      return Angle(from, to) * Math.Sign(Dot(axis, Cross(from, to)));
    }

    public static bool IsNegativeInfinity(Vector3d v3d)
    {
      return
        double.IsNegativeInfinity(v3d.m_X)
        && double.IsNegativeInfinity(v3d.m_Y)
        && double.IsNegativeInfinity(v3d.m_Z);
    }

    public static bool IsNaN(Vector3d v3d)
    {
      return
        double.IsNaN(v3d.m_X)
        && double.IsNaN(v3d.m_Y)
        && double.IsNaN(v3d.m_Z);
    }

    public static explicit operator Vector2d(Vector3d v3d)
    {
      return new Vector2d(v3d.X, v3d.Y);
    }

    public static explicit operator Vector2f(Vector3d v3d)
    {
      return new Vector2f((float)v3d.X, (float)v3d.Y);
    }

    public static explicit operator Vector3f(Vector3d v3d)
    {
      return new Vector3f((float)v3d.X, (float)v3d.Y, (float)v3d.Z);
    }

    public static Vector3d operator -(Vector3d vector)
    {
      return new Vector3d(-vector.X, -vector.Y, -vector.Z);
    }

    public static Vector3d operator +(Vector3d left, Vector3d right)
    {
      return new Vector3d(left.m_X + right.m_X, left.m_Y + right.m_Y, left.m_Z + right.m_Z);
    }

    public static Vector3d operator -(Vector3d left, Vector3d right)
    {
      return new Vector3d(left.m_X - right.m_X, left.m_Y - right.m_Y, left.m_Z - right.m_Z);
    }

    public static Vector3d operator *(Vector3d left, double right)
    {
      return new Vector3d(left.m_X * right, left.m_Y * right, left.m_Z * right);
    }

    public static Vector3d operator *(double left, Vector3d right)
    {
      return right * left;
    }

    public static Vector3d operator /(Vector3d left, double right)
    {
      return new Vector3d(left.m_X / right, left.m_Y / right, left.m_Z / right);
    }

    public static bool operator >(Vector3d left, Vector3d right)
    {
      return (left.X > right.X) && (left.Y > right.Y) && (left.Z > right.Z);
    }

    public static bool operator >=(Vector3d left, Vector3d right)
    {
      return (left.X >= right.X) && (left.Y >= right.Y) && (left.Z >= right.Z);
    }

    public static bool operator <(Vector3d left, Vector3d right)
    {
      return (left.X < right.X) && (left.Y < right.Y) && (left.Z < right.Z);
    }

    public static bool operator <=(Vector3d left, Vector3d right)
    {
      return (left.X <= right.X) && (left.Y <= right.Y) && (left.Z <= right.Z);
    }

    public static bool operator ==(Vector3d left, Vector3d right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(Vector3d left, Vector3d right)
    {
      return !left.Equals(right);
    }

    public static Vector3d Min(Vector3d a, Vector3d b)
    {
      return new Vector3d(
        Math.Min(a.X, b.X)
        , Math.Min(a.Y, b.Y)
        , Math.Min(a.Z, b.Z));
    }

    public static Vector3d Max(Vector3d a, Vector3d b)
    {
      return new Vector3d(
        Math.Max(a.X, b.X)
        , Math.Max(a.Y, b.Y)
        , Math.Max(a.Z, b.Z));
    }

    public double X
    {
      get { return m_X; }
    }

    public double Y
    {
      get { return m_Y; }
    }

    public double Z
    {
      get { return m_Z; }
    }

    public double x
    {
      get { return m_X; }
    }

    public double y
    {
      get { return m_Y; }
    }

    public double z
    {
      get { return m_Z; }
    }

    public Vector2d XY
    {
      get { return new Vector2d(X, Y); }
    }

    public double MagnitudeSquared
    {
      get { return m_X * m_X + m_Y * m_Y + m_Z * m_Z; }
    }

    public double Magnitude
    {
      get { return Math.Sqrt(MagnitudeSquared); }
    }

    public Vector3d Normalize(out double magnitude)
    {
      magnitude = Magnitude;
      return this / magnitude;
    }

    public Vector3d Normalize()
    {
      double magnitude;
      return Normalize(out magnitude);
    }

    public Vector3d Cross(Vector3d other)
    {
      return new Vector3d(Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X);
    }

    public double Dot(Vector3d other)
    {
      return X * other.X + Y * other.Y + Z * other.Z;
    }

    public Vector3d Add(Vector3d addend)
    {
      return this + addend;
    }

    public Vector3d Subtract(Vector3d subtrahend)
    {
      return this - subtrahend;
    }

    public Vector3d Multiply(double scalar)
    {
      return this * scalar;
    }

    public Vector3d MultiplyComponents(Vector3d scale)
    {
      return new Vector3d(X * scale.X, Y * scale.Y, Z * scale.Z);
    }

    public Vector3d Divide(double scalar)
    {
      return this / scalar;
    }

    public double MaximumComponent
    {
      get { return Math.Max(Math.Max(m_X, m_Y), m_Z); }
    }

    public double MinimumComponent
    {
      get { return Math.Min(Math.Min(m_X, m_Y), m_Z); }
    }

    public double AngleBetween(Vector3d other)
    {
      return Math.Acos(Normalize().Dot(other.Normalize()));
    }

    public double SignedAngleBetween(Vector3d other, Vector3d axis)
    {
      return AngleBetween(other) * Math.Sign(Dot(axis, Cross(other)));
    }

    public Vector3d RotateAroundAxis(Vector3d axis, double theta)
    {
      double u = axis.X;
      double v = axis.Y;
      double w = axis.Z;

      double cosTheta = Math.Cos(theta);
      double sinTheta = Math.Sin(theta);

      double ms = axis.MagnitudeSquared;
      double m = Math.Sqrt(ms);

      return new Vector3d(
        ((u * (u * m_X + v * m_Y + w * m_Z)) +
         (((m_X * (v * v + w * w)) - (u * (v * m_Y + w * m_Z))) * cosTheta) +
         (m * ((-w * m_Y) + (v * m_Z)) * sinTheta)) / ms,
        ((v * (u * m_X + v * m_Y + w * m_Z)) +
         (((m_Y * (u * u + w * w)) - (v * (u * m_X + w * m_Z))) * cosTheta) +
         (m * ((w * m_X) - (u * m_Z)) * sinTheta)) / ms,
        ((w * (u * m_X + v * m_Y + w * m_Z)) +
         (((m_Z * (u * u + v * v)) - (w * (u * m_X + v * m_Y))) * cosTheta) +
         (m * (-(v * m_X) + (u * m_Y)) * sinTheta)) / ms);
    }

    public Vector3d Negate()
    {
      return -this;
    }

    public bool EqualsEpsilon(Vector3d other, double epsilon = double.Epsilon)
    {
      return
        m_X.EqualsEpsilon(other.m_X, epsilon)
        && m_Y.EqualsEpsilon(other.m_Y, epsilon)
        && m_Z.EqualsEpsilon(other.m_Z, epsilon);
    }

    public bool Equals(Vector3d other)
    {
      return MathUtilities.IsEqual(m_X, other.m_X) && MathUtilities.IsEqual(m_Y, other.m_Y) && MathUtilities.IsEqual(m_Z, other.m_Z);
    }

    public override bool Equals(object obj)
    {
      if (obj is Vector3d)
      {
        return Equals((Vector3d)obj);
      }

      return false;
    }

    public override string ToString()
    {
      return string.Format(CultureInfo.CurrentCulture, "({0}, {1}, {2})", X, Y, Z);
    }

    public string ToString(string numberFormat)
    {
      return string.Format(CultureInfo.CurrentCulture, "({0}, {1}, {2})", X.ToString(numberFormat), Y.ToString(numberFormat), Z.ToString(numberFormat));
    }

    public override int GetHashCode()
    {
      return m_X.GetHashCode() ^ m_Y.GetHashCode() ^ m_Z.GetHashCode();
    }

    public Vector3d(double x)
      : this(x, 0, 0)
    {
    }

    public Vector3d(double x, double y)
      : this(x, y, 0)
    {
    }

    public Vector3d(double x, double y, double z)
    {
      m_X = x;
      m_Y = y;
      m_Z = z;
    }

    public Vector3d(Vector3f v3f)
      : this(v3f.x, v3f.y, v3f.z)
    {
    }

    public Vector3d(Vector2d v, double z)
      : this(v.x, v.y, z)
    {
    }

    public Vector3d(Vector2f v, double z)
      : this(v.x, v.y, z)
    {
    }
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

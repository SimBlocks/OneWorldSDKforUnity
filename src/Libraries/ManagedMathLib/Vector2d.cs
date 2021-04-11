//Copyright SimBlocks LLC 2021
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
  public struct Vector2d : IEquatable<Vector2d>
  {
    public static Vector2d Zero
    {
      get { return new Vector2d(0.0, 0.0); }
    }

    public static Vector2d Undefined
    {
      get { return new Vector2d(double.NaN, double.NaN); }
    }

    public static explicit operator Vector2f(Vector2d v2d)
    {
      return new Vector2f((float)v2d.X, (float)v2d.Y);
    }

    public static double Dot(Vector2d a, Vector2d b)
    {
      return a.X * b.X + a.Y * b.Y;
    }

    public Vector2d(double x)
      : this(x, 0)
    {
    }

    public Vector2d(double x, double y)
    {
      m_X = x;
      m_Y = y;
    }

    public Vector2d(Vector2f v2f)
      : this(v2f.x, v2f.y)
    {
    }

    public double X
    {
      get { return m_X; }
    }

    public double Y
    {
      get { return m_Y; }
    }

    public double x
    {
      get { return m_X; }
    }

    public double y
    {
      get { return m_Y; }
    }

    public double MagnitudeSquared
    {
      get { return m_X * m_X + m_Y * m_Y; }
    }

    public double Magnitude
    {
      get { return Math.Sqrt(MagnitudeSquared); }
    }

    public bool IsUndefined
    {
      get { return double.IsNaN(m_X) || double.IsNaN(m_Y); }
    }

    public Vector2d Normalize(out double magnitude)
    {
      magnitude = Magnitude;
      return this / magnitude;
    }

    public Vector2d Normalize()
    {
      return this / Magnitude;
    }

    public double Dot(Vector2d other)
    {
      return X * other.X + Y * other.Y;
    }

    public Vector2d Add(Vector2d addend)
    {
      return this + addend;
    }

    public Vector2d Subtract(Vector2d subtrahend)
    {
      return this - subtrahend;
    }

    public Vector2d Multiply(double scalar)
    {
      return this * scalar;
    }

    public Vector2d Divide(double scalar)
    {
      return this / scalar;
    }

    public Vector2d Negate()
    {
      return -this;
    }

    public bool EqualsEpsilon(Vector2d other, double epsilon)
    {
      return
        m_X.EqualsEpsilon(other.m_X, epsilon)
        && m_Y.EqualsEpsilon(other.m_Y, epsilon);
    }

    public bool Equals(Vector2d other)
    {
      return MathUtilities.IsEqual(m_X, other.m_X) && MathUtilities.IsEqual(m_Y, other.m_Y);
    }

    public static Vector2d operator -(Vector2d vector)
    {
      return new Vector2d(-vector.X, -vector.Y);
    }

    public static Vector2d operator +(Vector2d left, Vector2d right)
    {
      return new Vector2d(left.m_X + right.m_X, left.m_Y + right.m_Y);
    }

    public static Vector2d operator -(Vector2d left, Vector2d right)
    {
      return new Vector2d(left.m_X - right.m_X, left.m_Y - right.m_Y);
    }

    public static Vector2d operator *(Vector2d left, double right)
    {
      return new Vector2d(left.m_X * right, left.m_Y * right);
    }

    public static Vector2d operator *(double left, Vector2d right)
    {
      return right * left;
    }

    public static Vector2d operator /(Vector2d left, double right)
    {
      return new Vector2d(left.m_X / right, left.m_Y / right);
    }

    public static Vector2d operator /(Vector2d left, Vector2d right)
    {
      return new Vector2d(left.m_X / right.m_X, left.m_Y / right.m_Y);
    }

    public static bool operator ==(Vector2d left, Vector2d right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(Vector2d left, Vector2d right)
    {
      return !left.Equals(right);
    }

    public override bool Equals(object obj)
    {
      if (obj is Vector2d)
      {
        return Equals((Vector2d)obj);
      }

      return false;
    }

    public override string ToString()
    {
      return string.Format(CultureInfo.CurrentCulture, "({0}, {1})", X, Y);
    }

    public override int GetHashCode()
    {
      return m_X.GetHashCode() ^ m_Y.GetHashCode();
    }

    private readonly double m_X;
    private readonly double m_Y;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

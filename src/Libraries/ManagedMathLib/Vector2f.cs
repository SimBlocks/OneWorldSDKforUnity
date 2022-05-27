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
  public struct Vector2f : IEquatable<Vector2f>
  {
    public static Vector2f Zero
    {
      get { return new Vector2f(0.0f, 0.0f); }
    }

    public static Vector2f Undefined
    {
      get { return new Vector2f(float.NaN, float.NaN); }
    }

    public static implicit operator Vector2d(Vector2f v2f)
    {
      return new Vector2d(v2f.X, v2f.Y);
    }

    public static float Dot(Vector2f a, Vector2f b)
    {
      return a.X * b.X + a.Y * b.Y;
    }

    public Vector2f(float x)
      : this(x, 0)
    {
    }

    public Vector2f(float x, float y)
    {
      m_X = x;
      m_Y = y;
    }

    public float X
    {
      get { return m_X; }
    }

    public float Y
    {
      get { return m_Y; }
    }

    public float x
    {
      get { return m_X; }
    }

    public float y
    {
      get { return m_Y; }
    }

    public float MagnitudeSquared
    {
      get { return m_X * m_X + m_Y * m_Y; }
    }

    public float Magnitude
    {
      get { return (float)Math.Sqrt(MagnitudeSquared); }
    }

    public bool IsUndefined
    {
      get { return float.IsNaN(m_X) || float.IsNaN(m_Y); }
    }

    public Vector2f Normalize(out float magnitude)
    {
      magnitude = Magnitude;
      return this / magnitude;
    }

    public Vector2f Normalize()
    {
      return this / Magnitude;
    }

    public float Dot(Vector2f other)
    {
      return X * other.X + Y * other.Y;
    }

    public Vector2f Add(Vector2f addend)
    {
      return this + addend;
    }

    public Vector2f Subtract(Vector2f subtrahend)
    {
      return this - subtrahend;
    }

    public Vector2f Multiply(float scalar)
    {
      return this * scalar;
    }

    public Vector2f Divide(float scalar)
    {
      return this / scalar;
    }

    public Vector2f Negate()
    {
      return -this;
    }

    public bool EqualsEpsilon(Vector2f other, float epsilon)
    {
      return
        m_X.EqualsEpsilon(other.m_X, epsilon)
        && m_Y.EqualsEpsilon(other.m_Y, epsilon);
    }

    public bool Equals(Vector2f other)
    {
      return MathUtilities.IsEqual(m_X, other.m_X) && MathUtilities.IsEqual(m_Y, other.m_Y);
    }

    public static Vector2f operator -(Vector2f vector)
    {
      return new Vector2f(-vector.X, -vector.Y);
    }

    public static Vector2f operator +(Vector2f left, Vector2f right)
    {
      return new Vector2f(left.m_X + right.m_X, left.m_Y + right.m_Y);
    }

    public static Vector2f operator -(Vector2f left, Vector2f right)
    {
      return new Vector2f(left.m_X - right.m_X, left.m_Y - right.m_Y);
    }

    public static Vector2f operator *(Vector2f left, float right)
    {
      return new Vector2f(left.m_X * right, left.m_Y * right);
    }

    public static Vector2f operator *(float left, Vector2f right)
    {
      return new Vector2f(right.m_X * left, right.m_Y * left);
    }

    public static Vector2d operator *(Vector2f left, double right)
    {
      return new Vector2d(left.m_X * right, left.m_Y * right);
    }

    public static Vector2d operator *(double left, Vector2f right)
    {
      return new Vector2d(right.m_X * left, right.m_Y * left);
    }

    public static Vector2f operator /(Vector2f left, float right)
    {
      return new Vector2f(left.m_X / right, left.m_Y / right);
    }

    public static Vector2f operator /(Vector2f left, Vector2f right)
    {
      return new Vector2f(left.m_X / right.m_X, left.m_Y / right.m_Y);
    }

    public static Vector2d operator /(Vector2f left, double right)
    {
      return new Vector2d(left.m_X / right, left.m_Y / right);
    }

    public static bool operator ==(Vector2f left, Vector2f right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(Vector2f left, Vector2f right)
    {
      return !left.Equals(right);
    }

    public override bool Equals(object obj)
    {
      if (obj is Vector2f)
      {
        return Equals((Vector2f)obj);
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

    private readonly float m_X;
    private readonly float m_Y;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

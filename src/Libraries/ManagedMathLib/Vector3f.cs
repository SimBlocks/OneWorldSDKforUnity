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
  public struct Vector3f : IEquatable<Vector3f>
  {
    public static Vector3f Zero
    {
      get { return new Vector3f(0.0f, 0.0f, 0.0f); }
    }

    public static Vector3f NaN
    {
      get { return new Vector3f(float.NaN, float.NaN, float.NaN); }
    }

    public static explicit operator Vector2f(Vector3f v3f)
    {
      return new Vector2f(v3f.X, v3f.Y);
    }

    public static explicit operator Vector2d(Vector3f v3f)
    {
      return new Vector2d(v3f.X, v3f.Y);
    }

    public static implicit operator Vector3d(Vector3f v3f)
    {
      return new Vector3d(v3f.X, v3f.Y, v3f.Z);
    }

    public static Vector3f Cross(Vector3f a, Vector3f b)
    {
      return new Vector3f(a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);
    }

    public static float Dot(Vector3f a, Vector3f b)
    {
      return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    public static Vector3f Project(Vector3f vector, Vector3f normal)
    {
      return normal * Dot(vector, normal) / Dot(normal, normal);
    }

    public static Vector3f ProjectOnPlane(Vector3f vector, Vector3f planeNormal)
    {
      return vector - Project(vector, planeNormal);
    }

    public static float Angle(Vector3f from, Vector3f to)
    {
      return (float)Math.Acos(from.Normalize().Dot(to.Normalize()));
    }

    public static float SignedAngle(Vector3f from, Vector3f to, Vector3f axis)
    {
      return Angle(from, to) * Math.Sign(Dot(axis, Cross(from, to)));
    }

    public static Vector3f operator -(Vector3f vector)
    {
      return new Vector3f(-vector.X, -vector.Y, -vector.Z);
    }

    public static Vector3f operator +(Vector3f left, Vector3f right)
    {
      return new Vector3f(left.m_X + right.m_X, left.m_Y + right.m_Y, left.m_Z + right.m_Z);
    }

    public static Vector3f operator -(Vector3f left, Vector3f right)
    {
      return new Vector3f(left.m_X - right.m_X, left.m_Y - right.m_Y, left.m_Z - right.m_Z);
    }

    public static Vector3f operator *(Vector3f left, float right)
    {
      return new Vector3f(left.m_X * right, left.m_Y * right, left.m_Z * right);
    }

    public static Vector3f operator *(float left, Vector3f right)
    {
      return right * left;
    }

    public static Vector3f operator /(Vector3f left, float right)
    {
      return new Vector3f(left.m_X / right, left.m_Y / right, left.m_Z / right);
    }

    public static bool operator >(Vector3f left, Vector3f right)
    {
      return (left.X > right.X) && (left.Y > right.Y) && (left.Z > right.Z);
    }

    public static bool operator >=(Vector3f left, Vector3f right)
    {
      return (left.X >= right.X) && (left.Y >= right.Y) && (left.Z >= right.Z);
    }

    public static bool operator <(Vector3f left, Vector3f right)
    {
      return (left.X < right.X) && (left.Y < right.Y) && (left.Z < right.Z);
    }

    public static bool operator <=(Vector3f left, Vector3f right)
    {
      return (left.X <= right.X) && (left.Y <= right.Y) && (left.Z <= right.Z);
    }

    public static bool operator ==(Vector3f left, Vector3f right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(Vector3f left, Vector3f right)
    {
      return !left.Equals(right);
    }

    public static Vector3f Min(Vector3f a, Vector3f b)
    {
      return new Vector3f(
        Math.Min(a.X, b.X)
        , Math.Min(a.Y, b.Y)
        , Math.Min(a.Z, b.Z));
    }

    public static Vector3f Max(Vector3f a, Vector3f b)
    {
      return new Vector3f(
        Math.Max(a.X, b.X)
        , Math.Max(a.Y, b.Y)
        , Math.Max(a.Z, b.Z));
    }

    public float X
    {
      get { return m_X; }
    }

    public float Y
    {
      get { return m_Y; }
    }

    public float Z
    {
      get { return m_Z; }
    }

    public float x
    {
      get { return m_X; }
    }

    public float y
    {
      get { return m_Y; }
    }

    public float z
    {
      get { return m_Z; }
    }

    public Vector2d XY
    {
      get { return new Vector2d(X, Y); }
    }

    public float MagnitudeSquared
    {
      get { return m_X * m_X + m_Y * m_Y + m_Z * m_Z; }
    }

    public float Magnitude
    {
      get { return (float)Math.Sqrt(MagnitudeSquared); }
    }

    public Vector3f Normalize(out float magnitude)
    {
      magnitude = Magnitude;
      return this / magnitude;
    }

    public Vector3f Normalize()
    {
      float magnitude;
      return Normalize(out magnitude);
    }

    public Vector3f Cross(Vector3f other)
    {
      return new Vector3f(Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X);
    }

    public float Dot(Vector3f other)
    {
      return X * other.X + Y * other.Y + Z * other.Z;
    }

    public Vector3f Add(Vector3f addend)
    {
      return this + addend;
    }

    public Vector3f Subtract(Vector3f subtrahend)
    {
      return this - subtrahend;
    }

    public Vector3f Multiply(float scalar)
    {
      return this * scalar;
    }

    public Vector3f MultiplyComponents(Vector3f scale)
    {
      return new Vector3f(X * scale.X, Y * scale.Y, Z * scale.Z);
    }

    public Vector3f Divide(float scalar)
    {
      return this / scalar;
    }

    public float MaximumComponent
    {
      get { return Math.Max(Math.Max(m_X, m_Y), m_Z); }
    }

    public float MinimumComponent
    {
      get { return Math.Min(Math.Min(m_X, m_Y), m_Z); }
    }

    public float AngleBetween(Vector3f other)
    {
      return (float)Math.Acos(Normalize().Dot(other.Normalize()));
    }

    public float SignedAngleBetween(Vector3f other, Vector3f axis)
    {
      return AngleBetween(other) * Math.Sign(Dot(axis, Cross(other)));
    }

    public Vector3f RotateAroundAxis(Vector3f axis, float theta)
    {
      float u = axis.X;
      float v = axis.Y;
      float w = axis.Z;

      float cosTheta = (float)Math.Cos(theta);
      float sinTheta = (float)Math.Sin(theta);

      float ms = axis.MagnitudeSquared;
      float m = (float)Math.Sqrt(ms);

      return new Vector3f(
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

    public Vector3f Negate()
    {
      return -this;
    }

    public bool EqualsEpsilon(Vector3f other, float epsilon = float.Epsilon)
    {
      return
        m_X.EqualsEpsilon(other.m_X, epsilon)
        && m_Y.EqualsEpsilon(other.m_Y, epsilon)
        && m_Z.EqualsEpsilon(other.m_Z, epsilon);
    }

    public bool Equals(Vector3f other)
    {
      return MathUtilities.IsEqual(m_X, other.m_X) && MathUtilities.IsEqual(m_Y, other.m_Y) && MathUtilities.IsEqual(m_Z, other.m_Z);
    }

    public override bool Equals(object obj)
    {
      if (obj is Vector3f)
      {
        return Equals((Vector3f)obj);
      }

      return false;
    }

    public override string ToString()
    {
      return string.Format(CultureInfo.CurrentCulture, "({0}, {1}, {2})", X, Y, Z);
    }

    public override int GetHashCode()
    {
      return m_X.GetHashCode() ^ m_Y.GetHashCode() ^ m_Z.GetHashCode();
    }

    public Vector3f(float x)
      : this(x, 0, 0)
    {
    }

    public Vector3f(float x, float y)
      : this(x, y, 0)
    {
    }

    public Vector3f(float x, float y, float z)
    {
      m_X = x;
      m_Y = y;
      m_Z = z;
    }

    public Vector3f(Vector2f v, float z)
      : this(v.x, v.y, z)
    {
    }

    private readonly float m_X;
    private readonly float m_Y;
    private readonly float m_Z;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

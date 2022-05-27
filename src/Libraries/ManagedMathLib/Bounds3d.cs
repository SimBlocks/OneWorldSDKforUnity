//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Linq;

namespace sbio.Core.Math
{
  [Serializable]
  public struct Bounds3d : IEquatable<Bounds3d>
  {
    public static bool operator ==(Bounds3d lhs, Bounds3d rhs)
    {
      return lhs.Equals(rhs);
    }

    public static bool operator !=(Bounds3d lhs, Bounds3d rhs)
    {
      return !(lhs == rhs);
    }

    public static Bounds3d FromPoints(params Vector3d[] points)
    {
      var len = points.Length;
      if (len == 0)
      {
        return new Bounds3d(Vector3d.NaN, Vector3d.NaN);
      }


      var first = points[0];
      var min = first;
      var max = first;
      for (var i = 1; i < len; ++i)
      {
        var p = points[i];
        min = Vector3d.Min(min, p);
        max = Vector3d.Max(max, p);
      }

      var maxMinusMin = max - min;
      return new Bounds3d((maxMinusMin * 0.5) + min, maxMinusMin);
    }

    public static Bounds3d FromPoints(IEnumerable<Vector3d> points)
    {
      if (!points.Any())
      {
        return new Bounds3d(Vector3d.NaN, Vector3d.NaN);
      }

      var first = points.First();
      var min = first;
      var max = first;
      foreach (var p in points.Skip(1))
      {
        min = Vector3d.Min(min, p);
        max = Vector3d.Max(max, p);
      }

      var maxMinusMin = max - min;
      return new Bounds3d((maxMinusMin * 0.5) + min, maxMinusMin);
    }

    public static Bounds3d FromPoints(IList<Vector3d> points)
    {
      var len = points.Count;
      if (len == 0)
      {
        return new Bounds3d(Vector3d.NaN, Vector3d.NaN);
      }

      var first = points[0];
      var min = first;
      var max = first;
      for (var i = 1; i < len; ++i)
      {
        var p = points[i];
        min = Vector3d.Min(min, p);
        max = Vector3d.Max(max, p);
      }

      var maxMinusMin = max - min;
      return new Bounds3d((maxMinusMin * 0.5) + min, maxMinusMin);
    }

    public Vector3d Center
    {
      get { return m_Center; }
    }

    public Vector3d Size
    {
      get { return m_Extents * 2; }
    }

    public Vector3d Extents
    {
      get { return m_Extents; }
    }

    public Vector3d Min
    {
      get { return m_Center - m_Extents; }
    }

    public Vector3d Max
    {
      get { return m_Center + m_Extents; }
    }

    public bool Equals(Bounds3d other)
    {
      return m_Center.Equals(other.m_Center) && m_Extents.Equals(other.m_Extents);
    }

    public override bool Equals(object other)
    {
      if (other is Bounds3d)
      {
        return Equals((Bounds3d)other);
      }

      return false;
    }

    public override int GetHashCode()
    {
      return m_Center.GetHashCode() ^ m_Extents.GetHashCode();
    }

    public Bounds3d(Vector3d center)
    {
      m_Center = center;
      m_Extents = Vector3d.Zero;
    }

    public Bounds3d(Vector3d center, Vector3d size)
    {
      m_Center = center;
      m_Extents = size * 0.5;
    }

    public Bounds3d Encapsulate(Vector3d point)
    {
      var min = Vector3d.Min(Min, point);
      var max = Vector3d.Max(Max, point);

      return new Bounds3d(min + Extents, max - min);
    }

    public Bounds3d Encapsulate(Bounds3d bounds)
    {
      var p1 = bounds.m_Center - bounds.m_Extents;
      var p2 = bounds.m_Center - bounds.m_Extents;

      var min = Vector3d.Min(Vector3d.Min(Min, p1), p2);
      var max = Vector3d.Max(Vector3d.Max(Max, p1), p2);

      return new Bounds3d(min + Extents, max - min);
    }

    public Bounds3d Expand(double amount)
    {
      var size = Size;
      return new Bounds3d(m_Center, new Vector3d(size.X + amount, size.Y + amount, size.Z + amount));
    }

    public Bounds3d Expand(Vector3d amount)
    {
      return new Bounds3d(m_Center, Size + amount);
    }

    public override string ToString()
    {
      return string.Format("Center: {0}, Extents: {1}", m_Center, m_Extents);
    }

    private readonly Vector3d m_Center;
    private readonly Vector3d m_Extents;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

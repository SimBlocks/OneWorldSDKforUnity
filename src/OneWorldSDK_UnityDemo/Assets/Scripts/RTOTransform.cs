//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.Core.Math;
using UnityEngine;
using sbio.owsdk.Unity.Extensions;

namespace sbio.owsdk.Unity
{
  public class RTOTransform : IDisposable
  {
    public Transform Transform
    {
      get { return m_Transform; }
    }

    public Vec3LeftHandedGeocentric GlobalPosition
    {
      get { return m_GlobalPosition; }
      set
      {
        m_GlobalPosition = value;
        m_Transform.position = (value - m_LastWorldOrigin).ToVector3();
      }
    }

    public Vector3 GlobalPositionf
    {
      get { return GlobalPosition.ToVector3(); }
      set { GlobalPosition = value.ToVec3LeftHandedGeocentric(); }
    }

    public Vec3LeftHandedGeocentric RTOPosition
    {
      get { return (GlobalPosition - m_LastWorldOrigin); }
      set
      {
        var diff = value - RTOPosition;
        GlobalPosition += diff;
      }
    }

    public Vector3 RTOPositionf
    {
      get { return (GlobalPosition - m_LastWorldOrigin).ToVector3(); }
      set { RTOPosition = value.ToVec3LeftHandedGeocentric(); }
    }

    public void Dispose()
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      m_Disposed = true;

      m_RTOContext.WorldOriginChanged -= UpdateWorldOrigin;
      UpdateWorldOrigin(Vec3LeftHandedGeocentric.Zero);
    }

    /// <summary>
    /// Construct an RTOTransform for a transform
    /// </summary>
    /// <param name="rtoContext">The RTO Context</param>
    /// <param name="transform">The transform to keep updated</param>
    public RTOTransform(IRTOContext rtoContext, Transform transform)
      : this(rtoContext, transform, transform.position.ToVec3LeftHandedGeocentric())
    {
    }

    /// <summary>
    /// Construct an RTOTransform for a transform that is at the given position
    /// </summary>
    /// <param name="rtoContext"></param>
    /// <param name="transform">The transform to keep updated</param>
    /// <param name="position"></param>
    public RTOTransform(IRTOContext rtoContext, Transform transform, Vec3LeftHandedGeocentric position)
    {
      m_RTOContext = rtoContext;
      m_Transform = transform;
      m_GlobalPosition = position;

      UpdateWorldOrigin(rtoContext.WorldOrigin);
      m_RTOContext.WorldOriginChanged += UpdateWorldOrigin;
    }

    private void UpdateWorldOrigin(Vec3LeftHandedGeocentric newWorldOrigin)
    {
      if (m_Transform != null)
      {
        m_Transform.position = (GlobalPosition - newWorldOrigin).ToVector3();
        m_LastWorldOrigin = newWorldOrigin;
      }
    }

    private readonly IRTOContext m_RTOContext;
    private readonly Transform m_Transform;

    private bool m_Disposed;

    private Vec3LeftHandedGeocentric m_GlobalPosition;
    private Vec3LeftHandedGeocentric m_LastWorldOrigin;
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

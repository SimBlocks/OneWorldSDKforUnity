//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.Core.Math;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Services;
using sbio.owsdk.Unity.Events;
using UnityEngine;

namespace sbio.owsdk.Unity
{
  [CreateAssetMenu(menuName = "OWSDK/World Context", order = 110)]
  public class WorldContext : ScriptableObject
    , IWorldContext
  {
    #region ScriptableObject

    public Vector3dEvent WorldOriginChanged;

    private void OnEnable()
    {
      m_Ellipsoid = new Ellipsoid(m_RadiusX, m_RadiusY, m_RadiusZ);
      m_WorldOrigin = Vec3LeftHandedGeocentric.Zero;
    }

    private void OnDisable()
    {
      m_WorldOrigin = Vec3LeftHandedGeocentric.Zero;
      m_Ellipsoid = null;
    }

    #endregion

    event Action<Vec3LeftHandedGeocentric> IRTOContext.WorldOriginChanged
    {
      add { WorldOriginChanged.Event += value; }
      remove { WorldOriginChanged.Event -= value; }
    }

    public event Action<IElevationProvider> ElevationProviderChanged;
    public event Action<IFeatureProvider> FeatureProviderChanged;

    public Ellipsoid Ellipsoid
    {
      get { return m_Ellipsoid; }
    }

    public Vec3LeftHandedGeocentric WorldOrigin
    {
      get { return m_WorldOrigin; }
      set
      {
        if (value != m_WorldOrigin)
        {
          m_WorldOrigin = value;
          WorldOriginChanged?.Raise(value);
        }
      }
    }

    public IElevationProvider ElevationProvider
    {
      get { return m_ElevationProvider; }
      set
      {
        if (value != ElevationProvider)
        {
          m_ElevationProvider = value;
          ElevationProviderChanged?.Invoke(value);
        }
      }
    }

    public IFeatureProvider FeatureProvider
    {
      get { return m_FeatureProvider; }
      set
      {
        if (value != m_FeatureProvider)
        {
          m_FeatureProvider = value;
          FeatureProviderChanged?.Invoke(value);
        }
      }
    }

    [SerializeField] private double m_RadiusX = 6378137.0;
    [SerializeField] private double m_RadiusY = 6356752.314245;
    [SerializeField] private double m_RadiusZ = 6378137.0;

    [NonSerialized] private IElevationProvider m_ElevationProvider;
    [NonSerialized] private IFeatureProvider m_FeatureProvider;

    [NonSerialized] private Vec3LeftHandedGeocentric m_WorldOrigin;
    [NonSerialized] private Ellipsoid m_Ellipsoid;
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.Core.Math;
using sbio.owsdk.Geodetic;
using UnityEngine;

namespace sbio.owsdk.Unity
{
  [Serializable]
  public struct SerializedGeodetic3d
  {
    public Geodetic3d Value
    {
      get { return this; }
    }

    public double LatitudeRadians
    {
      get { return NumUtil.DegreesToRadians(m_Latitude); }
    }

    public double LatitudeDegrees
    {
      get { return m_Latitude; }
    }

    public double LongitudeRadians
    {
      get { return NumUtil.DegreesToRadians(m_Longitude); }
    }

    public double LongitudeDegrees
    {
      get { return m_Longitude; }
    }

    public double HeightMeters
    {
      get { return m_Height; }
    }

    public SerializedGeodetic3d(Geodetic3d value)
    {
      m_Latitude = value.LatitudeDegrees;
      m_Longitude = value.LongitudeDegrees;
      m_Height = value.HeightMeters;
    }

    public SerializedGeodetic3d(double latitude, double longitude, double heightMeters)
    {
      m_Latitude = latitude;
      m_Longitude = longitude;
      m_Height = heightMeters;
    }

    public static implicit operator Geodetic3d(SerializedGeodetic3d obj)
    {
      return Geodetic3d.FromDegrees(obj.m_Latitude, obj.m_Longitude, obj.m_Height);
    }

    public static implicit operator SerializedGeodetic3d(Geodetic3d obj)
    {
      return new SerializedGeodetic3d(obj);
    }

    public static explicit operator Geodetic2d(SerializedGeodetic3d obj)
    {
      return Geodetic2d.FromDegrees(obj.m_Latitude, obj.m_Longitude);
    }

    public static implicit operator SerializedGeodetic3d(Geodetic2d obj)
    {
      return new SerializedGeodetic3d(obj);
    }

    [Range(-85, 85)] [SerializeField] private double m_Latitude;
    [Range(-180, 180)] [SerializeField] private double m_Longitude;
    [SerializeField] private double m_Height;
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

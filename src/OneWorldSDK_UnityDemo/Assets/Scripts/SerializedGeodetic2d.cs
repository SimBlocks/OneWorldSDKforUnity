//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.Core.Math;
using sbio.owsdk.Geodetic;
using UnityEngine;

namespace sbio.owsdk.Unity
{
  [Serializable]
  public struct SerializedGeodetic2d
  {
    public Geodetic2d Value
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

    public SerializedGeodetic2d(Geodetic2d value)
    {
      m_Latitude = value.LatitudeDegrees;
      m_Longitude = value.LongitudeDegrees;
    }

    public static explicit operator Geodetic3d(SerializedGeodetic2d obj)
    {
      return Geodetic3d.FromDegrees(obj.m_Latitude, obj.m_Longitude, 0);
    }

    public static implicit operator SerializedGeodetic2d(Geodetic3d obj)
    {
      return new SerializedGeodetic2d((Geodetic2d)obj);
    }

    public static implicit operator Geodetic2d(SerializedGeodetic2d obj)
    {
      return Geodetic2d.FromDegrees(obj.m_Latitude, obj.m_Longitude);
    }

    public static implicit operator SerializedGeodetic2d(Geodetic2d obj)
    {
      return new SerializedGeodetic2d(obj);
    }

    [Range(-90, 90)] [SerializeField] private double m_Latitude;
    [Range(-180, 180)] [SerializeField] private double m_Longitude;
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

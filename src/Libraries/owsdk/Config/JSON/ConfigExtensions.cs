//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.owsdk.Config;
using sbio.owsdk.Geodetic;

namespace sbio.owsdk.Config.JSON
{
  public static class ConfigExtensions
  {
    /// <summary>
    /// Coerce to Geodetic2d value.
    /// Arrays are coerced as [lat, lon]
    /// Objects are coerced as { "lat": lat, "lon": lon }
    /// Otherwise returns 0,0
    /// </summary>
    /// <returns>The coerced value</returns>
    public static Geodetic2d Geodetic2dVal(this IConfigValue val)
    {
      return val.Geodetic2dVal(Geodetic2d.FromRadians(0, 0));
    }

    /// <summary>
    /// Coerce to Geodetic2d value, using a default value
    /// Arrays are coerced as [lat, lon]
    /// Objects are coerced as { "lat": lat, "lon": lon }
    /// Otherwise returns the default
    /// </summary>
    /// <returns>The coerced value</returns>
    public static Geodetic2d Geodetic2dVal(this IConfigValue val, Geodetic2d defaultVal)
    {
      switch (val.ValueType)
      {
        case ConfigValueType.Array:
        {
          var aryVal = val.ArrayValue;
          if (aryVal.Length >= 2)
          {
            return Geodetic2d.FromDegrees(aryVal[0].NumberValue, aryVal[1].NumberValue);
          }
          else if (aryVal.Length == 1)
          {
            return Geodetic2d.FromDegrees(aryVal[0].NumberValue, defaultVal.LongitudeDegrees);
          }
          else
          {
            return defaultVal;
          }
        }
        case ConfigValueType.Object:
        {
          var objVal = val.ObjectValue;
          double lat;
          if (!objVal.TryGetNumber("lat", out lat))
          {
            lat = defaultVal.LatitudeDegrees;
          }

          double lon;
          if (!objVal.TryGetNumber("lon", out lon))
          {
            lon = defaultVal.LongitudeDegrees;
          }

          return Geodetic2d.FromDegrees(lat, lon);
        }
        default: return defaultVal;
      }
    }

    /// <summary>
    /// Get the Geodetic2d value at path, or 0,0
    /// </summary>
    public static Geodetic2d GetGeodetic2d(this IConfigObject val, string path)
    {
      return val.GetGeodetic2d(path, Geodetic2d.FromRadians(0, 0));
    }

    /// <summary>
    /// Get the Geodetic2d value at path, or a default.
    /// </summary>
    public static Geodetic2d GetGeodetic2d(this IConfigObject obj, string path, Geodetic2d defaultVal)
    {
      IConfigValue val;
      if (!obj.TryGetValue(path, out val))
      {
        return defaultVal;
      }
      else
      {
        return val.Geodetic2dVal(defaultVal);
      }
    }

    /// <summary>
    /// Coerce to Geodetic3d value.
    /// Arrays are coerced as [lat, lon, height]
    /// Objects are coerced as { "lat": lat, "lon": lon, "height": height }
    /// Otherwise returns 0, 0, 0
    /// </summary>
    /// <returns>The coerced value</returns>
    public static Geodetic3d Geodetic3dVal(this IConfigValue val)
    {
      return val.Geodetic3dVal(Geodetic3d.FromRadians(0, 0, 0));
    }

    /// <summary>
    /// Coerce to Geodetic3d value, using a default
    /// Arrays are coerced as [lat, lon, height]
    /// Objects are coerced as { "lat": lat, "lon": lon, "height": height }
    /// Otherwise returns the default
    /// </summary>
    /// <returns>The coerced value</returns>
    public static Geodetic3d Geodetic3dVal(this IConfigValue val, Geodetic3d defaultVal)
    {
      switch (val.ValueType)
      {
        case ConfigValueType.Array:
        {
          var aryVal = val.ArrayValue;
          var height = aryVal.Length >= 3 ? aryVal[2].NumberValue : defaultVal.HeightMeters;
          var lon = aryVal.Length >= 2 ? aryVal[1].NumberValue : defaultVal.LongitudeDegrees;
          var lat = aryVal.Length >= 1 ? aryVal[0].NumberValue : defaultVal.LatitudeDegrees;
          return Geodetic3d.FromDegrees(lat, lon, height);
        }
        case ConfigValueType.Object:
        {
          var objVal = val.ObjectValue;
          double lat;
          if (!objVal.TryGetNumber("lat", out lat))
          {
            lat = defaultVal.LatitudeDegrees;
          }

          double lon;
          if (!objVal.TryGetNumber("lon", out lon))
          {
            lon = defaultVal.LongitudeDegrees;
          }

          double height;
          if (!objVal.TryGetNumber("height", out height))
          {
            height = defaultVal.HeightMeters;
          }

          return Geodetic3d.FromDegrees(lat, lon, height);
        }
        default: return defaultVal;
      }
    }

    /// <summary>
    /// Get the Geodetic3d value at path, or 0,0
    /// </summary>
    public static Geodetic3d GetGeodetic3d(this IConfigObject val, string path)
    {
      return val.GetGeodetic3d(path, Geodetic3d.FromRadians(0, 0, 0));
    }

    /// <summary>
    /// Get the Geodetic3d value at path, or a default.
    /// </summary>
    public static Geodetic3d GetGeodetic3d(this IConfigObject obj, string path, Geodetic3d defaultVal)
    {
      IConfigValue val;
      if (!obj.TryGetValue(path, out val))
      {
        return defaultVal;
      }
      else
      {
        return val.Geodetic3dVal(defaultVal);
      }
    }
  }
}



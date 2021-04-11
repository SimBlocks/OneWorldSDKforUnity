//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using sbio.owsdk.Geodetic;

namespace sbio.owsdk.Features
{
  public static class FeatureUtil
  {
    public const string IgnoreTagID = "<-ignore->";
    public const string IgnoreTagValue = "<-ignore->";
    public static FeatureTag IgnoreTag = new FeatureTag(IgnoreTagID, IgnoreTagValue);

    /// NOTE this method exists due to special processing of all buildings parts of the
    /// one relation. In general, we want to have ability to render complex building
    /// as one game object. If area/relation is already part of relation then we
    /// should avoid processing it as independent element. We cannot just delete
    /// element from store as it might be a part of other relation.
    public static bool HasIgnoreTag(this Feature feature)
    {
      return feature.HasTag(IgnoreTagID);
    }

    public static bool HasTag(this Feature feature, string tagName)
    {
      return feature.Tags.ContainsKey(tagName);
    }

    public static bool HasTag(this Feature feature, string tagName, string tagValue)
    {
      FeatureTag tmp;

      if (feature.Tags.TryGetValue(tagName, out tmp))
      {
        return tmp.Value == tagValue;
      }
      else
      {
        return false;
      }
    }

    public static bool HasTag(this Feature feature, FeatureTag tag)
    {
      FeatureTag tmp;
      if (feature.Tags.TryGetValue(tag.ID, out tmp))
      {
        return tmp.Value == tag.Value;
      }
      else
      {
        return false;
      }
    }

    public static string TagValue(this Feature feature, string tagName)
    {
      FeatureTag tmp;
      if (feature.Tags.TryGetValue(tagName, out tmp))
      {
        return tmp.Value;
      }
      else
      {
        return null;
      }
    }

    public static int TagIntValue(this Feature feature, string tagName, int defaultValue = 0)
    {
      FeatureTag tmp;
      int ret;
      if (feature.Tags.TryGetValue(tagName, out tmp) && int.TryParse(tmp.Value, out ret))
      {
        return ret;
      }
      else
      {
        return defaultValue;
      }
    }

    public static float TagFloatValue(this Feature feature, string tagName, float defaultValue = 0.0f)
    {
      FeatureTag tmp;
      float ret;
      if (feature.Tags.TryGetValue(tagName, out tmp) && float.TryParse(tmp.Value, out ret))
      {
        return ret;
      }
      else
      {
        return defaultValue;
      }
    }

    public static double TagDoubleValue(this Feature feature, string tagName, double defaultValue = 0.0)
    {
      FeatureTag tmp;
      double ret;
      if (feature.Tags.TryGetValue(tagName, out tmp) && double.TryParse(tmp.Value, out ret))
      {
        return ret;
      }
      else
      {
        return defaultValue;
      }
    }

    public static float TagMetersValueFloat(this Feature feature, string tagName, float defaultValue = 0.0f)
    {
      FeatureTag tmp;
      if (feature.Tags.TryGetValue(tagName, out tmp))
      {
        var match = sc_LengthCoordRegex.Match(tmp.Value);
        if (match.Success)
        {
          float multiplier;
          var units = match.Groups[2].Value;
          switch (units.ToLowerInvariant())
          {
            case "m":
              multiplier = 1.0f;
              break;
            case "km":
              multiplier = 1000.0f;
              break;
            case "cm":
              multiplier = 1.0f / 100.0f;
              break;
            case "ft":
              multiplier = 0.3048f;
              break;
            default:
              multiplier = 1.0f;
              break;
          }

          return float.Parse(match.Groups[1].Value) * multiplier;
        }
        else
        {
          return defaultValue;
        }
      }
      else
      {
        return defaultValue;
      }
    }

    public static double TagMetersValueDouble(this Feature feature, string tagName, double defaultValue = 0.0)
    {
      FeatureTag tmp;
      if (feature.Tags.TryGetValue(tagName, out tmp))
      {
        var match = sc_LengthCoordRegex.Match(tmp.Value);
        if (match.Success)
        {
          double multiplier;
          var units = match.Groups[2].Value;
          switch (units.ToLowerInvariant())
          {
            case "m":
              multiplier = 1.0;
              break;
            case "km":
              multiplier = 1000.0f;
              break;
            case "cm":
              multiplier = 1.0 / 100.0;
              break;
            case "ft":
              multiplier = 0.3048;
              break;
            default:
              multiplier = 1.0;
              break;
          }

          return double.Parse(match.Groups[1].Value) * multiplier;
        }
        else
        {
          return defaultValue;
        }
      }
      else
      {
        return defaultValue;
      }
    }

    public static bool TryGetTag(this Feature feature, string key, out string tagValue)
    {
      FeatureTag tag;
      if (feature.Tags.TryGetValue(key, out tag))
      {
        tagValue = tag.Value;
        return true;
      }
      else
      {
        tagValue = null;
        return false;
      }
    }

    public static void InsertCoordinates(IReadOnlyList<Geodetic2d> source, List<Geodetic2d> destination, bool isOuter)
    {
      // NOTE we need to remove the last coordinate in area
      int offset = source[0] == source[source.Count - 1] ? 1 : 0;

      bool isClockwise = IsClockwise(source);
      if ((isOuter && !isClockwise) || (!isOuter && isClockwise))
      {
        destination.AddRange(source.Take(source.Count - offset));
      }
      else
      {
        destination.AddRange(source.Reverse().Skip(offset));
      }
    }

    public static bool IsPointInPolygon(Geodetic2d point, IList<Geodetic2d> polygon)
    {
      bool c = false;
      const int begin = 0;
      int end = polygon.Count - 1;

      for (int i = begin, j = end - 1; i != end; j = i++)
      {
        var iCoord = polygon[i];
        var jCoord = polygon[j];

        if (((iCoord.LatitudeDegrees > point.LatitudeDegrees) != (jCoord.LatitudeDegrees > point.LatitudeDegrees)) &&
            (point.LongitudeRadians < (jCoord.LongitudeDegrees - iCoord.LongitudeDegrees) * (point.LatitudeDegrees - iCoord.LatitudeDegrees) /
              (jCoord.LatitudeDegrees - iCoord.LatitudeDegrees) + iCoord.LongitudeDegrees))
        {
          c = !c;
        }
      }

      return c;
    }

    public static bool IsPointInPolygon(Geodetic2d point, Geodetic2d[] polygon)
    {
      bool c = false;
      var begin = 0;
      var end = polygon.Length - 1;

      for (int i = begin, j = end - 1; i != end; j = i++)
      {
        var iCoord = polygon[i];
        var jCoord = polygon[j];

        if (((iCoord.LatitudeDegrees > point.LatitudeDegrees) != (jCoord.LatitudeDegrees > point.LatitudeDegrees)) &&
            (point.LongitudeRadians < (jCoord.LongitudeDegrees - iCoord.LongitudeDegrees) * (point.LatitudeDegrees - iCoord.LatitudeDegrees) /
              (jCoord.LatitudeDegrees - iCoord.LatitudeDegrees) + iCoord.LongitudeDegrees))
        {
          c = !c;
        }
      }

      return c;
    }

    public static double GetArea(IReadOnlyList<Geodetic2d> coordinates)
    {
      var size = coordinates.Count;
      double area = 0.0;
      for (int p = size - 1, q = 0; q < size; p = q++)
      {
        area += coordinates[p].LongitudeDegrees * coordinates[q].LatitudeDegrees - coordinates[q].LongitudeDegrees * coordinates[p].LatitudeDegrees;
      }

      return area;
    }

    public static double GetArea(IList<Geodetic2d> coordinates)
    {
      var size = coordinates.Count;
      double area = 0.0;
      for (int p = size - 1, q = 0; q < size; p = q++)
      {
        area += coordinates[p].LongitudeDegrees * coordinates[q].LatitudeDegrees - coordinates[q].LongitudeDegrees * coordinates[p].LatitudeDegrees;
      }

      return area;
    }

    public static double GetArea(Geodetic2d[] coordinates)
    {
      var size = coordinates.Length;
      double area = 0.0;
      for (int p = size - 1, q = 0; q < size; p = q++)
      {
        area += coordinates[p].LongitudeDegrees * coordinates[q].LatitudeDegrees - coordinates[q].LongitudeDegrees * coordinates[p].LatitudeDegrees;
      }

      return area;
    }

    public static bool IsClockwise(IReadOnlyList<Geodetic2d> coordinates)
    {
      return GetArea(coordinates) < 0;
    }

    public static bool IsClockwise(IList<Geodetic2d> coordinates)
    {
      return GetArea(coordinates) < 0;
    }

    public static bool IsClockwise(Geodetic2d[] coordinates)
    {
      return GetArea(coordinates) < 0;
    }

    private static readonly Regex sc_LengthCoordRegex = new Regex(@"^\s*(\d+\.?\d*)\s*(ft|m|cm|km|)?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

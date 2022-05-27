//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.IO;
using System.Numerics;
using sbio.Core.Math;

namespace sbio.owsdk.Extensions
{
  public static class BinaryIOExtensions
  {
    public static void Write(this BinaryWriter writer, int[] values)
    {
      var valCount = values.Length;
      writer.Write(valCount);
      for (var i = 0; i < valCount; ++i)
      {
        writer.Write(values[i]);
      }
    }

    public static void WriteAsUShortArray(this BinaryWriter writer, int[] values)
    {
      var valCount = values.Length;
      writer.Write(valCount);
      for (var i = 0; i < valCount; ++i)
      {
        writer.Write((ushort)values[i]);
      }
    }

    public static int[] ReadIntArray(this BinaryReader reader)
    {
      var len = reader.ReadInt32();
      var ret = new int[len];
      for (int i = 0; i < len; ++i)
      {
        ret[i] = reader.ReadInt32();
      }

      return ret;
    }

    public static void Write(this BinaryWriter writer, short[] values)
    {
      var valCount = values.Length;
      writer.Write(valCount);
      for (var i = 0; i < valCount; ++i)
      {
        writer.Write(values[i]);
      }
    }

    public static short[] ReadShortArray(this BinaryReader reader)
    {
      var len = reader.ReadInt32();
      var ret = new short[len];
      for (int i = 0; i < len; ++i)
      {
        ret[i] = reader.ReadInt16();
      }

      return ret;
    }

    public static int[] ReadUShortArrayAsInt(this BinaryReader reader)
    {
      var len = reader.ReadInt32();
      var ret = new int[len];
      for (int i = 0; i < len; ++i)
      {
        ret[i] = reader.ReadUInt16();
      }

      return ret;
    }

    public static void Write(this BinaryWriter writer, Vector2f v)
    {
      writer.Write(v.X);
      writer.Write(v.Y);
    }

    public static Vector2f ReadV2f(this BinaryReader reader)
    {
      return new Vector2f(reader.ReadSingle(), reader.ReadSingle());
    }

    public static void Write(this BinaryWriter writer, Vector2d v)
    {
      writer.Write(v.X);
      writer.Write(v.Y);
    }

    public static Vector2d ReadV2d(this BinaryReader reader)
    {
      return new Vector2d(reader.ReadDouble(), reader.ReadDouble());
    }

    public static void Write(this BinaryWriter writer, Vector3f v)
    {
      writer.Write(v.X);
      writer.Write(v.Y);
      writer.Write(v.Z);
    }

    public static Vector3f ReadV3f(this BinaryReader reader)
    {
      return new Vector3f(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    public static void Write(this BinaryWriter writer, Vector3d v)
    {
      writer.Write(v.X);
      writer.Write(v.Y);
      writer.Write(v.Z);
    }

    public static Vector3d ReadV3d(this BinaryReader reader)
    {
      return new Vector3d(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
    }

    public static void Write(this BinaryWriter writer, QuaternionLeftHandedGeocentric quat)
    {
      writer.Write(quat.X);
      writer.Write(quat.Y);
      writer.Write(quat.Z);
      writer.Write(quat.W);
    }

    public static QuaternionLeftHandedGeocentric ReadQuatf(this BinaryReader reader)
    {
      return new QuaternionLeftHandedGeocentric(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    public static void Write(this BinaryWriter writer, Quaternion quat)
    {
      writer.Write(quat.X);
      writer.Write(quat.Y);
      writer.Write(quat.Z);
      writer.Write(quat.W);
    }

    public static QuaternionLeftHandedGeocentric ReadQuatd(this BinaryReader reader)
    {
      return new QuaternionLeftHandedGeocentric(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
    }

    public static void Write(this BinaryWriter writer, Vector3d[] values)
    {
      var valCount = values.Length;
      writer.Write(valCount);
      for (var i = 0; i < valCount; ++i)
      {
        writer.Write(values[i]);
      }
    }

    public static void Write(this BinaryWriter writer, Vector2f[] values)
    {
      var valCount = values.Length;
      writer.Write(valCount);
      for (var i = 0; i < valCount; ++i)
      {
        writer.Write(values[i]);
      }
    }

    public static Vector2f[] ReadV2fArray(this BinaryReader reader)
    {
      var len = reader.ReadInt32();
      var ret = new Vector2f[len];
      for (int i = 0; i < len; ++i)
      {
        ret[i] = reader.ReadV2f();
      }

      return ret;
    }

    public static void Write(this BinaryWriter writer, Vector2d[] values)
    {
      var valCount = values.Length;
      writer.Write(valCount);
      for (var i = 0; i < valCount; ++i)
      {
        writer.Write(values[i]);
      }
    }

    public static Vector2d[] ReadV2dArray(this BinaryReader reader)
    {
      var len = reader.ReadInt32();
      var ret = new Vector2d[len];
      for (int i = 0; i < len; ++i)
      {
        ret[i] = reader.ReadV2d();
      }

      return ret;
    }

    public static void Write(this BinaryWriter writer, Vector3f[] values)
    {
      var valCount = values.Length;
      writer.Write(valCount);
      for (var i = 0; i < valCount; ++i)
      {
        writer.Write(values[i]);
      }
    }

    public static Vector3f[] ReadV3fArray(this BinaryReader reader)
    {
      var len = reader.ReadInt32();
      var ret = new Vector3f[len];
      for (int i = 0; i < len; ++i)
      {
        ret[i] = reader.ReadV3f();
      }

      return ret;
    }

    public static Vector3d[] ReadV3dArray(this BinaryReader reader)
    {
      var len = reader.ReadInt32();
      var ret = new Vector3d[len];
      for (int i = 0; i < len; ++i)
      {
        ret[i] = reader.ReadV3d();
      }

      return ret;
    }
  }
}
//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

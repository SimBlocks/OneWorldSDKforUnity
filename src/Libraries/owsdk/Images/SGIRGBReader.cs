//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.IO;

namespace sbio.owsdk.Images
{
  public static class SGIRGBReader
  {
    public static bool ReadInto(Stream stream, ArraySegment<int> pixels, out int width, out int height)
    {
      var header = ReadHeader(stream);
      if (header.Magic != 474)
      {
        throw new FormatException("Bad magic: " + header.Magic);
      }

      if (header.Dimensions == 1)
      {
        width = header.XSize;
        height = 1;
      }
      else if (header.Dimensions == 2 || header.Dimensions == 3)
      {
        width = header.XSize;
        height = header.YSize;
      }
      else
      {
        throw new FormatException("Bad number of dimensions: " + header.Dimensions);
      }

      if (pixels.Array == null || pixels.Count < (width * height))
      {
        return false;
      }

      ReadImpl(header, stream, pixels);

      return true;
    }

    public static ARGBImage Read(Stream stream)
    {
      var header = ReadHeader(stream);
      if (header.Magic != 474)
      {
        throw new FormatException("Bad magic: " + header.Magic);
      }

      int[] pixels;

      if (header.Dimensions == 1)
      {
        pixels = new int[header.XSize];
      }
      else if (header.Dimensions == 2 || header.Dimensions == 3)
      {
        pixels = new int[header.XSize * header.YSize];
      }
      else
      {
        throw new FormatException("Bad number of dimensions: " + header.Dimensions);
      }

      ReadImpl(header, stream, new ArraySegment<int>(pixels));

      return new ARGBImage(header.YSize, header.XSize, pixels);
    }

    private struct RGBHeader
    {
      /// <summary>
      /// IRIS image file magic
      /// decimal 474
      /// hex 0x01da
      /// </summary>
      public ushort Magic;

      /// <summary>
      /// Storage
      /// 0 for Uncompressed
      /// 1 for RLE compression
      /// </summary>
      public byte Storage;

      /// <summary>
      /// Bytes per channel. 
      /// 1 or 2
      /// </summary>
      public byte BytesPerChannel;

      /// <summary>
      /// Dimensions
      /// 1 = single row
      /// 2 = 2D image
      /// 3 = multiple 2D images
      /// </summary>
      public ushort Dimensions;

      /// <summary>
      /// X size in pixels
      /// </summary>
      public ushort XSize;

      /// <summary>
      /// Y size in pixels
      /// </summary>
      public ushort YSize;

      /// <summary>
      /// Number of channels
      /// 1 for greyscale
      /// 3 for RGB
      /// 4 for RGBA
      /// </summary>
      public ushort NumberOfChannels;

      /// <summary>
      /// Lowest pixel value in the image
      /// </summary>
      public int PixelMin;

      /// <summary>
      /// Highest pixel value in the image
      /// </summary>
      public int PixelMax;

      /// <summary>
      /// Image name. Null-terminated
      /// </summary>
      public byte[] ImageName;

      /// <summary>
      /// Colormap ID
      /// 0 for normal
      /// 1 for dithered, 3 mits for rg, 2 for b
      /// 2 for index colour
      /// 3 not an image, but a colourmap
      /// </summary>
      public int ColorMap;

      /// <summary>
      /// Ignored
      /// </summary>
      //public byte[] Dummy2;
    }

    private static RGBHeader ReadHeader(Stream stream)
    {
      var ret = new RGBHeader();
      ret.Magic = ReadBEUInt16(stream);
      ret.Storage = (byte)stream.ReadByte();
      ret.BytesPerChannel = (byte)stream.ReadByte();
      ret.Dimensions = ReadBEUInt16(stream);
      ret.XSize = ReadBEUInt16(stream);
      ret.YSize = ReadBEUInt16(stream);
      ret.NumberOfChannels = ReadBEUInt16(stream);
      ret.PixelMin = ReadBEInt32(stream);
      ret.PixelMax = ReadBEInt32(stream);

      //Just skip over four byte dummy1
      stream.Seek(4, SeekOrigin.Current);

      ret.ImageName = new byte[80];
      stream.Read(ret.ImageName, 0, ret.ImageName.Length);
      ret.ColorMap = ReadBEInt32(stream);

      //Skip 404 byte dummy2
      stream.Seek(404, SeekOrigin.Current);

      return ret;
    }

    private static ushort ReadBEUInt16(Stream stream)
    {
      return (ushort)((stream.ReadByte() << 8) | stream.ReadByte());
    }

    private static int ReadBEInt32(Stream stream)
    {
      return (stream.ReadByte() << 24 | (stream.ReadByte() << 16) | (stream.ReadByte() << 8) | stream.ReadByte());
    }

    private static void ReadImpl(RGBHeader header, Stream stream, ArraySegment<int> pixelBuf)
    {
      var ary = pixelBuf.Array;
      var offset = pixelBuf.Offset;

      if (header.Storage != 0)
      {
        throw new NotImplementedException("Unimplemented storage type: " + header.Storage);
      }

      switch (header.BytesPerChannel)
      {
        case 1:
        {
          var rowBuf = new byte[header.XSize];
          switch (header.NumberOfChannels)
          {
            case 1:
            {
              //Greyscale
              for (var row = 0; row < header.YSize; ++row)
              {
                var rowOff = row * header.XSize;
                stream.Read(rowBuf, 0, rowBuf.Length);
                for (var i = 0; i < rowBuf.Length; ++i)
                {
                  var greyVal = rowBuf[i];
                  ary[rowOff + i] = (0xFF << 24) | (greyVal << 16) | (greyVal << 8) | (greyVal | 0);
                }
              }
            }
              break;
            case 2:
            {
              //Grey and Alpha values
              for (var row = 0; row < header.YSize; ++row)
              {
                var rowOff = row * header.XSize;
                stream.Read(rowBuf, 0, rowBuf.Length);
                for (var i = 0; i < rowBuf.Length; ++i)
                {
                  var greyVal = rowBuf[i];
                  ary[rowOff + i] = (greyVal << 16) | (greyVal << 8) | (greyVal | 0);
                }
              }

              for (var row = 0; row < header.YSize; ++row)
              {
                var rowOff = row * header.XSize;
                stream.Read(rowBuf, 0, rowBuf.Length);
                for (var i = 0; i < rowBuf.Length; ++i)
                {
                  ary[rowOff + i] |= (rowBuf[i] << 24);
                }
              }
            }
              break;
            case 3:
            {
              //RGB
              for (var row = 0; row < header.YSize; ++row)
              {
                var rowOff = row * header.XSize;
                stream.Read(rowBuf, 0, rowBuf.Length);
                for (var i = 0; i < rowBuf.Length; ++i)
                {
                  ary[rowOff + i] = (0xFF << 24) | (rowBuf[i] << 16);
                }
              }

              for (var row = 0; row < header.YSize; ++row)
              {
                var rowOff = row * header.XSize;
                stream.Read(rowBuf, 0, rowBuf.Length);
                for (var i = 0; i < rowBuf.Length; ++i)
                {
                  ary[rowOff + i] |= (rowBuf[i] << 8);
                }
              }

              for (var row = 0; row < header.YSize; ++row)
              {
                var rowOff = row * header.XSize;
                stream.Read(rowBuf, 0, rowBuf.Length);
                for (var i = 0; i < rowBuf.Length; ++i)
                {
                  ary[rowOff + i] |= (rowBuf[i] << 0);
                }
              }
            }
              break;
            case 4:
            {
              //RGBA
              for (var row = 0; row < header.YSize; ++row)
              {
                var rowOff = row * header.XSize;
                stream.Read(rowBuf, 0, rowBuf.Length);
                for (var i = 0; i < rowBuf.Length; ++i)
                {
                  ary[rowOff + i] = (rowBuf[i] << 16);
                }
              }

              for (var row = 0; row < header.YSize; ++row)
              {
                var rowOff = row * header.XSize;
                stream.Read(rowBuf, 0, rowBuf.Length);
                for (var i = 0; i < rowBuf.Length; ++i)
                {
                  ary[rowOff + i] |= (rowBuf[i] << 8);
                }
              }

              for (var row = 0; row < header.YSize; ++row)
              {
                var rowOff = row * header.XSize;
                stream.Read(rowBuf, 0, rowBuf.Length);
                for (var i = 0; i < rowBuf.Length; ++i)
                {
                  ary[rowOff + i] |= (rowBuf[i] << 0);
                }
              }

              for (var row = 0; row < header.YSize; ++row)
              {
                var rowOff = row * header.XSize;
                stream.Read(rowBuf, 0, rowBuf.Length);
                for (var i = 0; i < rowBuf.Length; ++i)
                {
                  ary[rowOff + i] |= (rowBuf[i] << 24);
                }
              }
            }
              break;
            default: throw new FormatException("Bad number of channels: " + header.NumberOfChannels);
          }
        }
          break;
        case 2: throw new NotImplementedException("No support for two bytes per channel");
        default: throw new FormatException("Bad bytes per channel: " + header.BytesPerChannel);
      }
    }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

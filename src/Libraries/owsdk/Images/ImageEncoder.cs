//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.IO;
using BitMiracle.LibJpeg.Classic;
using Hjg.Pngcs;

namespace sbio.owsdk.Images
{
  public static class ImageEncoder
  {
    /// <summary>
    /// Encodes an image in the given format and returns the resulting bytes.
    /// </summary>
    /// <param name="argb">The image to encode.</param>
    /// <param name="destFormat">The format to encode the image in</param>
    /// <returns>The resulting bytes</returns>
    public static byte[] Encode(ARGBImage argb, ImageType destFormat)
    {
      using (var stream = new MemoryStream())
      {
        Encode(argb, destFormat, stream);
        return stream.ToArray();
      }
    }

    /// <summary>
    /// Encodes an ARGB image into a different image format
    /// and stores the result in a stream
    /// </summary>
    /// <param name="argb">The image to be encoded</param>
    /// <param name="destFormat">The format of the returned image</param>
    /// <param name="dest">The stream destination</param>
    /// <returns>The number of bytes written to the stream</returns>
    public static int Encode(ARGBImage argb, ImageType destFormat, Stream dest)
    {
      // Check the format of destFormat to see if we have an encoder
      switch (destFormat)
      {
        case ImageType.PNG: return EncodePNG(argb, dest);
        case ImageType.JPG: return EncodeJPG(argb, dest);
        default: throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Encodes an ARGB image into a different image format
    /// and stores the result in a byte array
    /// </summary>
    /// <param name="argb">The image to be encoded</param>
    /// <param name="destFormat">The format of the returned image</param>
    /// <param name="dest">The byte array destination</param>
    /// <returns>The number of bytes written to the stream</returns>
    public static int Encode(ARGBImage argb, ImageType destFormat, byte[] dest)
    {
      using (var stream = new MemoryStream(dest))
      {
        return Encode(argb, destFormat, stream);
      }
    }

    /// <summary>
    /// Encodes an ARGB image into a different image format
    /// and stores the result in an ArraySegment
    /// </summary>
    /// <param name="argb">Teh iamge to be encoded</param>
    /// <param name="destFormat">The format of the returned image</param>
    /// <param name="dest">The ArraySegment destination</param>
    /// <returns>The number of bytes written to the stream</returns>
    public static int Encode(ARGBImage argb, ImageType destFormat, ArraySegment<byte> dest)
    {
      using (var stream = new MemoryStream(dest.Array, dest.Offset, dest.Count))
      {
        return Encode(argb, ImageType.PNG, stream);
      }
    }

    /// <summary>
    /// Converts an array of pixels into a PNG with Alpha and stores the result in a Stream
    /// </summary>
    /// <param name="dest">The Stream where the resulting PNG will be stored</param>
    /// <param name="argb">The array of pixels stored as integers</param>
    /// <returns>The starting position of the PNG on the Stream</returns>
    public static int EncodePNG(ARGBImage argb, Stream dest)
    {
      var pixels = argb.Pixels;
      var width = argb.Width;
      var height = argb.Height;

      var startPos = dest.Position;
      var info = new ImageInfo(width, height, 8, true);
      var writer = new PngWriter(dest, info);
      writer.ShouldCloseStream = false;
      writer.CompLevel = 9;
      var line = new ImageLine(info, ImageLine.ESampleType.BYTE);
      var rowBuf = line.ScanlineB;
      var mult = info.BytesPixel;
      for (var j = 0; j < height; ++j)
      {
        var jOff = j * width;
        for (var i = 0; i < width; ++i)
        {
          var pOff = jOff + i;
          var iOff = i * mult;
          var px = pixels[pOff];
          rowBuf[iOff + 0] = (byte)((px >> 16) & 0xFF);
          rowBuf[iOff + 1] = (byte)((px >> 8) & 0xFF);
          rowBuf[iOff + 2] = (byte)((px >> 0) & 0xFF);
          rowBuf[iOff + 3] = (byte)((px >> 24) & 0xFF);
        }

        writer.WriteRow(line, j);
      }

      writer.End();

      return (int)(dest.Position - startPos);
    }

    public static int EncodeJPG(ARGBImage argb, Stream dest)
    {
      var startPos = dest.Position;
      var encoder = new jpeg_compress_struct();
      try
      {
        encoder.Input_components = 3;
        encoder.Image_width = argb.Width;
        encoder.Image_height = argb.Height;
        encoder.In_color_space = J_COLOR_SPACE.JCS_RGB;

        encoder.jpeg_set_defaults();

        encoder.jpeg_set_colorspace(J_COLOR_SPACE.JCS_RGB);
        encoder.Data_precision = 8;

        encoder.jpeg_stdio_dest(dest);
        encoder.jpeg_start_compress(true);

        var pixels = argb.Pixels;
        var width = argb.Width;
        var height = argb.Height;
        var stride = width * 3;
        var scanline = new byte[stride];
        var scanlines = new byte[][] {scanline};

        for (var j = 0; j < height; ++j)
        {
          var jOff = j * width;
          for (var i = 0; i < width; ++i)
          {
            var px = pixels[jOff + i];
            var pxOffset = i * 3;
            scanline[pxOffset + 0] = (byte)((px >> 16) & 0xFF);
            scanline[pxOffset + 1] = (byte)((px >> 8) & 0xFF);
            scanline[pxOffset + 2] = (byte)((px >> 0) & 0xFF);
          }

          encoder.jpeg_write_scanlines(scanlines, scanlines.Length);
        }

        encoder.jpeg_finish_compress();
        return (int)(dest.Position - startPos);
      }
      finally
      {
        encoder.jpeg_destroy();
      }
    }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

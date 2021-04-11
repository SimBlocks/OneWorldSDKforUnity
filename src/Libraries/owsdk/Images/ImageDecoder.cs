//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.IO;
using BitMiracle.LibJpeg.Classic;
using Hjg.Pngcs;

namespace sbio.owsdk.Images
{
  public static class ImageDecoder
  {
    /// <summary>
    /// Decodes an image from a stream into an existing buffer, and returns its width and height.
    /// </summary>
    /// <param name="stream">Stream to read the image data from</param>
    /// <param name="pixelBuf">Buffer to read image data into, or null</param>
    /// <param name="width">Width of the decoded image</param>
    /// <param name="height">Height of the decoded image</param>
    /// <returns>true if pixelBuf is not null and has enough capacity for the image</returns>
    public static bool DecodeInto(Stream stream, int[] pixelBuf, out int width, out int height)
    {
      return DecodeInto(stream, pixelBuf != null ? new ArraySegment<int>(pixelBuf) : default(ArraySegment<int>?), out width, out height);
    }

    /// <summary>
    /// Decodes an image from a stream into an existing buffer, and returns its width and height.
    /// </summary>
    /// <param name="stream">Stream to read the image data from</param>
    /// <param name="pixelBuf">Buffer to read image data into, or null</param>
    /// <param name="width">Width of the decoded image</param>
    /// <param name="height">Height of the decoded image</param>
    /// <returns>true if pixelBuf is not null and has enough capacity for the image</returns>
    public static bool DecodeInto(Stream stream, ArraySegment<int>? pixelBuf, out int width, out int height)
    {
      if (stream.CanSeek)
      {
        var magic = new byte[16];
        stream.Read(magic, 0, magic.Length);
        stream.Seek(-magic.Length, SeekOrigin.Current);

        var imageType = GuessImageType(new ArraySegment<byte>(magic));
        switch (imageType)
        {
          case ImageType.PNG: return DecodePNGInto(stream, pixelBuf, out width, out height);
          case ImageType.JPG: return DecodeJPGInto(stream, pixelBuf, out width, out height);
          case ImageType.SGIRGB: return DecodeSGIRGBInto(stream, pixelBuf, out width, out height);
          default:
            throw new NotImplementedException("Cannot handle image type '" + imageType + "'");
        }
      }
      else
      {
        throw new NotImplementedException("No support for non-seekable streams");
      }
    }

    /// <summary>
    /// Decodes an image from a stream.
    /// </summary>
    /// <param name="stream">The stream to read the image data from</param>
    /// <returns>An ARGBImage with the decoded information</returns>
    public static ARGBImage Decode(Stream stream)
    {
      if (stream.CanSeek)
      {
        var magic = new byte[16];
        stream.Read(magic, 0, magic.Length);
        stream.Seek(-magic.Length, SeekOrigin.Current);

        var imageType = GuessImageType(new ArraySegment<byte>(magic));
        switch (imageType)
        {
          case ImageType.PNG: return DecodePNG(stream);
          case ImageType.JPG: return DecodeJPG(stream);
          case ImageType.SGIRGB: return DecodeSGIRGB(stream);
          default:
            throw new NotImplementedException("Cannot handle image type '" + imageType + "'");
        }
      }
      else
      {
        throw new NotImplementedException("No support for non-seekable streams");
      }
    }

    /// <summary>
    /// Decodes an image from a byte buffer into an existing pixel buffer, and returns its width and height.
    /// </summary>
    /// <param name="bytes">Buffer containing image data</param>
    /// <param name="pixelBuf">Buffer to read image data into, or null</param>
    /// <param name="width">Width of the decoded image</param>
    /// <param name="height">Height of the decoded image</param>
    /// <returns>true if pixelBuf is not null and has enough capacity for the image</returns>
    public static bool DecodeInto(byte[] bytes, int[] pixelBuf, out int width, out int height)
    {
      return DecodeInto(bytes, pixelBuf != null ? new ArraySegment<int>(pixelBuf) : default(ArraySegment<int>?), out width, out height);
    }

    /// <summary>
    /// Decodes an image from a byte buffer into an existing pixel buffer, and returns its width and height.
    /// </summary>
    /// <param name="bytes">Buffer containing image data</param>
    /// <param name="pixelBuf">Buffer to read image data into, or null</param>
    /// <param name="width">Width of the decoded image</param>
    /// <param name="height">Height of the decoded image</param>
    /// <returns>true if pixelBuf is not null and has enough capacity for the image</returns>
    public static bool DecodeInto(byte[] bytes, ArraySegment<int>? pixelBuf, out int width, out int height)
    {
      return DecodeInto(new ArraySegment<byte>(bytes), pixelBuf, out width, out height);
    }

    /// <summary>
    /// Decodes an image from a buffer.
    /// </summary>
    /// <param name="bytes">The buffer to read the image data from</param>
    /// <returns>An ARGBImage with the decoded information</returns>
    public static ARGBImage Decode(byte[] bytes)
    {
      return Decode(new ArraySegment<byte>(bytes));
    }

    /// <summary>
    /// Decodes an image from a byte buffer into an existing pixel buffer, and returns its width and height.
    /// </summary>
    /// <param name="bytes">Buffer containing image data</param>
    /// <param name="pixelBuf">Buffer to read image data into, or null</param>
    /// <param name="width">Width of the decoded image</param>
    /// <param name="height">Height of the decoded image</param>
    /// <returns>true if pixelBuf is not null and has enough capacity for the image</returns>
    public static bool DecodeInto(ArraySegment<byte> bytes, int[] pixelBuf, out int width, out int height)
    {
      return DecodeInto(bytes, pixelBuf != null ? new ArraySegment<int>(pixelBuf) : default(ArraySegment<int>?), out width, out height);
    }

    /// <summary>
    /// Decodes an image from a byte buffer into an existing pixel buffer, and returns its width and height.
    /// </summary>
    /// <param name="bytes">Buffer containing image data</param>
    /// <param name="pixelBuf">Buffer to read image data into, or null</param>
    /// <param name="width">Width of the decoded image</param>
    /// <param name="height">Height of the decoded image</param>
    /// <returns>true if pixelBuf is not null and has enough capacity for the image</returns>
    public static bool DecodeInto(ArraySegment<byte> bytes, ArraySegment<int>? pixelBuf, out int width, out int height)
    {
      var imageType = GuessImageType(bytes);
      switch (imageType)
      {
        case ImageType.PNG: return DecodePNGInto(bytes, pixelBuf, out width, out height);
        case ImageType.JPG: return DecodeJPGInto(bytes, pixelBuf, out width, out height);
        case ImageType.SGIRGB: return DecodeSGIRGBInto(bytes, pixelBuf, out width, out height);
        default:
          throw new NotImplementedException("Cannot handle image type '" + imageType + "'");
      }
    }

    /// <summary>
    /// Decodes an image from a buffer.
    /// </summary>
    /// <param name="bytes">The buffer to read the image data from</param>
    /// <returns>An ARGBImage with the decoded information</returns>
    public static ARGBImage Decode(ArraySegment<byte> bytes)
    {
      var imageType = GuessImageType(bytes);
      switch (imageType)
      {
        case ImageType.PNG: return DecodePNG(bytes);
        case ImageType.JPG: return DecodeJPG(bytes);
        case ImageType.SGIRGB: return DecodeSGIRGB(bytes);
        default:
          throw new NotImplementedException("Cannot handle image type '" + imageType + "'");
      }
    }

    private static ARGBImage DecodePNG(ArraySegment<byte> bytes)
    {
      using (var stream = new MemoryStream(bytes.Array, bytes.Offset, bytes.Count))
      {
        return DecodePNG(stream);
      }
    }

    private static bool DecodePNGInto(ArraySegment<byte> bytes, ArraySegment<int>? pixels, out int width, out int height)
    {
      using (var stream = new MemoryStream(bytes.Array, bytes.Offset, bytes.Count))
      {
        return DecodePNGInto(stream, pixels, out width, out height);
      }
    }

    private static ARGBImage DecodePNG(Stream stream)
    {
      var reader = new PngReader(stream);
      var imageInfo = reader.ImgInfo;
      reader.SetUnpackedMode(true);
      var height = imageInfo.Rows;
      var width = imageInfo.Cols;

      var pixelBuf = new int[height * width];
      DecodePNGImpl(reader, pixelBuf, 0);
      return new ARGBImage(height, width, pixelBuf);
    }

    private static bool DecodePNGInto(Stream stream, ArraySegment<int>? pixels, out int width, out int height)
    {
      var reader = new PngReader(stream);
      var imageInfo = reader.ImgInfo;
      reader.SetUnpackedMode(true);
      height = imageInfo.Rows;
      width = imageInfo.Cols;

      if (pixels == null || pixels.Value.Count < (width * height))
      {
        return false;
      }

      DecodePNGImpl(reader, pixels.Value.Array, pixels.Value.Offset);
      return true;
    }

    private static ARGBImage DecodeJPG(ArraySegment<byte> bytes)
    {
      using (var stream = new MemoryStream(bytes.Array, bytes.Offset, bytes.Count))
      {
        return DecodeJPG(stream);
      }
    }

    private static bool DecodeJPGInto(ArraySegment<byte> bytes, ArraySegment<int>? pixels, out int width, out int height)
    {
      using (var stream = new MemoryStream(bytes.Array, bytes.Offset, bytes.Count))
      {
        return DecodeJPGInto(stream, pixels, out width, out height);
      }
    }

    private static ARGBImage DecodeJPG(Stream stream)
    {
      var decoder = new jpeg_decompress_struct();
      decoder.jpeg_stdio_src(stream);
      decoder.jpeg_read_header(true);
      decoder.Out_color_space = J_COLOR_SPACE.JCS_RGB;
      decoder.jpeg_start_decompress();
      var height = decoder.Output_height;
      var width = decoder.Output_width;
      var pixelBuf = new int[height * width];
      DecodeJPGImpl(decoder, pixelBuf, 0);
      return new ARGBImage(height, width, pixelBuf);
    }

    private static bool DecodeJPGInto(Stream stream, ArraySegment<int>? pixels, out int width, out int height)
    {
      var decoder = new jpeg_decompress_struct();
      decoder.jpeg_stdio_src(stream);
      decoder.jpeg_read_header(true);
      decoder.Out_color_space = J_COLOR_SPACE.JCS_RGB;
      decoder.jpeg_start_decompress();
      height = decoder.Output_height;
      width = decoder.Output_width;

      if (pixels == null || pixels.Value.Count < (width * height))
      {
        return false;
      }

      DecodeJPGImpl(decoder, pixels.Value.Array, pixels.Value.Offset);
      return true;
    }

    private static void DecodePNGImpl(PngReader reader, int[] pixels, int offset)
    {
      var imageInfo = reader.ImgInfo;
      var width = imageInfo.Cols;
      var height = imageInfo.Rows;
      if (imageInfo.Indexed)
      {
        var plte = reader.GetMetadata().GetPLTE();
        var rowBuf = new byte[width];
        for (var j = 0; j < height; ++j)
        {
          reader.ReadRowByte(rowBuf, j);
          var jOff = offset + j * width;
          for (var i = 0; i < width; ++i)
          {
            pixels[jOff + i] = plte.GetEntry(rowBuf[i]);
          }
        }
      }
      else if (imageInfo.Greyscale)
      {
        if (imageInfo.BitspPixel == 8)
        {
          var rowBuf = new byte[width];
          for (var j = 0; j < height; ++j)
          {
            reader.ReadRowByte(rowBuf, j);
            var jOff = offset + j * width;
            for (var i = 0; i < width; ++i)
            {
              var gray = rowBuf[i];
              pixels[jOff + i] = (0xFF << 24) | (gray << 16) | (gray << 8) | gray;
            }
          }
        }
        else
        {
          throw new NotImplementedException();
        }
      }
      else if (imageInfo.BitspPixel == 24)
      {
        var rowBuf = new byte[imageInfo.BytesPerRow];
        for (var j = 0; j < height; ++j)
        {
          var jOff = offset + j * width;
          reader.ReadRowByte(rowBuf, j);

          for (int i = 0, x = 0; i < imageInfo.BytesPerRow; i += 3, ++x)
          {
            pixels[jOff + x] = (rowBuf[i + 0] << 16) | (rowBuf[i + 1] << 8) | (rowBuf[i + 2] << 0);
          }
        }
      }
      else if (imageInfo.BitspPixel == 32)
      {
        var rowBuf = new byte[imageInfo.BytesPerRow];
        for (var j = 0; j < height; ++j)
        {
          var jOff = offset + j * width;
          reader.ReadRowByte(rowBuf, j);

          for (int i = 0, x = 0; i < imageInfo.BytesPerRow; i += 4, ++x)
          {
            pixels[jOff + x] = (rowBuf[i + 3] << 24) | (rowBuf[i + 0] << 16) | (rowBuf[i + 1] << 8) | (rowBuf[i + 2] << 0);
          }
        }
      }
      else
      {
        throw new NotImplementedException();
      }
    }

    private static void DecodeJPGImpl(jpeg_decompress_struct decoder, int[] pixels, int offset)
    {
      try
      {
        var height = decoder.Output_height;
        var width = decoder.Output_width;
        var numComponents = decoder.Output_components;
        var stride = decoder.Output_width * numComponents;

        /* Make a one-row-high sample array that will go away when done with image */
        var rowBufs = new byte[1][];
        var rowBuf = rowBufs[0] = new byte[stride];

        if (numComponents == 1)
        {
          while (decoder.Output_scanline < height)
          {
            decoder.jpeg_read_scanlines(rowBufs, 1);

            var jOff = offset + (decoder.Output_scanline - 1) * width;
            for (var i = 0; i < width; ++i)
            {
              var gray = rowBuf[i];
              pixels[jOff + i] = (0xFF << 24) | (gray << 16) | (gray << 8) | gray;
            }
          }
        }
        else if (numComponents == 3)
        {
          while (decoder.Output_scanline < height)
          {
            decoder.jpeg_read_scanlines(rowBufs, 1);

            var jOff = offset + (decoder.Output_scanline - 1) * width;
            for (var i = 0; i < width; ++i)
            {
              var rowOff = i * 3;
              var r = rowBuf[rowOff + 0];
              var g = rowBuf[rowOff + 1];
              var b = rowBuf[rowOff + 2];
              pixels[jOff + i] = (0xFF << 24) | (r << 16) | (g << 8) | b;
            }
          }
        }
        else
        {
          throw new NotImplementedException();
        }

        decoder.jpeg_finish_decompress();
      }
      finally
      {
        decoder.jpeg_destroy();
      }
    }

    private static ARGBImage DecodeSGIRGB(ArraySegment<byte> bytes)
    {
      using (var stream = new MemoryStream(bytes.Array, bytes.Offset, bytes.Count))
      {
        return DecodeSGIRGB(stream);
      }
    }

    private static ARGBImage DecodeSGIRGB(Stream stream)
    {
      return SGIRGBReader.Read(stream);
    }

    private static bool DecodeSGIRGBInto(ArraySegment<byte> bytes, ArraySegment<int>? pixels, out int width, out int height)
    {
      using (var stream = new MemoryStream(bytes.Array, bytes.Offset, bytes.Count))
      {
        return DecodeSGIRGBInto(stream, pixels, out width, out height);
      }
    }

    private static bool DecodeSGIRGBInto(Stream stream, ArraySegment<int>? pixels, out int width, out int height)
    {
      var buf = pixels.HasValue ? pixels.Value : default(ArraySegment<int>);
      return SGIRGBReader.ReadInto(stream, buf, out width, out height);
    }

    private static ImageType GuessImageType(ArraySegment<byte> buf)
    {
      if (buf.Count < 16)
      {
        throw new Exception("Can't decode image type");
      }
      else if (MatchesMagic(buf, JPGMagic))
      {
        return ImageType.JPG;
      }
      else if (MatchesMagic(buf, PNGMagic))
      {
        return ImageType.PNG;
      }
      else if (MatchesMagic(buf, GIFMagic1))
      {
        return ImageType.GIF;
      }
      else if (MatchesMagic(buf, GIFMagic2))
      {
        return ImageType.GIF;
      }
      else if (MatchesMagic(buf, TIFFMagic1))
      {
        return ImageType.TIFF;
      }
      else if (MatchesMagic(buf, TIFFMagic2))
      {
        return ImageType.TIFF;
      }
      else if (MatchesMagic(buf, BMPMagic))
      {
        return ImageType.BMP;
      }
      else if (MatchesMagic(buf, WEBPMagic1))
      {
        return ImageType.WEBP;
      }
      else if (MatchesMagic(buf, WEBPMagic2))
      {
        return ImageType.WEBP;
      }
      else if (MatchesMagic(buf, ICOMagic1))
      {
        return ImageType.ICO;
      }
      else if (MatchesMagic(buf, ICOMagic2))
      {
        return ImageType.ICO;
      }
      else if (MatchesMagic(buf, SGIRGBMagic))
      {
        return ImageType.SGIRGB;
      }
      else
      {
        throw new Exception("Unknown image type");
      }
    }

    private static bool MatchesMagic(ArraySegment<byte> buf, byte[] magic)
    {
      var count = buf.Count;
      if (count < magic.Length)
      {
        return false;
      }

      var ary = buf.Array;
      var offset = buf.Offset;
      for (var i = 0; i < magic.Length; ++i)
      {
        if (ary[offset + i] != magic[i])
        {
          return false;
        }
      }

      return true;
    }

    private static readonly byte[] JPGMagic = new byte[] {0xFF, 0xD8, 0xFF};

    private static readonly byte[] PNGMagic = new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A};

    //GIF87a
    private static readonly byte[] GIFMagic1 = new byte[] {0x47, 0x49, 0x46, 0x38, 0x37, 0x61};

    //GIF89a
    private static readonly byte[] GIFMagic2 = new byte[] {0x47, 0x49, 0x46, 0x38, 0x39, 0x61};
    private static readonly byte[] TIFFMagic1 = new byte[] {0x49, 0x49, 0x2A, 0x00};

    private static readonly byte[] TIFFMagic2 = new byte[] {0x4D, 0x4D, 0x00, 0x2A};

    //BM
    private static readonly byte[] BMPMagic = new byte[] {0x42, 0x4D};

    //RIFF
    private static readonly byte[] WEBPMagic1 = new byte[] {0x52, 0x49, 0x46, 0x46};

    //RWEBP
    private static readonly byte[] WEBPMagic2 = new byte[] {0x52, 0x57, 0x45, 0x42, 0x50};

    //ICO
    private static readonly byte[] ICOMagic1 = new byte[] {0x00, 0x00, 0x01, 0x00};

    private static readonly byte[] ICOMagic2 = new byte[] {0x00, 0x00, 0x02, 0x00};

    //SGI RGB
    private static readonly byte[] SGIRGBMagic = new byte[] {0x01, 0xda};
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

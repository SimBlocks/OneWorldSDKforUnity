//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.owsdk.Services;

namespace sbio.owsdk.OSM
{
  public class TileAttributeMask2d : ITileAttributeMask
  {
    public bool IsWater(double xPx, double yPx)
    {
      xPx = Math.Min(Math.Max(xPx, 0), 255);
      yPx = Math.Min(Math.Max(yPx, 0), 255);

      var rowTruncate = (int)yPx;
      var rowFraction = yPx - rowTruncate;

      if (rowTruncate == 255)
      {
        rowTruncate = 254;
        rowFraction = 1;
      }

      var colTruncate = (int)xPx;
      var colFraction = xPx - colTruncate;
      if (colTruncate == 255)
      {
        colTruncate = 254;
        colFraction = 1;
      }

      var b00 = (1 - colFraction) * WaterBit(m_Mask[rowTruncate][colTruncate]);
      var b01 = colFraction * WaterBit(m_Mask[rowTruncate][colTruncate + 1]);
      var b10 = (1 - colFraction) * WaterBit(m_Mask[rowTruncate + 1][colTruncate]);
      var b11 = colFraction * WaterBit(m_Mask[rowTruncate + 1][colTruncate + 1]);
      return (1 - rowFraction) * (b00 + b01) + rowFraction * (b10 + b11) > 0.66;
    }

    public TileAttributeMask2d(byte[][] maskData)
    {
      m_Mask = maskData;
    }

    private static byte WaterBit(byte b)
    {
      return (byte)(b != 0 ? 1 : 0);
    }

    private enum AttributeBits
    {
      Water = 7
    }

    private readonly byte[][] m_Mask;
  }
}



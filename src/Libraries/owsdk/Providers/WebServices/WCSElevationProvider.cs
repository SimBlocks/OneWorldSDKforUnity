//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using OSGeo.GDAL;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Services;
using sbio.owsdk.ThirdParty.GDAL;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using sbio.owsdk.Tiles;

namespace sbio.owsdk.Providers.WebServices
{
  public sealed class WCSElevationProvider : IElevationProvider
  {
    public sealed class Settings
    {

      public static Settings Default
      {
        get { return new Settings(); }
      }

      public Settings()
      {
        XMLLocation = null;
        DataDirectory = null;
        SampleSize = 256;
      }

      /// <summary>
      /// The directory location of the WMC XML file you want to load
      /// </summary>
      public String XMLLocation { get; set; }

      /// <summary>
      /// The directory location to cache the WCS server data
      /// </summary>
      public String DataDirectory { get; set; }

      /// <summary>
      /// The side length of each terrain sample in pixels to query with each server request
      /// Servers usually have a max request size
      /// </summary>
      public int SampleSize { get; set; }

    }

    public ITileMapper TileMapper { get; set; }

    public WCSElevationProvider(Settings settings)
    {
      m_XMLLocation = @settings.XMLLocation;
      m_sampleSize = settings.SampleSize;
      m_cacheLocation = @settings.DataDirectory;
      GdalConfiguration.ConfigureGdal();
      Gdal.AllRegister();
      Driver driver = Gdal.GetDriverByName("wcs");

      using (m_dataset = Gdal.Open(m_XMLLocation, Access.GA_ReadOnly))
      {
        m_dataset.GetGeoTransform(m_GT);
        m_imageWidth = m_dataset.RasterXSize;
        m_imageHeight = m_dataset.RasterYSize;
        m_widthInTiles = m_imageWidth / m_sampleSize;
        m_heightInTiles = m_imageHeight / m_sampleSize;
      }

      //Checks if requested tiles are already cached, caches them if they aren't
      Parallel.For(0, m_widthInTiles, new ParallelOptions { MaxDegreeOfParallelism = 12 }, i =>
      {
        double[] tempBuffer = new double[m_sampleSize * m_sampleSize];
        using (var tempDataset = Gdal.Open(m_XMLLocation, Access.GA_ReadOnly))
        {
          for (int j = 0; j < m_heightInTiles; j++)
          {
            if (File.Exists(settings.DataDirectory + "Tile[" + i.ToString() + "][" + j.ToString() + "].WCSData"))
              Console.WriteLine("Tile[" + i.ToString() + "][" + j.ToString() + "] already cached");
            else
            {
              try
              {
                tempDataset.ReadRaster(i * m_sampleSize, j * m_sampleSize, m_sampleSize, m_sampleSize, tempBuffer, m_sampleSize, m_sampleSize, 1, new int[] { 1 }, 0, 0, 1);
                Serialize(tempBuffer, settings.DataDirectory + "Tile[" + i.ToString() + "][" + j.ToString() + "].WCSData");
                Console.WriteLine("Tile[" + i.ToString() + "][" + j.ToString() + "].WCSData cached");
              }
              catch
              {
                Console.WriteLine("Tile[" + i.ToString() + "][" + j.ToString() + "].WCSData cache failed");
              }
            }
          }
        }
      });

      m_extent = GetImageExtents();
    }



    private void Serialize(object t, string path)
    {
      lock (m_locker)
      {
        using (Stream stream = File.Open(path, FileMode.Create))
        {
          BinaryFormatter bformatter = new BinaryFormatter();
          bformatter.Serialize(stream, t);
        }
      }
    }

    private object Deserialize(string path)
    {
      lock (m_locker)
      {
        using (Stream stream = File.Open(path, FileMode.Open))
        {
          BinaryFormatter bformatter = new BinaryFormatter();
          return bformatter.Deserialize(stream);
        }
      }
    }

    void GeoToPixel(double Xgeo, double Ygeo, out int Xpixel, out int Yline)
    {
      Xpixel = (int)((Xgeo - m_GT[0]) / m_GT[1]);
      Yline = (int)((Ygeo - m_GT[3]) / m_GT[5]);
    }

    void PixelToGeo(int Xpixel, int Yline, out double Xgeo, out double Ygeo)
    {
      Xgeo = m_GT[0] + Xpixel * m_GT[1]; Ygeo = m_GT[3] + Yline * m_GT[5];
    }

    GeoBoundingBox GetImageExtents()
    {
      double topLeftGeoX, topLeftGeoY, bottomRightGeoX, bottomRightGeoY;
      PixelToGeo(0, 0, out topLeftGeoX, out topLeftGeoY);
      PixelToGeo(m_imageWidth, m_imageHeight, out bottomRightGeoX, out bottomRightGeoY);
      return GeoBoundingBox.FromDegrees(topLeftGeoY, topLeftGeoX, bottomRightGeoY, bottomRightGeoX);
    }

    private bool inBuffer(int minX, int minY, int buffMinX, int buffMinY)
    {
      if ((buffMinX == minX) && (buffMinY == minY))
        return true;

      return false;
    }

    private bool inRange(int x, int y)
    {
      if ((x > 0) && (x < m_imageWidth))
        if ((y > 0) && (y < m_imageHeight))
          return true;

      return false;
    }

    private void findTileCoord(out int minX, out int minY, int row, int col)
    {
      minX = row / m_sampleSize;
      minY = col / m_sampleSize;
    }

    public Task QueryPointSamplesAsyncInto(ArraySegment<ElevationPointSample> points, CancellationToken tok)
    {
      return Task.Run(() =>
      {
        Parallel.ForEach(points, (point, state, index) =>
        {
          double[] tempBuff;
          int minX = 0, minY = 0, row, col;
          if (m_extent.Contains(point.Position))
          {
            GeoToPixel(point.Position.LongitudeDegrees, point.Position.LatitudeDegrees, out col, out row);
            if (inRange(row, col))
            {
              findTileCoord(out minX, out minY, row, col);
              tempBuff = new double[m_sampleSize * m_sampleSize];

              tempBuff = (double[])Deserialize(m_cacheLocation + "Tile[" + minY.ToString() + "][" + minX.ToString() + "].WCSData");
              points.Array[index] = new ElevationPointSample(point.Position, tempBuff[(m_sampleSize * (row % m_sampleSize)) + (col % m_sampleSize)]);
              tempBuff = null;
            }
          }
        });
      });
    }

    private GeoBoundingBox m_extent;
    private double[] m_GT = new double[6];
    private object m_locker = new object();
    private int m_sampleSize, m_imageWidth, m_imageHeight, m_widthInTiles, m_heightInTiles;
    private string m_XMLLocation, m_cacheLocation;
    private Dataset m_dataset;

  }
}




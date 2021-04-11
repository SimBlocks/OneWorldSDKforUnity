//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using sbio.OWSDK.Services;
using System.Threading;
using sbio.OWSDK.Geodetic;
using OSGeo.OGR;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;

namespace sbio.OWSDK
{
  //This class contains functions that pull positional, geometric, and texture data from a WFS server
  //This data can be converted to useable assets in OpenFlight and using the positional data placed correctly in GlobeViewer
  //TODO: Clean this up more and make it more useful/implementable. Maybe add to WMSTools?
  public sealed class WFSFeatureScraper
  {
    public sealed class Settings
    {
      //Put URL parameters loaded from config file here
      public static Settings Default
      {
        get { return new Settings(); }
      }

      public Settings()
      {
        XMLLocation = null;
        DataDirectory = null;
      }

      /// <summary>
      /// The directory location of the WMC XML file you want to load
      /// </summary>
      public String XMLLocation { get; set; }

      public String DataDirectory { get; set; }
    }

    public WFSFeatureScraper(Settings settings)
    {
      m_XMLLocation = settings.XMLLocation;
      m_dataDirectory = settings.DataDirectory;
      GdalConfiguration.ConfigureGdal();
      Gdal.AllRegister();
      m_datasource = Ogr.Open(m_XMLLocation, 0);

      int layerCount = m_datasource.GetLayerCount();
      Console.WriteLine("Number of layers: " + layerCount.ToString()+"\n");
    }

    private void Serialize(string t, string path)
    {
      lock (m_locker)
        File.WriteAllBytes(path, Convert.FromBase64String(t));
    }

    //Gets a zip of .flt files or textures by name
    public void GetZip(string name)
    {
      long featureCount = 0;
      m_layer = m_datasource.GetLayerByName(name);
      Console.WriteLine(m_layer.GetName());

      for (int i = 0; i <= 2; i++)
      {
        for (int j = 0; j <= 2; j++)
        {
          string selectData = ("uref = \'" + i.ToString() + "\' AND rref = \'" + j.ToString() + "\'");
          m_layer.SetAttributeFilter(selectData);
          string tileCoords = "[" + i.ToString() + "][" + j.ToString() + "]";
          Console.WriteLine(tileCoords + ":");

          featureCount = m_layer.GetFeatureCount(0);
          Console.WriteLine("Feature count: " + featureCount.ToString());

          if (featureCount > 0)
          {
            m_feature = m_layer.GetNextFeature();
            Console.Write(featureCount.ToString() + ": ");
          }
          if (m_feature != null)
          {
            int index = m_feature.GetFieldIndex("data");
            if (index >= 0)
            {
              string buffer = m_feature.GetFieldAsString(index);
              Serialize(buffer, m_dataDirectory + "Test" + tileCoords + ".zip");
              m_extractFolderName = m_dataDirectory + "Test" + tileCoords;
            }
            m_feature = null;
            Console.WriteLine("");
          }
        }
        Console.WriteLine("");
      }
    }

    public void GetZip(int index)
    {
      m_layer = m_datasource.GetLayerByIndex(index);
      GetZip(m_layer.GetName());
    }

    //Gets coordinate values by name and dumps all the relevant data in a parsable text file
    public void GetCoords(string name)
    {
      long featureCount = 0;
      int i = 0;
      double[] doubleCoord = null;
      m_layer = m_datasource.GetLayerByName(name);
      Console.WriteLine(m_layer.GetName());
      featureCount = m_layer.GetFeatureCount(0);
      Console.WriteLine("Feature count: " + featureCount.ToString());

      if (featureCount > 0)
      {
        using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(m_dataDirectory + "geoPos.txt"))
        {
          m_feature = m_layer.GetNextFeature();
          while (m_feature != null)
          {
            //Console.Write(i.ToString() + ": ");
            i++;
            var reference = m_feature.GetFieldAsString(14);
            var geom = m_feature.GetGeometryRef();
            string geomout;
            string[] geomjson = null;
            geomout = geom.ExportToJson(geomjson);
            doubleCoord = ParseJsonCoords(geomout);
            file.WriteLine(reference.ToString() + " "+doubleCoord[0]+" "+doubleCoord[1] + " " + doubleCoord[2]);
            m_feature = m_layer.GetNextFeature();
          }
        }
      }
    }

    public void GetCoords(int index)
    {
      m_layer = m_datasource.GetLayerByIndex(index);
      GetCoords(m_layer.GetName());
    }

    //for out array 0 is lon, 1 is lat, 2 is height
    private double[] ParseJsonCoords(string json)
    {
      double[] outDoubles = new double[3];
      string[] parseArray = json.Split();
      Console.WriteLine(parseArray[5].TrimEnd(',') +"  " + parseArray[6].TrimEnd(',') + " " + parseArray[7]);

      outDoubles[0] = Convert.ToDouble(parseArray[5].TrimEnd(','));
      outDoubles[1] = Convert.ToDouble(parseArray[6].TrimEnd(','));
      outDoubles[2] = Convert.ToDouble(parseArray[7]);

      return outDoubles;
    }


    private string ShortenFileName(string fileName)
    {
      string[] parseArray = fileName.Split('_');
      return parseArray[9]+"_"+ parseArray[10] + "_" + parseArray[11] + "_" + parseArray[12];
    }

    //Changes model file names in a zip to match the names given for their coordinate locations
    public void ExtractWithFixedNames(string zipPath, string extractDirectory)
    {
      using (ZipArchive archive = ZipFile.OpenRead(zipPath))
      {
        foreach(ZipArchiveEntry entry in archive.Entries)
        {
          string fixedName = ShortenFileName(entry.Name);
          entry.ExtractToFile(extractDirectory + fixedName);
        }

      }
    }

    private object m_locker = new object();
    private DataSource m_datasource;
    private string m_XMLLocation;
    private string m_dataDirectory;
    private string m_extractFolderName;
    private Layer m_layer;
    private Feature m_feature;
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

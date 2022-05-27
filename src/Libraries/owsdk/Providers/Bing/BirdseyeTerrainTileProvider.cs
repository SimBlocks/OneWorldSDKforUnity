//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using sbio.Core.Math;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Globalization;
using sbio.owsdk.Services;
using sbio.owsdk.Geodetic;
using System.Net.Security;
using sbio.owsdk.Images;
using sbio.owsdk.Tiles;
using sbio.owsdk.WMS;

namespace sbio.owsdk.Providers.Bing
{
  public sealed class BirdseyeTileProvider : ITerrainTileProvider,
    IDisposable
  {
    public sealed class Settings
    {
      public static Settings Default
      {
        get { return new Settings(); }
      }

      public Settings()
      {
        BaseURL = null;
        Key = "";
        CenterPoint = "0,0";
        Include = false;
        Orientation = -1.0;
        UriScheme = null;
        ZoomLevel = -1;
      }

      /// <summary>
      /// Terrain tile provider to query if the database doesn't contain a requested image.
      /// </summary>
      public ITerrainTileProvider Fallback { get; set; }

      /// <summary>
      /// The service's base URL, prior to parameters.
      /// eg 
      ///   'http://website.com/geoserver/'
      /// which is turned to
      ///   'http://website.com/geoserver/?key=abc&centerPoint=0,0'
      /// </summary>
      public Uri BaseURL { get; set; }

      /// <summary>
      /// The Bing API key
      /// </summary>
      public string Key { get; set; }

      /// <summary>
      /// The latitude, longitude coordinates of the center point of the imagery requested
      /// </summary>
      public string CenterPoint { get; set; }

      /// <summary>
      /// A boolean indicating weather or not to include the Imagery Provider in the response
      /// </summary>
      public bool Include { get; set; }

      /// <summary>
      /// A double value in the range from 0 to 360 indicating the direction the user is facing
      /// 0 is North (default)
      /// 90 is East
      /// 180 is South
      /// 270 is West
      /// </summary>
      public double Orientation { get; set; }

      /// <summary>
      /// One of two values: http (default) and https
      /// </summary>
      public string UriScheme { get; set; }

      /// <summary>
      /// The level of zoom to be used for the imagery data
      /// Valid levels are between 1 and 21 inclusive
      /// example: 10
      /// </summary>
      public int ZoomLevel { get; set; }
    }

    public sealed class BirdseyeImageryMetadata
    {
      public BirdseyeImageryMetadata Default
      {
        get
        {
          return new BirdseyeImageryMetadata
          {
            minZoom = -1,
            maxZoom = -1,
            tilesX = -1,
            tilesY = -1,
            imageUrl = null,
            imageHeight = -1,
            imageWidth = -1,
            valid = false
          };
        }
      }

      public int minZoom { get; set; }
      public int maxZoom { get; set; }
      public int tilesX { get; set; }
      public int tilesY { get; set; }
      public string imageUrl { get; set; }
      public int imageHeight { get; set; }
      public int imageWidth { get; set; }
      public bool valid { get; set; }
    }

    public void Dispose()
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      m_Disposed = true;
    }

    public Task<int> LoadTerrainTileAsyncInto(TerrainTileIndex id, ArraySegment<byte> buffer, CancellationToken tok)
    {
      return Task.Run(
        async () =>
        {
          bool withinBounds;
          if (id.Level <= m_birdseyeMetadata.maxZoom && id.Level >= m_birdseyeMetadata.minZoom && m_birdseyeMetadata.valid)
          {
            withinBounds = true;
          }
          else
          {
            withinBounds = false;
          }

          var uri = TileToURL(id);
          if (withinBounds)
          {
            return m_TileFetch(uri, id, buffer);
          }

          if (m_Fallback != null)
          {
            return await m_Fallback.LoadTerrainTileAsyncInto(id, buffer, tok);
          }

          throw new ArgumentOutOfRangeException();
        }, tok);
    }

    public BirdseyeTileProvider(Settings settings)
    {
      counter = 0;
      m_settings = settings;
      m_Fallback = m_settings.Fallback;

      // Required fields
      var baseUri = settings.BaseURL.ToString();
      var key = settings.Key.ToString();
      var centerPoint = settings.CenterPoint.ToString();

      // Optional fields
      var include = settings.Include;
      var orientation = settings.Orientation;
      string uriScheme = null;
      if (settings.UriScheme != null)
      {
        uriScheme = settings.UriScheme.ToString();
      }

      var zoomLevel = settings.ZoomLevel;

      m_BaseURLStr = $"{baseUri}{{centerPoint}}?key={key}";

      if (include == true)
      {
        // add the ImageryProvider to the response
        m_BaseURLStr += "&include=ImageryProviders";
      }

      // if orientation is valid, include it. Otherwise just use the default
      if (orientation >= 0 && orientation < 360)
      {
        string orientationStr = orientation.ToString(CultureInfo.InvariantCulture);
        m_BaseURLStr += "&orientation=" + orientationStr;
      }

      // if the uriScheme is valid, include it. Otherwise just use the default
      if (uriScheme != null)
      {
        m_BaseURLStr += "&uriScheme=" + uriScheme;
      }

      // if the zoomLevel is specified, include it. Otherwise ignore it. 
      if (zoomLevel >= 1 && zoomLevel <= 21)
      {
        m_BaseURLStr += "&zoomLevel=" + zoomLevel.ToString();
      }

      m_birdseyeMetadata = RefreshImageryMetadata(m_BaseURLStr, centerPoint);
      m_TileFetch = AuthFetcher;
    }

    private BirdseyeImageryMetadata RefreshImageryMetadata(string url, string centerPoint)
    {
      BirdseyeImageryMetadata metadata = new BirdseyeImageryMetadata();
      url = url.Replace("{centerPoint}", centerPoint);
      Uri uri = new Uri(url);

      // Make a web request
      var webRequest = (HttpWebRequest)WebRequest.Create(uri);
      var response = (HttpWebResponse)webRequest.GetResponse();
      bool valid = false;
      if (response.StatusCode != HttpStatusCode.OK)
      {
        metadata.valid = false;
        return metadata;
      }

      using (var input = response.GetResponseStream())
      {
        if (!input.CanRead)
        {
          metadata.valid = false;
          return metadata;
        }

        JsonTextReader jsonReader = new JsonTextReader(new StreamReader(input));
        while (jsonReader.Read())
        {
          if (jsonReader.TokenType == JsonToken.StartObject)
          {
            JObject inputObject = JObject.Load(jsonReader);
            string resourceSets = (inputObject["resourceSets"] + "");
            JsonTextReader resourceReader = new JsonTextReader(new StringReader(resourceSets));
            while (resourceReader.Read())
            {
              if (resourceReader.TokenType == JsonToken.StartObject)
              {
                JObject resourceObject = JObject.Load(resourceReader);
                string resourceString = (resourceObject["resources"] + "");

                JsonTextReader thirdReader = new JsonTextReader(new StringReader(resourceString));
                while (thirdReader.Read())
                {
                  if (thirdReader.TokenType == JsonToken.StartObject)
                  {
                    JObject resource = JObject.Load(thirdReader);
                    metadata.imageUrl = (resource["imageUrl"] + "");
                    metadata.imageWidth = int.Parse(resource["imageWidth"] + "");
                    metadata.imageHeight = int.Parse(resource["imageHeight"] + "");
                    metadata.minZoom = int.Parse(resource["zoomMin"] + "");
                    metadata.maxZoom = int.Parse(resource["zoomMax"] + "");
                    metadata.tilesX = int.Parse(resource["tilesX"] + "");
                    metadata.tilesY = int.Parse(resource["tilesY"] + "");
                    valid = true;
                  }
                }
              }
            }
          }
        }
      }

      var latStr = m_settings.CenterPoint.Substring(0, m_settings.CenterPoint.IndexOf(",", StringComparison.CurrentCulture));
      var lonStr = m_settings.CenterPoint.Substring(m_settings.CenterPoint.IndexOf(",", StringComparison.CurrentCulture) + 1);
      double lat = double.Parse(latStr);
      double lon = double.Parse(lonStr);
      m_originTile = WMSConversions.GeoToTile(Geodetic2d.FromDegrees(lat, lon), metadata.minZoom);
      metadata.valid = valid;
      return metadata;
    }

    private static int AuthFetcher(Uri uri, TerrainTileIndex idx, ArraySegment<byte> buffer)
    {
      var request = (HttpWebRequest)WebRequest.Create(uri);
      request.ServerCertificateValidationCallback = CertificateValidationCallback;
      return DoFetch(request, idx, buffer);
    }

    private static int GetImageUrl(HttpWebRequest request)
    {
      var response = (HttpWebResponse)request.GetResponse();

      using (var input = response.GetResponseStream())
      {
        string inputString = input.ToString();
        JsonTextReader reader = new JsonTextReader(new StringReader(inputString));
      }

      return -1;
    }

    private static int DoFetch(HttpWebRequest request, TerrainTileIndex idx, ArraySegment<byte> buffer)
    {
      counter++;
      string currentImageUrl = m_birdseyeMetadata.imageUrl;
      currentImageUrl = currentImageUrl.Replace("{subdomain}", GetSubdomain());
      currentImageUrl = currentImageUrl.Replace("{zoom}", GetZoom(idx));
      currentImageUrl = currentImageUrl.Replace("{tileId}", GetTileId(idx, m_birdseyeMetadata.tilesX, m_birdseyeMetadata.tilesY));

      // Get the image from imageUrl
      var imageUri = new Uri(currentImageUrl);
      var imageRequest = (HttpWebRequest)WebRequest.Create(imageUri);
      imageRequest.ServerCertificateValidationCallback = CertificateValidationCallback;
      var imageResponse = (HttpWebResponse)imageRequest.GetResponse();


      using (var imageInput = imageResponse.GetResponseStream())
      {
        var tempStream = new MemoryStream(m_birdseyeMetadata.imageWidth * m_birdseyeMetadata.imageHeight * 4);
        imageInput.CopyTo(tempStream);

        // Resize the image to IMAGE_SIZExIMAGE_SIZE pixels.
        ResizeStream(tempStream, buffer, m_birdseyeMetadata.imageHeight, m_birdseyeMetadata.imageWidth, IMAGE_SIZE, IMAGE_SIZE);

        return buffer.Count;
      }
    }

    private Uri TileToURL(TerrainTileIndex idx)
    {
      return new Uri(m_BaseURLStr);
    }

    /// <summary>
    /// Returns one of the possible subdomains for the request
    /// Using subdomains allows more requests to go through at a time as some
    /// browsers limit concurrent domain requests
    /// </summary>
    /// <returns></returns>
    private static string GetSubdomain()
    {
      return "t0";
    }

    private static string GetZoom(TerrainTileIndex idx)
    {
      // use the level in TerrainTileIndex for this. 
      // check to make sure it is within acceptable bounds
      var level = idx.Level;
      return m_birdseyeMetadata.maxZoom.ToString();
    }

    /// <summary>
    /// Returns the zoom level or level of detail clipped to the minimum and maximum zoom provided by the birdseye request
    /// </summary>
    /// <param name="idx"></param>
    /// <param name="minZoom">the lowest zoom or level of detail supported</param>
    /// <param name="maxZoom">the highest zoom or level of detail supported</param>
    /// <returns>the clipped zoom or level of detail as a string</returns>
    private static string GetZoom(TerrainTileIndex idx, int minZoom, int maxZoom)
    {
      var level = idx.Level;
      int clippedLevel = Math.Min(Math.Max(idx.Level, minZoom), maxZoom);
      return clippedLevel + "";
    }

    public static string GetTileId()
    {
      return "0";
    }

    /// <summary>
    /// Returns the tileID that identifies which tile needs to be fetched
    /// </summary>
    /// <returns>The tile ID as a string</returns>
    private static string GetTileId(TerrainTileIndex idx, int tilesX, int tilesY)
    {
      // convert the string with the center point to doubles and use that as the center. 
      var latStr = m_settings.CenterPoint.Substring(0, m_settings.CenterPoint.IndexOf(",", StringComparison.CurrentCulture));
      var lonStr = m_settings.CenterPoint.Substring(m_settings.CenterPoint.IndexOf(",", StringComparison.CurrentCulture) + 1);
      var lat = double.Parse(latStr);
      var lon = double.Parse(lonStr);

      // find distance from the center of that image
      // convert the center point to rows and columns at the current LOD/Zoom level and compare to the row and column held in m_Id
      var originTile = WMSConversions.GeoToTile(Geodetic2d.FromDegrees(lat, lon), idx.Level);

      // tile needs to be within half of tilesX in the x direction and tilesY in the y direction as given by the JSON packet and zoom level
      if (idx.Column <= (originTile.Column + (tilesX / 2)) && idx.Column >= (originTile.Column - (tilesX / 2)))
      {
        if (idx.Row <= (originTile.Row + (tilesY / 2)) && idx.Row >= (originTile.Row - (tilesY / 2)))
        {
          int x = idx.Column - originTile.Column + (tilesX / 2);
          int y = idx.Row - originTile.Row + (tilesY / 2);
          int tileIndex = (y * 14) + x;

          return tileIndex.ToString();
        }
      }

      return "-1";
    }

    /// <summary>
    /// Resizes an image from width x height pixels to newWidth x newHeight pixels and returns the result in dest
    /// </summary>
    /// <param name="src">The memory stream containing the original image in a format that can be decoded by the ImageDecoder</param>
    /// <param name="dest">An ArraySegment where the resulting PNG will be stored</param>
    /// <param name="height">The height in pixels of the original image</param>
    /// <param name="width">The width in pixels of the original image</param>
    /// <param name="newHeight">The height in pixels of the resized image</param>
    /// <param name="newWidth">The width in pixels of the resized image</param>
    private static void ResizeStream(MemoryStream src, ArraySegment<byte> dest, int height, int width, int newHeight, int newWidth)
    {
      pixelImageLength = src.Length;
      int[] pixels = new int[src.Length];

      // Decode the original image into an ARGB image format
      ARGBImage argb = ImageDecoder.Decode(src);

      pixels = argb.Pixels;
      // create the pixel array and calculate the scale in the x and y directions
      var destImagePixels = new int[newWidth * newHeight];
      var xScale = newWidth / (double)width;
      var yScale = newHeight / (double)height;

      for (var y = 0; y < newHeight; ++y)
      {
        // calculate the y position of the source pixel
        var srcY = (y / yScale);
        for (var x = 0; x < newWidth; ++x)
        {
          // calculate the x position of the source pixel
          var srcX = (x / xScale);
          // Interpolate the pixel at (srcX, srcY) and set the pixel in the new image at position (x, y)
          destImagePixels[y * newWidth + x] = NumUtil.InterpolateColor(srcY, srcX, pixels, height, width);
        }
      }

      // Reformat the pixel array as a PNG so it can be consumed by the OneWorldSDK Unity Demo
      ImageEncoder.Encode(new ARGBImage(newHeight, newWidth, destImagePixels), ImageType.PNG, dest);
    }

    private static bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      //https://www.mono-project.com/archived/usingtrustedrootsrespectfully/
      //¯\_(ツ)_/¯
      return true;
    }

    public int ArraySize()
    {
      return (int)pixelImageLength;
    }

    public static void SavePNGA(string path, int[] argb, int width, int height)
    {
      var file = new FileInfo(path);
      file.Directory.Create();
      using (var stream = file.OpenWrite())
      {
        ImageEncoder.Encode(new ARGBImage(height, width, argb), ImageType.PNG, stream);
      }
    }

    private readonly Func<Uri, TerrainTileIndex, ArraySegment<byte>, int> m_TileFetch;
    private readonly ITerrainTileProvider m_Fallback;
    public readonly string m_BaseURLStr;

    private bool m_Disposed;
    private static Settings m_settings;

    public static long pixelImageLength;
    public static int counter;
    private const int IMAGE_SIZE = 256;
    private static BirdseyeImageryMetadata m_birdseyeMetadata;
    private static TerrainTileIndex m_originTile;
  }
}



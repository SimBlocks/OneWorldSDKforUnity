//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using sbio.owsdk.Services;
using sbio.owsdk.Tiles;
using sbio.owsdk.WMS;
using DotSpatial.Projections;

namespace sbio.owsdk.Providers.WebServices
{
  public sealed class WMSTileProvider : ITerrainTileProvider,
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
        Username = null;
        Password = null;
        Layers = Enumerable.Empty<string>();
        Format = "jpeg";
        Srs = "EPSG:3857";
      }

      /// <summary>
      /// The service's base URL, prior to parameters.
      /// eg 
      ///   'http://website.com/geoserver/wms/'
      /// which is turned to
      ///   'http://website.com/geoserver/wms/?service=WMS'
      /// </summary>
      public Uri BaseURL { get; set; }

      /// <summary>
      /// Username, if any is required, for this server
      /// </summary>
      public string Username { get; set; }

      /// <summary>
      /// Password, if any is required, for this server
      /// </summary>
      public string Password { get; set; }

      /// <summary>
      /// The layers to retrieve
      /// </summary>
      public IEnumerable<string> Layers { get; set; }

      /// <summary>
      /// The format to use, such as 'png' and 'jpeg'
      /// </summary>
      public string Format { get; set; }

      /// <summary>
      /// The Srs to use, in <Authority>:<Code> form,
      /// eg 'EPSG:3857'
      /// </summary>
      public string Srs { get; set; }
    }

    public ITileMapper TileMapper { get; set; }

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
      return Task.Run(() =>
      {
        var uri = TileToURL(id);
        return m_TileFetch(uri, buffer);
      }, tok);
    }

    public WMSTileProvider(Settings settings)
    {
      var baseUri = settings.BaseURL.ToString();
      var format = Uri.EscapeDataString("image/" + settings.Format);
      var layers = string.Join(",", settings.Layers.Select(Uri.EscapeDataString));
      {
        var match = sc_SrsRegex.Match(settings.Srs);
        if (!match.Success)
        {
          throw new InvalidDataException("Malformed SRS string");
        }

        var authority = match.Groups[1].Value;
        var code = int.Parse(match.Groups[2].Value);
        m_SrsProjection = ProjectionInfo.FromAuthorityCode(authority, code);
      }

      var srs = Uri.EscapeDataString(settings.Srs);

      m_BaseURLStr = $"{baseUri}?service=WMS&version=1.1.0&request=GetMap&layers={layers}&styles=&width=256&height=256&srs={srs}&format={format}";

      if (settings.Username != null)
      {
        m_TileFetch = BasicAuthFetcher(settings.Username, settings.Password);
      }
      else
      {
        m_TileFetch = NoAuthFetcher;
      }

      TileMapper = WMSTileMapper.Instance;
    }

    private static Func<Uri, ArraySegment<byte>, int> BasicAuthFetcher(string username, string password)
    {
      var encoded = Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
      return (uri, buffer) =>
      {
        var request = (HttpWebRequest)WebRequest.Create(uri);
        request.Headers.Add("Authorization", "Basic " + encoded);
        request.ServerCertificateValidationCallback = CertificateValidationCallback;
        return DoFetch(request, buffer);
      };
    }

    private static int NoAuthFetcher(Uri uri, ArraySegment<byte> buffer)
    {
      var request = (HttpWebRequest)WebRequest.Create(uri);
      request.ServerCertificateValidationCallback = CertificateValidationCallback;
      return DoFetch(request, buffer);
    }

    private static int DoFetch(HttpWebRequest request, ArraySegment<byte> buffer)
    {
      var response = (HttpWebResponse)request.GetResponse();

      using (var input = response.GetResponseStream())
      {
        var memStream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count, true);
        input.CopyTo(memStream);
        return (int)memStream.Position;
      }
    }

    private Uri TileToURL(TerrainTileIndex idx)
    {
      var bounds = TileMapper.TileToBounds(idx);
      var south = bounds.SouthDegrees;
      var west = bounds.WestDegrees;
      var north = bounds.NorthDegrees;
      var east = bounds.EastDegrees;
      var points = new double[] {west, south, east, north};
      var zs = new double[points.Length / 2];
      Reproject.ReprojectPoints(points, zs, sc_WGS84Projection, m_SrsProjection, 0, zs.Length);
      return new Uri($"{m_BaseURLStr}&bbox={points[0]:0.###},{points[1]:0.###},{points[2]:0.###},{points[3]:0.###}");
    }

    private static bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      //https://www.mono-project.com/archived/usingtrustedrootsrespectfully/
      //¯\_(ツ)_/¯
      return true;
    }

    private static readonly Regex sc_SrsRegex = new Regex(@"(.*)\:(\d+)");
    private static readonly ProjectionInfo sc_WGS84Projection = ProjectionInfo.FromEpsgCode(4326);

    private readonly Func<Uri, ArraySegment<byte>, int> m_TileFetch;
    private readonly string m_BaseURLStr;
    private readonly ProjectionInfo m_SrsProjection;
    private bool m_Disposed;
  }
}



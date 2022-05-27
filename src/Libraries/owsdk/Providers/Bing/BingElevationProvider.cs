//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sbio.owsdk.Services;

namespace sbio.owsdk.Providers.Bing
{
  public sealed class BingElevationProvider : IElevationProvider
  {
    public sealed class Settings
    {
      public static Settings Default
      {
        get { return new Settings(); }
      }

      public Settings()
      {
        APIKey = null;
        retryCount = 0;
      }

      public string APIKey { get; set; }
      public int retryCount { get; set; }
    }

    public Task QueryPointSamplesAsyncInto(ArraySegment<ElevationPointSample> points, CancellationToken tok)
    {
      return QueryPointSamplesAsyncInto(points, tok, 0);
    }

    public Task QueryPointSamplesAsyncInto(ArraySegment<ElevationPointSample> points, CancellationToken tok, int recCount)
    {
      return Task.Run(async () =>
      {
        var ary = points.Array;
        var startIdx = points.Offset;
        var endIdx = startIdx + points.Count;
        try
        {
          using (var responseStream = await RequestPointsAsync(points))
          {
            JObject response;
            using (var textReader = new StreamReader(responseStream))
            using (var jsonReader = new JsonTextReader(textReader))
            {
              response = (JObject)JToken.ReadFrom(jsonReader);
            }

            var statusCode = (int)response["statusCode"];

            if (statusCode != 200)
            {
              var statusDescription = (string)response["statusDescription"];
              throw new Exception(string.Format("Error ({0}) while requesting elevations: {1}", statusCode, statusDescription));
            }

            var elevationsData = (JArray)response["resourceSets"][0]["resources"][0]["elevations"];

            for (var i = startIdx; i < endIdx; ++i)
            {
              var pos = ary[i].Position;
              var elev = (int)elevationsData[i - startIdx];
              ary[i] = new ElevationPointSample(pos, elev);
            }
          }
        }
        catch
        {
          //Some public bing keys are slow to respond
          //Retry grabbing terrain a number of times specified in the json file
          recCount++;
          if (recCount < m_retryCount)
            await QueryPointSamplesAsyncInto(points, tok, recCount);
          else
          {
            //On failure, zero out the points
            for (var i = startIdx; i < endIdx; ++i)
            {
              var pos = ary[i].Position;
              ary[i] = new ElevationPointSample(pos, 0);
            }
          }
        }
      }, tok);
    }

    public BingElevationProvider(Settings settings)
    {
      m_APIKey = settings.APIKey;
      m_PostRequestURL = string.Format(@"https://dev.virtualearth.net/REST/v1/Elevation/List?key={0}", m_APIKey);
      m_retryCount = settings.retryCount;
    }

    private static bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      //https://www.mono-project.com/archived/usingtrustedrootsrespectfully/
      //¯\_(ツ)_/¯
      return true;
    }

    private static MemoryStream CopyToMemoryStream(Stream inputStream)
    {
      var ms = new MemoryStream();
      inputStream.CopyTo(ms);
      ms.Position = 0;
      return ms;
    }

    private static string GetPointsAsString(ArraySegment<ElevationPointSample> points)
    {
      var sb = new StringBuilder("points=");
      var ary = points.Array;
      var startIdx = points.Offset;
      var endIdx = startIdx + points.Count;
      for (var i = startIdx; i < endIdx; ++i)
      {
        var pos = ary[i].Position;
        //Only need 5 decimal places. Any more are insignificant.
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:0.#####},{1:0.#####}", pos.LatitudeDegrees, pos.LongitudeDegrees);

        if (i < endIdx - 1)
        {
          sb.Append(",");
        }
      }

      return sb.ToString();
    }

    private static Task<Stream> PostStringAsync(Uri url, string data, string contentType)
    {
      var tcs = new TaskCompletionSource<Stream>();

      var request = WebRequest.Create(url);
      ((HttpWebRequest)request).ServerCertificateValidationCallback = CertificateValidationCallback;
      request.Method = "POST";

      if (!string.IsNullOrWhiteSpace(contentType))
      {
        request.ContentType = contentType;
      }
      else
      {
        request.ContentType = "text/plain;charset=utf-8";
      }

      request.BeginGetRequestStream((a) =>
      {
        try
        {
          var r = (HttpWebRequest)a.AsyncState;

          //Add data to request stream
          using (var requestStream = r.EndGetRequestStream(a))
          {
            var bytes = Encoding.UTF8.GetBytes(data);
            requestStream.Write(bytes, 0, bytes.Length);
          }

          request.BeginGetResponse((a2) =>
          {
            try
            {
              var r2 = (HttpWebRequest)a2.AsyncState;

              using (var response = (HttpWebResponse)r2.EndGetResponse(a2))
              {
                tcs.SetResult(CopyToMemoryStream(response.GetResponseStream()));
              }
            }
            catch (WebException ex)
            {
              if (ex.Response != null)
              {
                tcs.SetResult(CopyToMemoryStream(ex.Response.GetResponseStream()));
              }
              else
              {
                tcs.SetException(ex);
              }
            }
            catch (Exception ex)
            {
              tcs.SetException(ex);
            }
          }, request);
        }
        catch (WebException ex)
        {
          if (ex.Response != null)
          {
            tcs.SetResult(CopyToMemoryStream(ex.Response.GetResponseStream()));
          }
          else
          {
            tcs.SetException(ex);
          }
        }
        catch (Exception ex)
        {
          tcs.SetException(ex);
        }
      }, request);

      return tcs.Task;
    }

    /// <summary>
    /// Downloads data as a stream from a URL.
    /// </summary>
    /// <param name="url">URL that points to data to download.</param>
    /// <returns>A stream with the data.</returns>
    private static Task<Stream> GetStreamAsync(Uri url)
    {
      var tcs = new TaskCompletionSource<Stream>();

      var request = WebRequest.Create(url);
      ((HttpWebRequest)request).ServerCertificateValidationCallback = CertificateValidationCallback;
      request.BeginGetResponse((a) =>
      {
        try
        {
          var r = (HttpWebRequest)a.AsyncState;
          using (var response = (HttpWebResponse)r.EndGetResponse(a))
          {
            tcs.SetResult(CopyToMemoryStream(response.GetResponseStream()));
          }
        }
        catch (WebException ex)
        {
          if (ex.Response != null)
          {
            tcs.SetResult(CopyToMemoryStream(ex.Response.GetResponseStream()));
          }
          else
          {
            tcs.SetException(ex);
          }
        }
        catch (Exception ex)
        {
          tcs.SetException(ex);
        }
      }, request);

      return tcs.Task;
    }

    private Task<Stream> RequestPointsAsync(ArraySegment<ElevationPointSample> points)
    {
      if (points.Count > 50)
      {
        //Make a post request when there are more than 50 points as there is a risk of URL becoming too large for a GET request.
        return PostStringAsync(new Uri(m_PostRequestURL), GetPointsAsString(points), null);
      }
      else
      {
        return GetStreamAsync(new Uri(m_PostRequestURL + "&" + GetPointsAsString(points)));
      }
    }

    private readonly string m_APIKey;
    private readonly string m_PostRequestURL;
    private readonly int m_retryCount;
  }
}



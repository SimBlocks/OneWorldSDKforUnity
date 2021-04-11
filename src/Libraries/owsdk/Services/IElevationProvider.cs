//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using sbio.owsdk.Geodetic;

namespace sbio.owsdk.Services
{
  public interface IElevationProvider
  {
    Task QueryPointSamplesAsyncInto(ArraySegment<ElevationPointSample> points, CancellationToken tok);
  }

  public static class IElevationProviderExtensions
  {
    public static double QueryElevation(this IElevationProvider provider, Geodetic2d point)
    {
      return QueryElevations(provider, Enumerable.Repeat(point, 1)).First();
    }

    public static double[] QueryElevations(this IElevationProvider provider, IEnumerable<Geodetic2d> points)
    {
      var samples = points.Select(p => new ElevationPointSample(p)).ToArray();

      provider.QueryPointSamplesInto(samples);

      var ret = new double[samples.Length];
      for (var i = 0; i < ret.Length; ++i)
      {
        ret[i] = samples[i].Elevation;
      }

      return ret;
    }

    public static void QueryPointSamplesInto(this IElevationProvider provider, ArraySegment<ElevationPointSample> points)
    {
      provider.QueryPointSamplesAsyncInto(points).Wait();
    }

    public static Task QueryPointSamplesAsyncInto(this IElevationProvider provider, ArraySegment<ElevationPointSample> points)
    {
      return provider.QueryPointSamplesAsyncInto(points, CancellationToken.None);
    }

    public static void QueryPointSamplesInto(this IElevationProvider provider, ElevationPointSample[] points)
    {
      provider.QueryPointSamplesAsyncInto(points).Wait();
    }

    public static Task QueryPointSamplesAsyncInto(this IElevationProvider provider, ElevationPointSample[] points)
    {
      return provider.QueryPointSamplesAsyncInto(points, CancellationToken.None);
    }

    public static Task QueryPointSamplesAsyncInto(this IElevationProvider provider, ElevationPointSample[] points, CancellationToken tok)
    {
      return provider.QueryPointSamplesAsyncInto(new ArraySegment<ElevationPointSample>(points), tok);
    }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

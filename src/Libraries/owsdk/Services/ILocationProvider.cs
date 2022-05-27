//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace sbio.owsdk.Services
{
  public interface ILocationProvider
  {
    Task QueryLocationsAsyncInto(string locationName, List<LocationInfo> results, CancellationToken tok);
  }

  public static class ILocationProviderExtensions
  {
    public static Task QueryLocationsAsyncInto(this ILocationProvider obj, string locationName, List<LocationInfo> results)
    {
      return obj.QueryLocationsAsyncInto(locationName, results, CancellationToken.None);
    }

    public static IEnumerable<LocationInfo> QueryLocations(this ILocationProvider obj, string locationName)
    {
      return obj.QueryLocationsAsync(locationName).Result;
    }

    public static Task<IEnumerable<LocationInfo>> QueryLocationsAsync(this ILocationProvider obj, string locationName)
    {
      return obj.QueryLocationsAsync(locationName, CancellationToken.None);
    }

    public static async Task<IEnumerable<LocationInfo>> QueryLocationsAsync(this ILocationProvider obj, string locationName, CancellationToken tok)
    {
      var result = new List<LocationInfo>();
      await obj.QueryLocationsAsyncInto(locationName, result, tok);
      return result;
    }
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

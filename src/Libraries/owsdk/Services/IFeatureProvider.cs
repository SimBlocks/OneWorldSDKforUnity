//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Threading;
using System.Threading.Tasks;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Features;

namespace sbio.owsdk.Services
{
  public interface IFeatureProvider
  {
    Task QueryFeaturesIn(GeoBoundingBox id, Action<Feature> observer, CancellationToken tok);
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

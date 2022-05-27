//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Services;

namespace sbio.owsdk
{
  public interface IWorldContext : IRTOContext
  {
    event Action<IElevationProvider> ElevationProviderChanged;
    event Action<IFeatureProvider> FeatureProviderChanged;

    Ellipsoid Ellipsoid { get; }

    IElevationProvider ElevationProvider { get; set; }

    IFeatureProvider FeatureProvider { get; set; }
  }
}



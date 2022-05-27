//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.owsdk.Features;
using sbio.owsdk.Geodetic;

namespace sbio.OneWorldSDKViewer
{
  /// <summary>
  /// An ICreateFeature represents a 'creatable' feature.
  /// These may actually include a group of objects (a forest)
  /// Or just one (a building)
  /// </summary>
  public interface ICreateFeature
  {
    Feature Feature { get; }

    /// <summary>
    /// Creates any objects which fall inside the given bounding box.
    /// </summary>
    /// <param name="bbox">The bounding box to restrict objects to.</param>
    /// <returns>An IManageFeature which manages the set of objects in the bounding box, or null if no objects are available.</returns>
    IManageFeature Create(GeoBoundingBox bbox);
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

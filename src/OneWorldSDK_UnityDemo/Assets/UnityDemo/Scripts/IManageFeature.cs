//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;

namespace sbio.OneWorldSDKViewer
{
  /// <summary>
  /// An IManageFeature represents a 'created' feature
  /// A 'created' feature may actually be 0 or more objects
  /// </summary>
  public interface IManageFeature : IDisposable
  {
    /// <summary>
    /// The the ICreateFeature this manager is referring to
    /// </summary>
    ICreateFeature Creator { get; }

    /// <summary>
    /// Get whether or not this manager is visible
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Show this set of features.
    /// </summary>
    void ShowFeature();

    /// <summary>
    /// Hide this set of features.
    /// </summary>
    void HideFeature();
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

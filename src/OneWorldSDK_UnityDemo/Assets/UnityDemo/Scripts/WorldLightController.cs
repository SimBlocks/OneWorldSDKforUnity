//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.Core.Math;
using sbio.owsdk.Geodetic;
using UnityEngine;
using sbio.owsdk.Unity.Extensions;

namespace sbio.OneWorldSDKViewer
{
  /// <summary>
  /// Points a transform straight 'down' from where the viewer's camera base currently is
  /// </summary>
  public class WorldLightController : MonoBehaviour
  {
    public OneWorldSDKViewerContext OneWorldSDKViewerContext;

    public void Update()
    {
      //Rotate to point straight 'down'
      var cam = OneWorldSDKViewerContext.Camera;
      if (cam != null)
      {
        Vec3LeftHandedGeocentric normal;
        var north = OneWorldSDKViewerContext.WorldContext.Ellipsoid.NorthDirection((Geodetic2d)OneWorldSDKViewerContext.WorldContext.RTOToGeo(cam.transform.position), out normal);
        transform.rotation = Quaternion.LookRotation(-normal.ToVector3(), north.ToVector3());
      }
    }
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.Core.Math;
using UnityEngine;
using sbio.owsdk.Unity.Extensions;

namespace sbio.owsdk.Unity
{
  public class GeocentricTransform : MonoBehaviour
  {
    #region MonoBehaviour

    public WorldContext WorldContext;
    public double X;
    public double Y;
    public double Z;

    private void OnEnable()
    {
      OnWorldOriginChanged(WorldContext.WorldOrigin);
      WorldContext.WorldOriginChanged.Event += OnWorldOriginChanged;
    }

    private void OnDisable()
    {
      WorldContext.WorldOriginChanged.Event -= OnWorldOriginChanged;
      OnWorldOriginChanged(Vec3LeftHandedGeocentric.Zero);
    }

    #endregion

    private void OnWorldOriginChanged(Vec3LeftHandedGeocentric newOrigin)
    {
      var globalPos = new Vec3LeftHandedGeocentric(X, Y, Z);
      transform.position = (globalPos - newOrigin).ToVector3();
    }
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

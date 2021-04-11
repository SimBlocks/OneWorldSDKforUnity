//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.Core.Math;
using UnityEngine.Events;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Concrete implementation of a GameEventListener receiving a Vector3d argument
  /// </summary>
  public sealed class Vector3dEventListener : GameEventListener<Vec3LeftHandedGeocentric>
  {
    public Vector3dEvent EventSource;
    public Vector3dUnityEvent EventResponse;

    protected override GameEvent<Vec3LeftHandedGeocentric> Source
    {
      get { return EventSource; }
    }

    protected override UnityEvent<Vec3LeftHandedGeocentric> Response
    {
      get { return EventResponse; }
    }
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

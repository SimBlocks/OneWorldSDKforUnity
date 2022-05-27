//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.Core.Math;
using UnityEngine;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Concrete implementation of GameEvent receiving a Vec3LeftHandedGeocentric argument
  /// </summary>
  [CreateAssetMenu(menuName = "OWSDK/Events/Vector3d Event", order = 160)]
  public sealed class Vector3dEvent : GameEvent<Vec3LeftHandedGeocentric>
  {
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

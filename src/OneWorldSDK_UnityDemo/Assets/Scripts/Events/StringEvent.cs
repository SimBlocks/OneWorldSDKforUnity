//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using UnityEngine;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Concrete implementation of GameEvent receiving a string argument
  /// </summary>
  [CreateAssetMenu(menuName = "OWSDK/Events/String Event", order = 150)]
  public sealed class StringEvent : GameEvent<string>
  {
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

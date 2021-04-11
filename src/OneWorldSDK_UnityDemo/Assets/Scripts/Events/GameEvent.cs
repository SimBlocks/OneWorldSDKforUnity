//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using UnityEngine;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// An event implemented as a ScriptableObject, which recives no arguments
  /// </summary>
  [CreateAssetMenu(menuName = "OWSDK/Events/Event", order = 100)]
  public sealed class GameEvent : ScriptableObject
  {
    public event Action Event;

    public void Raise()
    {
      Event?.Invoke();
    }
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;
using sbio.owsdk.Async;
using UnityEngine;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Concrete implementation of GameEvent receiving an int argument
  /// </summary>
  [CreateAssetMenu(menuName = "OWSDK/Events/Async Op Event", order = 180)]
  public sealed class BeginAsyncOpEvent : GameEvent<IList<IEnumerator<bool>>, AsyncCancellationToken>
  {
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

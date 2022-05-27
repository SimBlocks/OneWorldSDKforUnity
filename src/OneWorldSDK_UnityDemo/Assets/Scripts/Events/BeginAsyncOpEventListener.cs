//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Collections.Generic;
using sbio.owsdk.Async;
using UnityEngine.Events;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Concrete implementation of a GameEventListener receiving an async op argument
  /// </summary>
  public sealed class BeginAsyncOpEventListener : GameEventListener<IList<IEnumerator<bool>>, AsyncCancellationToken>
  {
    public BeginAsyncOpEvent EventSource;
    public BeginAsyncOpUnityEvent EventResponse;

    protected override GameEvent<IList<IEnumerator<bool>>, AsyncCancellationToken> Source
    {
      get { return EventSource; }
    }

    protected override UnityEvent<IList<IEnumerator<bool>>, AsyncCancellationToken> Response
    {
      get { return EventResponse; }
    }
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

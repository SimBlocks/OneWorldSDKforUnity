//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using UnityEngine.Events;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Concrete implementation of a GameEventListener receiving an int argument
  /// </summary>
  public sealed class IntEventListener : GameEventListener<int>
  {
    public IntEvent EventSource;
    public IntUnityEvent EventResponse;

    protected override GameEvent<int> Source
    {
      get { return EventSource; }
    }

    protected override UnityEvent<int> Response
    {
      get { return EventResponse; }
    }
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

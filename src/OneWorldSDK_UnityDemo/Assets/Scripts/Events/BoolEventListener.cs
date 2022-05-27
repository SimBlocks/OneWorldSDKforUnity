//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using UnityEngine.Events;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Concrete implementation of a GameEventListener receiving a bool argument
  /// </summary>
  public sealed class BoolEventListener : GameEventListener<bool>
  {
    public BoolEvent EventSource;
    public BoolUnityEvent EventResponse;

    protected override GameEvent<bool> Source
    {
      get { return EventSource; }
    }

    protected override UnityEvent<bool> Response
    {
      get { return EventResponse; }
    }
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

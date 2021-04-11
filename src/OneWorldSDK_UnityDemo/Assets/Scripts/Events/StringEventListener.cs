//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using UnityEngine.Events;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Concrete implementation of a GameEventListener receiving a string argument
  /// </summary>
  public sealed class StringEventListener : GameEventListener<string>
  {
    public StringEvent EventSource;
    public StringUnityEvent EventResponse;

    protected override GameEvent<string> Source
    {
      get { return EventSource; }
    }

    protected override UnityEvent<string> Response
    {
      get { return EventResponse; }
    }
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

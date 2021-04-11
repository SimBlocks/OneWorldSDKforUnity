//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using UnityEngine.Events;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Concrete implementation of a GameEventListener receiving a float argument
  /// </summary>
  public sealed class FloatEventListener : GameEventListener<float>
  {
    public FloatEvent EventSource;
    public FloatUnityEvent EventResponse;

    protected override GameEvent<float> Source
    {
      get { return EventSource; }
    }

    protected override UnityEvent<float> Response
    {
      get { return EventResponse; }
    }
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

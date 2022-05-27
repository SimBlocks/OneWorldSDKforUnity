//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using UnityEngine.Events;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Concrete implementation of a GameEventListener receiving a double argument
  /// </summary>
  public sealed class DoubleEventListener : GameEventListener<double>
  {
    public DoubleEvent EventSource;
    public DoubleUnityEvent EventResponse;

    protected override GameEvent<double> Source
    {
      get { return EventSource; }
    }

    protected override UnityEvent<double> Response
    {
      get { return EventResponse; }
    }
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

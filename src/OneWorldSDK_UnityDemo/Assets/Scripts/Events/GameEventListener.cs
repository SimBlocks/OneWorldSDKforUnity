//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using UnityEngine;
using UnityEngine.Events;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// A MonoBehaviour that forwards a GameEvent invocation to a UnityEvent
  /// </summary>
  public sealed class GameEventListener : MonoBehaviour
  {
    #region MonoBehaviour

    public GameEvent Event;
    public UnityEvent Response;

    private void OnEnable()
    {
      Event.Event += Response.Invoke;
    }

    private void OnDisable()
    {
      Event.Event -= Response.Invoke;
    }

    #endregion

    public void OnEventRaised()
    {
      Response.Invoke();
    }
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

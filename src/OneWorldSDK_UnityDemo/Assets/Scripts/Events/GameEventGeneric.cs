//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using UnityEngine;

namespace sbio.owsdk.Unity.Events
{
  /// <summary>
  /// Abstract base class for events implemented as ScriptableObjects
  /// These are for events that receive a single argument
  /// </summary>
  /// <typeparam name="T">The type of the event argument</typeparam>
  public abstract class GameEvent<T> : ScriptableObject
  {
    public event Action<T> Event;

    public void Raise(T arg)
    {
      Event?.Invoke(arg);
    }
  }

  /// <summary>
  /// Abstract base class for events implemented as ScriptableObjects
  /// These are for events that receive two arguments
  /// </summary>
  /// <typeparam name="T1">The type of the event argument</typeparam>
  public abstract class GameEvent<T1, T2> : ScriptableObject
  {
    public event Action<T1, T2> Event;

    public void Raise(T1 arg1, T2 arg2)
    {
      Event?.Invoke(arg1, arg2);
    }
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

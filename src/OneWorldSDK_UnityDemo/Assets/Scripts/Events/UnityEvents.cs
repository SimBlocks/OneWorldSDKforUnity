//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using sbio.owsdk.Async;
using sbio.owsdk.Tiles;
using sbio.Core.Math;
using UnityEngine.Events;
using System.Collections.Generic;

//Various concrete implementations of UnityEvent<T>
namespace sbio.owsdk.Unity.Events
{
  [Serializable]
  public sealed class BoolUnityEvent : UnityEvent<bool>
  {
  }

  [Serializable]
  public sealed class IntUnityEvent : UnityEvent<int>
  {
  }

  [Serializable]
  public sealed class FloatUnityEvent : UnityEvent<float>
  {
  }

  [Serializable]
  public sealed class DoubleUnityEvent : UnityEvent<double>
  {
  }

  [Serializable]
  public sealed class StringUnityEvent : UnityEvent<string>
  {
  }

  [Serializable]
  public sealed class Vector3dUnityEvent : UnityEvent<Vec3LeftHandedGeocentric>
  {
  }

  [Serializable]
  public sealed class TerrainTileUnityEvent : UnityEvent<TerrainTileIndex>
  {
  }

  [Serializable]
  public sealed class BeginAsyncOpUnityEvent : UnityEvent<IList<IEnumerator<bool>>, AsyncCancellationToken>
  {
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

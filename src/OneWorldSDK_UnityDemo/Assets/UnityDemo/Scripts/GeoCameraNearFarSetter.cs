//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
﻿using UnityEngine;
using sbio.owsdk.Unity.Extensions;

namespace sbio.OneWorldSDKViewer
{
  [RequireComponent(typeof(Camera))]
  public sealed class GeoCameraNearFarSetter : MonoBehaviour
  {
    #region MonoBehaviour

    public OneWorldSDKViewerContext OneWorldSDKViewerContext;

    private void Start()
    {
      m_Camera = GetComponent<Camera>();
    }

    private void Update()
    {
      var geoPos = OneWorldSDKViewerContext.WorldContext.RTOToGeo(transform.position);

      //Update near/far planes
      for (var i = 0; i < OneWorldSDKViewerContext.Config.NearFarSwitchouts.Length; ++i)
      {
        var info = OneWorldSDKViewerContext.Config.NearFarSwitchouts[i];
        if (geoPos.HeightMeters <= info.Distance)
        {
          var near = (float)info.Near;
          var far = (float)info.Far;

          m_Camera.nearClipPlane = near;
          m_Camera.farClipPlane = far;

          break;
        }
      }
    }

    #endregion

    private Camera m_Camera;
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

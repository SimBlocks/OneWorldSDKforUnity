//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.Core.Math;
using sbio.owsdk.Geodetic;
using UnityEngine;
using sbio.owsdk.Unity.Extensions;

namespace sbio.OneWorldSDKViewer
{
  //[RequireComponent(typeof(Camera))]
  //[RequireComponent(typeof(Skybox))]
  public sealed class OneWorldSDKViewerSkybox : MonoBehaviour
  {
    #region MonoBehaviour

    public OneWorldSDKViewerContext OneWorldSDKViewerContext;
    public Transform CameraBase;
    public Material SkySkyboxMaterial;
    public Material GalaxySkyboxMaterial;

    public void Start()
    {
      m_Camera = transform.GetChild(0).GetComponent<Camera>();
      m_Skybox = transform.GetChild(0).GetComponent<Skybox>();
    }

    public void OnEnable()
    {
      if (SkySkyboxMaterial != null)
      {
        m_SkySkyboxMaterial = new Material(SkySkyboxMaterial);
      }

      if (GalaxySkyboxMaterial != null)
      {
        m_GalaxySkyboxMaterial = new Material(GalaxySkyboxMaterial);
      }

      RenderSettings.fogMode = FogMode.Exponential;
      RenderSettings.fogDensity = OneWorldSDKViewerContext.Config.FogDensity;
    }

    public void LateUpdate()
    {
      var cam = OneWorldSDKViewerContext.Camera;
      if (cam != null)
      {
        //Sync our near and far planes with the main camera
        m_Camera.projectionMatrix = cam.projectionMatrix;
        m_Camera.nearClipPlane = cam.nearClipPlane;
        m_Camera.farClipPlane = cam.farClipPlane;

        //Also synchronize our position
        transform.position = cam.transform.position;

        //Figure out the geo position of the camera
        var cameraGeoPos = Ellipsoid.ToGeodetic3d(cam.transform.position.ToVec3LeftHandedGeocentric() + WorldOrigin);

        //Update sky position and rotation
        //Figure out 
        var heightMeters = cameraGeoPos.HeightMeters;

        var switchoutDistance = OneWorldSDKViewerContext.Config.SkyboxSwitchoutDistance;
        if (heightMeters < switchoutDistance)
        {
          //We're within the "atmosphere"
          if (m_SkySkyboxMaterial != null)
          {
            m_Skybox.material = m_SkySkyboxMaterial;
            m_SkySkyboxMaterial.SetFloat("_Exposure", 1 - Mathf.Clamp01((float)(heightMeters / switchoutDistance)));
          }

          RenderSettings.fog = true;

          //Rotate so that locally the skybox is oriented NED
          var nedRotation = Ellipsoid.NEDRotation((Geodetic2d)cameraGeoPos);
          transform.rotation = Quaternion.Inverse(nedRotation) * CameraBase.rotation;
        }
        else
        {
          if (m_GalaxySkyboxMaterial != null)
          {
            m_Skybox.material = m_GalaxySkyboxMaterial;
            m_GalaxySkyboxMaterial.SetFloat("_Exposure", Mathf.Clamp01((float)((heightMeters - switchoutDistance) / switchoutDistance)));
          }

          RenderSettings.fog = false;
          transform.rotation = CameraBase.rotation;
        }
      }
    }

    public void OnDisable()
    {
      if (m_GalaxySkyboxMaterial != null)
      {
        DestroyImmediate(m_GalaxySkyboxMaterial);
        m_GalaxySkyboxMaterial = null;
      }

      if (m_SkySkyboxMaterial != null)
      {
        DestroyImmediate(m_SkySkyboxMaterial);
        m_SkySkyboxMaterial = null;
      }

      m_Camera = null;
    }

    #endregion

    private Vec3LeftHandedGeocentric WorldOrigin
    {
      get { return OneWorldSDKViewerContext.WorldContext.WorldOrigin; }
    }

    private Ellipsoid Ellipsoid
    {
      get { return OneWorldSDKViewerContext.WorldContext.Ellipsoid; }
    }

    private Camera Camera
    {
      get { return OneWorldSDKViewerContext.Camera; }
    }

    private Camera m_Camera;
    private Skybox m_Skybox;

    //Copies of the materials since we're modifying properties on them at runtime
    private Material m_SkySkyboxMaterial;
    private Material m_GalaxySkyboxMaterial;
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

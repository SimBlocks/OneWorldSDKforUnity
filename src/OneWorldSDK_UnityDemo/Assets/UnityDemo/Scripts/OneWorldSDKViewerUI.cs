//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.Text;
using sbio.Core.Math;
using sbio.owsdk.Geodetic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.UI;
using sbio.owsdk.Unity;
using sbio.owsdk.Unity.Extensions;


namespace sbio.OneWorldSDKViewer
{
  /// <summary>
  /// UI and controls for a camera in OneWorldSDKViewer, allowing highlighting areas on the globe
  /// and switching other UI options such as wireframe
  /// Also installs itself as the main OneWorldSDKViewer camera
  /// </summary>
  [RequireComponent(typeof(Camera))]
  public sealed class OneWorldSDKViewerUI : MonoBehaviour
  {
    #region MonoBehaviour

    public OneWorldSDKViewerContext OneWorldSDKViewerContext;
    public bool DevMode = false;
    public bool ShowDevUI = true;
    public bool CheckResources = false;
    public Material BBoxMaterial;
    public GameObject SelectionPanel;
    public Text ObjectIDLabel;
    public Transform AttributesPanel;
    public GameObject AttributePrefab;

    private void Awake()
    {
      m_Camera = GetComponent<Camera>();
      OneWorldSDKViewerContext.Camera = m_Camera;

      m_ResourceChecker = new ResourceChecker
      {
        IncludeGuiElements = false,
        IncludeScriptReferences = false
      };

      m_TimeSinceLastResourceCheck = 0;
    }

    private void OnEnable()
    {
      m_BBoxObject = new GameObject("BBox");
      {
        m_BBoxRenderer = m_BBoxObject.AddComponent<MeshRenderer>();
        m_BBoxRenderer.sharedMaterial = BBoxMaterial;
        m_BBoxFilter = m_BBoxObject.AddComponent<MeshFilter>();
        m_BBoxObject.layer = LayerMask.NameToLayer("EarthOverlays");
      }
      m_BBoxRTO = new RTOTransform(OneWorldSDKViewerContext.WorldContext, m_BBoxObject.transform);

      OneWorldSDKViewerContext.WorldContext.WorldOriginChanged.Event += UpdateWorldOrigin;
    }

    //DEPRECATED DUE TO LOW PERFORMANCE
    //NEW VERSION USES MUCH FASTER CANVASES IN MouseCoordinateUI.cs
    /*private void OnGUI()
    {
      if (!EventSystem.current.IsPointerOverGameObject()
          && EventSystem.current.currentSelectedGameObject == null
          && Input.GetKey(KeyCode.Mouse0))
      {
        var x = Event.current.mousePosition.x;
        var y = Event.current.mousePosition.y;
        var latLonText = string.Format("{0,9:###.00000}°\n{1,9:###.00000}°", m_MouseGeopos.LatitudeDegrees, m_MouseGeopos.LongitudeDegrees);
        var content = new GUIContent(latLonText);
        var style = GUI.skin.box;
        style.alignment = TextAnchor.UpperLeft;
        // Compute how large the button needs to be.
        var size = style.CalcSize(content);

        GUI.color = Color.yellow;
        GUI.backgroundColor = new Color32(64, 64, 64, 128);
        GUI.Box(new Rect(x, y - size.y, size.x, size.y), latLonText);
      }

      if (DevMode)
      {
        if (ShowDevUI)
        {
          var builder = new StringBuilder();
          builder.Append("DEV");

          {
            var cameraPos3d = m_Camera.transform.position.ToVec3LeftHandedGeocentric() + WorldOrigin;
            var cameraGeopos = Ellipsoid.ToGeodetic3d(cameraPos3d);
            builder.AppendFormat("\nCamera Position:\n  {0}\n  {1} km", cameraGeopos.ToString("#00"), (cameraPos3d / 1000).ToString("F2"));
          }

          if (CheckResources)
          {
            builder.AppendFormat("\nVertices: {0}", m_ResourceChecker.TotalMeshVertices);
            builder.AppendFormat("\nTriangles: {0}", m_ResourceChecker.TotalMeshTriangles);
            builder.AppendFormat("\nTexture Usage: {0}", ResourceChecker.FormatSizeString(m_ResourceChecker.TotalTextureMemoryKB));
          }

          var content = new GUIContent(builder.ToString());
          var style = GUI.skin.box;
          var size = style.CalcSize(content);
          GUI.color = Color.red;
          GUI.backgroundColor = new Color32(64, 64, 64, 128);
          GUI.Box(new Rect(5, 5, size.x, size.y), content);
        }
      }
    }*/
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
      if (Input.GetKey(KeyCode.Mouse0))
      {
        var pos3d = Ellipsoid.ToVec3LeftHandedGeocentric(m_MouseGeopos);
        var posRTO = pos3d - WorldOrigin;
        var distanceFromScene = pos3d - m_SceneCameraPos;

        Gizmos.color = UnityEngine.Color.red;
        Gizmos.DrawSphere(posRTO.ToVector3(), (float)(distanceFromScene.Magnitude / 10));
      }
    }
#endif
    private void Update()
    {
#if UNITY_EDITOR
      {
        //Record position of the editor camera in globe viewer world space
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
          m_SceneCameraPos = sceneView.pivot.ToVec3LeftHandedGeocentric() + WorldOrigin;
        }
      }
#endif


      GameObject hoverTarget;
      m_MouseGeopos = CalcMouseGeopos(out hoverTarget);

      if (!EventSystem.current.IsPointerOverGameObject() && Input.GetKeyDown(KeyCode.Mouse0))
      {
        var featureObj = hoverTarget?.GetComponent<FeatureObjectInfo>();
        if (m_SelectedFeature == featureObj)
        {
          if (m_SelectedFeature != null)
          {
            m_SelectedFeature.Deselect();
            m_SelectedFeature = null;
          }
        }
        else
        {
          if (m_SelectedFeature != null)
          {
            m_SelectedFeature.Deselect();
          }

          m_SelectedFeature = featureObj;

          if (m_SelectedFeature != null)
          {
            m_SelectedFeature.Select();

            if (ObjectIDLabel != null)
            {
              ObjectIDLabel.text = m_SelectedFeature.name;
            }

            if (AttributesPanel != null)
            {
              while (AttributesPanel.childCount != 0)
              {
                var child = AttributesPanel.GetChild(0);
                child.SetParent(null, false);
                UnityEngine.Object.DestroyImmediate(child.gameObject);
              }

              if (AttributePrefab != null)
              {
                foreach (var attribute in m_SelectedFeature.Attributes)
                {
                  var obj = UnityEngine.Object.Instantiate(AttributePrefab, AttributesPanel);
                  var label = obj.transform.GetChild(0).GetComponent<Text>();
                  var value = obj.transform.GetChild(1).GetComponent<Text>();
                  label.text = attribute.Name;
                  value.text = attribute.Value;
                }
              }
            }
          }
        }
      }

      if (SelectionPanel != null)
      {
        SelectionPanel.SetActive(m_SelectedFeature != null);
      }

      if (Input.GetKeyDown(KeyCode.Mouse1))
      {
        //Select first point of bbox or second
        if (!m_BBoxP1.HasValue)
        {
          m_BBoxP1 = (Geodetic2d)m_MouseGeopos;
          m_BBoxObject.SetActive(false);
        }
        else
        {
          //We've already got a first point
          var p1 = m_BBoxP1.Value;
          var p2 = (Geodetic2d)m_MouseGeopos;

          Vec3LeftHandedGeocentric center;
          Quaternion rot;
          m_BBoxFilter.sharedMesh = OneWorldSDKViewerContext.WorldContext.GeoMesh(p1, p2, 50, out center, out rot);
          m_BBoxRTO.GlobalPosition = center;
          m_BBoxObject.transform.rotation = rot;
          m_BBoxP1 = null;
          m_BBoxObject.SetActive(true);
        }
      }

      if (Input.GetKeyDown(KeyCode.Mouse2))
      {
        //Clear bbox
        m_BBoxP1 = null;
        m_BBoxObject.SetActive(false);
      }

      if (Input.GetKeyDown(KeyCode.BackQuote))
      {
        //Toggle dev mode
        DevMode = !DevMode;

        if (!DevMode)
        {
          //TODO
          //m_TileChunker.ShowWireframe = false;
          //m_TileChunker.UseTestPattern = false;
        }
      }

      if (Input.GetKeyDown(KeyCode.LeftBracket))
      {
        OneWorldSDKViewerContext.CyclePreviousImageryProvider();
      }
      else if (Input.GetKeyDown(KeyCode.RightBracket))
      {
        OneWorldSDKViewerContext.CycleNextImageryProvider();
      }

      if (DevMode)
      {
        if (Input.GetKeyDown(KeyCode.K))
        {
          OneWorldSDKViewerContext.ShowWireframe = !OneWorldSDKViewerContext.ShowWireframe;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
          CheckResources = !CheckResources;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
          OneWorldSDKViewerContext.UseTestPattern = !OneWorldSDKViewerContext.UseTestPattern;
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
          ShowDevUI = !ShowDevUI;
        }

        if (CheckResources)
        {
          m_TimeSinceLastResourceCheck += Time.deltaTime;
          if (m_TimeSinceLastResourceCheck > 1)
          {
            Profiler.BeginSample("CheckResources");
            m_TimeSinceLastResourceCheck = 0;
            m_ResourceChecker.CheckResources();
            Profiler.EndSample();
          }
        }
      }
    }

    private void LateUpdate()
    {
      if (Input.GetKeyDown(KeyCode.F12))
      {
        ScreenshotManager.TakeScreenshot(OneWorldSDKViewerContext.Config.ScreenshotDirectory);
      }
    }

    private void OnDisable()
    {
      OneWorldSDKViewerContext.WorldContext.WorldOriginChanged.Event -= UpdateWorldOrigin;
      m_BBoxRTO.Dispose();
      m_BBoxRTO = null;
      Destroy(m_BBoxObject);
      m_BBoxObject = null;
    }

    #endregion

    private Ellipsoid Ellipsoid
    {
      get { return OneWorldSDKViewerContext.WorldContext.Ellipsoid; }
    }

    private Vec3LeftHandedGeocentric WorldOrigin
    {
      get { return OneWorldSDKViewerContext.WorldContext.WorldOrigin; }
    }

    private void UpdateWorldOrigin(Vec3LeftHandedGeocentric newOrigin)
    {
#if UNITY_EDITOR
      {
        //Move the editor camera opposite to the movement of the camera in order to keep it 'still'
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
          sceneView.pivot = (m_SceneCameraPos - newOrigin).ToVector3();
          sceneView.Repaint();
        }
      }
#endif
    }

    private Geodetic3d CalcMouseGeopos(out GameObject hoverTarget)
    {
      var mousPos2d = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
      var mousePos3d = m_Camera.ScreenToWorldPoint(new Vector3(mousPos2d.x, mousPos2d.y, m_Camera.nearClipPlane));

      Vector3? hitPos;
      {
        RaycastHit hitInfo;
        if (Physics.Raycast(m_Camera.transform.position, (mousePos3d - m_Camera.transform.position).normalized, out hitInfo))
        {
          hitPos = hitInfo.point;
          hoverTarget = hitInfo.collider.gameObject;
        }
        else
        {
          hitPos = null;
          hoverTarget = null;
        }
      }

      if (hitPos.HasValue)
      {
        return OneWorldSDKViewerContext.WorldContext.RTOToGeo(hitPos.Value);
      }

      return OneWorldSDKViewerContext.WorldContext.RTOToGeo(transform.position);
    }

    private Camera m_Camera;
    private Geodetic3d m_MouseGeopos;
    private FeatureObjectInfo m_SelectedFeature;

    private GameObject m_BBoxObject;
    private MeshRenderer m_BBoxRenderer;
    private MeshFilter m_BBoxFilter;
    private RTOTransform m_BBoxRTO;
    private Geodetic2d? m_BBoxP1;

    private ResourceChecker m_ResourceChecker;
    private double m_TimeSinceLastResourceCheck;

#if UNITY_EDITOR
    private Vec3LeftHandedGeocentric m_SceneCameraPos;
#endif
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

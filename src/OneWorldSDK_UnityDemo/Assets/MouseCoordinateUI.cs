//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.OneWorldSDKViewer;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Unity.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseCoordinateUI : MonoBehaviour
{
  public OneWorldSDKViewerContext OneWorldSDKViewerContext;
  public GameObject eyeCamera;

  void Start()
  {
    m_Canvas = transform.parent.parent.gameObject.GetComponent<Canvas>();
    m_Camera = eyeCamera.GetComponent<Camera>();
    m_Background = transform.GetChild(0).gameObject;
    OneWorldSDKViewerContext.Camera = m_Camera;
    m_Text = GetComponent<Text>();
  }

  void Update()
  {
    GameObject hoverTarget;

    if (Input.GetMouseButton(0))
    {
      if (!m_Background.activeInHierarchy)
        m_Background.SetActive(true);

      m_MouseGeopos = CalcMouseGeopos(out hoverTarget);
      transform.position = Input.mousePosition + new Vector3(50, 20, 0)*m_Canvas.scaleFactor;
      var latLonText = string.Format("{0,9:###.00000}°\n{1,9:###.00000}°", m_MouseGeopos.LatitudeDegrees, m_MouseGeopos.LongitudeDegrees);
      m_Text.text = latLonText;
    }
    else
    {
      if (m_Background.activeInHierarchy)
      {
        m_Background.SetActive(false);
        m_Text.text = "";
      }
    }
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

  private Text m_Text;
  private Geodetic3d m_MouseGeopos;
  private Camera m_Camera;
  private Canvas m_Canvas;
  private GameObject m_Background;
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

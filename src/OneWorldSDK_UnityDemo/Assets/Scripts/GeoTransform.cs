//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.Core.Math;
using sbio.owsdk.Geodetic;
using UnityEngine;
using sbio.owsdk.Unity.Extensions;

namespace sbio.owsdk.Unity
{
  public sealed class GeoTransform : MonoBehaviour
  {
    #region MonoBehaviour

    public WorldContext WorldContext;
    public SerializedGeodetic3d GeoPosition;

    //If true, object rotation will be kept the same relative to the surface of the globe
    public bool RotateUp;

    //If true along with RotateUp, object rotation will be kept the same relative to the normal of the terrain surface below it
    //Needs terrain tiles to have mesh colliders enabled, otherwise defaults to Surface Normal
    public bool RotateFromTerrainSurface;

    //If true, object altitude will be determined from the height of the terrain surface below it, instead of from sea level
    public bool AltitudeFromTerrainSurface;

    //If true, initial position is determined from GeoPosition, rather than transform.position
    public bool InitPositionWithGeo;

    //If true, initial rotation will be determined to be transform.rotation relative to the surface of the globe
    //eg A 90 degree rotation on the Y axis will result in transform.forward pointing East
    public bool InitRotationWithGeo;

    private void OnEnable()
    {
      //NOTE: On startup, OnEnable is called right after Awake, before calling Awake on any subsequent scripts
      //Because the OneWorldSDKViewer hasn't initialized yet, we can't do things with it yet
      if (m_HaveStarted)
      {
        InitOneWorldSDKViewer();
      }
    }

    private void Start()
    {
      //NOTE: As a workaround to OnEnable being called prematurely, call Init here for the first time
      //Subsequent Enable calls will be handled through OnEnable()
      m_HaveStarted = true;
      InitOneWorldSDKViewer();
    }

    public void InitGeoTransform(WorldContext WorldContext)
    {
      m_LastWorldContext = WorldContext;
    }

    public void Update()
    {
      surfaceHit = false;
      if (WorldContext != m_LastWorldContext)
      {
        if (m_LastWorldContext != null)
        {
          m_LastWorldContext.WorldOriginChanged.Event -= UpdateWorldOrigin;
        }

        InitOneWorldSDKViewer();
      }

      if (WorldContext != null)
      {
        if (RotateFromTerrainSurface || AltitudeFromTerrainSurface)
        {
          var rayDir = WorldContext.Ellipsoid.GeodeticSurfaceNormal(GeoPosition).ToVector3();
          var intersections = Physics.RaycastAll(transform.position, -rayDir, 10000f);
          if (intersections.Length == 0)
          {
            intersections = Physics.RaycastAll(transform.position + rayDir * 10000f, -rayDir, 10000f);
          }
          foreach (var intersection in intersections)
          {
            if (intersection.transform.gameObject.name.StartsWith("Tile ("))
            {
              hit = intersection;
              surfaceHit = true;
              break;
            }
          }
          if (!surfaceHit)
            return;
        }

        if (RotateUp)
        {
          m_LocalRotation = m_LastUpRotInv * transform.rotation;
        }

        if (m_LastTransformPos != transform.position)
        {
          GeoPosition = WorldContext.Ellipsoid.ToGeodetic3d(transform.position.ToVec3LeftHandedGeocentric() + WorldContext.WorldOrigin);
        }
        else
        {
          var pos3d = WorldContext.Ellipsoid.ToVec3LeftHandedGeocentric(GeoPosition);
          transform.position = (pos3d - WorldContext.WorldOrigin).ToVector3();
        }

        if (AltitudeFromTerrainSurface)
        {
          transform.position = hit.point + WorldContext.Ellipsoid.GeodeticSurfaceNormal(GeoPosition).ToVector3() * (float)GeoPosition.HeightMeters;
        }

        if (RotateUp)
        {
          if (RotateFromTerrainSurface)
          {
            transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
          }
          else
          {
            var upRot = WorldContext.Ellipsoid.NEDRotation((Geodetic2d)GeoPosition);
            transform.rotation = upRot * m_LocalRotation;
            m_LastUpRotInv = Quaternion.Inverse(upRot);
          }
        }

        m_LastTransformPos = transform.position;
      }
      else
      {
        if (m_LastWorldContext != null)
        {
          m_LastWorldContext.WorldOriginChanged.Event -= UpdateWorldOrigin;
        }
      }
    }

    private void OnDisable()
    {
      if (m_LastWorldContext != null)
      {
        m_LastWorldContext.WorldOriginChanged.Event -= UpdateWorldOrigin;
      }
    }

    #endregion

    private void UpdateWorldOrigin(Vec3LeftHandedGeocentric newOrigin)
    {
      var pos3d = WorldContext.Ellipsoid.ToVec3LeftHandedGeocentric(GeoPosition);
      transform.position = (pos3d - newOrigin).ToVector3();
      m_LastTransformPos = transform.position;
    }

    private void InitOneWorldSDKViewer()
    {
      m_LastWorldContext = WorldContext;

      if (WorldContext != null)
      {
        WorldContext.WorldOriginChanged.Event += UpdateWorldOrigin;

        if (InitPositionWithGeo)
        {
          var pos3d = WorldContext.Ellipsoid.ToVec3LeftHandedGeocentric(GeoPosition);
          transform.position = (pos3d - WorldContext.WorldOrigin).ToVector3();
        }
        else
        {
          GeoPosition = WorldContext.Ellipsoid.ToGeodetic3d(transform.position.ToVec3LeftHandedGeocentric() + WorldContext.WorldOrigin);
        }

        m_LastTransformPos = transform.position;

        var upRot = WorldContext.Ellipsoid.NEDRotation((Geodetic2d)GeoPosition);

        if (InitRotationWithGeo)
        {
          m_LocalRotation = transform.rotation;
        }
        else
        {
          m_LocalRotation = Quaternion.Inverse(upRot) * transform.rotation;
        }

        if (RotateUp)
        {
          transform.rotation = upRot * m_LocalRotation;
          m_LastUpRotInv = Quaternion.Inverse(upRot);
        }
      }
    }

    private bool m_HaveStarted;

    private WorldContext m_LastWorldContext;

    private Vector3 m_LastTransformPos;
    private Quaternion m_LastUpRotInv;
    private Quaternion m_LocalRotation;
    private RaycastHit hit;
    private bool surfaceHit = false;
  }
}




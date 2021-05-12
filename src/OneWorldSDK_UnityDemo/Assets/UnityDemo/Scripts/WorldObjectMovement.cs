//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.Core.Math;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Unity;
using sbio.owsdk.Unity.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace sbio.OneWorldSDKViewer
{
  public enum WorldObjectMoveMode
  {
    LatLonLock,
    FreeFly,
    FirstPerson,
    ThirdPerson,
    Orbit,
    Oblique,
    AttachedView,
  }

  public class AttachedView
  {
    public ulong CameraID;
    public ulong EntityID;
    public GameObject EntityGameObject;
    public Vector3 Offset;
		public Quaternion Rotation = Quaternion.identity;
	}

  /// <summary>
  /// Controls a transform via joystick inputs in a variety of fly modes, such as free-fly, or lat-lon-lock
  /// </summary>
  public class WorldObjectMovement : MonoBehaviour
  {
    #region MonoBehaviour

    public OneWorldSDKViewerContext OneWorldSDKViewerContext;
    public WorldObjectMoveMode MoveMode;
    public SerializedGeodetic3d GeoPos;
    public SerializedGeodetic3d[] Destinations;
    public List<Transform> Entities = new List<Transform>();
    public List<AttachedView> AttachedViews = new List<AttachedView>();
    private uint DesiredView = 0;
    public double BaseMetersPerSecond = 1.0;
    public double BaseDegreesPerSecond = 5e-06;

    public float OrbitDistance = 10.0f;
    public float OrbitXDegrees = 0.0f;
    public float OrbitYDegrees = 0.0f;

    [Tooltip("If the Left Shift key should be pressed to control the movement of this view in LatLonLock mode. Use this when adding a secondary view.")]
    public bool  MoveUsingShift = false;

    public AttachedView GetAttachedView(ulong CameraID)
    {
      foreach (AttachedView attachedView in AttachedViews)
      {
        if (attachedView.CameraID == CameraID)
        {
          return attachedView;
        }
      }

      return null;
    }

    public void AddAttachedView(AttachedView attachedView)
    {
      foreach (AttachedView view in AttachedViews)
      {
        if (view.CameraID == attachedView.CameraID)
        {
          view.EntityGameObject = attachedView.EntityGameObject;
          view.EntityID = attachedView.EntityID;
          return;
        }
      }

      //add if not found
      AttachedViews.Add(attachedView);
    }

    public void RemoveAttachedView(AttachedView attachedView)
    {
      foreach(AttachedView view in AttachedViews)
      {
        if(view.CameraID == attachedView.CameraID)
        {
          AttachedViews.Remove(view);
          return;
        }
      }
    }

    public void Reset()
    {
      AttachedViews.Clear();
    }

    private void Start()
    {
      m_RTOTransform = new RTOTransform(OneWorldSDKViewerContext.WorldContext, transform);

      GeoPos = OneWorldSDKViewerContext.Config.StartPosition;
      Destinations = OneWorldSDKViewerContext.Config.DestinationPoints.Select(g => (SerializedGeodetic3d)g).ToArray();

      switch (MoveMode)
      {
        case WorldObjectMoveMode.FreeFly:
        case WorldObjectMoveMode.LatLonLock:
        {
          //Just initialize to geopos
          //Also factor in the transform's position
          m_RTOTransform.GlobalPosition = transform.position.ToVec3LeftHandedGeocentric() + Ellipsoid.ToVec3LeftHandedGeocentric(GeoPos);
          //Recalculate geopos since we also shifted by the transform position
          GeoPos = Ellipsoid.ToGeodetic3d(m_RTOTransform.GlobalPosition);

          //Look straight down
          Vec3LeftHandedGeocentric normal;
          var north = Ellipsoid.NorthDirection((Geodetic2d)GeoPos, out normal);
          transform.rotation = Quaternion.LookRotation(-normal.ToVector3(), north.ToVector3());
        }
        break;
        case WorldObjectMoveMode.Oblique:
        {
          m_RTOTransform.GlobalPosition = transform.position.ToVec3LeftHandedGeocentric() + Ellipsoid.ToVec3LeftHandedGeocentric(GeoPos);
          GeoPos = Ellipsoid.ToGeodetic3d(m_RTOTransform.GlobalPosition);

          //Look at a 45 degree angle to the ground
          var surfacePos3d = Ellipsoid.ToVec3LeftHandedGeocentric((Geodetic2d)GeoPos);
          transform.LookAt((surfacePos3d - WorldOrigin).ToVector3());
          float targetDegrees = (float)GeoPos.LatitudeDegrees - 45;
          float degrees = targetDegrees - transform.rotation.eulerAngles.x;
          transform.Rotate(new Vector3(degrees, 0, 0));
        }
          break;
        case WorldObjectMoveMode.FirstPerson:
        case WorldObjectMoveMode.ThirdPerson:

        case WorldObjectMoveMode.Orbit:
        {
          EnsureFollowedTransform();
          if (m_FollowedTransform != null)
          {
            //Follow and all that jazz
            UpdateFollowPosition();
          }
          //otherwise place the camera initially at configured geopos
          else
          {
            //Also factor in the transform's position
            m_RTOTransform.GlobalPosition = transform.position.ToVec3LeftHandedGeocentric() + Ellipsoid.ToVec3LeftHandedGeocentric(GeoPos);
            //Recalculate geopos since we also shifted by the transform position
            GeoPos = Ellipsoid.ToGeodetic3d(m_RTOTransform.GlobalPosition);
          }
        }
          break;
        case WorldObjectMoveMode.AttachedView:
        {
          break;
        }
        default:
          throw new NotImplementedException();
      }
    }

    private void Update()
    {
      //Update the RTO with our current position in case it was moved not by us
      m_RTOTransform.RTOPositionf = transform.position;

      if (EventSystem.current.currentSelectedGameObject != null)
      {
        return;
      }

      if(this.EyepointCamera != this.OneWorldSDKViewerContext.Camera)
      {
        return;
      }

      if (Input.GetButtonUp("CycleCameraMode"))
      {
        var dir = Input.GetAxis("CycleCameraMode");
        var idx = Array.IndexOf(sc_MoveModes, MoveMode);
        if (dir < 0)
        {
          --idx;
          if (idx < 0)
          {
            idx = sc_MoveModes.Length - 1;
          }
        }
        else if (dir > 0)
        {
          idx = (idx + 1) % sc_MoveModes.Length;
        }

        MoveMode = sc_MoveModes[idx];
      }

      {
        //Do keyboard offset things
        var rightOff = 0.0f;
        var upOff = 0.0f;
        var forwardOff = 0.0f;

        if (Input.GetKey(KeyCode.RightArrow))
        {
          rightOff = 0.01f;
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
          rightOff = -0.01f;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
          forwardOff = 0.01f;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
          forwardOff = -0.01f;
        }

        if (Input.GetKey(KeyCode.PageUp))
        {
          upOff = 0.01f;
        }

        if (Input.GetKey(KeyCode.PageDown))
        {
          upOff = -0.01f;
        }

        var offset = new Vector3(rightOff, upOff, forwardOff);

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
          offset *= 5;
        }

        switch (MoveMode)
        {
          case WorldObjectMoveMode.FirstPerson:
            m_FirstPersonFollowOffset += offset;
            break;
          case WorldObjectMoveMode.ThirdPerson:
            m_ThirdPersonFollowOffset += offset;
            break;
          case WorldObjectMoveMode.Orbit:
          {
            OrbitXDegrees += 5 * Input.GetAxis("RotateUpDown");

            OrbitXDegrees = OrbitXDegrees > 80 ? 80 : OrbitXDegrees;
            OrbitXDegrees = OrbitXDegrees < -80 ? -80 : OrbitXDegrees;

            OrbitYDegrees += 5 * Input.GetAxis("RollRightLeft");
            OrbitDistance += 5 * Input.GetAxis("HeightUpDown");

            OrbitDistance = OrbitDistance < 0 ? 0 : OrbitDistance;
          }
            break;
        }
      }
      {
        var xOff = 0.0f;
        var yOff = 0.0f;
        var zOff = 0.0f;

        if (Input.GetKey(KeyCode.Keypad4))
        {
          yOff = -1;
        }

        if (Input.GetKey(KeyCode.Keypad6))
        {
          yOff = 1;
        }

        if (Input.GetKey(KeyCode.Keypad8))
        {
          xOff = -1;
        }

        if (Input.GetKey(KeyCode.Keypad2))
        {
          xOff = 1;
        }

        if (Input.GetKey(KeyCode.Keypad7))
        {
          zOff = -1;
        }

        if (Input.GetKey(KeyCode.Keypad9))
        {
          zOff = 1;
        }

        var offset = new Vector3(xOff, yOff, zOff);

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
          offset *= 5;
        }

        m_ThirdPersonRotOffset += offset;
      }

      if (m_FollowedTransform != null)
      {
        m_EntityTypeOffsets[m_FollowedTransform.name]
          = new EntityOffsets
          {
            FirstPerson = m_FirstPersonFollowOffset,
            ThirdPerson = m_ThirdPersonFollowOffset,
            ThirdPersonRot = m_ThirdPersonRotOffset
          };
      }

      if (Entities.Count != 0)
      {
        var changed = false;

        if (Input.GetButtonDown("CycleEntity"))
        {
          var dir = Input.GetAxis("CycleEntity");
          if (dir > 0)
          {
            changed = true;
            ++m_EntityFollowIndex;
          }
          else if (dir < 0)
          {
            changed = true;
            --m_EntityFollowIndex;
          }
        }

        if (changed)
        {
          m_FollowedTransform = null;

          if (m_EntityFollowIndex < 0)
          {
            m_EntityFollowIndex = 0;
          }

          if (Entities.Count != 0)
          {
            m_EntityFollowIndex = m_EntityFollowIndex % Entities.Count;
          }
          else
          {
            m_EntityFollowIndex = -1;
          }

          if (m_EntityFollowIndex >= 0)
          {
            m_FollowedTransform = Entities[m_EntityFollowIndex];
          }

          if (m_FollowedTransform != null)
          {
            EntityOffsets offsets;
            if (m_EntityTypeOffsets.TryGetValue(m_FollowedTransform.name, out offsets))
            {
              m_FirstPersonFollowOffset = offsets.FirstPerson;
              m_ThirdPersonFollowOffset = offsets.ThirdPerson;
              m_ThirdPersonRotOffset = offsets.ThirdPersonRot;
            }
          }
        }
      }

      var throttleRaw0To1 = (Input.GetAxis("Throttle") + 1) / 2;
      //Smooth out throttle as it reaches 1, and make it time relative
      var throttle = (2 - Mathf.Pow(1 - throttleRaw0To1, 3));

      var xRot = throttle * Input.GetAxis("RotateUpDown");
      var yRot = throttle * Input.GetAxis("RotateLeftRight");
      var zRot = throttle * Input.GetAxis("RollRightLeft");

      // Rotate if we're not following something
      switch (MoveMode)
      {
        case WorldObjectMoveMode.LatLonLock:

        {
          //Rotate relative to ground
          var constSpeed = 70 * Time.deltaTime;
          var ned = Ellipsoid.NEDRotation((Geodetic2d)GeoPos);
          var localUp = ned * Vector3.up;
          transform.rotation = Quaternion.AngleAxis(yRot*constSpeed, localUp) * transform.rotation;
          transform.Rotate(xRot*constSpeed, 0, zRot*constSpeed);
        }
          break;
        case WorldObjectMoveMode.Oblique:
        {
          //Move but keep rotation relative to ground
          var ned = Ellipsoid.NEDRotation((Geodetic2d)GeoPos);
          var localUp = ned * Vector3.up;
          transform.rotation = Quaternion.AngleAxis(0, localUp) * transform.rotation;
        }
          break;
        case WorldObjectMoveMode.FreeFly:
        {
          //Rotate relative to self
          transform.Rotate(xRot, yRot, zRot);
        }
          break;
      }

      //Counter-Clockwise heading from north
      //North = 0
      //West = π/2
      double compassHeading;
      {
        Vec3LeftHandedGeocentric normal;
        var north = Ellipsoid.NorthDirection((Geodetic2d)GeoPos, out normal);

        var headingForward = transform.forward.ToVec3LeftHandedGeocentric();
        var headingUp = transform.up.ToVec3LeftHandedGeocentric();

        var projectedForward = Vector3d.ProjectOnPlane(headingForward.ToVector3d(), normal.ToVector3d());
        var projectedUp = Vector3d.ProjectOnPlane(headingUp.ToVector3d(), normal.ToVector3d());

        var mainHeading = projectedForward.MagnitudeSquared > projectedUp.MagnitudeSquared ? projectedForward : projectedUp;

        compassHeading = Vector3d.SignedAngle(mainHeading, north.ToVector3d(), normal.ToVector3d());

        if (double.IsNaN(compassHeading))
        {
          compassHeading = 0;
        }
      }

      var degreesDelta = (throttle * GeoPos.HeightMeters) * BaseDegreesPerSecond * Time.deltaTime;
      var metersDelta = (throttle * GeoPos.HeightMeters) * BaseMetersPerSecond * Time.deltaTime;

      //Update our position
      switch (MoveMode)
      {
        case WorldObjectMoveMode.LatLonLock:
        {
          float fEnabled = 0.0F;
          if (MoveUsingShift == false && !Input.GetKey(KeyCode.LeftShift) || (MoveUsingShift == true && Input.GetKey(KeyCode.LeftShift))) //move the view with or without using the shift key 
              fEnabled = 1.0F;

          //Figure out our rotation relative to the surface prior to moving
          var relRot = Quaternion.Inverse(Ellipsoid.NEDRotation((Geodetic2d)GeoPos)) * transform.rotation;

          var newHeight = GeoPos.HeightMeters + Input.GetAxis("UpDown") * metersDelta* fEnabled;

          var moveVec = new Vector2d(Input.GetAxis("LeftRight"), Input.GetAxis("ForwardBack")).RotateRadians(compassHeading);

          var newLongitude = GeoPos.LongitudeDegrees + moveVec.x * degreesDelta * fEnabled;
          var newLatitude = GeoPos.LatitudeDegrees + moveVec.y * degreesDelta * fEnabled;

          //Restrict height
          newHeight = Math.Max(newHeight, 1);
          //Restrict latitude
          newLatitude = Math.Min(Math.Max(newLatitude, -85), 85);

          NumUtil.WrapLatLongDegrees(ref newLatitude, ref newLongitude);

          GeoPos = Geodetic3d.FromDegrees(newLatitude, newLongitude, newHeight);
          m_RTOTransform.GlobalPosition = Ellipsoid.ToVec3LeftHandedGeocentric(GeoPos);
          transform.rotation = Ellipsoid.NEDRotation((Geodetic2d)GeoPos) * relRot;
        }
          break;
        case WorldObjectMoveMode.Oblique:
        {
          var newHeight = GeoPos.HeightMeters + Input.GetAxis("UpDown") * metersDelta;
          var moveVec = new Vector2d(Input.GetAxis("LeftRight"), Input.GetAxis("ForwardBack")).RotateRadians(compassHeading);

          var relRot = Quaternion.Inverse(Ellipsoid.NEDRotation((Geodetic2d)GeoPos)) * transform.rotation;

          var newLongitude = GeoPos.LongitudeDegrees + moveVec.x * degreesDelta;
          var newLatitude = GeoPos.LatitudeDegrees + moveVec.y * degreesDelta;

          newHeight = Math.Max(newHeight, 1);
          newLatitude = Math.Min(Math.Max(newLatitude, -85), 85);

          NumUtil.WrapLatLongDegrees(ref newLatitude, ref newLongitude);

          GeoPos = Geodetic3d.FromDegrees(newLatitude, newLongitude, newHeight);
          m_RTOTransform.GlobalPosition = Ellipsoid.ToVec3LeftHandedGeocentric(GeoPos);
          transform.rotation = Ellipsoid.NEDRotation((Geodetic2d)GeoPos) * relRot;
        }
          break;
        case WorldObjectMoveMode.FreeFly:
        {
          var moveDelta = (Input.GetAxis("ForwardBack") * transform.forward.ToVec3LeftHandedGeocentric())
                          + (Input.GetAxis("LeftRight") * transform.right.ToVec3LeftHandedGeocentric())
                          + (Input.GetAxis("UpDown") * transform.up.ToVec3LeftHandedGeocentric());

          m_RTOTransform.GlobalPosition += moveDelta * metersDelta;

          GeoPos = Ellipsoid.ToGeodetic3d(m_RTOTransform.GlobalPosition);
        }
          break;
        case WorldObjectMoveMode.FirstPerson:
        case WorldObjectMoveMode.ThirdPerson:
        case WorldObjectMoveMode.Orbit:
        {
          UpdateFollowPosition();
        }
          break;
        case WorldObjectMoveMode.AttachedView:
        {
          foreach (AttachedView view in AttachedViews)
          {
            if (view.CameraID == DesiredView)
            {
              if (view.EntityGameObject != null)
              {
              m_RTOTransform.RTOPositionf = view.EntityGameObject.transform.position + view.EntityGameObject.transform.rotation * view.Offset;
              GeoPos = Ellipsoid.ToGeodetic3d(m_RTOTransform.GlobalPosition);
							transform.rotation = view.EntityGameObject.transform.rotation * view.Rotation;
            }
          }
        }
        }
          break;
        default:
          throw new NotImplementedException();
      }

      if (Input.GetButtonUp("ResetCameraLook"))
      {
        switch (MoveMode)
        {
          case WorldObjectMoveMode.LatLonLock:
          case WorldObjectMoveMode.FreeFly:
          {
            //Look at the ground
            Vec3LeftHandedGeocentric normal;
            var north = Ellipsoid.NorthDirection((Geodetic2d)GeoPos, out normal);
            transform.rotation = Quaternion.LookRotation(-normal.ToVector3(), north.ToVector3());
          }
            break;
          case WorldObjectMoveMode.Oblique:
          {
            //Look at a 45 degree to the ground
            var surfacePos3d = Ellipsoid.ToVec3LeftHandedGeocentric((Geodetic2d)GeoPos);
            transform.LookAt((surfacePos3d - WorldOrigin).ToVector3());
            float targetDegrees = (float)GeoPos.LatitudeDegrees - 45;
            float degrees = targetDegrees - transform.rotation.eulerAngles.x;
            transform.Rotate(new Vector3(degrees, 0, 0));
          }
            break;
          default:
            break;
        }
      }
    }

    private void OnDestroy()
    {
      if (m_RTOTransform != null)
      {
        m_RTOTransform.Dispose();
        m_RTOTransform = null;
      }
    }

    #endregion

    private struct EntityOffsets
    {
      public Vector3 FirstPerson { get; set; }
      public Vector3 ThirdPerson { get; set; }
      public Vector3 ThirdPersonRot { get; set; }
    }

    private IEnumerator MoveToPosition(Geodetic3d position)
    {
      var initialHeight = GeoPos.HeightMeters;
      var initialLat = GeoPos.LatitudeDegrees;
      var initialLon = GeoPos.LongitudeDegrees;

      // zoom out
      int zoomOutFrames = 200;
      double zoomedOutHeight = 30000000;
      var heightPerFrame = (zoomedOutHeight - initialHeight) / zoomOutFrames;

      if (heightPerFrame > 0)
      {
        for (int i = 0; i < zoomOutFrames; i++)
        {
          GeoPos = new Geodetic3d((Geodetic2d)GeoPos, GeoPos.HeightMeters + heightPerFrame);

          yield return null;
        }
      }

      //rotate 
      var numFrames = 30;
      var latDegreesPerFrame = ((position.LatitudeDegrees - initialLat) / numFrames);
      var lonDegreesPerFrame = ((position.LongitudeDegrees - initialLon) / numFrames);

      for (int i = 0; i < numFrames; i++)
      {
        GeoPos = Geodetic3d.FromDegrees(GeoPos.LatitudeDegrees + latDegreesPerFrame, GeoPos.LongitudeDegrees + lonDegreesPerFrame, GeoPos.HeightMeters);
        yield return null;
      }

      // zoom in
      double zoomInFrames = 40;
      double zoomedInHeight = MathUtilities.IsEqual(position.HeightMeters, -1) ? initialHeight : position.HeightMeters;

      while (GeoPos.HeightMeters > zoomedInHeight)
      {
        heightPerFrame = (zoomedInHeight - GeoPos.HeightMeters) / zoomInFrames;
        GeoPos = new Geodetic3d((Geodetic2d)GeoPos, GeoPos.HeightMeters + heightPerFrame);
        yield return null;
      }
    }

    private void DemoModeAnimate()
    {
      if (m_AutoMoveOperation != null &&
          (Input.GetKeyDown(KeyCode.Alpha0)
           || !Input.GetAxis("ForwardBack").EqualsEpsilon(0)
           || !Input.GetAxis("LeftRight").EqualsEpsilon(0)
           || !Input.GetAxis("UpDown").EqualsEpsilon(0)))
      {
        StopCoroutine(m_AutoMoveOperation);
        m_AutoMoveOperation = null;
      }
      else
      {
        //Figure out if [1-9] was pressed
        int aryPos = -1;
        var len = sc_KeyCodesToNumbers.Length;
        for (var i = 0; i < len; ++i)
        {
          var kvp = sc_KeyCodesToNumbers[i];
          if (Input.GetKeyDown(kvp.Key))
          {
            aryPos = kvp.Value;
            break;
          }
        }

        if (aryPos != -1)
        {
          if (m_AutoMoveOperation != null)
          {
            //Stop any existing operation
            StopCoroutine(m_AutoMoveOperation);
          }

          if (aryPos < Destinations.Length)
          {
            //Set up our new destination
            var destination = Destinations[aryPos];

            //Start moving us there
            m_AutoMoveOperation = StartCoroutine(MoveToPosition(destination));
          }
        }
      }
    }

    private void EnsureFollowedTransform()
    {
      if (Entities.Count != 0 && m_FollowedTransform == null)
      {
        m_FollowedTransform = null;

        if (m_EntityFollowIndex < 0)
        {
          m_EntityFollowIndex = 0;
        }

        if (Entities.Count != 0)
        {
          m_EntityFollowIndex = m_EntityFollowIndex % Entities.Count;
        }
        else
        {
          m_EntityFollowIndex = -1;
        }

        if (m_EntityFollowIndex >= 0)
        {
          m_FollowedTransform = Entities[m_EntityFollowIndex];
        }

        if (m_FollowedTransform != null)
        {
          EntityOffsets offsets;
          if (m_EntityTypeOffsets.TryGetValue(m_FollowedTransform.name, out offsets))
          {
            m_FirstPersonFollowOffset = offsets.FirstPerson;
            m_ThirdPersonFollowOffset = offsets.ThirdPerson;
            m_ThirdPersonRotOffset = offsets.ThirdPersonRot;
          }
        }
      }
    }

    private void UpdateFollowPosition()
    {
      switch (MoveMode)
      {
        case WorldObjectMoveMode.FirstPerson:
        {
          EnsureFollowedTransform();

          if (m_FollowedTransform != null)
          {
            m_RTOTransform.RTOPositionf = m_FollowedTransform.position + m_FollowedTransform.rotation * m_FirstPersonFollowOffset;
            transform.rotation = m_FollowedTransform.rotation;
          }

          GeoPos = Ellipsoid.ToGeodetic3d(transform.position.ToVec3LeftHandedGeocentric() + WorldOrigin);
        }
          break;
        case WorldObjectMoveMode.ThirdPerson:
        {
          EnsureFollowedTransform();

          if (m_FollowedTransform != null)
          {
            //Get the normal (surface of the earth up) at the target's current location
            var targetNormal = Ellipsoid.GeodeticSurfaceNormal((m_FollowedTransform.position.ToVec3LeftHandedGeocentric() + WorldOrigin)).ToVector3();
            //Project their forward on it to figure out the forward relative to the surface 
            var targetForward = Vector3.ProjectOnPlane(m_FollowedTransform.forward, targetNormal).normalized;
            var rot = Quaternion.LookRotation(targetForward, targetNormal);

            m_RTOTransform.RTOPositionf = m_FollowedTransform.position + (rot * m_ThirdPersonFollowOffset);
            transform.rotation = rot * Quaternion.Euler(m_ThirdPersonRotOffset);
          }

          GeoPos = Ellipsoid.ToGeodetic3d(transform.position.ToVec3LeftHandedGeocentric() + WorldOrigin);
        }
          break;
        case WorldObjectMoveMode.Orbit:
        {
          EnsureFollowedTransform();

          if (m_FollowedTransform != null)
          {
            var targetGeopos = Ellipsoid.ToGeodetic2d(m_FollowedTransform.position.ToVec3LeftHandedGeocentric() + WorldOrigin);
            var localRot = Ellipsoid.NEDRotation(targetGeopos);
            var offset = localRot * ((Quaternion.Euler(OrbitXDegrees, OrbitYDegrees, 0) * new Vector3(0, 0, OrbitDistance)));
            m_RTOTransform.RTOPositionf = m_FollowedTransform.position + offset;
            transform.LookAt(m_FollowedTransform, Ellipsoid.GeodeticSurfaceNormal(targetGeopos).ToVector3());
          }

          GeoPos = Ellipsoid.ToGeodetic3d(transform.position.ToVec3LeftHandedGeocentric() + WorldOrigin);
        }
          break;
      }
    }

    private Ellipsoid Ellipsoid
    {
      get { return OneWorldSDKViewerContext.WorldContext.Ellipsoid; }
    }

    private Vec3LeftHandedGeocentric WorldOrigin
    {
      get { return OneWorldSDKViewerContext.WorldContext.WorldOrigin; }
    }

    private Camera EyepointCamera
    {
      get
      {
        for (int n = 0; n < transform.childCount; ++n)
        {
          GameObject gameObject = transform.GetChild(n).gameObject;

          if (gameObject.GetComponentInChildren<Camera>() != null)
          {
            return gameObject.GetComponentInChildren<Camera>();
          }
        }

        return null;
      }
    }

    private double ConvertLatitudeToMeters(double lat)
    {
      return lat * 111111;
    }

    private double ConvertMetersToLatitude(double meters)
    {
      return meters / 111111;
    }

    private static readonly KeyValuePair<KeyCode, int>[] sc_KeyCodesToNumbers = new KeyValuePair<KeyCode, int>[]
    {
      new KeyValuePair<KeyCode, int>(KeyCode.Alpha1, 0),
      new KeyValuePair<KeyCode, int>(KeyCode.Alpha2, 1),
      new KeyValuePair<KeyCode, int>(KeyCode.Alpha3, 2),
      new KeyValuePair<KeyCode, int>(KeyCode.Alpha4, 3),
      new KeyValuePair<KeyCode, int>(KeyCode.Alpha5, 4),
      new KeyValuePair<KeyCode, int>(KeyCode.Alpha6, 5),
      new KeyValuePair<KeyCode, int>(KeyCode.Alpha7, 6),
      new KeyValuePair<KeyCode, int>(KeyCode.Alpha8, 7),
      new KeyValuePair<KeyCode, int>(KeyCode.Alpha9, 8)
    };

    private static readonly WorldObjectMoveMode[] sc_MoveModes = new WorldObjectMoveMode[]
    {
      WorldObjectMoveMode.LatLonLock,
      WorldObjectMoveMode.FreeFly,
      WorldObjectMoveMode.FirstPerson,
      WorldObjectMoveMode.ThirdPerson,
      WorldObjectMoveMode.Orbit,
      WorldObjectMoveMode.Oblique
    };

    private readonly IDictionary<string, EntityOffsets> m_EntityTypeOffsets = new Dictionary<string, EntityOffsets>();

    private RTOTransform m_RTOTransform;

    private Coroutine m_AutoMoveOperation;

    private int m_EntityFollowIndex = -1;
    private Transform m_FollowedTransform;
    private Vector3 m_FirstPersonFollowOffset = Vector3.zero;
    private Vector3 m_ThirdPersonFollowOffset = Vector3.zero;
    private Vector3 m_ThirdPersonRotOffset = Vector3.zero;
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

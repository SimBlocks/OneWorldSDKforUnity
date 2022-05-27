//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.Core.Math;
using UnityEngine;

namespace sbio.owsdk.Unity
{
  /// <summary>
  /// Controls the RTO context by dividing the world into a 3D grid and
  /// putting the origin at the closest grid position to this transform
  /// </summary>
  public sealed class RTOGridOrigin : MonoBehaviour
  {
    #region MonoBehaviour

    public WorldContext WorldContext;
    public Transform Origin;
    public int GridSize = 4000;

    public void OnEnable()
    {
      CheckWorldOrigin();
    }

    public void LateUpdate()
    {
      CheckWorldOrigin();
    }

    public void OnDisable()
    {
      WorldContext.WorldOrigin = Vec3LeftHandedGeocentric.Zero;
    }

    #endregion

    private void CalculateGridOffset(Vector3 pos, out int xOffset, out int yOffset, out int zOffset)
    {
      xOffset = yOffset = zOffset = 0;
      var gridExtent = GridSize / 2;

      if(pos.x > gridExtent)
      {
        xOffset = GridSize + (int)(pos.x / GridSize) * GridSize;
        pos.x = -GridSize + pos.x % GridSize;
      }
      else if(pos.x < -gridExtent)
      {
        xOffset = -GridSize + (int)(pos.x / GridSize) * GridSize;
        pos.x = GridSize + pos.x % GridSize;
      }

      if (pos.y > gridExtent)
      {
        yOffset = GridSize + (int)(pos.y / GridSize) * GridSize;
        pos.y = -GridSize + pos.y % GridSize;
      }
      else if (pos.y < -gridExtent)
      {
        yOffset = -GridSize + (int)(pos.y / GridSize) * GridSize;
        pos.y = GridSize + pos.y % GridSize;
      }

      if (pos.z > gridExtent)
      {
        zOffset = GridSize + (int)(pos.z / GridSize) * GridSize;
        pos.z = -GridSize + pos.z % GridSize;
      }
      else if (pos.z < -gridExtent)
      {
        zOffset = -GridSize + (int)(pos.z / GridSize) * GridSize;
        pos.z = GridSize + pos.z % GridSize;
      }
    }

    private void CheckWorldOrigin()
    {
      int xOffset, yOffset, zOffset;
      CalculateGridOffset(Origin.position, out xOffset, out yOffset, out zOffset);

      if (xOffset != 0 || yOffset != 0 || zOffset != 0)
      {
        WorldContext.WorldOrigin += new Vec3LeftHandedGeocentric(xOffset, yOffset, zOffset);
      }
    }
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

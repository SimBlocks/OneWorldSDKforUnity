//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using UnityEngine;
using sbio.Core.Math;

public class FlyScript : MonoBehaviour
{
  float xMovement;
  float yMovement;
  public float speed = 2.50f;
  float throttleDelta = 0;
  float xDelta = 0;
  float yDelta = 0;

  void Update()
  {
    transform.position += speed * transform.forward;
    xDelta = Input.GetAxisRaw("Mouse X");
    yDelta = Input.GetAxisRaw("Mouse Y");
    throttleDelta = Input.GetAxis("Mouse ScrollWheel");

    if (!MathUtilities.IsEqual(xDelta, 0) || !MathUtilities.IsEqual(yDelta, 0))
    {
      xMovement += xDelta;
      yMovement += yDelta;
      transform.rotation = Quaternion.Euler(-yMovement, xMovement, 0);
    }

    if (!MathUtilities.IsEqual(throttleDelta, 0))
    {
      if (throttleDelta > 0f)
      {
        if (MathUtilities.IsEqual(speed, 0))
        {
          speed = 1;
        }

        speed *= 1.5f;
      }
      else
      {
        speed /= 1.5f;
      }
    }
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

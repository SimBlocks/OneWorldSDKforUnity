//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
﻿namespace sbio.Core.Math
{
  //Earth Centered Earth Fixed (ECEF) origin at Earth's center of mass. right-handed. 
  //X towards intersection of prime meridian and equator. 
  //Z towards north pole.
  public struct Vec3Geocentric
  {
    public double X;
    public double Y;
    public double Z;
    
    public Vec3Geocentric(double x, double y, double z)
    {
      X = x;
      Y = y;
      Z = z;
    }

    public void Set(double x, double y, double z)
    {
      X = x;
      Y = y;
      Z = z;
    }

    public Vec3Geocentric Negated()
    {
      return new Vec3Geocentric(-X, -Y, -Z);
    }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

namespace sbio.Core.Math
{
  public struct QuaternionLeftHandedGeocentric
  {
    public double X;
    public double Y;
    public double Z;
    public double W;

    public QuaternionLeftHandedGeocentric(double x, double y, double z, double w)
    {
      X = x;
      Y = y;
      Z = z;
      W = w;
    }

    public QuaternionLeftHandedGeocentric Inverse()
    {
      //TODO

      System.Numerics.Quaternion q = new System.Numerics.Quaternion((float)X, (float)Y, (float)Z, (float)W);
      q = System.Numerics.Quaternion.Inverse(q);

      QuaternionLeftHandedGeocentric t = new QuaternionLeftHandedGeocentric();
      t.X = q.X;
      t.Y = q.Y;
      t.Z = q.Z;
      t.W = q.W;

      return t;
    }

    public Vec3LeftHandedGeocentric Multiply(Vec3LeftHandedGeocentric point)
    {
      // Can use a hamilton product to get the correct results for this
      // https://en.wikipedia.org/wiki/Quaternion#Hamilton_product
      double w = -X * point.X - Y * point.Y - Z * point.Z;
      double i = W * point.X + Y * point.Z - Z * point.Y;
      double j = W * point.Y - X * point.Z + Z * point.X;
      double k = W * point.Z + X * point.Y - Y * point.X;

      double i2 = -w * X + i * W - j * Z + k * Y;
      double j2 = -w * Y + i * Z + j * W - k * X;
      double k2 = -w * Z - i * Y + j * X + k * W;

      return new Vec3LeftHandedGeocentric(i2, j2, k2);
    }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

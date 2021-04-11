//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
namespace sbio.owsdk.Images
{
  public struct ARGBImage
  {
    public int Height { get; }
    public int Width { get; }
    public int[] Pixels { get; }

    public ARGBImage(int height, int width, int[] pixels)
    {
      Height = height;
      Width = width;
      Pixels = pixels;
    }
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

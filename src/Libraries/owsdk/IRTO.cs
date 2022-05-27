//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.Core.Math;

namespace sbio.owsdk
{
  //Relative-To-Origin Interface
  public interface IRTO
  {
    void UpdateWorldOrigin(Vector3d newWorldOrigin);
  }
}



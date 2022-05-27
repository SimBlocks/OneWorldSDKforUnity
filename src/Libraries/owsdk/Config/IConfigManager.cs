//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System.IO;

namespace sbio.owsdk.Config
{
  public interface IConfigManager : IConfigObject
  {
    DirectoryInfo SystemAppDataDir { get; }
    DirectoryInfo SystemProductDataDir { get; }
    DirectoryInfo UserAppDataDir { get; }
    DirectoryInfo UserProductDataDir { get; }
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using sbio.owsdk.Geodetic;

namespace sbio.owsdk.Services
{
  public struct LocationInfo
  {
    public string Name
    {
      get { return m_Name; }
    }

    public Geodetic2d Position
    {
      get { return m_Position; }
    }

    public LocationInfo(string name, Geodetic2d position)
    {
      m_Name = name;
      m_Position = position;
    }

    private readonly string m_Name;
    private readonly Geodetic2d m_Position;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

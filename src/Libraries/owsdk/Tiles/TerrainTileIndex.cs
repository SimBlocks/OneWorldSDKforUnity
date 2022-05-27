//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;

namespace sbio.owsdk.Tiles
{
  public struct TerrainTileIndex : IEquatable<TerrainTileIndex>
  {
    public static bool operator ==(TerrainTileIndex lhs, TerrainTileIndex rhs)
    {
      return lhs.Equals(rhs);
    }

    public static bool operator !=(TerrainTileIndex lhs, TerrainTileIndex rhs)
    {
      return !lhs.Equals(rhs);
    }

    public int Level
    {
      get { return m_Level; }
    }

    public int Row
    {
      get { return m_Row; }
    }

    public int Column
    {
      get { return m_Column; }
    }

    public override bool Equals(object obj)
    {
      if (obj is TerrainTileIndex)
      {
        return Equals((TerrainTileIndex)obj);
      }

      return false;
    }

    public bool Equals(TerrainTileIndex other)
    {
      return m_Level == other.m_Level
             && m_Row == other.m_Row
             && m_Column == other.m_Column;
    }

    public override int GetHashCode()
    {
      return m_Level.GetHashCode()
             ^ m_Row.GetHashCode()
             ^ m_Column.GetHashCode();
    }

    public TerrainTileIndex(int level, int row, int column)
    {
      m_Level = (byte)level;
      m_Row = row;
      m_Column = column;
    }

    private readonly byte m_Level;
    private readonly int m_Row;
    private readonly int m_Column;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

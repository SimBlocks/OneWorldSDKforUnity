//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using sbio.owsdk.Unity.Events;
using sbio.owsdk.Utilities;
using sbio.owsdk.Tiles;
using sbio.owsdk.WMS;
using UnityEngine;

namespace sbio.owsdk.Unity
{
  [CreateAssetMenu(menuName = "OWSDK/Tile Context", order = 120)]
  public class TileLoadContext : ScriptableObject
    , ITileLoadContext
  {
    #region ScriptableObject

    public GameEvent MapperChanged;
    public TerrainTileEvent TileActivated;
    public TerrainTileEvent TileIsVisible;
    public TerrainTileEvent TileObjectActive;
    public TerrainTileEvent TileObjectInactive;
    public TerrainTileEvent TileIsInvisible;
    public TerrainTileEvent TileDeactivated;
    public TerrainTileEvent TileUnloaded;

    #endregion

    public event Action TileMapperChanged
    {
      add { MapperChanged.Event += value; }
      remove { MapperChanged.Event -= value; }
    }

    public event Action<TerrainTileIndex> Activated
    {
      add { TileActivated.Event += value; }
      remove { TileActivated.Event -= value; }
    }

    public event Action<TerrainTileIndex> IsVisible
    {
      add { TileIsVisible.Event += value; }
      remove { TileIsVisible.Event -= value; }
    }

    public event Action<TerrainTileIndex> ObjectActive
    {
      add { TileObjectActive.Event += value; }
      remove { TileObjectActive.Event -= value; }
    }

    public event Action<TerrainTileIndex> ObjectInactive
    {
      add { TileObjectInactive.Event += value; }
      remove { TileObjectInactive.Event -= value; }
    }

    public event Action<TerrainTileIndex> IsInvisible
    {
      add { TileIsInvisible.Event += value; }
      remove { TileIsInvisible.Event -= value; }
    }

    public event Action<TerrainTileIndex> Deactivated
    {
      add { TileDeactivated.Event += value; }
      remove { TileDeactivated.Event -= value; }
    }

    public event Action<TerrainTileIndex> Unloaded
    {
      add { TileUnloaded.Event += value; }
      remove { TileUnloaded.Event -= value; }
    }

    public IDisposable RegisterTileLoader(ILoadTile loader)
    {
#if DEBUG
      //Make sure it's not in there already
      if (m_TileLoaders.Contains(loader))
      {
        throw new InvalidOperationException(string.Format("The tile loader '{0}' is already registered", loader));
      }
#endif

      m_TileLoaders.Add(loader);
      return new DisposableAction(() => m_TileLoaders.Remove(loader));
    }

    public IReadOnlyList<ILoadTile> ActiveLoaders
    {
      get { return m_TileLoaders; }
    }

    public ITileMapper ActiveTileMapper
    {
      get { return m_ActiveTileMapper; }
      set
      {
        if (m_ActiveTileMapper != value)
        {
          m_ActiveTileMapper = value;

          MapperChanged.Raise();
        }
      }
    }

    [NonSerialized] private readonly List<ILoadTile> m_TileLoaders = new List<ILoadTile>();
    private ITileMapper m_ActiveTileMapper = WMSTileMapper.Instance;
  }
}




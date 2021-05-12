//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using sbio.Core.Math;
using sbio.owsdk.Extensions;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Services;
using UnityEngine;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif
using sbio.owsdk.Unity.Extensions;
using UnityMesh = UnityEngine.Mesh;
using sbio.owsdk.Async;
using sbio.owsdk.Images;
using sbio.owsdk.Tiles;
using sbio.owsdk.WMS;
using sbio.OneWorldSDKViewer;

namespace sbio.owsdk.Unity
{
  public sealed class TerrainTileChunker : IDisposable
  {
    public class Settings
    {
      public static Settings Default => new Settings();

      public int MaxNumTiles { get; set; } = 4095;
      public double PreloadPercent { get; set; } = 5;
      public bool DisablePhysics { get; set; } = false;
      public int MaxConcurrentLoad { get; set; } = Environment.ProcessorCount;
      public int MaxTileLOD { get; set; } = 18;
      public int MaxPhysicsLOD { get; set; } = 13;
      public int LoadFrameBudget { get; set; } = 3;
      public int AtlasTileSize { get; set; } = 4;
      public bool CompressTextures { get; set; } = true;
      public float ResolutionDistanceBias { get; set; } = 1.0f;
    }

    public Camera Camera
    {
      get { return m_Camera; }
      set
      {
        if (value != m_Camera)
        {
          m_Camera = value;
          m_CullingGroup.targetCamera = m_Camera;
          m_CullingGroup.SetDistanceReferencePoint(m_Camera?.transform);
        }
      }
    }

    public ITerrainTileProvider TerrainTileProvider
    {
      get { return m_TerrainTileProvider; }
      set
      {
        if (m_TerrainTileProvider != value)
        {
          //Cancel any load operations that are going on
          CancelPendingOps();

          m_TerrainTileProvider = value;

          //Deactivate all chunks (they'll recursively kill each-other off)
          foreach (var chunk in m_Children)
          {
            DeallocateChunk(chunk);
          }

          m_Children.Clear();

          //Reset textures on each chunk
          for (var i = 0; i < m_Chunks.Length; ++i)
          {
            m_Chunks[i].ResetTexture();
          }

          if (m_CullingGroup.IsVisible(0))
          {
            //Regenerate toplevel
            GenerateLOD(m_Children);
          }
        }
      }
    }

    public ITileMapper TileMapper
    {
      get { return m_TileMapper; }
      set
      {
        if (TileMapper != value)
        {
          //Cancel any load operations that are going on
          CancelPendingOps();

          m_TileMapper = value;

          //Deactivate all chunks (they'll recursively kill each-other off)
          foreach (var chunk in m_Children)
          {
            DeallocateChunk(chunk);
          }

          m_Children.Clear();

          //Reset textures on each chunk
          for (var i = 0; i < m_Chunks.Length; ++i)
          {
            m_Chunks[i].ResetMesh();
          }

          if (m_CullingGroup.IsVisible(0))
          {
            //Regenerate toplevel
            GenerateLOD(m_Children);
          }
        }
      }
    }

    public void SetAdditionalTileMaterials(Material[] additionalMaterials)
    {
      if (m_AdditionalMaterials != additionalMaterials)
      {
        m_AdditionalMaterials = additionalMaterials;

        var len = m_Chunks.Length;
        for (var i = 0; i < len; ++i)
        {
          m_Chunks[i].SetAdditionalMaterials(m_AdditionalMaterials);
        }
      }
    }

#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
      var sphereFadeoutDurationSeconds = 5;

      m_TileEvents.RemoveAll(ev => (ev.Time + sphereFadeoutDurationSeconds) - Time.time <= 0);
      foreach (var eventInfo in m_TileEvents)
      {
        var time = eventInfo.Time;
        var timeTillFade = (time + sphereFadeoutDurationSeconds) - Time.time;

        var eventColor = eventInfo.Color;
        eventColor.a = Mathf.Lerp(0, 1, timeTillFade / sphereFadeoutDurationSeconds);

        var center = eventInfo.Center;
        var radius = eventInfo.Radius;

        Gizmos.color = eventColor;
        Gizmos.DrawWireSphere((center - m_WorldContext.WorldOrigin).ToVector3(), radius);
      }

      for (int i = 0; i < m_ChunkCount; ++i)
      {
        var chunk = m_Chunks[i];

        if (chunk.ChunkIndex != i)
        {
          UnityEngine.Debug.LogError("Bad chunk index: '" + i + "' expected, got '" + chunk.ChunkIndex + "'");
        }

        if (!Selection.transforms.Any(t => t.IsChildOf(chunk.GameObject.transform)))
        {
          continue;
        }

        var sphereIdx = i + 1;
        if (m_CullingGroup.IsVisible(sphereIdx))
        {
          Gizmos.color = UnityEngine.Color.Lerp(UnityEngine.Color.green, UnityEngine.Color.blue, (float)chunk.LOD / 20);
        }
        else
        {
          Gizmos.color = UnityEngine.Color.Lerp(UnityEngine.Color.red, UnityEngine.Color.yellow, (float)chunk.LOD / 20);
        }

        {
          var sphere = m_BoundingSpheres[sphereIdx];
          Gizmos.DrawWireSphere(sphere.position, sphere.radius);
        }
      }
    }
#endif

    private IEnumerable<TerrainTileIndex> TilesToPreload(int maxCount)
    {
      int numGenerated = 0;

      for (int lod = 1; lod < 20; ++lod)
      {
        var numCols = m_TileLoadContext.ActiveTileMapper.NumTilesX(lod);
        var numRows = m_TileLoadContext.ActiveTileMapper.NumTilesY(lod);
        for (int row = 0; row < numRows; ++row)
        {
          for (int col = 0; col < numCols; ++col)
          {
            var idx = new TerrainTileIndex(lod, row, col);
            if (m_LoadedChunks.ContainsKey(idx))
            {
              //We've already got that loaded
              continue;
            }

            yield return idx;
            if (++numGenerated >= maxCount)
            {
              yield break;
            }
          }
        }
      }
    }

    public IEnumerator<bool> PreloadTiles(AsyncCancellationToken tok)
    {
      if (MathUtilities.IsEqual(m_PreloadPercent, 0))
      {
        yield break;
      }

      var timeSpentInFrame = new Stopwatch();
      timeSpentInFrame.Start();

      //Disable the culling group during preload
      m_CullingGroup.SetBoundingSphereCount(0);

      var maxTilesToPreload = (int)(m_Chunks.Length * m_PreloadPercent / 100.0);
      var tilesToPreloadEnum = TilesToPreload(maxTilesToPreload).GetEnumerator();

      var currentLoadOp = -1;
      do
      {
        currentLoadOp = (currentLoadOp + 1) % m_ActiveLoadOps.Length;
        var loadOp = m_ActiveLoadOps[currentLoadOp];

        bool done;
        if (loadOp != null)
        {
          done = ProgressLoading(loadOp);

          if (done)
          {
            var chunk = loadOp.Chunk;
            switch (chunk.LoadStatus)
            {
              case TileLoadStatus.Loaded:
                m_LoadedChunks.Add(chunk.TileInfo, chunk);
                break;
              case TileLoadStatus.FailedLoad:
                UnityEngine.Debug.LogErrorFormat("Tile {0} Failed to load:\n{1}\n", FormatTileID(chunk.TileInfo), loadOp.Task.Exception);
                break;
              case TileLoadStatus.Idle:
                UnityEngine.Debug.LogErrorFormat("Tile {0} Cancelled loading", FormatTileID(chunk.TileInfo));
                break;
            }
          }
        }
        else
        {
          done = true;
        }

        if (done)
        {
          var nextChunkToLoad = m_Chunks.FirstOrDefault(c => !c.Loaded && !c.Loading);

          if (nextChunkToLoad != null && tilesToPreloadEnum.MoveNext())
          {
            var nextTileToLoad = tilesToPreloadEnum.Current;
            nextChunkToLoad.ReinitializeWithTile(this, null, nextTileToLoad);
            m_ActiveLoadOps[currentLoadOp] = new TileLoadOp(this, currentLoadOp, nextChunkToLoad, m_TileLoadContext.ActiveLoaders);
          }
          else
          {
            m_ActiveLoadOps[currentLoadOp] = null;
            break;
          }
        }

        if (timeSpentInFrame.ElapsedMilliseconds > 30)
        {
          yield return false;
          timeSpentInFrame.Restart();
        }
      } while (!tok.IsCancellationRequested && m_ActiveLoadOps.Any(t => t != null));

      if (tok.IsCancellationRequested)
      {
        foreach (var loadOp in m_ActiveLoadOps)
        {
          loadOp?.TokenSource.Cancel();
        }
      }

      //Wait for them all to finish
      foreach (var loadOp in m_ActiveLoadOps)
      {
        if (loadOp != null)
        {
          while (!ProgressLoading(loadOp))
          {
            if (timeSpentInFrame.ElapsedMilliseconds > 30)
            {
              yield return false;
              timeSpentInFrame.Restart();
            }
          }
        }
      }

      m_ActiveLoadOps.Fill(default(TileLoadOp));

      m_CullingGroup.SetBoundingSphereCount(m_ChunkCount + 1);

      yield return true;
    }

    public void Update()
    {
      if (m_Camera == null)
      {
        return;
      }

      Profiler.BeginSample("TerrainTileChunker.Update");

      Profiler.BeginSample("TerrainTileChunker.Update Children");

      var screenHeightPixels = m_Camera.scaledPixelHeight;
      var screenWidthPixels = m_Camera.scaledPixelWidth;
      var vertFOVRadians = NumUtil.DegreesToRadians(m_Camera.fieldOfView);
      var horzFOVRadians = 2 * Math.Atan(Math.Tan(vertFOVRadians / 2) * m_Camera.aspect);

      var vertResolutionPP = (m_ResolutionDistanceBias * 2 * Math.Tan(vertFOVRadians / 2)) / screenHeightPixels;
      var horzResolutionPP = (m_ResolutionDistanceBias * 2 * Math.Tan(horzFOVRadians / 2)) / screenWidthPixels;

      var updateChunkCount = 0;
      var childCount = m_Children.Count;
      for (var i = 0; i < childCount; ++i)
      {
        var child = m_Children[i];
        if (child.Loaded)
        {
          m_UpdateChunks[updateChunkCount++] = child;
        }
      }

      var cameraPos = m_Camera.transform.position;
      var cameraPos3d = cameraPos.ToVec3LeftHandedGeocentric() + m_WorldContext.WorldOrigin;

      for (var i = 0; i < updateChunkCount; ++i)
      {
        var chunk = m_UpdateChunks[i];

        Profiler.BeginSample("TerrainTileChunk.Update");

        if (chunk.LOD < m_MaxTileLOD)
        {
          //Check if we should subdivide/recombine
          var chunkChildren = chunk.Children;
          var chunkChildrenCount = chunkChildren.Length;

          var distanceToEyeMeters = chunk.DistanceTo(ref cameraPos);
          if (ViewerContext)
          {
            //check distance to all cameras
            foreach (Camera camera in ViewerContext.Cameras)
            {
              var Pos = camera.transform.position;
              var dist = chunk.DistanceTo(ref Pos);
              if (dist < distanceToEyeMeters)
                distanceToEyeMeters = dist;
            }
          }

          var activeVertResolutionMetersPerPixel = m_ResolutionDistanceBias * distanceToEyeMeters * vertResolutionPP;
          var activeHorzResolutionMetersPerPixel = m_ResolutionDistanceBias * distanceToEyeMeters * horzResolutionPP;
          var chunkResolution = chunk.ResolutionMPP;

          if (activeVertResolutionMetersPerPixel < chunkResolution || activeHorzResolutionMetersPerPixel < chunkResolution)
          {
            //If we haven't subdivided yet, do that now
            if (!chunk.HaveSubdivided)
            {
              //Subdivide
              Profiler.BeginSample("Subdividing");

              chunk.HaveSubdivided = true;

              if (!chunk.AnyChildrenFailedToLoad)
              {
                //Only go through with it if we hadn't failed loading these children before
                TerrainTileIndex tl, bl, tr, br;
                WMSConversions.Subtiles(chunk.TileInfo, out tl, out bl, out tr, out br);

                chunkChildren[0] = AllocateChunk(chunk, tl);
                chunkChildren[1] = AllocateChunk(chunk, bl);
                chunkChildren[2] = AllocateChunk(chunk, tr);
                chunkChildren[3] = AllocateChunk(chunk, br);

                if (chunkChildren[0] != null
                    && chunkChildren[1] != null
                    && chunkChildren[2] != null
                    && chunkChildren[3] != null)
                {
                  chunk.AllChildrenLoaded = chunkChildren[0].Loaded && chunkChildren[1].Loaded && chunkChildren[2].Loaded && chunkChildren[3].Loaded;

                  if (chunk.AllChildrenLoaded)
                  {
                    //If all children are already loaded, no need to show this chunk
                    //Since they just loaded, we can't look at their visibilities to
                    //consider if their extents are visible or not. We'll have to wait
                    //a frame for that at best
                    for (var cidx = 0; cidx < chunkChildrenCount; ++cidx)
                    {
                      var child = chunkChildren[cidx];
                      ChunkActivated(child);

                      if (IsChunkSphereVisible(child))
                      {
                        ChunkVisible(child);
                        ShowChunk(child);
                      }
                    }

                    HideChunk(chunk);
                  }
                }
                else
                {
                  //Ran out of tiles. Cancel subdividing and don't try again
                  UnityEngine.Debug.LogError("TerrainTileChunker: Ran out of tiles while subdividing");
                  for (var cidx = 0; cidx < chunkChildrenCount; ++cidx)
                  {
                    var child = chunkChildren[cidx];
                    if (child != null)
                    {
                      DeallocateChunk(child);
                    }
                  }

                  chunkChildren.Fill(default(TerrainTileChunk));
                  chunk.AllChildrenLoaded = false;
                }
              }

              Profiler.EndSample();
            }
            else if (chunk.AllChildrenLoaded)
            {
              //Otherwise just update the children to see if they need to recombine/subdivide
              // Update children
              Profiler.BeginSample("Updating Children");

              Array.Copy(chunkChildren, 0, m_UpdateChunks, updateChunkCount, chunkChildrenCount);
              updateChunkCount += chunkChildrenCount;

              Profiler.EndSample();
            }
          }
          else if (chunk.HaveSubdivided)
          {
            //Recombine
            Profiler.BeginSample("Recombining");

            chunk.HaveSubdivided = false;
            chunk.AllChildrenLoaded = false;

            for (var cidx = 0; cidx < chunkChildrenCount; ++cidx)
            {
              var child = chunkChildren[cidx];
              if (child != null)
              {
                DeallocateChunk(child);
              }
            }

            chunkChildren.Fill(default(TerrainTileChunk));

            if (IsChunkSphereVisible(chunk))
            {
              //The children might previously have been covering
              ShowChunk(chunk);
            }

            Profiler.EndSample();
          }
        }

        Profiler.EndSample();
      }

      Profiler.EndSample();

      m_LoadFrameStopwatch.Restart();

      Profiler.BeginSample("TerrainTileChunker.Update Loading");

      //Used to see if there's just nothing to do right now
      var numTried = 0;
      for (var opIdx = m_NextLoadOp; numTried < m_ActiveLoadOps.Length; opIdx = (opIdx + 1) % m_ActiveLoadOps.Length, numTried++)
      {
        if (m_LoadFrameStopwatch.ElapsedMilliseconds >= m_LoadFrameBudget)
        {
          //No more time left
          m_NextLoadOp = opIdx;
          break;
        }

        var loadOp = m_ActiveLoadOps[opIdx];

        bool done;

        if (loadOp != null)
        {
          done = ProgressLoading(loadOp);
          if (done)
          {
            HandleLoadComplete(loadOp);
          }
        }
        else
        {
          done = true;
        }

        if (done)
        {
          Profiler.BeginSample("Selecting Chunk To Load");
          var nextChunkToLoad = NextChunkToLoad(ref cameraPos3d);
          Profiler.EndSample();
          if (nextChunkToLoad != null)
          {
            //Prepare it for loading
            numTried = 0;
            NoticeTileEvent(TileEventType.Loading, nextChunkToLoad.Center, nextChunkToLoad.Radius);

            Profiler.BeginSample("Initiating Load");
            m_ActiveLoadOps[opIdx] = new TileLoadOp(this, opIdx, nextChunkToLoad, m_TileLoadContext.ActiveLoaders);
            Profiler.EndSample();
          }
          else
          {
            m_ActiveLoadOps[opIdx] = null;

            if (++numTried == m_ActiveLoadOps.Length)
            {
              //Nothing else to do for now
              break;
            }
          }
        }
      }

      Profiler.EndSample();

      Profiler.BeginSample("Applying Atlases");
      for (var i = 0; i < m_TextureAtlases.Length; ++i)
      {
        m_TextureAtlases[i].ApplyIfDirty();
      }

      Profiler.EndSample();

      Profiler.EndSample();
    }

    private void UpdateWorldOrigin(Vec3LeftHandedGeocentric newWorldOrigin)
    {
      Profiler.BeginSample("TerrainTileChunker.UpdateWorldOrigin");

      //Update our bounding sphere position
      m_BoundingSpheres[0].position = -newWorldOrigin.ToVector3();

      //Update all the active chunks
      for (int i = 0; i < m_ChunkCount; ++i)
      {
        Profiler.BeginSample("TerrainTileChunk.UpdateWorldOrigin");

        var chunk = m_Chunks[i];
        var rtoPos3 = (chunk.Center - newWorldOrigin).ToVector3();

        m_BoundingSpheres[i + 1].position = rtoPos3;
        chunk.GameObject.transform.position = rtoPos3;

        Profiler.EndSample();
      }

      Profiler.EndSample();
    }

    public void Dispose()
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      m_Disposed = true;

      //Cancel any load operations that are going on
      CancelPendingOps();

      //Deallocate all chunks to make sure events get triggered
      foreach (var chunk in m_Children)
      {
        DeallocateChunk(chunk);
      }

      m_Children.Clear();
      m_LoadedChunks.Clear();

      m_TileLoadContext.TileMapperChanged -= TileMapperChanged;
      m_WorldContext.WorldOriginChanged -= UpdateWorldOrigin;
      m_CullingGroup.Dispose();

      var len = m_Chunks.Length;
      for (var i = 0; i < len; ++i)
      {
        var chunk = m_Chunks[i];
        if (chunk.Loaded)
        {
          ChunkUnloaded(chunk);
        }

        chunk.Dispose();
      }

      m_Chunks.Fill(default(TerrainTileChunk));

      len = m_TextureAtlases.Length;
      for (var i = 0; i < len; ++i)
      {
        m_TextureAtlases[i].Dispose();
      }

      m_TextureAtlases.Fill(default(TileAtlasInfo));

      UnityEngine.Object.DestroyImmediate(m_ChunkPoolObject);
      UnityEngine.Object.DestroyImmediate(m_ActiveChunksObject);

      m_MipsBuffers.Fill(default(Color32[][]));
      m_PixelBuffers.Fill(default(int[]));
      m_TextureBuffers.Fill(default(byte[]));
    }

    public TerrainTileChunker(Shader singleShader, Shader atlasShader, IWorldContext context, TileLoadContext tileLoadContext, ITileMeshProvider meshProvider)
      : this(Settings.Default, singleShader, atlasShader, context, tileLoadContext, meshProvider)
    {
    }

    public TerrainTileChunker(Settings settings, Shader singleShader, Shader atlasShader, IWorldContext context, TileLoadContext tileLoadContext, ITileMeshProvider meshProvider)
    {
      m_PreloadPercent = settings.PreloadPercent;
      m_MaxTileLOD = settings.MaxTileLOD;
      m_DisablePhysics = settings.DisablePhysics;
      m_MaxPhysicsLOD = settings.MaxPhysicsLOD;
      m_LoadFrameBudget = settings.LoadFrameBudget;
      m_WorldContext = context;
      m_TileLoadContext = tileLoadContext;
      m_MeshProvider = meshProvider;
      m_AtlasTileSize = (byte)settings.AtlasTileSize;
      m_ResolutionDistanceBias = settings.ResolutionDistanceBias;

      m_TextureBuffers = new byte[settings.MaxConcurrentLoad][];
      for (int i = 0; i < m_TextureBuffers.Length; ++i)
      {
        m_TextureBuffers[i] = new byte[256 * 256 * 4];
      }

      m_PixelBuffers = new int[settings.MaxConcurrentLoad][];
      for (var i = 0; i < m_PixelBuffers.Length; ++i)
      {
        m_PixelBuffers[i] = new int[256 * 256];
      }

      m_MipsBuffers = new Color32[settings.MaxConcurrentLoad][][];
      for (var j = 0; j < m_MipsBuffers.Length; ++j)
      {
        // Number of mips = Math.Log(256, 2)+1;
        var mipBuf = new Color32[9][];
        for (var i = 0; i < mipBuf.Length; ++i)
        {
          var mipSize = 256 >> i;
          mipBuf[i] = new Color32[mipSize * mipSize];
        }

        m_MipsBuffers[j] = mipBuf;
      }

      m_LoadTasks = Enumerable.Repeat(0, settings.MaxConcurrentLoad).Select(_ => new List<Task>()).ToArray();

      m_ActiveLoadOps = new TileLoadOp[settings.MaxConcurrentLoad];

      m_ChunkPoolObject = new GameObject("Chunk Pool");
      m_ActiveChunksObject = new GameObject("Active Chunks");

      m_Chunks = new TerrainTileChunk[settings.MaxNumTiles];
      m_UpdateChunks = new TerrainTileChunk[settings.MaxNumTiles];
      m_LoadedChunks = new Dictionary<TerrainTileIndex, TerrainTileChunk>(settings.MaxNumTiles);

      m_TextureAtlases = new TileAtlasInfo[(int)Math.Ceiling(settings.MaxNumTiles / (float)(m_AtlasTileSize * m_AtlasTileSize))];
      for (var i = 0; i < m_TextureAtlases.Length; ++i)
      {
        m_TextureAtlases[i] = new TileAtlasInfo(i, m_AtlasTileSize, singleShader, atlasShader, settings.CompressTextures);
      }

      m_ChunkCount = 0;
      for (ushort i = 0; i < m_Chunks.Length; ++i)
      {
        m_Chunks[i] = new TerrainTileChunk(this, i);
      }

      m_BoundingSpheres = new BoundingSphere[settings.MaxNumTiles + 1];
      m_BoundingSpheres[0] = new BoundingSphere(-m_WorldContext.WorldOrigin.ToVector3(), (float)m_WorldContext.Ellipsoid.MaximumRadius);

      m_CullingGroup = new CullingGroup();
      m_CullingGroup.SetBoundingSpheres(m_BoundingSpheres);
      m_CullingGroup.SetBoundingSphereCount(m_ChunkCount + 1);
      m_CullingGroup.SetBoundingDistances(m_LODDistances);
      m_CullingGroup.onStateChanged = OnCullingGroupStateChanged;

      UpdateWorldOrigin(m_WorldContext.WorldOrigin);
      m_WorldContext.WorldOriginChanged += UpdateWorldOrigin;
      m_TileLoadContext.TileMapperChanged += TileMapperChanged;
    }

#if DEBUG
    ~TerrainTileChunker()
    {
      if (!m_Disposed)
      {
        UnityEngine.Debug.LogErrorFormat("'{0}' was not disposed", this);
      }
    }
#endif

    private void TileMapperChanged()
    {
      TileMapper = m_TileLoadContext.ActiveTileMapper;
    }

    private int AtlasIDFor(TerrainTileChunk chunk)
    {
      return chunk.ChunkID / (m_AtlasTileSize * m_AtlasTileSize);
    }

    private int AtlasIndexFor(TerrainTileChunk chunk, out int atlasIndex)
    {
      return Math.DivRem(chunk.ChunkID, m_AtlasTileSize * m_AtlasTileSize, out atlasIndex);
    }

    private void NoticeTileEvent(TileEventType eventType, Vec3LeftHandedGeocentric center, float radius)
    {
      UnityEngine.Color color;
      switch (eventType)
      {
        case TileEventType.Loading:
          color = UnityEngine.Color.magenta;
          break;
        case TileEventType.Loaded:
          color = UnityEngine.Color.white;
          break;
        case TileEventType.Cancelled:
          color = UnityEngine.Color.cyan;
          break;
        case TileEventType.Failed:
          color = UnityEngine.Color.yellow;
          break;
        default:
          color = UnityEngine.Color.black;
          break;
      }

      var time = Time.time;

      m_TileEvents.Add(new TileEvent(color, time, center, radius));
    }

    private bool ProgressLoading(TileLoadOp op)
    {
      var task = op.Task;

      if (task.IsCompleted)
      {
        throw new InvalidOperationException("Loading already completed");
      }

      Profiler.BeginSample("Progressing Loading");
      var hasMoreToLoad = task.MoveNext();
      Profiler.EndSample();

      if (hasMoreToLoad)
      {
        //Still loading
        return false;
      }
      else
      {
        //Loading complete
        var chunk = op.Chunk;

        if (task.IsCancelled)
        {
          //Cancelled loading
          chunk.LoadStatus = TileLoadStatus.Idle;
        }
        else if (task.IsFaulted)
        {
          //Failed to load
          chunk.LoadStatus = TileLoadStatus.FailedLoad;
        }
        else
        {
          //Loaded successfully
          chunk.LoadStatus = TileLoadStatus.Loaded;
        }

        chunk.LoadOp = null;
        return true;
      }
    }

    private void HandleLoadComplete(TileLoadOp op)
    {
      var task = op.Task;

      if (!task.IsCompleted)
      {
        throw new InvalidOperationException("Loading not complete");
      }

      var chunk = op.Chunk;

      if (!chunk.PendingCancellation && IsChunkAllocated(chunk))
      {
        //If the chunk is still allocated and participating
        switch (chunk.LoadStatus)
        {
          case TileLoadStatus.Loaded:
          {
            NoticeTileEvent(TileEventType.Loaded, chunk.Center, chunk.Radius);

            var parent = chunk.Parent;
            if (parent != null)
            {
              //See if all the children are loaded now
              parent.AllChildrenLoaded = true;
              var parentChildren = parent.Children;
              var parentChildrenCount = parent.Children.Length;
              for (var cidx = 0; cidx < parentChildrenCount; ++cidx)
              {
                if (!parentChildren[cidx].Loaded)
                {
                  parent.AllChildrenLoaded = false;
                  break;
                }
              }

              if (parent.AllChildrenLoaded)
              {
                for (var cidx = 0; cidx < parentChildrenCount; ++cidx)
                {
                  var child = parentChildren[cidx];

                  //Activate it now that all the siblings have loaded
                  ChunkActivated(child);

                  if (IsChunkSphereVisible(child))
                  {
                    //If it's also visible right now, make it so
                    ShowChunk(child);
                    ChunkVisible(child);
                  }
                }

                HideChunk(parent);
              }
            }
            else
            {
              //No parent. Activate immediately
              //Activate it now that all the siblings have loaded
              ChunkActivated(chunk);

              if (IsChunkSphereVisible(chunk))
              {
                //If it's also visible right now, make it so
                ShowChunk(chunk);
                ChunkVisible(chunk);
              }
            }
          }
            break;
          case TileLoadStatus.FailedLoad:
          {
            NoticeTileEvent(TileEventType.Failed, chunk.Center, chunk.Radius);
            UnityEngine.Debug.LogErrorFormat("Tile {0} failed to load:\n{1}\n", FormatTileID(chunk.TileInfo), task.Exception);

            var parent = chunk.Parent;
            if (parent != null)
            {
              //Deactivate all siblings
              var parentChildren = parent.Children;
              var parentChildrenCount = parentChildren.Length;
              for (var cidx = 0; cidx < parentChildrenCount; ++cidx)
              {
                DeallocateChunk(parentChildren[cidx]);
              }

              parentChildren.Fill(default(TerrainTileChunk));
              parent.AnyChildrenFailedToLoad = true;
            }
          }
            break;
          case TileLoadStatus.Idle:
          {
            NoticeTileEvent(TileEventType.Cancelled, chunk.Center, chunk.Radius);
            UnityEngine.Debug.LogFormat("Tile {0} cancelled loading", FormatTileID(chunk.TileInfo));
          }
            break;
        }
      }
      else if (chunk.PendingCancellation && IsChunkAllocated(chunk))
      {
        chunk.PendingCancellation = false;
        chunk.LoadStatus = TileLoadStatus.Idle;
      }
      else
      {
        switch (chunk.LoadStatus)
        {
          case TileLoadStatus.Loaded:
            chunk.GameObject.name = string.Format("Inactive Tile (loaded) {0}", FormatTileID(chunk.TileInfo));
            break;
          default:
            chunk.GameObject.name = string.Format("Inactive {0}", FormatTileID(chunk.TileInfo));
            m_LoadedChunks.Remove(chunk.TileInfo);
            chunk.ReinitializeWithTile(this, null, default(TerrainTileIndex));
            break;
        }
      }
    }

    private void ChunkActivated(TerrainTileChunk chunk)
    {
      if (chunk.Active)
      {
        throw new InvalidOperationException(string.Format("Chunk flagged for activation is already active '{0}'", FormatTileID(chunk.TileInfo)));
      }

      m_TileLoadContext.TileActivated.Raise(chunk.TileInfo);
      chunk.Active = true;

      var rtoPos3 = (chunk.Center - m_WorldContext.WorldOrigin).ToVector3();
      chunk.GameObject.transform.position = rtoPos3;
      if (chunk.LOD > 16)
        m_BoundingSpheres[chunk.ChunkIndex + 1] = new BoundingSphere(rtoPos3, chunk.Radius * 1.5f);
      else
        m_BoundingSpheres[chunk.ChunkIndex + 1] = new BoundingSphere(rtoPos3, chunk.Radius);
    }

    private void ChunkVisible(TerrainTileChunk chunk)
    {
      if (chunk.Visible)
      {
        throw new InvalidOperationException(string.Format("Chunk flagged to be visible is already visible '{0}'", FormatTileID(chunk.TileInfo)));
      }

      m_TileLoadContext.TileIsVisible.Raise(chunk.TileInfo);
      chunk.Visible = true;
    }

    private void ChunkNotVisible(TerrainTileChunk chunk)
    {
      if (!chunk.Visible)
      {
        throw new InvalidOperationException(string.Format("Chunk flagged to be invisible is already not visible '{0}'", FormatTileID(chunk.TileInfo)));
      }

      m_TileLoadContext.TileIsInvisible.Raise(chunk.TileInfo);
      chunk.Visible = false;
    }

    private void ChunkDeactivated(TerrainTileChunk chunk)
    {
      if (!chunk.Active)
      {
        throw new InvalidOperationException(string.Format("Chunk flagged for deactivation is not active '{0}'", FormatTileID(chunk.TileInfo)));
      }

      m_TileLoadContext.TileDeactivated.Raise(chunk.TileInfo);
      chunk.Active = false;
    }

    private void ChunkUnloaded(TerrainTileChunk chunk)
    {
      if (!chunk.Loaded)
      {
        throw new InvalidOperationException(string.Format("Chunk flagged for deactivation is not active '{0}'", FormatTileID(chunk.TileInfo)));
      }

      m_TileLoadContext.TileUnloaded.Raise(chunk.TileInfo);
      chunk.LoadStatus = TileLoadStatus.Idle;
    }

    private void CancelPendingOps()
    {
      var len = m_ActiveLoadOps.Length;
      for (var i = 0; i < len; ++i)
      {
        var loadOp = m_ActiveLoadOps[i];
        if (loadOp != null)
        {
          loadOp.TokenSource.Cancel();
          while (!ProgressLoading(loadOp)) ;
        }
      }

      m_ActiveLoadOps.Fill(default(TileLoadOp));
    }

    private void DeallocateChunk(TerrainTileChunk chunk)
    {
      //Flag it for cancellation if it was loading
      if (chunk.Loading)
      {
        chunk.LoadOp.TokenSource.Cancel();
        chunk.PendingCancellation = true;
      }

      if (chunk.HaveSubdivided)
      {
        chunk.HaveSubdivided = false;

        var chunkChildren = chunk.Children;
        var childCount = chunkChildren.Length;
        for (var cidx = 0; cidx < childCount; ++cidx)
        {
          var child = chunkChildren[cidx];
          if (child != null)
          {
            DeallocateChunk(child);
          }
        }

        chunkChildren.Fill(default(TerrainTileChunk));
      }

      //Make sure our components are enabled and the tile object disabled
      chunk.GameObject.SetActive(false);
      chunk.Renderer.enabled = true;
      if (chunk.Collider != null)
      {
        chunk.Collider.enabled = true;
      }

      switch (chunk.LoadStatus)
      {
        case TileLoadStatus.Idle:
          chunk.GameObject.name = "Inactive Tile";
          chunk.TileInfo = default(TerrainTileIndex);
          break;
        case TileLoadStatus.Loading:
          chunk.GameObject.name = string.Format("Inactive Tile (loading) {0}", FormatTileID(chunk.TileInfo));
          m_LoadedChunks.Add(chunk.TileInfo, chunk);
          break;
        case TileLoadStatus.Loaded:
          chunk.GameObject.name = string.Format("Inactive tile (loaded) {0}", FormatTileID(chunk.TileInfo));
          m_LoadedChunks.Add(chunk.TileInfo, chunk);
          break;
      }

      m_CullingGroup.EraseSwapBack(chunk.ChunkIndex + 1);

      var lastChunkIndex = (ushort)(m_ChunkCount - 1);
      //If we're not the last thing, do a swap
      if (chunk.ChunkIndex != lastChunkIndex)
      {
        var swapChunk = m_Chunks[lastChunkIndex];

        //put that chunk in our position
        m_Chunks[chunk.ChunkIndex] = swapChunk;

        //Tell it about its new index
        swapChunk.ChunkIndex = chunk.ChunkIndex;

        //Place us where it previously was
        m_Chunks[lastChunkIndex] = chunk;
        //Don't need to bother with updating our sphere since we're gone anyway

        //Update our index
        chunk.ChunkIndex = lastChunkIndex;
      }

      --m_ChunkCount;

      if (chunk.Visible)
      {
        //If it was visible, flag for visibility change
        ChunkNotVisible(chunk);
      }

      if (chunk.Active)
      {
        //If it was active, flag for deactivation
        ChunkDeactivated(chunk);
      }

      chunk.GameObject.transform.parent = m_ChunkPoolObject.transform;

      chunk.AllChildrenLoaded = false;
      chunk.Parent = null;
    }

    private bool IsChunkAllocated(TerrainTileChunk chunk)
    {
      return chunk.ChunkIndex < m_ChunkCount;
    }

    private bool IsChunkSphereVisible(TerrainTileChunk chunk)
    {
      return m_CullingGroup.IsVisible(chunk.ChunkIndex + 1);
    }

    private TerrainTileChunk NextChunkToLoad(ref Vec3LeftHandedGeocentric cameraPos3d)
    {
      var ret = default(TerrainTileChunk);
      for (var cidx = 0; cidx < m_ChunkCount; ++cidx)
      {
        var chunk = m_Chunks[cidx];
        var loadStatus = chunk.LoadStatus;

        switch (loadStatus)
        {
          case TileLoadStatus.Loaded:
          case TileLoadStatus.Loading:
          case TileLoadStatus.FailedLoad:
            continue;
        }

        if (ret == null)
        {
          ret = chunk;
        }
        else if (chunk.LOD == 1)
        {
          ret = chunk;
          return ret;
        }
        else if (chunk.LOD < ret.LOD)
        {
          //Lower LOD's first
          ret = chunk;
        }
        else if (chunk.LOD == ret.LOD)
        {
          if (chunk.ApproxPrevSqrDistanceTo(ref cameraPos3d) < ret.ApproxPrevSqrDistanceTo(ref cameraPos3d))
          {
            ret = chunk;
          }
        }
      }

      return ret;
    }

    private void OnCullingGroupStateChanged(CullingGroupEvent args)
    {
      if (args.index == 0)
      {
        if (args.hasBecomeVisible)
        {
          if (m_ChunkCount == 0)
          {
            GenerateLOD(m_Children);
          }
        }
      }
      else
      {
        var chunk = m_Chunks[args.index - 1];

        if (chunk.Active)
        {
          if (args.hasBecomeVisible)
          {
            var parent = chunk.Parent;
            if (parent == null || parent.AllChildrenLoaded)
            {
              if (chunk.AllChildrenLoaded)
              {
                //They've got it under control
                HideChunk(chunk);
              }
              else
              {
                //Show this chunk
                ShowChunk(chunk);
              }
            }

            ChunkVisible(chunk);
          }
          else if (args.hasBecomeInvisible)
          {
            HideChunk(chunk);
            ChunkNotVisible(chunk);
          }
        }
      }
    }

    private void ShowChunk(TerrainTileChunk chunk)
    {
      if (m_MaxPhysicsLOD == chunk.LOD)
      {
        chunk.GameObject.SetActive(true);
        if (!chunk.Renderer.enabled)
        {
          chunk.Renderer.enabled = true;
          m_TileLoadContext.TileObjectActive.Raise(chunk.TileInfo);
        }
      }
      else
      {
        if (!chunk.GameObject.activeSelf)
        {
          chunk.GameObject.SetActive(true);
          m_TileLoadContext.TileObjectActive.Raise(chunk.TileInfo);
        }
      }
    }

    private void HideChunk(TerrainTileChunk chunk)
    {
      if (m_MaxPhysicsLOD == chunk.LOD)
      {
        //Just disable our renderer but keep the collider active. Child collider will be inactive
        chunk.GameObject.SetActive(true);
        if (chunk.Renderer.enabled)
        {
          chunk.Renderer.enabled = false;
          m_TileLoadContext.TileObjectInactive.Raise(chunk.TileInfo);
        }
      }
      else
      {
        if (chunk.GameObject.activeSelf)
        {
          chunk.GameObject.SetActive(false);
          m_TileLoadContext.TileObjectInactive.Raise(chunk.TileInfo);
        }
      }
    }

    private TerrainTileChunk AllocateChunk(TerrainTileChunk parent, TerrainTileIndex index)
    {
      if (m_ChunkCount + 1 >= m_Chunks.Length)
      {
        //No more chunks left
        UnityEngine.Debug.LogError("TerrainTileChunker: no chunks remaining");
        return null;
      }

      Profiler.BeginSample("Allocate Chunk");

      Profiler.BeginSample("Selecting Chunk");

      TerrainTileChunk chunkToReuse;
      if (m_LoadedChunks.TryGetValue(index, out chunkToReuse))
      {
        //We already have a chunk with that tile loaded or partially loaded
        m_LoadedChunks.Remove(index);
        chunkToReuse.Parent = parent;
      }
      else
      {
        double furthestDistance = 0;
        var cameraPos3d = m_Camera.transform.position.ToVec3LeftHandedGeocentric() + m_WorldContext.WorldOrigin;

        for (int i = m_ChunkCount; i < m_Chunks.Length; ++i)
        {
          var chunk = m_Chunks[i];

          if (chunk.LOD == 0)
          {
            //Hasn't loaded anything
            chunkToReuse = chunk;
            break;
          }

          //Skip chunks that haven't finished cancelling out
          if (chunk.Loading)
          {
            continue;
          }

          var newDistance = chunk.ApproxPrevSqrDistanceTo(ref cameraPos3d);

          if (chunkToReuse == null || newDistance > furthestDistance)
          {
            chunkToReuse = chunk;
            furthestDistance = newDistance;
          }
        }

        if (chunkToReuse == null)
        {
          UnityEngine.Debug.LogError("TerrainTileChunker: no ellegible chunks remaining");
          return null;
        }

        m_LoadedChunks.Remove(chunkToReuse.TileInfo);

        if (chunkToReuse.Loaded)
        {
          ChunkUnloaded(chunkToReuse);
        }

        chunkToReuse.ReinitializeWithTile(this, parent, index);
      }

      Profiler.EndSample();

      var reusedIndex = chunkToReuse.ChunkIndex;
      if (reusedIndex != m_ChunkCount)
      {
        //Swap it so it's at m_ChunkCount
        var tmp = m_Chunks[m_ChunkCount];

        //Move and set new index
        m_Chunks[reusedIndex] = tmp;
        tmp.ChunkIndex = reusedIndex;

        //Move and set new index
        m_Chunks[m_ChunkCount] = chunkToReuse;
        chunkToReuse.ChunkIndex = m_ChunkCount;
      }

      ++m_ChunkCount;
      m_CullingGroup.SetBoundingSphereCount(m_ChunkCount + 1);

      if (!chunkToReuse.PendingCancellation)
      {
        chunkToReuse.GameObject.name = string.Format("Tile {0}", FormatTileID(index));
      }
      else
      {
        chunkToReuse.GameObject.name = string.Format("Tile (cancelling) {0}", FormatTileID(index));
      }

      chunkToReuse.GameObject.transform.SetParent(m_ActiveChunksObject.transform, false);

      Profiler.EndSample();

      return chunkToReuse;
    }

    private void GenerateLOD(List<TerrainTileChunk> generatedChunks)
    {
      var lod = 1;
      var numXTiles = m_TileLoadContext.ActiveTileMapper.NumTilesX(lod);
      var numYTiles = m_TileLoadContext.ActiveTileMapper.NumTilesY(lod);

      for (var y = 0; y < numYTiles; ++y)
      {
        for (var x = 0; x < numXTiles; ++x)
        {
          generatedChunks.Add(AllocateChunk(null, new TerrainTileIndex(lod, y, x)));
        }
      }
    }

    private readonly double m_PreloadPercent;
    private readonly int m_MaxTileLOD;
    private readonly bool m_DisablePhysics;
    private readonly int m_MaxPhysicsLOD;
    private readonly int m_LoadFrameBudget;
    private readonly byte m_AtlasTileSize;
    private readonly float m_ResolutionDistanceBias;
    private readonly Stopwatch m_LoadFrameStopwatch = new Stopwatch();
    private readonly IWorldContext m_WorldContext;
    private readonly TileLoadContext m_TileLoadContext;

    //The current viewer context. Set by the World Chunker Update before our Update is called
    public OneWorldSDKViewerContext ViewerContext;

    private readonly ITileMeshProvider m_MeshProvider;

    //Object to organize pooled chunk objects in the hirearchy
    private readonly GameObject m_ChunkPoolObject;
    private readonly GameObject m_ActiveChunksObject;

    private readonly CullingGroup m_CullingGroup;

    //All the chunks we're able to use. Only the first m_ChunkCount chunks are 'active'
    private readonly TerrainTileChunk[] m_Chunks;
    private readonly TerrainTileChunk[] m_UpdateChunks;
    private readonly TileAtlasInfo[] m_TextureAtlases;

    //Chunks that have loaded or partially loaded the given tiles
    private readonly IDictionary<TerrainTileIndex, TerrainTileChunk> m_LoadedChunks;

    private readonly BoundingSphere[] m_BoundingSpheres;
    private Camera m_Camera;
    private ITerrainTileProvider m_TerrainTileProvider;
    private ITileMapper m_TileMapper = WMSTileMapper.Instance;

    //Number of active chunks
    private ushort m_ChunkCount;
    private Material[] m_AdditionalMaterials;

    private readonly List<TerrainTileChunk> m_Children = new List<TerrainTileChunk>();

    private readonly List<TileEvent> m_TileEvents = new List<TileEvent>();

    //An array of buffers for load ops to use while loading
    private readonly byte[][] m_TextureBuffers;

    // An array of buffers for load ops to decode texture data into
    private readonly int[][] m_PixelBuffers;

    //An array of buffers for load ops to use while loading pixel data
    private readonly Color32[][][] m_MipsBuffers;

    //An array of buffers for load ops to use while loading
    private readonly List<Task>[] m_LoadTasks;

    //The active tile load operations we've got going
    private readonly TileLoadOp[] m_ActiveLoadOps;

    //The next load op we should check
    private int m_NextLoadOp = 0;

    private readonly float[] m_LODDistances = new float[]
    {
      float.PositiveInfinity
    };

    private bool m_Disposed;

    private enum TileEventType
    {
      Loading,
      Loaded,
      Cancelled,
      Failed
    }

    public enum TileLoadStatus
    {
      Idle,
      Loading,
      FailedLoad,
      Loaded
    }

    private struct TileEvent
    {
      public UnityEngine.Color Color
      {
        get { return m_Color; }
      }

      public float Time
      {
        get { return m_Time; }
      }

      public Vec3LeftHandedGeocentric Center
      {
        get { return m_Center; }
      }

      public float Radius
      {
        get { return m_Radius; }
      }

      public TileEvent(UnityEngine.Color color, float time, Vec3LeftHandedGeocentric center, float radius)
      {
        m_Color = color;
        m_Time = time;
        m_Center = center;
        m_Radius = radius;
      }

      private readonly UnityEngine.Color m_Color;
      private readonly float m_Time;
      private readonly Vec3LeftHandedGeocentric m_Center;
      private readonly float m_Radius;
    }

    private class TileAtlasInfo : IDisposable
    {
      public Material Material
      {
        get { return m_Material; }
      }

      public TileAtlasInfo(int id, byte tileSize, Shader singleShader, Shader atlasShader, bool compressTextures)
      {
        if (tileSize != 1)
        {
          m_Texture = new Texture2DArray(256, 256, tileSize * tileSize, TextureFormat.RGBA32, true);
        }
        else
        {
          m_Texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
        }

        m_Texture.wrapMode = TextureWrapMode.Clamp;

        if (tileSize != 1)
        {
          // When using a texture atlas, set up render queue so materials are batched
          m_Material = new Material(atlasShader);
          m_Material.renderQueue += id;
        }
        else
        {
          m_Material = new Material(singleShader);
        }

        m_Material.mainTexture = m_Texture;

        m_Dirty = false;
        m_Disposed = false;
        m_Compress = compressTextures;
      }

      public void ApplyIfDirty()
      {
        if (m_Dirty)
        {
          if (m_Texture is Texture2DArray)
          {
            ((Texture2DArray)m_Texture).Apply(false);
          }
          else
          {
            var tex = (Texture2D)m_Texture;
            tex.Apply(false);
            if (m_Compress)
              tex.Compress(false);
          }

          m_Dirty = false;
        }
      }

      public void CopyTexture(Color32[][] mips, int atlasIndex)
      {
        if (m_Texture is Texture2DArray)
        {
          var tex = (Texture2DArray)m_Texture;
          for (var i = 0; i < mips.Length; ++i)
          {
            tex.SetPixels32(mips[i], atlasIndex, i);
          }
        }
        else
        {
          var tex = new Texture2D(256, 256, TextureFormat.RGBA32, true);
          tex.wrapMode = TextureWrapMode.Clamp;

          for (var i = 0; i < mips.Length; ++i)
          {
            tex.SetPixels32(mips[i], i);
          }

          m_Material.mainTexture = tex;
          UnityEngine.Object.DestroyImmediate(m_Texture);
          m_Texture = tex;
        }

        m_Dirty = true;
      }

      public void Dispose()
      {
        if (m_Disposed)
        {
          throw new ObjectDisposedException(ToString());
        }

        m_Disposed = true;

        UnityEngine.Object.DestroyImmediate(m_Material);
        UnityEngine.Object.DestroyImmediate(m_Texture);
      }

      private Texture m_Texture;
      private readonly Material m_Material;
      private bool m_Disposed;

      private bool m_Dirty;
      private bool m_Compress;
    }

    private sealed class TileLoadOp
    {
      public TerrainTileChunk Chunk
      {
        get { return m_Chunk; }
      }

      public AsyncTask Task
      {
        get { return m_Task; }
      }

      public AsyncCancellationTokenSource TokenSource
      {
        get { return m_CancellationSource; }
      }

      public TileLoadOp(TerrainTileChunker chunker, int opIdx, TerrainTileChunk chunk, IReadOnlyList<ILoadTile> loaders)
      {
        m_Chunk = chunk;
        m_CancellationSource = new AsyncCancellationTokenSource();

        var tileIndex = chunk.TileInfo;
        var token = m_CancellationSource.Token;

        var loadersCount = loaders.Count;
        var tasks = new AsyncTask[loadersCount + 1];
        for (var i = 0; i < loadersCount; ++i)
        {
          tasks[i] = new AsyncTask(loaders[i].BeginLoading(tileIndex, token), token);
        }

        tasks[loadersCount] = new AsyncTask(chunk.Load(chunker, chunker.m_TextureBuffers[opIdx], chunker.m_PixelBuffers[opIdx], chunker.m_MipsBuffers[opIdx], chunker.m_LoadTasks[opIdx], token), token);

        m_Task = new AsyncTask(DoLoad(tasks, m_CancellationSource), token);

        chunk.LoadStatus = TileLoadStatus.Loading;
        chunk.LoadOp = this;
      }

      private static bool AllCancellable(AsyncTask[] loaders)
      {
        for (var i = 0; i < loaders.Length; ++i)
        {
          if (!loaders[i].Current)
          {
            return false;
          }
        }

        return true;
      }

      private static IEnumerator<bool> DoLoad(AsyncTask[] loaders, AsyncCancellationTokenSource cancellationSource)
      {
        var loadersLen = loaders.Length;
        bool allDone;
        bool haveException = false;

        do
        {
          allDone = true;
          for (var i = 0; i < loadersLen; ++i)
          {
            var loader = loaders[i];
            if (loader.IsCompleted)
            {
              continue;
            }

            if (loader.MoveNext())
            {
              allDone = false;
            }
            else if (loader.IsFaulted)
            {
              if (!haveException)
              {
                haveException = true;
                cancellationSource.Cancel();
              }
            }

            yield return !haveException && AllCancellable(loaders);
          }
        } while (!allDone);

        if (haveException)
        {
          //Check for exceptions
          var exceptions = new List<Exception>();
          for (var i = 0; i < loadersLen; ++i)
          {
            var task = loaders[i];
            if (task.IsFaulted)
            {
              exceptions.Add(task.Exception);
            }
          }

          if (exceptions.Count == 1)
          {
            throw exceptions[0];
          }
          else
          {
            throw new AggregateException(exceptions);
          }
        }

        yield return true;
      }

      private readonly TerrainTileChunk m_Chunk;
      private readonly AsyncTask m_Task;
      private readonly AsyncCancellationTokenSource m_CancellationSource;
    }

    private static string FormatTileID(TerrainTileIndex idx)
    {
      var quadKey = WMSConversions.TileToQuadKey(idx);
      var lod = idx.Level;
      var row = idx.Row;
      var col = idx.Column;

      return string.Format("({1})[{2}][{3}]{{{0}}}", quadKey, lod, row, col);
    }

    private static IEnumerable<Geodetic2d> BoundPoints(GeoBoundingBox box)
    {
      yield return box.West;
      yield return box.NorthWest;
      yield return box.North;
      yield return box.NorthEast;
      yield return box.East;
      yield return box.SouthEast;
      yield return box.South;
      yield return box.SouthWest;
    }

    private static Bounds3d EstimateBounds(GeoBoundingBox geoBounds, Ellipsoid ellipsoid)
    {
      return Bounds3d.FromPoints(BoundPoints(geoBounds).Select(p => ellipsoid.ToVec3LeftHandedGeocentric(p).ToVector3d()));
    }

    private sealed class TerrainTileChunk : IDisposable
    {
      public ushort ChunkID
      {
        get { return m_ChunkID; }
      }

      public ushort ChunkIndex
      {
        get { return m_ChunkIndex; }
        set { m_ChunkIndex = value; }
      }

      public GameObject GameObject
      {
        get { return m_TileObject; }
      }

      public TerrainTileIndex TileInfo
      {
        get { return m_TileInfo; }
        set { m_TileInfo = value; }
      }

      public Vec3LeftHandedGeocentric Center
      {
        get { return m_Center3d; }
      }

      public float Radius
      {
        get { return m_Radius; }
      }

      public int LOD
      {
        get { return m_TileInfo.Level; }
      }

      public double ResolutionMPP
      {
        get { return m_ResolutionMPP; }
      }

      public TerrainTileChunk Parent
      {
        get { return m_Parent; }
        set { m_Parent = value; }
      }

      public TerrainTileChunk[] Children
      {
        get { return m_Children; }
      }

      public bool HaveSubdivided
      {
        get { return m_HaveSubdivided; }
        set { m_HaveSubdivided = value; }
      }

      public bool AnyChildrenFailedToLoad
      {
        get { return m_AnyChildrenFailedToLoad; }
        set { m_AnyChildrenFailedToLoad = value; }
      }

      public bool AllChildrenLoaded
      {
        get { return m_AllChildrenLoaded; }
        set { m_AllChildrenLoaded = value; }
      }

      public bool PendingCancellation
      {
        get { return m_PendingCancellation; }
        set { m_PendingCancellation = value; }
      }

      public Renderer Renderer
      {
        get { return m_Renderer; }
      }

      public Collider Collider
      {
        get { return m_Collider; }
      }

      public TileLoadOp LoadOp
      {
        get { return m_LoadOp; }
        set { m_LoadOp = value; }
      }

      public bool Loaded
      {
        get { return m_LoadStatus == TileLoadStatus.Loaded; }
      }

      public bool Loading
      {
        get { return m_LoadStatus == TileLoadStatus.Loading; }
      }

      public TileLoadStatus LoadStatus
      {
        get { return m_LoadStatus; }
        set { m_LoadStatus = value; }
      }

      public bool Active
      {
        get { return m_Active; }
        set { m_Active = value; }
      }

      public bool Visible
      {
        get { return m_Visible; }
        set { m_Visible = value; }
      }

      public void ResetTexture()
      {
        m_HaveLoadedTexture = false;
        m_HaveSubdivided = false;
        m_AnyChildrenFailedToLoad = false;
        m_AllChildrenLoaded = false;
        m_LoadStatus = TileLoadStatus.Idle;
      }

      public void ResetMesh()
      {
        m_HaveLoadedMesh = false;
        m_HaveSubdivided = false;
        m_AnyChildrenFailedToLoad = false;
        m_AllChildrenLoaded = false;
        m_LoadStatus = TileLoadStatus.Idle;
      }

      public void SetAdditionalMaterials(Material[] materials)
      {
        var tileMaterial = m_Renderer.sharedMaterials[0];
        if (materials == null)
        {
          m_Renderer.sharedMaterial = tileMaterial;
        }
        else
        {
          var newMaterials = new Material[1 + materials.Length];
          newMaterials[0] = tileMaterial;
          for (var i = 0; i < materials.Length; ++i)
          {
            newMaterials[1 + i] = materials[i];
          }

          m_Renderer.sharedMaterials = newMaterials;
        }
      }

      public float DistanceTo(ref Vector3 posRTO)
      {
        return (m_TileObject.transform.position - posRTO).magnitude - (float)m_Radius;
      }

      public double ApproxPrevSqrDistanceTo(ref Vec3LeftHandedGeocentric pos3d)
      {
        return (m_Center3d - pos3d).MagnitudeSquared;
      }

      public void Dispose()
      {
        if (m_Disposed)
        {
          throw new ObjectDisposedException(ToString());
        }

        m_Disposed = true;

        UnityEngine.Object.DestroyImmediate(m_TileObject);
        UnityEngine.Object.DestroyImmediate(m_Mesh);
      }

#if DEBUG
      ~TerrainTileChunk()
      {
        if (!m_Disposed)
        {
          UnityEngine.Debug.LogError(string.Format("'{0}' was not disposed", this));
        }
      }
#endif

      internal TerrainTileChunk(TerrainTileChunker chunker, ushort index)
      {
        m_ChunkID = index;
        m_ChunkIndex = index;

        m_Mesh = new UnityMesh();
        m_TileObject = new GameObject("Inactive Tile");
        m_TileObject.SetActive(false);
        m_Renderer = m_TileObject.AddComponent<MeshRenderer>();
        int atlasIndex;
        chunker.AtlasIndexFor(this, out atlasIndex);
        m_Renderer.sharedMaterial = chunker.m_TextureAtlases[chunker.AtlasIDFor(this)].Material;

        var filter = m_TileObject.AddComponent<MeshFilter>();
        filter.sharedMesh = m_Mesh;
        if (!chunker.m_DisablePhysics)
        {
          m_Collider = m_TileObject.AddComponent<MeshCollider>();
          m_Collider.sharedMesh = m_Mesh;
        }
        else
        {
          m_Collider = null;
        }

        m_TileObject.transform.parent = chunker.m_ChunkPoolObject.transform;
      }

      public void ReinitializeWithTile(TerrainTileChunker chunker, TerrainTileChunk parent, TerrainTileIndex tileInfo)
      {
        m_Parent = parent;
        m_TileInfo = tileInfo;
        m_HaveLoadedTexture = false;
        m_HaveLoadedMesh = false;
        m_HaveSubdivided = false;
        m_AnyChildrenFailedToLoad = false;
        m_AllChildrenLoaded = false;
        m_LoadStatus = TileLoadStatus.Idle;

        var lod = m_TileInfo.Level;

        if (lod != 0)
        {
          var geoBounds = chunker.m_TileLoadContext.ActiveTileMapper.TileToBounds(tileInfo);

          {
            var geo = geoBounds.Center;

            m_ResolutionMPP = (float)((Math.Cos(geo.LatitudeRadians) * (Math.Abs(geoBounds.EastRadians - geoBounds.WestRadians)) * chunker.m_WorldContext.Ellipsoid.MaximumRadius) /
                                      chunker.m_TileLoadContext.ActiveTileMapper.TilePixelWidth(lod));
          }

          if (parent != null)
          {
            //Use our parent's bounds
            m_Center3d = parent.m_Center3d;
            m_Radius = parent.m_Radius;
          }
          else
          {
            //No parent. Calculate our position based on our generated samples
            //Set our position to an estimate for now so we can use it to calculate load priority
            var bounds = EstimateBounds(geoBounds, chunker.m_WorldContext.Ellipsoid);
            m_Center3d = new Vec3LeftHandedGeocentric(bounds.Center);
            m_Radius = (float)bounds.Extents.Magnitude;
          }

          if (!chunker.m_DisablePhysics)
          {
            //Disable collider if it's above the LOD threshold for physics
            if (lod > chunker.m_MaxPhysicsLOD)
            {
              m_Collider.enabled = false;
            }
            else
            {
              m_Collider.enabled = true;
            }
          }
        }
      }

      public IEnumerator<bool> Load(TerrainTileChunker chunker, byte[] textureBuffer, int[] pixelsBuffer, Color32[][] mipsBuffer, List<Task> loadTasks, AsyncCancellationToken tok)
      {
        loadTasks.Clear();

        //A cancellation token for our 'sub-tasks' so that we can cancel them if something goes wrong
        var loadTokenSource = new CancellationTokenSource();
        var registration = tok.Register(() => loadTokenSource.Cancel());

        Profiler.BeginSample("Initiating Load Tasks");

        //If we need to load mesh
        var tileMesh = default(TileMesh);
        var vertices = default(Vector3[]);
        var normals = default(Vector3[]);
        var uvs = default(List<Vector3>);
        var needsResize = default(bool);

        if (!m_HaveLoadedTexture)
        {
          Profiler.BeginSample("Initiating texture load");

          loadTasks.Add(chunker.m_TerrainTileProvider.LoadTerrainTileAsyncInto(m_TileInfo, textureBuffer, loadTokenSource.Token)
            .ContinueWith(t =>
            {
              var len = t.Result;
              int width, height;
              {
                if (!ImageDecoder.DecodeInto(new ArraySegment<byte>(textureBuffer, 0, len), pixelsBuffer, out width, out height)
                    || width != 256
                    || height != 256)
                {
                  throw new InvalidDataException("The image is an invalid size");
                }

                var mip0 = mipsBuffer[0];
                for (var j = 0; j < height; ++j)
                {
                  var jOff = j * width;
                  var pixeljOff = (((height - 1) - j) * width);
                  for (var i = 0; i < width; ++i)
                  {
                    //Inverted on the Y axis
                    var argb = pixelsBuffer[pixeljOff + i];
                    mip0[jOff + i] = new Color32((byte)((argb >> 16) & 0xFF), (byte)((argb >> 8) & 0xFF), (byte)((argb >> 0) & 0xFF), (byte)((argb >> 24) & 0xFF));
                  }
                }
              }

              // Generate the mips
              var numMips = mipsBuffer.Length;
              for (var mipLevel = 1; mipLevel < numMips; ++mipLevel)
              {
                var srcMipLevel = mipLevel - 1;
                var srcMipSize = 256 >> srcMipLevel;
                var srcMip = mipsBuffer[srcMipLevel];

                var dstMipLevel = mipLevel;
                var dstMipSize = 256 >> dstMipLevel;
                var dstMip = mipsBuffer[dstMipLevel];

                for (var j = 0; j < dstMipSize; ++j)
                {
                  var srcJ = j * 2;
                  for (var i = 0; i < dstMipSize; ++i)
                  {
                    var srcI = i * 2;
                    var c00 = srcMip[(srcJ + 0) * srcMipSize + (srcI + 0)];
                    var c01 = srcMip[(srcJ + 0) * srcMipSize + (srcI + 1)];
                    var c10 = srcMip[(srcJ + 1) * srcMipSize + (srcI + 1)];
                    var c11 = srcMip[(srcJ + 1) * srcMipSize + (srcI + 1)];

                    dstMip[j * dstMipSize + i] = UnityUtil.ColorBlerp(c00, c01, c10, c11);
                  }
                }
              }
            }, loadTokenSource.Token));

          Profiler.EndSample();
        }

        if (!m_HaveLoadedMesh)
        {
          Profiler.BeginSample("Initiating mesh load");

          vertices = m_Mesh.vertices;
          normals = m_Mesh.normals;
          uvs = new List<Vector3>();
          m_Mesh.GetUVs(0, uvs);

          loadTasks.Add(
            chunker.m_MeshProvider.QueryTileMeshAsync(m_TileInfo, loadTokenSource.Token)
              .ContinueWith(t =>
              {
                tileMesh = t.Result;

                var numVertices = tileMesh.Vertices.Length;
                needsResize = numVertices != vertices.Length;

                if (needsResize)
                {
                  //can't reuse existing buffers
                  vertices = new Vector3[numVertices];
                  normals = new Vector3[numVertices];
                  uvs.Clear();
                  uvs.Capacity = numVertices;
                  for (var i = 0; i < numVertices; ++i)
                  {
                    uvs.Add(Vector3.zero);
                  }
                }

                int atlasIndex;
                chunker.AtlasIndexFor(this, out atlasIndex);
                //Copy mesh data over
                for (int i = 0; i < numVertices; ++i)
                {
                  vertices[i] = tileMesh.Vertices[i].ToVector3();
                  normals[i] = tileMesh.Normals[i].ToVector3();
                  var uvf = tileMesh.Uvs[i];
                  uvs[i] = new Vector3(uvf.x, uvf.y, atlasIndex);
                }
              }, loadTokenSource.Token));

          Profiler.EndSample();
        }

        Profiler.EndSample();

        var taskCount = loadTasks.Count;

        do
        {
          var allTasksDone = true;
          var anyTaskFaulted = false;
          for (var i = 0; i < taskCount; ++i)
          {
            var task = loadTasks[i];
            if (!task.IsCompleted)
            {
              allTasksDone = false;
            }
            else if (task.IsFaulted)
            {
              anyTaskFaulted = true;
            }
          }

          if(anyTaskFaulted)
          {
            if (!loadTokenSource.IsCancellationRequested && anyTaskFaulted)
            {
              //If any load task fails, cancel the others
              loadTokenSource.Cancel();
            }
          }

          if (allTasksDone)
          {
            break;
          }

          if (tok.IsCancellationRequested)
          {
            loadTokenSource.Cancel();
          }

          yield return false;
        } while (true);

        registration.Dispose();

        yield return true;

        {
          //Check for exceptions
          var exceptions = default(List<Exception>);
          for (var i = 0; i < taskCount; ++i)
          {
            var task = loadTasks[i];
            if (task.IsFaulted)
            {
              if (exceptions == null)
              {
                exceptions = new List<Exception>();
              }

              exceptions.Add(task.Exception);
            }
          }

          loadTasks.Clear();

          if (exceptions != null)
          {
            if (exceptions.Count == 1)
            {
              throw exceptions[0];
            }
            else
            {
              throw new AggregateException(exceptions);
            }
          }
        }

        if (!m_HaveLoadedTexture)
        {
          //Actually upload the texture
          Profiler.BeginSample("Copying Texture");

          int atlasIndex;
          var atlasID = chunker.AtlasIndexFor(this, out atlasIndex);
          var atlas = chunker.m_TextureAtlases[atlasID];
          atlas.CopyTexture(mipsBuffer, atlasIndex);

          Profiler.EndSample();

          m_HaveLoadedTexture = true;

          yield return true;
        }

        if (!m_HaveLoadedMesh)
        {
          Profiler.BeginSample("Updating mesh");

          //Only clear out mesh data if we're resizing
          if (needsResize)
          {
            Profiler.BeginSample("Clearing mesh");
            m_Mesh.Clear();
            Profiler.EndSample();
          }

          Profiler.BeginSample("Setting Vertex Data");

          m_Mesh.vertices = vertices;
          m_Mesh.normals = normals;
          m_Mesh.SetUVs(0, uvs);

          Profiler.EndSample();

          //Only set triangles if needed
          if (needsResize)
          {
            Profiler.EndSample();
            //Setting triangles can be expensive, so take a break prior to doing so
            yield return false;
            Profiler.BeginSample("Updating mesh");

            Profiler.BeginSample("Setting Triangles");

            m_Mesh.SetTriangles(tileMesh.Triangles, 0, false);

            Profiler.EndSample();
          }

          //Set bounds
          Profiler.BeginSample("Setting Bounds");
          m_Mesh.bounds = new Bounds(Vector3.zero, (tileMesh.Extents * 2).ToVector3());
          m_Mesh.RecalculateBounds();
          Profiler.EndSample();

          m_Center3d = tileMesh.Center;
          m_Radius = tileMesh.Extents.Magnitude;
          m_TileObject.transform.rotation = UnityExtensions.ToUnityQuaternion(tileMesh.Rotation);

          m_HaveLoadedMesh = true;

          Profiler.EndSample();

          yield return true;
        }
      }

      private readonly ushort m_ChunkID;
      private readonly GameObject m_TileObject;
      private readonly UnityMesh m_Mesh;
      private readonly MeshRenderer m_Renderer;
      private readonly MeshCollider m_Collider;
      private readonly TerrainTileChunk[] m_Children = new TerrainTileChunk[4];

      //IDisposable pattern
      private bool m_Disposed;

      private TerrainTileChunk m_Parent;

      //The tile we've been asked to load
      private TerrainTileIndex m_TileInfo;

      //Our current approximate resolution, in Meters Per Pixel
      private float m_ResolutionMPP = float.NaN;

      //The actual position of this tile in 3d world space
      //If a tile has not been loaded, it is an estimate
      //based on its parent, or its estimated tile bounds
      private Vec3LeftHandedGeocentric m_Center3d = Vec3LeftHandedGeocentric.NaN;
      private float m_Radius = float.NaN;
      private bool m_HaveSubdivided = false;
      private bool m_AnyChildrenFailedToLoad = false;
      private bool m_AllChildrenLoaded = false;
      private bool m_PendingCancellation = false;

      /// <summary>
      /// If the tile is currently 'active', meaning it has completed loading
      /// and it is participating in the scene.
      /// </summary>
      private bool m_Active = false;

      /// <summary>
      /// If the tile is currently 'visible'
      /// </summary>
      private bool m_Visible = false;

      ///Set when moved, as well
      //Our index in the chunker's object pool
      private ushort m_ChunkIndex;

      private TileLoadStatus m_LoadStatus = TileLoadStatus.Idle;
      private TileLoadOp m_LoadOp;

      //If we've already loaded texture data for this tile
      private bool m_HaveLoadedTexture;

      //If we've already loaded elevation data for this tile
      private bool m_HaveLoadedMesh;
    }
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

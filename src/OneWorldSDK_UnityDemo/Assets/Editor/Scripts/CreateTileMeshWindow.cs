//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.IO;
using System.Linq;
using System.Threading;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Providers;
using sbio.owsdk.Providers.Bing;
using sbio.owsdk.Providers.SQL;
using sbio.owsdk.Services;
using sbio.owsdk.Tiles;
using sbio.owsdk.Unity.Extensions;
using sbio.owsdk.WMS;
using UnityEditor;
using UnityEngine;

namespace sbio.owsdk.Unity.Editor
{
  public class CreateTileMeshWindow : EditorWindow
  {
    [MenuItem("Window/OWSDK/Terrain Mesh Window")]
    public static void ShowWindow()
    {
      //Show existing window instance. If one doesn't exist, make one.
      EditorWindow.GetWindow(typeof(CreateTileMeshWindow));
    }

    public CreateTileMeshWindow()
    {
      titleContent = new GUIContent("Terrain Tile Mesh");
    }

    private static TerrainTileIndex? ParseTileID(string tileID)
    {
      if (string.IsNullOrEmpty(tileID))
      {
        return null;
      }

      try
      {
        return WMSConversions.QuadKeyToTile(tileID);
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    private static string DirectoryNameOrNull(string path)
    {
      try
      {
        return Path.GetDirectoryName(path);
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    private void OnGUI()
    {
      GUILayout.Label("Elevation Provider", EditorStyles.boldLabel);

      m_ElevationProviderTab = GUILayout.Toolbar(m_ElevationProviderTab, new string[] {"Elevation DB", "Bing"});

      Action<Action<IElevationProvider>> withElevationProvider;

      switch (m_ElevationProviderTab)
      {
        case 0:
        {
          EditorGUILayout.BeginHorizontal();
          m_ElevationDatabasePath = EditorGUILayout.TextField("Database File", m_ElevationDatabasePath);
          if (GUILayout.Button("Browse"))
          {
            var path = EditorUtility.OpenFilePanel("Database File", DirectoryNameOrNull(m_ElevationDatabasePath), "db");
            if (!string.IsNullOrEmpty(path))
            {
              m_ElevationDatabasePath = path;
            }
          }

          EditorGUILayout.EndHorizontal();

          m_ElevationDBBaseLOD = EditorGUILayout.IntField("Base LOD", m_ElevationDBBaseLOD);
          if (m_ElevationDBBaseLOD < 1 || 23 < m_ElevationDBBaseLOD)
          {
            EditorGUILayout.HelpBox("Invalid level of detail. Must be between 1 and 23", MessageType.Error);
          }

          m_ElevationDBDownsample = EditorGUILayout.Toggle("Use downsamples", m_ElevationDBDownsample);

          withElevationProvider = (fn) =>
          {
            if (!File.Exists(m_ElevationDatabasePath)
                || (m_ElevationDBBaseLOD < 1 || 23 < m_ElevationDBBaseLOD))
            {
              return;
            }
            else
            {
              using (var provider = new SQLiteElevationProvider(new FileInfo(m_ElevationDatabasePath), new SQLiteElevationProvider.Settings
              {
                BaseLOD = m_ElevationDBBaseLOD,
                UseDownsamples = m_ElevationDBDownsample
              }))
              {
                fn(provider);
              }
            }
          };
        }
          break;
        case 1:
        {
          m_BingAPIKey = EditorGUILayout.TextField("Bing API Key", m_BingAPIKey);
          withElevationProvider = (fn) =>
          {
            fn(new BingElevationProvider(new BingElevationProvider.Settings
            {
              APIKey = m_BingAPIKey
            }));
          };
        }
          break;
        default:
          throw new InvalidProgramException();
      }


      GUILayout.Label("Tile Selection", EditorStyles.boldLabel);

      TerrainTileIndex? tileID;

      m_SelectedTileInputTab = GUILayout.Toolbar(m_SelectedTileInputTab, new string[] {"Lat/Lon", "Tile ID"});
      switch (m_SelectedTileInputTab)
      {
        case 0:
        {
          m_Latitude = EditorGUILayout.DoubleField("Latitude", m_Latitude);
          m_Longitude = EditorGUILayout.DoubleField("Longitude", m_Longitude);
          var geo = Geodetic2d.FromDegrees(m_Latitude, m_Longitude);
          m_Latitude = geo.LatitudeDegrees;
          m_Longitude = geo.LongitudeDegrees;
          m_LOD = EditorGUILayout.IntField("Level of Detail", m_LOD);
          if (m_LOD < 1 || 23 < m_LOD)
          {
            EditorGUILayout.HelpBox("Invalid level of detail. Must be between 1 and 23", MessageType.Error);
            tileID = null;
          }
          else
          {
            tileID = WMSConversions.GeoToTile(geo, m_LOD);
          }

          EditorGUI.BeginDisabledGroup(!tileID.HasValue);
          EditorGUILayout.LabelField("Tile ID:", tileID.HasValue ? WMSConversions.TileToQuadKey(tileID.Value) : "");
          EditorGUI.EndDisabledGroup();
        }
          break;
        case 1:
        {
          m_TileID = EditorGUILayout.TextField("Tile ID", m_TileID);
          tileID = ParseTileID(m_TileID);
          if (!tileID.HasValue)
          {
            EditorGUILayout.HelpBox("Invalid tile ID", MessageType.Error);
          }
        }
          break;
        default:
          throw new InvalidProgramException();
      }

      GUILayout.Label("Mesh Generation", EditorStyles.boldLabel);

      m_NumSamples = EditorGUILayout.IntField("Number of Samples", m_NumSamples);
      if (m_NumSamples < 2)
      {
        EditorGUILayout.HelpBox("Invalid number of samples. Should be >= 2", MessageType.Error);
      }

      m_MeshPath = EditorGUILayout.TextField("Mesh File", m_MeshPath);

      EditorGUI.BeginDisabledGroup(!tileID.HasValue || m_NumSamples < 2);
      if (GUILayout.Button("Create"))
      {
        withElevationProvider(elevationProvider =>
        {
          using (var meshProvider = new ElevationTileMeshProvider(new Ellipsoid(6378137.0, 6356752.314245, 6378137.0), elevationProvider, new ElevationTileMeshProvider.Settings
          {
            NumSamples = m_NumSamples
          }))
          {
            var mesh = meshProvider.QueryTileMeshAsync(tileID.Value, CancellationToken.None).Result;

            var assetPath = $"Assets/{m_MeshPath}";
            var uMesh = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(assetPath);
            if (uMesh == null)
            {
              uMesh = new UnityEngine.Mesh();
              AssetDatabase.CreateAsset(uMesh, assetPath);
            }

            uMesh.Clear();
            uMesh.SetVertices(mesh.Vertices.Select(v => v.ToVector3()).ToList());
            uMesh.SetNormals(mesh.Normals.Select(v => v.ToVector3()).ToList());
            uMesh.SetUVs(0, mesh.Uvs.Select(uv => uv.ToVector2()).ToList());
            uMesh.SetTriangles(mesh.Triangles, 0, false);
            uMesh.bounds = new Bounds(Vector3.zero, (mesh.Extents * 2).ToVector3());
          }
        });
      }

      EditorGUI.EndDisabledGroup();
    }

    private int m_ElevationProviderTab = 0;
    private string m_ElevationDatabasePath;
    private int m_ElevationDBBaseLOD = 13;
    private bool m_ElevationDBDownsample = true;
    private string m_BingAPIKey;
    private int m_SelectedTileInputTab = 0;
    private double m_Latitude;
    private double m_Longitude;
    private int m_LOD = 1;
    private string m_TileID;

    private int m_NumSamples = 20;
    private string m_MeshPath = "Tile Mesh.asset";
  }
}




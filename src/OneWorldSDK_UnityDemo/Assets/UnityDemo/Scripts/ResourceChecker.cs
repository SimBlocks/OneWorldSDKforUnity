//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
// Resource Checker
// Modification of 
// https://github.com/handcircus/Unity-Resource-Checker
// for runtime usage

using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace sbio.OneWorldSDKViewer
{
  public struct TextureDetails
  {
    public int MemSizeKB;
    public Texture Texture;
    public TextureFormat Format;
  }

  public class ResourceChecker
  {
    public bool IncludeDisabledObjects = true;
    public bool IncludeScriptReferences = true;
    public bool IncludeGuiElements = true;

    public HashSet<Texture> ActiveTextures = new HashSet<Texture>();
    public HashSet<Material> ActiveMaterials = new HashSet<Material>();
    public HashSet<Mesh> ActiveMeshes = new HashSet<Mesh>();

    public int TotalTextureMemoryKB { get; private set; }
    public int TotalMeshVertices { get; private set; }
    public int TotalMeshTriangles { get; private set; }

    public void CheckResources()
    {
      TotalTextureMemoryKB = 0;
      TotalMeshVertices = 0;
      TotalMeshTriangles = 0;
      ActiveTextures.Clear();
      ActiveMaterials.Clear();
      ActiveMeshes.Clear();

      var renderers = FindObjects<Renderer>();

      if (RenderSettings.skybox != null)
      {
        RecordMaterial(RenderSettings.skybox);
      }

      foreach (var renderer in renderers)
      {
        foreach (var material in renderer.sharedMaterials)
        {
          if (material != null)
          {
            RecordMaterial(material);
          }
        }

        if (renderer is SpriteRenderer)
        {
          var tSpriteRenderer = (SpriteRenderer)renderer;

          if (tSpriteRenderer.sprite != null)
          {
            RecordTexture(tSpriteRenderer.sprite.texture);
          }
        }
        else if (renderer is SkinnedMeshRenderer)
        {
          var tSkinnedMeshRenderer = (SkinnedMeshRenderer)renderer;
          var tMesh = tSkinnedMeshRenderer.sharedMesh;
          if (tMesh != null)
          {
            RecordMesh(tMesh);
          }
        }
      }

      if (IncludeGuiElements)
      {
        var graphics = FindObjects<Graphic>();

        foreach (var graphic in graphics)
        {
          if (graphic.mainTexture)
          {
            RecordTexture(graphic.mainTexture);
          }

          if (graphic.materialForRendering)
          {
            RecordMaterial(graphic.materialForRendering);
          }
        }

        var buttons = FindObjects<Button>();
        foreach (var button in buttons)
        {
          CheckButtonSpriteState(button, button.spriteState.disabledSprite);
          CheckButtonSpriteState(button, button.spriteState.highlightedSprite);
          CheckButtonSpriteState(button, button.spriteState.pressedSprite);
        }
      }

      var meshFilters = FindObjects<MeshFilter>();

      foreach (var tMeshFilter in meshFilters)
      {
        var tMesh = tMeshFilter.sharedMesh;
        if (tMesh != null)
        {
          RecordMesh(tMesh);
        }
      }

      if (IncludeScriptReferences)
      {
        var scripts = FindObjects<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
          var flags = BindingFlags.Public | BindingFlags.Instance; // only public non-static fields are bound to by Unity.
          var fields = script.GetType().GetFields(flags);

          foreach (FieldInfo field in fields)
          {
            var fieldType = field.FieldType;
            if (fieldType == typeof(Sprite))
            {
              var tSprite = field.GetValue(script) as Sprite;
              if (tSprite != null)
              {
                RecordTexture(tSprite.texture);
              }
            }

            if (fieldType == typeof(Mesh))
            {
              var tMesh = field.GetValue(script) as Mesh;
              if (tMesh != null)
              {
                RecordMesh(tMesh);
              }
            }

            if (fieldType == typeof(Material))
            {
              var tMaterial = field.GetValue(script) as Material;
              if (tMaterial != null)
              {
                RecordMaterial(tMaterial);
              }
            }
          }
        }
      }
    }

    public static string FormatSizeString(int memSizeKB)
    {
      if (memSizeKB < 1024) return "" + memSizeKB + "k";
      else
      {
        float memSizeMB = (memSizeKB) / 1024.0f;
        return memSizeMB.ToString("0.00") + "Mb";
      }
    }

    public static int GetBitsPerPixel(TextureFormat format)
    {
      switch (format)
      {
        case TextureFormat.Alpha8: //	 Alpha-only texture format.
          return 8;
        case TextureFormat.ARGB4444: //	 A 16 bits/pixel texture format. Texture stores color with an alpha channel.
          return 16;
        case TextureFormat.RGBA4444: //	 A 16 bits/pixel texture format.
          return 16;
        case TextureFormat.RGB24: // A color texture format.
          return 24;
        case TextureFormat.RGBA32: //Color with an alpha channel texture format.
          return 32;
        case TextureFormat.ARGB32: //Color with an alpha channel texture format.
          return 32;
        case TextureFormat.RGB565: //	 A 16 bit color texture format.
          return 16;
        case TextureFormat.DXT1: // Compressed color texture format.
          return 4;
        case TextureFormat.DXT5: // Compressed color with alpha channel texture format.
          return 8;
        /*
        case TextureFormat.WiiI4:	// Wii texture format.
        case TextureFormat.WiiI8:	// Wii texture format. Intensity 8 bit.
        case TextureFormat.WiiIA4:	// Wii texture format. Intensity + Alpha 8 bit (4 + 4).
        case TextureFormat.WiiIA8:	// Wii texture format. Intensity + Alpha 16 bit (8 + 8).
        case TextureFormat.WiiRGB565:	// Wii texture format. RGB 16 bit (565).
        case TextureFormat.WiiRGB5A3:	// Wii texture format. RGBA 16 bit (4443).
        case TextureFormat.WiiRGBA8:	// Wii texture format. RGBA 32 bit (8888).
        case TextureFormat.WiiCMPR:	//	 Compressed Wii texture format. 4 bits/texel, ~RGB8A1 (Outline alpha is not currently supported).
          return 0;  //Not supported yet
        */
        case TextureFormat.PVRTC_RGB2: //	 PowerVR (iOS) 2 bits/pixel compressed color texture format.
          return 2;
        case TextureFormat.PVRTC_RGBA2: //	 PowerVR (iOS) 2 bits/pixel compressed with alpha channel texture format
          return 2;
        case TextureFormat.PVRTC_RGB4: //	 PowerVR (iOS) 4 bits/pixel compressed color texture format.
          return 4;
        case TextureFormat.PVRTC_RGBA4: //	 PowerVR (iOS) 4 bits/pixel compressed with alpha channel texture format
          return 4;
        case TextureFormat.ETC_RGB4: //	 ETC (GLES2.0) 4 bits/pixel compressed RGB texture format.
          return 4;
        case TextureFormat.BGRA32: //	 Format returned by iPhone camera
          return 32;
      }

      return 0;
    }

    public static int CalculateTextureSizeBytes(Texture tTexture)
    {
      int tWidth = tTexture.width;
      int tHeight = tTexture.height;
      if (tTexture is Texture2D)
      {
        var tTex2D = (Texture2D)tTexture;
        var bitsPerPixel = GetBitsPerPixel(tTex2D.format);
        var mipMapCount = tTex2D.mipmapCount;
        var mipLevel = 1;
        var tSize = 0;
        while (mipLevel <= mipMapCount)
        {
          tSize += tWidth * tHeight * bitsPerPixel / 8;
          tWidth = tWidth / 2;
          tHeight = tHeight / 2;
          mipLevel++;
        }

        return tSize;
      }

      if (tTexture is Texture2DArray)
      {
        var tTex2D = (Texture2DArray)tTexture;
        var bitsPerPixel = GetBitsPerPixel(tTex2D.format);
        var mipMapCount = 10;
        var mipLevel = 1;
        var tSize = 0;
        while (mipLevel <= mipMapCount)
        {
          tSize += tWidth * tHeight * bitsPerPixel / 8;
          tWidth = tWidth / 2;
          tHeight = tHeight / 2;
          mipLevel++;
        }

        return tSize * tTex2D.depth;
      }

      if (tTexture is Cubemap)
      {
        var tCubemap = tTexture as Cubemap;
        var bitsPerPixel = GetBitsPerPixel(tCubemap.format);
        return tWidth * tHeight * 6 * bitsPerPixel / 8;
      }

      return 0;
    }

    private void CheckButtonSpriteState(Button button, Sprite sprite)
    {
      if (sprite == null) return;

      var texture = sprite.texture;
      if (ActiveTextures.Add(texture))
      {
        var tButtonTextureDetail = CalculateTextureDetails(texture);
        TotalTextureMemoryKB += tButtonTextureDetail.MemSizeKB;
      }
    }

    private static IList<GameObject> GetAllRootGameObjects()
    {
#if !UNITY_5 && !UNITY_5_3_OR_NEWER
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
#else
      var allGo = new List<GameObject>();
      for (int sceneIdx = 0; sceneIdx < UnityEngine.SceneManagement.SceneManager.sceneCount; ++sceneIdx)
      {
        allGo.AddRange(UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIdx).GetRootGameObjects());
      }

      return allGo;
#endif
    }

    private IList<T> FindObjects<T>() where T : Object
    {
      if (IncludeDisabledObjects)
      {
        var meshfilters = new List<T>();
        foreach (var go in GetAllRootGameObjects())
        {
          foreach (var tr in go.GetComponentsInChildren<Transform>(true))
          {
            var comp = tr.GetComponent<T>();
            if (comp)
            {
              meshfilters.Add(comp);
            }
          }
        }

        return meshfilters;
      }
      else
      {
        return Object.FindObjectsOfType<T>();
      }
    }

    private void RecordMesh(Mesh mesh)
    {
      if (ActiveMeshes.Add(mesh))
      {
        TotalMeshVertices += mesh.vertexCount;
        TotalMeshTriangles += mesh.triangles.Length;
      }
    }

    private void RecordMaterial(Material material)
    {
      if (ActiveMaterials.Add(material))
      {
        if (material.HasProperty("_MainTex") && material.mainTexture != null)
        {
          RecordTexture(material.mainTexture);
        }
      }
    }

    private void RecordTexture(Texture texture)
    {
      if (ActiveTextures.Add(texture))
      {
        var details = CalculateTextureDetails(texture);
        TotalTextureMemoryKB += details.MemSizeKB;
      }
    }

    private static TextureDetails CalculateTextureDetails(Texture tTexture)
    {
      var ret = new TextureDetails()
      {
        Texture = tTexture
      };

      var memSize = CalculateTextureSizeBytes(tTexture);

      var tFormat = TextureFormat.RGBA32;
      if (tTexture is Texture2D)
      {
        tFormat = (tTexture as Texture2D).format;
      }

      if (tTexture is Cubemap)
      {
        tFormat = (tTexture as Cubemap).format;
        memSize = 8 * tTexture.height * tTexture.width;
      }

      if (tTexture is Texture2DArray)
      {
        tFormat = (tTexture as Texture2DArray).format;
      }

      ret.MemSizeKB = memSize / 1024;
      ret.Format = tFormat;

      return ret;
    }
  }
}



//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

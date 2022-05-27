//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using sbio.Core.Math;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Services;
using Hjg.Pngcs;
using sbio.owsdk.Images;
using sbio.owsdk.Tiles;
using sbio.owsdk.WMS;

namespace sbio.owsdk.Exporters
{
  public sealed class GeoOBJExporter
  {
    public sealed class ExportSettings
    {
      public static ExportSettings Default
      {
        get { return new ExportSettings(); }
      }

      public GeoBoundingBox Area { get; set; }
      public string MeshName { get; set; }
      public string MaterialsDirName { get; set; }
      public DirectoryInfo ExportDirectory { get; set; }
      public int ImageryLOD { get; set; }
      public int ElevationLOD { get; set; }
      public int AtlasTileSize { get; set; }

      public ExportSettings()
      {
        Area = GeoBoundingBox.FromRadians(0, 0, 0, 0);
        MeshName = "AreaExport";
        MaterialsDirName = "Materials";
        ExportDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
        ImageryLOD = 19;
        ElevationLOD = 13;
        AtlasTileSize = 8;
      }
    }

    public GeoOBJExporter(ITerrainTileProvider imageryProvider = null, IElevationProvider elevationProvider = null)
      : this(new Ellipsoid(6378137.0, 6356752.314245, 6378137.0), imageryProvider, elevationProvider)
    {
    }

    public GeoOBJExporter(Ellipsoid ellipsoid, ITerrainTileProvider imageryProvider = null, IElevationProvider elevationProvider = null)
    {
      m_Ellipsoid = ellipsoid;
      m_ImageryProvider = imageryProvider;
      m_ElevationProvider = elevationProvider;
    }

    public void Export(ExportSettings settings)
    {
      var area = settings.Area;
      var ellipsoid = m_Ellipsoid;
      var imageryLOD = settings.ImageryLOD;
      var elevationLOD = settings.ElevationLOD;
      var atlasTileSize = settings.AtlasTileSize;
      var meshName = settings.MeshName;
      var exportDir = settings.ExportDirectory;
      var imageryProvider = m_ImageryProvider;
      var elevationProvider = m_ElevationProvider;
      var materialsDirName = settings.MaterialsDirName;

      var atlasPixelSize = atlasTileSize * 256;
      var lodDiff = imageryLOD - elevationLOD;

      exportDir.Create();

      int atlasesStartPixelX, atlasesStartPixelY;
      {
        int atlasesStartCol, atlasesStartRow;
        WMSConversions.GeoToTileXY(area.NorthWest, imageryLOD, out atlasesStartCol, out atlasesStartRow);
        WMSConversions.TileXYToPixelXY(atlasesStartCol, atlasesStartRow, out atlasesStartPixelX, out atlasesStartPixelY);
      }

      int atlasesEndPixelX, atlasesEndPixelY;
      {
        int atlasesEndCol, atlasesEndRow;
        WMSConversions.GeoToTileXY(area.SouthEast, imageryLOD, out atlasesEndCol, out atlasesEndRow);
        WMSConversions.TileXYToPixelXY(atlasesEndCol, atlasesEndRow, out atlasesEndPixelX, out atlasesEndPixelY);
      }

      var geoCenter = area.Center;

      Vec3LeftHandedGeocentric origin3d;
      QuaternionLeftHandedGeocentric originRot;
      {
        Vec3LeftHandedGeocentric normal, north;
        origin3d = ellipsoid.ToVec3LeftHandedGeocentric(geoCenter, out normal, out north);
        originRot = MathUtilities.CreateGeocentricRotation(north, normal);
      }
      var originInvRot = originRot.Inverse();

      int numAtlasesX, numAtlasesY;
      {
        int leftCol, topRow;
        WMSConversions.GeoToTileXY(area.NorthWest, imageryLOD, out leftCol, out topRow);
        int rightCol, bottomRow;
        WMSConversions.GeoToTileXY(area.SouthEast, imageryLOD, out rightCol, out bottomRow);
        {
          int rem;
          numAtlasesX = Math.DivRem(rightCol - leftCol, atlasTileSize, out rem) + (rem == 0 ? 0 : 1);
        }
        {
          int rem;
          numAtlasesY = Math.DivRem(bottomRow - topRow, atlasTileSize, out rem) + (rem == 0 ? 0 : 1);
        }
      }

      var numAtlases = numAtlasesX * numAtlasesY;

      #region Generate MTL File

      if (imageryProvider != null)
      {
        using (var mtlFile = File.Open(Path.Combine(exportDir.FullName, meshName + ".mtl"), FileMode.Create, FileAccess.Write, FileShare.None))
        using (var mtlWriter = new StreamWriter(mtlFile))
        {
          mtlWriter.WriteLine("#" + meshName + ".mtl");
          mtlWriter.WriteLine("#" + DateTime.Now.ToLongDateString());
          mtlWriter.WriteLine("#" + DateTime.Now.ToLongTimeString());
          mtlWriter.WriteLine("#-------");
          mtlWriter.WriteLine();
          mtlWriter.WriteLine();

          foreach (var blockID in TileBlocksInExtent(area, imageryLOD, atlasTileSize).Select((b, i) => i + 1))
          {
            var materialName = $"MAT_{blockID}";
            var materialFile = Path.Combine(materialsDirName, materialName + ".png");
            AddMaterial(mtlWriter, materialName, materialFile);
          }
        }
      }

      #endregion

      #region Generate Atlases

      if (imageryProvider != null)
      {
        var materialsDir = new DirectoryInfo(Path.Combine(exportDir.FullName, materialsDirName));
        materialsDir.Create();
        Parallel.ForEach(TileBlocksInExtent(area, imageryLOD, atlasTileSize),
          new ParallelOptions {MaxDegreeOfParallelism = Environment.ProcessorCount},
          () => new {ImgBuf = new byte[256 * 256 * 3], PixelBuf = new int[atlasPixelSize * atlasPixelSize]},
          (block, loopState, blockIdx, tls) =>
          {
            var blockPixelWidth = block.Width * 256;
            var blockPixelHeight = block.Height * 256;
            var oddBlock = block.Width != atlasTileSize || block.Height != atlasTileSize;

            var imageBuf = tls.ImgBuf;
            var blockPixelsBuffer = oddBlock ? new int[blockPixelWidth * blockPixelHeight] : tls.PixelBuf;

            foreach (var tile in block.Tiles())
            {
              imageryProvider.LoadTerrainTileInto(tile, imageBuf);

              var localX = block.LocalX(tile);
              var localY = block.LocalY(tile);

              var tileImage = ImageDecoder.Decode(imageBuf);
              var pixels = tileImage.Pixels;
              for (var j = 0; j < 256; ++j)
              {
                var jOff = j * 256;
                var pixelJOff = (j + (localY * 256)) * blockPixelWidth;
                for (var i = 0; i < 256; ++i)
                {
                  var pixelOff = pixelJOff + (i + (localX * 256));
                  blockPixelsBuffer[pixelOff] = pixels[jOff + i];
                }
              }
            }

            var materialName = $"MAT_{blockIdx + 1}";
            var materialFileName = materialName + ".png";
            SavePNG(Path.Combine(materialsDir.FullName, materialFileName), blockPixelsBuffer, blockPixelWidth, blockPixelHeight);
            return tls;
          },
          tls => { });
      }

      #endregion

      #region Generate Mesh Data

      // Get pixel size of the area at the elevation LOD
      // We will generate a vertex for every pixel, with
      // additional vertices added to accomodate seams
      // between the imagery lod tiles 
      int pixelX, pixelY, pixelWidth, pixelHeight;
      WMSConversions.ExtentPixelSize(area, elevationLOD, out pixelX, out pixelY, out pixelWidth, out pixelHeight);

      var numVertSeams = numAtlasesX - 1;
      var numHorzSeams = numAtlasesY - 1;

      var numHorzSamples = pixelWidth + 2 * numVertSeams;
      var numVertSamples = pixelHeight + 2 * numHorzSeams;

      var numSamples = numHorzSamples * numVertSamples;

      var samples = new ElevationPointSample[numSamples];

      var leftSeams = new HashSet<int>();
      var rightSeams = new HashSet<int>();
      var topSeams = new HashSet<int>();
      var bottomSeams = new HashSet<int>();

      {
        // Generate samples
        Action<int, double> addRow =
          (jOff, latRadians) =>
          {
            var x = pixelX;
            var currVertAtlas = ((x << lodDiff) - atlasesStartPixelX) / atlasPixelSize;
            for (var i = 0; i < numHorzSamples; ++i)
            {
              samples[jOff + i] = new ElevationPointSample(Geodetic2d.FromRadians(latRadians, WMSConversions.PixelXToLongitudeRadians(x, elevationLOD)));

              var nextVertAtlas = (((x + 1) << lodDiff) - atlasesStartPixelX) / atlasPixelSize;

              if (i < numHorzSamples - 2)
              {
                //If we're about to cross a vertical seam
                if (currVertAtlas != nextVertAtlas)
                {
                  //Generate two samples. Left and right of the seam
                  var seamLonRadians = WMSConversions.PixelXToLongitudeRadians(atlasesStartPixelX + (nextVertAtlas * atlasPixelSize), imageryLOD);
                  var seamGeo = Geodetic2d.FromRadians(latRadians, seamLonRadians);
                  samples[jOff + i + 1] = new ElevationPointSample(seamGeo);
                  samples[jOff + i + 2] = new ElevationPointSample(seamGeo);
                  leftSeams.Add(i + 1);
                  rightSeams.Add(i + 2);

                  i += 2;
                }
              }

              currVertAtlas = nextVertAtlas;
              ++x;
            }
          };
        var y = pixelY;
        var currHorzAtlas = ((y << lodDiff) - atlasesStartPixelY) / atlasPixelSize;
        for (var j = 0; j < numVertSamples; ++j)
        {
          var jOff = j * numHorzSamples;

          addRow(jOff, WMSConversions.PixelYToLatitudeRadians(y, elevationLOD));

          var nextHorzAtlas = (((y + 1) << lodDiff) - atlasesStartPixelY) / atlasPixelSize;

          if (j < numVertSamples - 2)
          {
            //If we're about to cross a vertical seam
            if (currHorzAtlas != nextHorzAtlas)
            {
              //Generate two rows. Top and Bottom of the seam
              var seamLat = WMSConversions.PixelYToLatitudeRadians(atlasesStartPixelY + (nextHorzAtlas * atlasPixelSize), imageryLOD);
              addRow((j + 1) * numHorzSamples, seamLat);
              addRow((j + 2) * numHorzSamples, seamLat);

              topSeams.Add(j + 1);
              bottomSeams.Add(j + 2);

              j += 2;
            }
          }

          currHorzAtlas = nextHorzAtlas;
          ++y;
        }
      }

      var triangleGroups = new List<int>[numAtlases];
      for (var i = 0; i < numAtlases; ++i)
      {
        triangleGroups[i] = new List<int>();
      }

      var vertices = new Vector3f[numSamples];
      var normals = new Vector3f[numSamples];
      var uvs = new Vector2f[numSamples];

      if (elevationProvider != null)
      {
        elevationProvider.QueryPointSamplesInto(samples);
      }

      for (var vertIndex = 0; vertIndex < numSamples; ++vertIndex)
      {
        Geodetic2d coordGeo;
        double elev;
        {
          var sample = samples[vertIndex];
          coordGeo = sample.Position;
          elev = sample.Elevation;
        }

        {
          //Calculate vertex and normal
          Vec3LeftHandedGeocentric normal;
          var point3d = ellipsoid.ToVec3LeftHandedGeocentric(coordGeo, elev, out normal);

          vertices[vertIndex] = originInvRot.Multiply(point3d - origin3d).ToVector3f();
          normals[vertIndex] = originInvRot.Multiply(normal).ToVector3f();
        }

        {
          //Calculate UVs

          //Our pixel position at atlas LOD
          int xPx, yPx;
          WMSConversions.GeoToPixelXY(coordGeo, imageryLOD, out xPx, out yPx);

          // Figure out which atlas we're using and calculate relative UV's
          int atlasLocalX;
          var xAtlas = Math.DivRem((xPx - atlasesStartPixelX + 1), atlasPixelSize, out atlasLocalX);

          int atlasLocalY;
          var yAtlas = Math.DivRem((yPx - atlasesStartPixelY + 1), atlasPixelSize, out atlasLocalY);


          int vertI;
          var vertJ = Math.DivRem(vertIndex, numHorzSamples, out vertI);
          var is1stSeam = false;

          if (leftSeams.Contains(vertI))
          {
            atlasLocalX = atlasPixelSize;
            --xAtlas;
            is1stSeam = true;
          }
          else if (rightSeams.Contains(vertI))
          {
            atlasLocalX = 0;
          }

          if (topSeams.Contains(vertJ))
          {
            atlasLocalY = atlasPixelSize;
            --yAtlas;
            is1stSeam = true;
          }
          else if (bottomSeams.Contains(vertJ))
          {
            atlasLocalY = 0;
          }

          uvs[vertIndex] = new Vector2f((float)(atlasLocalX) / atlasPixelSize, 1 - ((float)(atlasLocalY) / atlasPixelSize));

          if (!is1stSeam)
          {
            var atlasID = yAtlas * numAtlasesX + xAtlas;
            if (vertI != numHorzSamples - 1 && vertJ != numVertSamples - 1)
            {
              var topLeftVert = (vertJ * numHorzSamples + vertI);
              var topRightVert = (vertJ * numHorzSamples + (vertI + 1));
              var bottomLeftVert = ((vertJ + 1) * numHorzSamples + vertI);
              var bottomRightVert = ((vertJ + 1) * numHorzSamples + (vertI + 1));

              var triangleGroup = triangleGroups[atlasID];

              triangleGroup.Add(topLeftVert);
              triangleGroup.Add(topRightVert);
              triangleGroup.Add(bottomRightVert);

              triangleGroup.Add(topLeftVert);
              triangleGroup.Add(bottomRightVert);
              triangleGroup.Add(bottomLeftVert);
            }
          }
        }
      }

      #endregion

      #region Output OBJ File

      using (var objFile = File.Open(Path.Combine(exportDir.FullName, meshName + ".obj"), FileMode.Create, FileAccess.Write, FileShare.None))
      using (var objWriter = new StreamWriter(objFile))
      {
        objWriter.WriteLine("#" + meshName + ".obj");
        objWriter.WriteLine("#" + DateTime.Now.ToLongDateString());
        objWriter.WriteLine("#" + DateTime.Now.ToLongTimeString());
        objWriter.WriteLine("#-------");
        objWriter.WriteLine();
        objWriter.WriteLine();

        objWriter.WriteLine($"mtllib {meshName}.mtl");
        objWriter.WriteLine();
        AddMesh(objWriter, meshName, vertices, normals, uvs, triangleGroups);
      }

      #endregion
    }

    private struct TileBlock
    {
      public int LOD { get; }
      public int LeftCol { get; }
      public int TopRow { get; }
      public int RightCol { get; }
      public int BottomRow { get; }

      public int Width
      {
        get { return (RightCol - LeftCol) + 1; }
      }

      public int Height
      {
        get { return (BottomRow - TopRow) + 1; }
      }

      public TileBlock(int lod, int left, int top, int right, int bottom)
      {
        LOD = lod;
        LeftCol = left;
        TopRow = top;
        RightCol = right;
        BottomRow = bottom;
      }

      public IEnumerable<TerrainTileIndex> Tiles()
      {
        for (var j = TopRow; j <= BottomRow; ++j)
        {
          for (var i = LeftCol; i <= RightCol; ++i)
          {
            yield return new TerrainTileIndex(LOD, j, i);
          }
        }
      }

      public int LocalX(TerrainTileIndex idx)
      {
        return idx.Column - LeftCol;
      }

      public int LocalY(TerrainTileIndex idx)
      {
        return idx.Row - TopRow;
      }
    }

    private static IEnumerable<TileBlock> TileBlocksInExtent(GeoBoundingBox area, int lod, int blockSize)
    {
      int leftCol, topRow;
      WMSConversions.GeoToTileXY(area.NorthWest, lod, out leftCol, out topRow);
      int rightCol, bottomRow;
      WMSConversions.GeoToTileXY(area.SouthEast, lod, out rightCol, out bottomRow);

      int widthBlocks;
      {
        int rem;
        widthBlocks = Math.DivRem(rightCol - leftCol, blockSize, out rem) + (rem == 0 ? 0 : 1);
      }
      int heightBlocks;
      {
        int rem;
        heightBlocks = Math.DivRem(bottomRow - topRow, blockSize, out rem) + (rem == 0 ? 0 : 1);
      }

      for (var j = 0; j < heightBlocks; ++j)
      {
        var startRow = topRow + (j * blockSize);
        var endRow = Math.Min(startRow + blockSize - 1, bottomRow);
        var blockHeight = endRow - startRow;
        for (var i = 0; i < widthBlocks; ++i)
        {
          var startCol = leftCol + (i * blockSize);
          var endCol = Math.Min(startCol + blockSize - 1, rightCol);
          var blockWidth = endCol - startCol;

          yield return new TileBlock(lod, startCol, startRow, endCol, endRow);
        }
      }
    }

    private static void AddMesh(TextWriter writer, string meshName, Vector3f[] vertices, Vector3f[] normals, Vector2f[] uvs, List<int>[] triangleGroups)
    {
      writer.WriteLine("#" + meshName);
      writer.WriteLine("#-------");
      writer.WriteLine();
      writer.WriteLine("g " + meshName);

      foreach (var v in vertices)
      {
        // Note: Flipped X to go into OBJ coordinate system
        writer.WriteLine($"v {-v.X} {v.Y} {v.Z}");
      }

      writer.WriteLine();
      foreach (var normal in normals)
      {
        writer.WriteLine($"vn {-normal.X} {normal.Y} {normal.Z}");
      }

      writer.WriteLine();

      foreach (var uv in uvs)
      {
        writer.WriteLine($"vt {uv.X} {uv.Y}");
      }

      for (var groupIdx = 0; groupIdx < triangleGroups.Length; ++groupIdx)
      {
        var group = triangleGroups[groupIdx];
        var materialName = $"MAT_{groupIdx + 1}";

        writer.WriteLine();
        writer.WriteLine("usemtl " + materialName);

        for (var i = 0; i < group.Count; i += 3)
        {
          // Note: Flipped triangles to go into OBJ coordinate system
          writer.WriteLine(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}",
            group[i + 2] + 1, group[i + 1] + 1, group[i + 0] + 1));
        }
      }
    }

    private static void AddMaterial(TextWriter writer, string materialName, string textureFilePath)
    {
      writer.WriteLine($"newmtl {materialName}");
      writer.WriteLine("Ka 1.0000, 1.0000, 1.0000");
      writer.WriteLine("Kd 1.0000, 1.0000, 1.0000");
      writer.WriteLine("Ks 1.0000, 1.0000, 1.0000");
      writer.WriteLine("Ns 1");
      writer.WriteLine("illum 0");
      writer.WriteLine($"map_Ka {textureFilePath}");
      writer.WriteLine($"map_Kd {textureFilePath}");
      writer.WriteLine();
      writer.WriteLine();
    }

    public static void SavePNG(string path, int[] rgb, int width, int height)
    {
      var file = new FileInfo(path);
      file.Directory.Create();
      using (var stream = file.OpenWrite())
      {
        FormatPNG(stream, rgb, width, height);
      }
    }

    private static int FormatPNG(Stream dest, int[] rgb, int width, int height)
    {
      var startPos = dest.Position;
      var info = new ImageInfo(width, height, 8, false);
      var writer = new PngWriter(dest, info);
      writer.ShouldCloseStream = false;
      writer.CompLevel = 9;
      var line = new ImageLine(info, ImageLine.ESampleType.BYTE);
      var rowBuf = line.ScanlineB;
      var mult = info.BytesPixel;
      for (var j = 0; j < height; ++j)
      {
        var jOff = j * width;
        for (var i = 0; i < width; ++i)
        {
          var pOff = jOff + i;
          var iOff = i * mult;
          rowBuf[iOff + 0] = (byte)((rgb[pOff] >> 16) & 0xFF);
          rowBuf[iOff + 1] = (byte)((rgb[pOff] >> 8) & 0xFF);
          rowBuf[iOff + 2] = (byte)((rgb[pOff] >> 0) & 0xFF);
        }

        writer.WriteRow(line, j);
      }

      writer.End();

      return (int)(dest.Position - startPos);
    }

    private readonly Ellipsoid m_Ellipsoid;
    private readonly ITerrainTileProvider m_ImageryProvider;
    private readonly IElevationProvider m_ElevationProvider;
  }
}



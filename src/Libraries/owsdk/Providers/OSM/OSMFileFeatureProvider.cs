//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OsmSharp.Streams;
using sbio.owsdk.Features;
using sbio.owsdk.OSM;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Services;
using sbio.owsdk.Tiles;
using sbio.owsdk.WMS;

namespace sbio.owsdk.Providers.OSM
{
  public sealed class OSMFileFeatureProvider : IFeatureProvider
  {
    public sealed class Settings
    {
      public static Settings Default
      {
        get { return new Settings(); }
      }

      public Settings()
      {
        LOD = 16;
      }

      public int LOD { get; set; }
    }

    public Task QueryFeaturesIn(GeoBoundingBox area, Action<Feature> consumer, CancellationToken tok)
    {
      return Task.Run(() =>
      {
        var context = new OSMFeatureContext(consumer, tok);

        foreach (var file in WMSConversions.TilesInExtent(area, m_LOD)
          .Select(FileForTile)
          .Where(f => f.Exists))
        {
          tok.ThrowIfCancellationRequested();
          LoadFeaturesFrom(file, context);
        }

        context.Complete();
      }, tok);
    }

    public OSMFileFeatureProvider(DirectoryInfo osmTileDir)
      : this(Settings.Default, osmTileDir)
    {
    }

    public OSMFileFeatureProvider(Settings settings, DirectoryInfo osmTileDir)
    {
      m_OSMTileFileDir = osmTileDir;
      m_LOD = settings.LOD;
    }

    private static Tuple<Stream, OsmStreamSource> OpenStreamSource(FileInfo source)
    {
      Stream stream;
      OsmStreamSource osmStream;

      switch (source.Extension.ToLowerInvariant())
      {
        case ".xml":
        case ".osm":
        {
          stream = new FileStream(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
          osmStream = new XmlOsmStreamSource(stream);
        }
          break;
        case ".pbf":
        {
          stream = new FileStream(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 16384);
          osmStream = new PBFOsmStreamSource(stream);
        }
          break;
        default:
          throw new ArgumentException("OSM Database must be .xml, .osm, or .pbf file", nameof(source));
      }

      return Tuple.Create(stream, osmStream);
    }

    private void LoadFeaturesFrom(FileInfo osmFile, OSMFeatureContext context)
    {
      try
      {
        if (osmFile.Exists)
        {
          var tuple = OpenStreamSource(osmFile);
          using (var stream = tuple.Item1)
          using (var osmStreamSource = tuple.Item2)
          {
            context.Add(osmStreamSource);
          }
        }
      }
      catch (Exception)
      {
        throw;
      }
    }

    private FileInfo FileForTile(TerrainTileIndex index)
    {
      return new FileInfo(Path.Combine(
        m_OSMTileFileDir.FullName,
        "tile[" + index.Row + "][" + index.Column + "].pbf"));
    }

    private readonly DirectoryInfo m_OSMTileFileDir;
    private readonly int m_LOD;
  }
}



//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using sbio.owsdk.Geodetic;
using sbio.owsdk.Services;
using System.Data.SQLite;

namespace sbio.owsdk.Providers.SQL
{
  public sealed class SQLiteLocationProvider : ILocationProvider
    , IDisposable
  {
    public void Dispose()
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      m_Disposed = true;

      if (m_Connection != null)
      {
        m_Connection.Dispose();
      }

#if DEBUG
      GC.SuppressFinalize(this);
#endif
    }

    public Task QueryLocationsAsyncInto(string locationName, List<LocationInfo> results, CancellationToken tok)
    {
      return Task.Run(() => QueryLocationsWorker(locationName, results, tok), tok);
    }

    public SQLiteLocationProvider(FileInfo databaseFile)
    {
      m_DatabaseFile = databaseFile;

      if (databaseFile.Exists)
      {
        m_Connection = new SQLiteConnection(new SQLiteConnectionStringBuilder() {DataSource = m_DatabaseFile.FullName}.ConnectionString);

        m_Connection.Open();
      }
      else
      {
        m_Connection = null;
      }
    }

#if DEBUG
    ~SQLiteLocationProvider()
    {
      if (!m_Disposed)
      {
        Trace.TraceError(string.Format("'{0}' was not disposed", this));
      }
    }
#endif

    private void QueryLocationsWorker(string locationName, List<LocationInfo> results, CancellationToken tok)
    {
      results.Clear();

      if (m_Connection == null)
      {
        return;
      }

      using (var cmd = m_Connection.CreateCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = "SELECT `display_name`, `lat`, `lon` FROM `locations` JOIN `locations_fts` ON `locations`.`rowid` = `locations_fts`.`rowid` WHERE `locations_fts` MATCH @fts_query ORDER BY `importance` desc LIMIT 20";
        var queryParam = cmd.Parameters.Add("fts_query", DbType.String);

        queryParam.Value = locationName;
        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            var dbDisplayName = reader.GetString(0);
            var dbLat = reader.GetDouble(1);
            var dbLon = reader.GetDouble(2);

            results.Add(new LocationInfo(dbDisplayName, Geodetic2d.FromDegrees(dbLat, dbLon)));
          }
        }
      }
    }

    private readonly FileInfo m_DatabaseFile;
    private readonly SQLiteConnection m_Connection;
    private bool m_Disposed;
  }
}



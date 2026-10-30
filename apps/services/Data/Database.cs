using System.Data.Common;
using Microsoft.Data.Sqlite;
using Data.Abstractions;

namespace Data
{
    public sealed class Database : IDatabase
    {
        private readonly string _cs;

        public Database(string dbPath)
        {
            _cs = $"Data Source={dbPath}";

            // WAL lets readers run alongside a writer (e.g. a cache write while an endpoint
            // reads). The mode is stored in the file, so setting it once per start is enough.
            NonQuery("PRAGMA journal_mode=WAL");
        }

        private SqliteConnection Open()
        {
            var conn = new SqliteConnection(_cs);
            conn.Open();

            return conn;
        }

        public void NonQuery(string sql, Action<DbCommand>? bind = null)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            bind?.Invoke(cmd);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<T> Query<T>(string sql, Func<DbDataReader, T> map, Action<DbCommand>? bind = null)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            bind?.Invoke(cmd);

            using var r = cmd.ExecuteReader();

            var results = new List<T>();
            while (r.Read()) results.Add(map(r));

            return results;
        }

        public T? QueryOne<T>(string sql, Func<DbDataReader, T> map, Action<DbCommand>? bind = null)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            bind?.Invoke(cmd);

            using var r = cmd.ExecuteReader();

            return r.Read() ? map(r) : default;
        }
    }
}

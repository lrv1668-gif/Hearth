using System.Data.Common;
using Microsoft.Data.Sqlite;
using Data.Abstractions;

namespace Data
{
    public sealed class Database(string dbPath) : IDatabase
    {
        private readonly string _cs = $"Data Source={dbPath}";

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

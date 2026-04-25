using Microsoft.Data.Sqlite;

namespace Data
{
    public sealed class Database(string dbPath)
    {
        private readonly string _cs = $"Data Source={dbPath}";

        public SqliteConnection Open()
        {
            var conn = new SqliteConnection(_cs);
            conn.Open();
            return conn;
        }

        public void NonQuery(string sql, Action<SqliteCommand>? bind = null)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            bind?.Invoke(cmd);
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<T> Query<T>(string sql, Func<SqliteDataReader, T> map, Action<SqliteCommand>? bind = null)
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

        public T? QueryOne<T>(string sql, Func<SqliteDataReader, T> map, Action<SqliteCommand>? bind = null)
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

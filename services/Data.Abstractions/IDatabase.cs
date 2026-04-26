using System.Data.Common;

namespace Data.Abstractions
{
    public interface IDatabase
    {
        void NonQuery(string sql, Action<DbCommand>? bind = null);
        IEnumerable<T> Query<T>(string sql, Func<DbDataReader, T> map, Action<DbCommand>? bind = null);
        T? QueryOne<T>(string sql, Func<DbDataReader, T> map, Action<DbCommand>? bind = null);
    }
}

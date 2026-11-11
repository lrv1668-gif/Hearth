using System.Data.Common;

namespace Data.Abstractions
{
    public interface IDatabase
    {
        void NonQuery(string sql, Action<DbCommand>? bind = null);

        /// <summary>
        /// Runs <paramref name="sql"/> once per entry of <paramref name="binds"/> in one
        /// transaction: all of them land or none do, and they share a single commit.
        /// </summary>
        void NonQueryBatch(string sql, IEnumerable<Action<DbCommand>> binds);
        IEnumerable<T> Query<T>(string sql, Func<DbDataReader, T> map, Action<DbCommand>? bind = null);
        T? QueryOne<T>(string sql, Func<DbDataReader, T> map, Action<DbCommand>? bind = null);
    }
}

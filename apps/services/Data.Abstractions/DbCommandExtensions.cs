using System.Data.Common;

namespace Data.Abstractions
{
    public static class DbCommandExtensions
    {
        public static void AddParam(this DbCommand cmd, string name, object? value = null)
        {
            var p = cmd.CreateParameter();

            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;

            cmd.Parameters.Add(p);
        }
    }
}
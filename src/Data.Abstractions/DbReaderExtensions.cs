using System.Data.Common;

namespace Data.Abstractions;

public static class DbReaderExtensions
{
    public static T? Field<T>(this DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal))
            return default;
        return reader.GetFieldValue<T>(ordinal);
    }
}

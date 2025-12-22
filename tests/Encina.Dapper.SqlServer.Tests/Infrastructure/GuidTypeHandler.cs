using System.Data;
using Dapper;

namespace Encina.Dapper.SqlServer.Tests.Infrastructure;

/// <summary>
/// Dapper TypeHandler to convert SQLite TEXT columns to System.Guid.
/// SQLite stores GUIDs as TEXT, so this handler performs the conversion.
/// </summary>
public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value)
    {
        return value switch
        {
            string stringValue => Guid.Parse(stringValue),
            Guid guidValue => guidValue,
            _ => throw new InvalidCastException($"Cannot convert {value?.GetType().Name ?? "null"} to Guid")
        };
    }

    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value.ToString();
        parameter.DbType = DbType.String;
    }
}

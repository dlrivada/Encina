using System.Runtime.CompilerServices;
using Dapper;

namespace Encina.Dapper.SqlServer.Tests.Infrastructure;

/// <summary>
/// Initializes Dapper configuration for test assembly.
/// This runs once before any tests execute.
/// </summary>
public static class DapperTestsInitializer
{
    private static bool _initialized;
    private static readonly object _lock = new();

    [ModuleInitializer]
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized)
                return;

            // Register GUID handler for SQLite compatibility
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            // Register nullable GUID handler
            SqlMapper.AddTypeHandler(new NullableGuidTypeHandler());

            _initialized = true;
        }
    }
}

/// <summary>
/// TypeHandler for nullable Guids in SQLite.
/// </summary>
public class NullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
{
    public override Guid? Parse(object value)
    {
        if (value is null or DBNull)
            return null;

        return value switch
        {
            string stringValue when string.IsNullOrWhiteSpace(stringValue) => null,
            string stringValue => Guid.Parse(stringValue),
            Guid guidValue => guidValue,
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Guid?")
        };
    }

    public override void SetValue(System.Data.IDbDataParameter parameter, Guid? value)
    {
        if (value.HasValue)
        {
            parameter.Value = value.Value.ToString();
            parameter.DbType = System.Data.DbType.String;
        }
        else
        {
            parameter.Value = DBNull.Value;
            parameter.DbType = System.Data.DbType.String;
        }
    }
}

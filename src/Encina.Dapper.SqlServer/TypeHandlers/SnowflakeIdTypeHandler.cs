using System.Data;
using System.Globalization;
using Dapper;
using Encina.IdGeneration;

namespace Encina.Dapper.SqlServer.TypeHandlers;

/// <summary>
/// Dapper type handler for <see cref="SnowflakeId"/> values in SQL Server.
/// SQL Server stores Snowflake IDs as BIGINT, so this handler converts between <see cref="SnowflakeId"/> and <see cref="long"/>.
/// </summary>
public sealed class SnowflakeIdTypeHandler : SqlMapper.TypeHandler<SnowflakeId>
{
    /// <summary>
    /// Gets a singleton instance of the handler.
    /// </summary>
    public static readonly SnowflakeIdTypeHandler Instance = new();

    private static bool _isRegistered;
    private static readonly object _lock = new();

    /// <summary>
    /// Ensures the handler is registered with Dapper.
    /// This method is thread-safe and idempotent.
    /// </summary>
    public static void EnsureRegistered()
    {
        if (_isRegistered) return;

        lock (_lock)
        {
            if (_isRegistered) return;
            SqlMapper.AddTypeHandler(Instance);
            _isRegistered = true;
        }
    }

    /// <inheritdoc/>
    public override SnowflakeId Parse(object value)
    {
        return value switch
        {
            long longValue => new SnowflakeId(longValue),
            string stringValue => SnowflakeId.Parse(stringValue),
            SnowflakeId snowflakeId => snowflakeId,
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to SnowflakeId")
        };
    }

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, SnowflakeId value)
    {
        parameter.Value = value.Value;
        parameter.DbType = DbType.Int64;
    }
}

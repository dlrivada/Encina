using System.Data;
using Dapper;
using Encina.IdGeneration;

namespace Encina.Dapper.MySQL.TypeHandlers;

/// <summary>
/// Dapper type handler for <see cref="ShardPrefixedId"/> values in MySQL.
/// MySQL stores shard-prefixed IDs as VARCHAR, so this handler converts between <see cref="ShardPrefixedId"/> and <see cref="string"/>.
/// </summary>
public sealed class ShardPrefixedIdTypeHandler : SqlMapper.TypeHandler<ShardPrefixedId>
{
    /// <summary>
    /// Gets a singleton instance of the handler.
    /// </summary>
    public static readonly ShardPrefixedIdTypeHandler Instance = new();

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
    public override ShardPrefixedId Parse(object value)
    {
        return value switch
        {
            string stringValue => ShardPrefixedId.Parse(stringValue),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to ShardPrefixedId")
        };
    }

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, ShardPrefixedId value)
    {
        parameter.Value = value.ToString();
        parameter.DbType = DbType.String;
    }
}

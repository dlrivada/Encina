using System.Data;
using Dapper;
using Encina.IdGeneration;

namespace Encina.Dapper.Sqlite.TypeHandlers;

/// <summary>
/// Dapper type handler for <see cref="UuidV7Id"/> values in SQLite.
/// SQLite stores UUIDv7 values as TEXT, so this handler converts between <see cref="UuidV7Id"/> and <see cref="string"/>.
/// </summary>
public sealed class UuidV7IdTypeHandler : SqlMapper.TypeHandler<UuidV7Id>
{
    /// <summary>
    /// Gets a singleton instance of the handler.
    /// </summary>
    public static readonly UuidV7IdTypeHandler Instance = new();

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
    public override UuidV7Id Parse(object value)
    {
        return value switch
        {
            string stringValue => UuidV7Id.Parse(stringValue),
            Guid guid => new UuidV7Id(guid),
            byte[] bytes => new UuidV7Id(new Guid(bytes)),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to UuidV7Id")
        };
    }

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, UuidV7Id value)
    {
        parameter.Value = value.Value.ToString();
        parameter.DbType = DbType.String;
    }
}

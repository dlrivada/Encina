using System.Data;
using Dapper;

namespace Encina.Dapper.MySQL.TypeHandlers;

/// <summary>
/// Dapper type handler for Guid values in MySQL.
/// MySQL stores GUIDs as CHAR(36), so this handler converts between Guid and string.
/// </summary>
public sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    /// <summary>
    /// Gets a singleton instance of the handler.
    /// </summary>
    public static readonly GuidTypeHandler Instance = new();

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
    public override Guid Parse(object value)
    {
        return value switch
        {
            string stringValue => Guid.Parse(stringValue),
            byte[] bytes => new Guid(bytes),
            Guid guid => guid,
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Guid")
        };
    }

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value.ToString();
        parameter.DbType = DbType.String;
    }
}

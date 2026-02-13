using System.Data;
using Dapper;
using Encina.IdGeneration;

namespace Encina.Dapper.SqlServer.TypeHandlers;

/// <summary>
/// Dapper type handler for <see cref="UlidId"/> values in SQL Server.
/// SQL Server stores ULIDs as UNIQUEIDENTIFIER (Guid), so this handler converts between <see cref="UlidId"/> and <see cref="Guid"/>.
/// </summary>
public sealed class UlidIdTypeHandler : SqlMapper.TypeHandler<UlidId>
{
    /// <summary>
    /// Gets a singleton instance of the handler.
    /// </summary>
    public static readonly UlidIdTypeHandler Instance = new();

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
    public override UlidId Parse(object value)
    {
        return value switch
        {
            Guid guid => new UlidId(guid),
            byte[] bytes => new UlidId(bytes),
            string stringValue => UlidId.Parse(stringValue),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to UlidId")
        };
    }

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, UlidId value)
    {
        parameter.Value = value.ToGuid();
        parameter.DbType = DbType.Guid;
    }
}

using System.Data;
using Dapper;

namespace Encina.Dapper.Benchmarks.Infrastructure;

/// <summary>
/// Registers Dapper type handlers for all supported providers.
/// </summary>
public static class ProviderTypeHandlers
{
    private static bool _isRegistered;
    private static readonly object _lock = new();

    /// <summary>
    /// Ensures all required type handlers are registered for the specified provider.
    /// This method is thread-safe and idempotent.
    /// </summary>
    /// <param name="provider">The database provider.</param>
    public static void EnsureRegistered(DatabaseProvider provider)
    {
        if (_isRegistered)
        {
            return;
        }

        lock (_lock)
        {
            if (_isRegistered)
            {
                return;
            }

            // Register Guid handler for SQLite (stores as TEXT)
            if (provider == DatabaseProvider.Sqlite)
            {
                SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
            }

            // Register DateTime handler for SQLite (stores as TEXT in ISO 8601)
            if (provider == DatabaseProvider.Sqlite)
            {
                SqlMapper.AddTypeHandler(new SqliteDateTimeTypeHandler());
            }

            _isRegistered = true;
        }
    }

    /// <summary>
    /// Resets the registration state. Used for testing.
    /// </summary>
    internal static void ResetRegistration()
    {
        lock (_lock)
        {
            _isRegistered = false;
            SqlMapper.ResetTypeHandlers();
        }
    }
}

/// <summary>
/// Dapper type handler for Guid values in SQLite.
/// SQLite stores GUIDs as TEXT, so this handler converts between Guid and string.
/// </summary>
internal sealed class SqliteGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
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

/// <summary>
/// Dapper type handler for DateTime values in SQLite.
/// SQLite stores DateTime as TEXT in ISO 8601 format.
/// </summary>
internal sealed class SqliteDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
{
    /// <inheritdoc/>
    public override DateTime Parse(object value)
    {
        return value switch
        {
            string stringValue => DateTime.Parse(stringValue, null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTime dateTime => dateTime,
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to DateTime")
        };
    }

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value.ToString("O"); // ISO 8601 format
        parameter.DbType = DbType.String;
    }
}

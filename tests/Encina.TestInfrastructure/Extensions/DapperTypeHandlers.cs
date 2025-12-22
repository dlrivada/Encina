using System.Data;
using Dapper;

namespace Encina.TestInfrastructure.Extensions;

/// <summary>
/// Dapper type handlers for SQLite compatibility.
/// Handles conversion between .NET types and SQLite storage.
/// </summary>
public static class DapperTypeHandlers
{
    private static bool s_registered;

    /// <summary>
    /// Registers all Dapper type handlers for SQLite.
    /// Safe to call multiple times (idempotent).
    /// </summary>
    public static void RegisterSqliteHandlers()
    {
        if (s_registered)
        {
            return;
        }

        SqlMapper.AddTypeHandler(new GuidTypeHandler());
        SqlMapper.AddTypeHandler(new NullableGuidTypeHandler());
        SqlMapper.AddTypeHandler(new DateTimeTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeTypeHandler());

        s_registered = true;
    }

    /// <summary>
    /// Type handler for Guid → TEXT conversion in SQLite.
    /// </summary>
    private sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value)
        {
            return value switch
            {
                string stringValue => Guid.Parse(stringValue),
                Guid guidValue => guidValue,
                byte[] byteArrayValue when byteArrayValue.Length == 16 => new Guid(byteArrayValue),
                _ => throw new InvalidCastException($"Cannot convert {value?.GetType().Name ?? "null"} to Guid")
            };
        }

        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString();
            parameter.DbType = DbType.String;
        }
    }

    /// <summary>
    /// Type handler for Guid? → TEXT conversion in SQLite.
    /// </summary>
    private sealed class NullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
    {
        public override Guid? Parse(object value)
        {
            if (value is null or DBNull)
            {
                return null;
            }

            return value switch
            {
                string stringValue => Guid.Parse(stringValue),
                Guid guidValue => guidValue,
                byte[] byteArrayValue when byteArrayValue.Length == 16 => new Guid(byteArrayValue),
                _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Guid?")
            };
        }

        public override void SetValue(IDbDataParameter parameter, Guid? value)
        {
            if (value.HasValue)
            {
                parameter.Value = value.Value.ToString();
                parameter.DbType = DbType.String;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
        }
    }

    /// <summary>
    /// Type handler for DateTime → TEXT (ISO8601) conversion in SQLite.
    /// </summary>
    private sealed class DateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override DateTime Parse(object value)
        {
            return value switch
            {
                string stringValue => DateTime.Parse(stringValue, null, System.Globalization.DateTimeStyles.RoundtripKind),
                DateTime dateTimeValue => dateTimeValue,
                _ => throw new InvalidCastException($"Cannot convert {value?.GetType().Name ?? "null"} to DateTime")
            };
        }

        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = value.ToString("O"); // ISO8601
            parameter.DbType = DbType.String;
        }
    }

    /// <summary>
    /// Type handler for DateTime? → TEXT (ISO8601) conversion in SQLite.
    /// </summary>
    private sealed class NullableDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime?>
    {
        public override DateTime? Parse(object value)
        {
            if (value is null or DBNull)
            {
                return null;
            }

            return value switch
            {
                string stringValue => DateTime.Parse(stringValue, null, System.Globalization.DateTimeStyles.RoundtripKind),
                DateTime dateTimeValue => dateTimeValue,
                _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to DateTime?")
            };
        }

        public override void SetValue(IDbDataParameter parameter, DateTime? value)
        {
            if (value.HasValue)
            {
                parameter.Value = value.Value.ToString("O"); // ISO8601
                parameter.DbType = DbType.String;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
        }
    }
}

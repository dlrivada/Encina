using System.Data;
using Dapper;

namespace Encina.TestInfrastructure.Extensions;

/// <summary>
/// Dapper type handlers for database compatibility.
/// Handles conversion between .NET types and database-specific storage.
/// </summary>
public static class DapperTypeHandlers
{
    private static bool s_sqliteRegistered;
    private static bool s_oracleRegistered;

    /// <summary>
    /// Registers all Dapper type handlers for SQLite.
    /// Safe to call multiple times (idempotent).
    /// </summary>
    public static void RegisterSqliteHandlers()
    {
        if (s_sqliteRegistered)
        {
            return;
        }

        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
        SqlMapper.AddTypeHandler(new SqliteNullableGuidTypeHandler());
        SqlMapper.AddTypeHandler(new DateTimeTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeTypeHandler());

        s_sqliteRegistered = true;
    }

    /// <summary>
    /// Registers all Dapper type handlers for Oracle.
    /// Oracle stores GUIDs as RAW(16) byte arrays.
    /// Safe to call multiple times (idempotent).
    /// </summary>
    public static void RegisterOracleHandlers()
    {
        if (s_oracleRegistered)
        {
            return;
        }

        SqlMapper.AddTypeHandler(new OracleGuidTypeHandler());
        SqlMapper.AddTypeHandler(new OracleNullableGuidTypeHandler());

        s_oracleRegistered = true;
    }

    /// <summary>
    /// Type handler for Guid → TEXT conversion in SQLite.
    /// </summary>
    private sealed class SqliteGuidTypeHandler : SqlMapper.TypeHandler<Guid>
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
    private sealed class SqliteNullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
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

    /// <summary>
    /// Type handler for Guid → RAW(16) conversion in Oracle.
    /// </summary>
    private sealed class OracleGuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value)
        {
            return value switch
            {
                byte[] bytes => new Guid(bytes),
                string stringValue => Guid.Parse(stringValue),
                Guid guidValue => guidValue,
                _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Guid")
            };
        }

        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToByteArray();
            parameter.DbType = DbType.Binary;
            parameter.Size = 16;
        }
    }

    /// <summary>
    /// Type handler for Guid? → RAW(16) conversion in Oracle.
    /// </summary>
    private sealed class OracleNullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
    {
        public override Guid? Parse(object value)
        {
            if (value is null or DBNull)
            {
                return null;
            }

            return value switch
            {
                byte[] bytes => new Guid(bytes),
                string stringValue => Guid.Parse(stringValue),
                Guid guidValue => guidValue,
                _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Guid?")
            };
        }

        public override void SetValue(IDbDataParameter parameter, Guid? value)
        {
            if (value.HasValue)
            {
                parameter.Value = value.Value.ToByteArray();
                parameter.DbType = DbType.Binary;
                parameter.Size = 16;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
        }
    }
}

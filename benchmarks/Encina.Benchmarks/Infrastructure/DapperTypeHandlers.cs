using System.Data;
using Dapper;

namespace Encina.Benchmarks.Infrastructure;

/// <summary>
/// Shared Dapper type handlers for SQLite benchmarks.
/// Handles Guid and DateTime conversions between .NET and SQLite TEXT storage.
/// </summary>
public static class DapperTypeHandlers
{
    private static bool s_registered;

    /// <summary>
    /// Registers all type handlers. Safe to call multiple times (idempotent).
    /// </summary>
    public static void Register()
    {
        if (s_registered) return;

        SqlMapper.AddTypeHandler(new GuidTypeHandler());
        SqlMapper.AddTypeHandler(new NullableGuidTypeHandler());
        SqlMapper.AddTypeHandler(new DateTimeTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeTypeHandler());

        s_registered = true;
    }

    private sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value) => Guid.Parse((string)value);

        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString();
            parameter.DbType = DbType.String;
        }
    }

    private sealed class NullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
    {
        public override Guid? Parse(object value)
        {
            if (value == null || value is DBNull) return null;
            return Guid.Parse((string)value);
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

    private sealed class DateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override DateTime Parse(object value) =>
            DateTime.Parse((string)value, System.Globalization.CultureInfo.InvariantCulture);

        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = value.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            parameter.DbType = DbType.String;
        }
    }

    private sealed class NullableDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime?>
    {
        public override DateTime? Parse(object value)
        {
            if (value == null || value is DBNull) return null;
            return DateTime.Parse((string)value, System.Globalization.CultureInfo.InvariantCulture);
        }

        public override void SetValue(IDbDataParameter parameter, DateTime? value)
        {
            if (value.HasValue)
            {
                parameter.Value = value.Value.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                parameter.DbType = DbType.String;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
        }
    }
}

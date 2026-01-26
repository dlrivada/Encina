using System.Data;
using Dapper;

namespace Encina.Dapper.Oracle;

/// <summary>
/// Dapper type handler for Oracle GUID storage using RAW(16).
/// Converts GUIDs to byte arrays for writing and byte arrays to GUIDs for reading.
/// </summary>
/// <remarks>
/// Oracle does not have a native GUID type. This handler stores GUIDs as RAW(16) which is
/// more efficient than VARCHAR2(36) and provides proper byte-level storage.
/// </remarks>
public sealed class OracleGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    /// <summary>
    /// Parses a GUID value from the database RAW(16) format.
    /// </summary>
    /// <param name="value">The database value (byte array).</param>
    /// <returns>The parsed GUID.</returns>
    public override Guid Parse(object value)
    {
        return value switch
        {
            byte[] bytes => new Guid(bytes),
            string str => Guid.Parse(str),
            Guid guid => guid,
            _ => throw new InvalidOperationException($"Cannot convert {value.GetType().Name} to Guid")
        };
    }

    /// <summary>
    /// Sets the GUID parameter value as RAW(16) byte array.
    /// </summary>
    /// <param name="parameter">The database parameter.</param>
    /// <param name="value">The GUID value.</param>
    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value.ToByteArray();
        parameter.DbType = DbType.Binary;
        parameter.Size = 16;
    }
}

/// <summary>
/// Dapper type handler for nullable Oracle GUID storage using RAW(16).
/// </summary>
public sealed class OracleNullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
{
    /// <summary>
    /// Parses a nullable GUID value from the database RAW(16) format.
    /// </summary>
    /// <param name="value">The database value (byte array or DBNull).</param>
    /// <returns>The parsed GUID or null.</returns>
    public override Guid? Parse(object value)
    {
        if (value is null || value is DBNull)
            return null;

        return value switch
        {
            byte[] bytes => new Guid(bytes),
            string str => Guid.Parse(str),
            Guid guid => guid,
            _ => throw new InvalidOperationException($"Cannot convert {value.GetType().Name} to Guid?")
        };
    }

    /// <summary>
    /// Sets the nullable GUID parameter value as RAW(16) byte array.
    /// </summary>
    /// <param name="parameter">The database parameter.</param>
    /// <param name="value">The nullable GUID value.</param>
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

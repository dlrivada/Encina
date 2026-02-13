using System.Data;
using Encina.IdGeneration;

namespace Encina.ADO.Sqlite;

/// <summary>
/// Extension methods for adding Encina ID generation type parameters to ADO.NET commands
/// with correct <see cref="DbType"/> mappings for SQLite.
/// </summary>
/// <remarks>
/// <para>
/// SQLite stores all values as one of TEXT, INTEGER, REAL, or BLOB. These extension methods
/// map each Encina ID type to the most appropriate SQLite storage class:
/// <list type="bullet">
///   <item><see cref="SnowflakeId"/> maps to <see cref="DbType.Int64"/> (INTEGER).</item>
///   <item><see cref="UlidId"/> maps to <see cref="DbType.String"/> (TEXT, Crockford Base32).</item>
///   <item><see cref="UuidV7Id"/> maps to <see cref="DbType.String"/> (TEXT, standard GUID format).</item>
///   <item><see cref="ShardPrefixedId"/> maps to <see cref="DbType.String"/> (TEXT).</item>
/// </list>
/// </para>
/// </remarks>
public static class IdParameterExtensions
{
    /// <summary>
    /// Adds a <see cref="SnowflakeId"/> parameter to the command.
    /// </summary>
    /// <param name="command">The ADO.NET command to add the parameter to.</param>
    /// <param name="name">The parameter name (e.g., <c>@Id</c>).</param>
    /// <param name="value">The <see cref="SnowflakeId"/> value.</param>
    /// <remarks>
    /// Maps to SQLite column type <c>INTEGER</c>. The raw 64-bit value is stored directly.
    /// </remarks>
    public static void AddSnowflakeIdParameter(this IDbCommand command, string name, SnowflakeId value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value.Value;
        parameter.DbType = DbType.Int64;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Adds a <see cref="UlidId"/> parameter to the command.
    /// </summary>
    /// <param name="command">The ADO.NET command to add the parameter to.</param>
    /// <param name="name">The parameter name (e.g., <c>@Id</c>).</param>
    /// <param name="value">The <see cref="UlidId"/> value.</param>
    /// <remarks>
    /// Maps to SQLite column type <c>TEXT</c>. The ULID is stored as a 26-character
    /// Crockford Base32 encoded string, preserving lexicographic sort order.
    /// </remarks>
    public static void AddUlidIdParameter(this IDbCommand command, string name, UlidId value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value.ToString();
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Adds a <see cref="UuidV7Id"/> parameter to the command.
    /// </summary>
    /// <param name="command">The ADO.NET command to add the parameter to.</param>
    /// <param name="name">The parameter name (e.g., <c>@Id</c>).</param>
    /// <param name="value">The <see cref="UuidV7Id"/> value.</param>
    /// <remarks>
    /// Maps to SQLite column type <c>TEXT</c>. The UUID is stored as a standard
    /// GUID string (e.g., <c>019374c8-7b00-7000-8000-000000000001</c>).
    /// </remarks>
    public static void AddUuidV7IdParameter(this IDbCommand command, string name, UuidV7Id value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value.Value.ToString();
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Adds a <see cref="ShardPrefixedId"/> parameter to the command.
    /// </summary>
    /// <param name="command">The ADO.NET command to add the parameter to.</param>
    /// <param name="name">The parameter name (e.g., <c>@Id</c>).</param>
    /// <param name="value">The <see cref="ShardPrefixedId"/> value.</param>
    /// <remarks>
    /// Maps to SQLite column type <c>TEXT</c>. The full shard-prefixed string
    /// (e.g., <c>shard-01:01ARZ3NDEKTSV4RRFFQ69G5FAV</c>) is stored as-is.
    /// </remarks>
    public static void AddShardPrefixedIdParameter(this IDbCommand command, string name, ShardPrefixedId value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value.ToString();
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);
    }
}

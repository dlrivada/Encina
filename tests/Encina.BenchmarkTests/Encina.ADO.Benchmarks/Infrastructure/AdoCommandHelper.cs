using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;

namespace Encina.ADO.Benchmarks.Infrastructure;

/// <summary>
/// Helper methods for working with ADO.NET commands across different providers.
/// Provides async wrappers and parameter handling.
/// </summary>
public static class AdoCommandHelper
{
    /// <summary>
    /// Adds a parameter to a command with the specified name and value.
    /// </summary>
    /// <param name="command">The database command.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    public static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Adds a parameter with explicit type to a command.
    /// </summary>
    /// <param name="command">The database command.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="dbType">The database type.</param>
    public static void AddParameter(IDbCommand command, string name, object? value, DbType dbType)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        parameter.DbType = dbType;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Adds a GUID parameter, handling provider-specific formatting.
    /// </summary>
    /// <param name="command">The database command.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The GUID value.</param>
    /// <param name="provider">The database provider.</param>
    public static void AddGuidParameter(IDbCommand command, string name, Guid value, DatabaseProvider provider)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;

        // SQLite and MySQL store GUIDs as strings
        if (provider is DatabaseProvider.Sqlite or DatabaseProvider.MySql)
        {
            parameter.Value = value.ToString();
            parameter.DbType = DbType.String;
        }
        else
        {
            parameter.Value = value;
            parameter.DbType = DbType.Guid;
        }

        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Adds a DateTime parameter, handling provider-specific formatting.
    /// </summary>
    /// <param name="command">The database command.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The DateTime value.</param>
    /// <param name="provider">The database provider.</param>
    public static void AddDateTimeParameter(IDbCommand command, string name, DateTime? value, DatabaseProvider provider)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;

        if (value is null)
        {
            parameter.Value = DBNull.Value;
        }
        else if (provider == DatabaseProvider.Sqlite)
        {
            // SQLite stores DateTime as ISO 8601 text
            parameter.Value = value.Value.ToString("O");
            parameter.DbType = DbType.String;
        }
        else
        {
            parameter.Value = value.Value;
            parameter.DbType = DbType.DateTime2;
        }

        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Executes a non-query command asynchronously.
    /// </summary>
    /// <param name="command">The database command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken = default)
    {
        return command switch
        {
            SqliteCommand sqliteCmd => await sqliteCmd.ExecuteNonQueryAsync(cancellationToken),
            SqlCommand sqlCmd => await sqlCmd.ExecuteNonQueryAsync(cancellationToken),
            NpgsqlCommand npgsqlCmd => await npgsqlCmd.ExecuteNonQueryAsync(cancellationToken),
            MySqlCommand mysqlCmd => await mysqlCmd.ExecuteNonQueryAsync(cancellationToken),
            _ => await Task.Run(command.ExecuteNonQuery, cancellationToken)
        };
    }

    /// <summary>
    /// Executes a reader command asynchronously.
    /// </summary>
    /// <param name="command">The database command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A data reader.</returns>
    public static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken = default)
    {
        return command switch
        {
            SqliteCommand sqliteCmd => await sqliteCmd.ExecuteReaderAsync(cancellationToken),
            SqlCommand sqlCmd => await sqlCmd.ExecuteReaderAsync(cancellationToken),
            NpgsqlCommand npgsqlCmd => await npgsqlCmd.ExecuteReaderAsync(cancellationToken),
            MySqlCommand mysqlCmd => await mysqlCmd.ExecuteReaderAsync(cancellationToken),
            _ => await Task.Run(command.ExecuteReader, cancellationToken)
        };
    }

    /// <summary>
    /// Executes a scalar command asynchronously.
    /// </summary>
    /// <param name="command">The database command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The scalar result.</returns>
    public static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken = default)
    {
        return command switch
        {
            SqliteCommand sqliteCmd => await sqliteCmd.ExecuteScalarAsync(cancellationToken),
            SqlCommand sqlCmd => await sqlCmd.ExecuteScalarAsync(cancellationToken),
            NpgsqlCommand npgsqlCmd => await npgsqlCmd.ExecuteScalarAsync(cancellationToken),
            MySqlCommand mysqlCmd => await mysqlCmd.ExecuteScalarAsync(cancellationToken),
            _ => await Task.Run(command.ExecuteScalar, cancellationToken)
        };
    }

    /// <summary>
    /// Reads the next row asynchronously.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if a row was read, false otherwise.</returns>
    public static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken = default)
    {
        return reader switch
        {
            SqliteDataReader sqliteReader => await sqliteReader.ReadAsync(cancellationToken),
            SqlDataReader sqlReader => await sqlReader.ReadAsync(cancellationToken),
            NpgsqlDataReader npgsqlReader => await npgsqlReader.ReadAsync(cancellationToken),
            MySqlDataReader mysqlReader => await mysqlReader.ReadAsync(cancellationToken),
            _ => await Task.Run(reader.Read, cancellationToken)
        };
    }

    /// <summary>
    /// Opens a connection asynchronously.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task OpenAsync(IDbConnection connection, CancellationToken cancellationToken = default)
    {
        if (connection.State == ConnectionState.Open)
        {
            return;
        }

        switch (connection)
        {
            case SqliteConnection sqliteConn:
                await sqliteConn.OpenAsync(cancellationToken);
                break;
            case SqlConnection sqlConn:
                await sqlConn.OpenAsync(cancellationToken);
                break;
            case NpgsqlConnection npgsqlConn:
                await npgsqlConn.OpenAsync(cancellationToken);
                break;
            case MySqlConnection mysqlConn:
                await mysqlConn.OpenAsync(cancellationToken);
                break;
            default:
                await Task.Run(connection.Open, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Gets a GUID value from a data reader, handling provider-specific formats.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="ordinal">The column ordinal.</param>
    /// <param name="provider">The database provider.</param>
    /// <returns>The GUID value.</returns>
    public static Guid GetGuid(IDataReader reader, int ordinal, DatabaseProvider provider)
    {
        if (provider is DatabaseProvider.Sqlite or DatabaseProvider.MySql)
        {
            return Guid.Parse(reader.GetString(ordinal));
        }

        return reader.GetGuid(ordinal);
    }

    /// <summary>
    /// Gets a nullable GUID value from a data reader.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="ordinal">The column ordinal.</param>
    /// <param name="provider">The database provider.</param>
    /// <returns>The GUID value or null.</returns>
    public static Guid? GetNullableGuid(IDataReader reader, int ordinal, DatabaseProvider provider)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return GetGuid(reader, ordinal, provider);
    }

    /// <summary>
    /// Gets a DateTime value from a data reader, handling provider-specific formats.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="ordinal">The column ordinal.</param>
    /// <param name="provider">The database provider.</param>
    /// <returns>The DateTime value.</returns>
    public static DateTime GetDateTime(IDataReader reader, int ordinal, DatabaseProvider provider)
    {
        if (provider == DatabaseProvider.Sqlite)
        {
            return DateTime.Parse(reader.GetString(ordinal), null, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        return reader.GetDateTime(ordinal);
    }

    /// <summary>
    /// Gets a nullable DateTime value from a data reader.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="ordinal">The column ordinal.</param>
    /// <param name="provider">The database provider.</param>
    /// <returns>The DateTime value or null.</returns>
    public static DateTime? GetNullableDateTime(IDataReader reader, int ordinal, DatabaseProvider provider)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return GetDateTime(reader, ordinal, provider);
    }

    /// <summary>
    /// Gets a nullable string value from a data reader.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="ordinal">The column ordinal.</param>
    /// <returns>The string value or null.</returns>
    public static string? GetNullableString(IDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}

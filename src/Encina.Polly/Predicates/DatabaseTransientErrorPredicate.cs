using Encina.Database;

namespace Encina.Polly.Predicates;

/// <summary>
/// Identifies transient database errors that should trigger the circuit breaker.
/// </summary>
/// <remarks>
/// <para>
/// This predicate detects provider-specific transient exceptions without taking a hard
/// dependency on provider assemblies. It uses type name matching to identify exceptions
/// from SQL Server (<c>SqlException</c>), PostgreSQL (<c>NpgsqlException</c>),
/// MySQL (<c>MySqlException</c>), and MongoDB (<c>MongoException</c>).
/// </para>
/// <para>
/// The predicate is configurable via <see cref="DatabaseCircuitBreakerOptions"/> to include
/// or exclude timeouts and connection failures from circuit breaker evaluation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new DatabaseCircuitBreakerOptions
/// {
///     IncludeTimeouts = true,
///     IncludeConnectionFailures = true
/// };
///
/// var predicate = new DatabaseTransientErrorPredicate(options);
/// bool isTransient = predicate.IsTransient(exception);
/// </code>
/// </example>
public sealed class DatabaseTransientErrorPredicate
{
    private readonly DatabaseCircuitBreakerOptions _options;

    /// <summary>
    /// Known transient database exception type names from provider-specific assemblies.
    /// </summary>
    /// <remarks>
    /// These names are matched against the exception type hierarchy to avoid
    /// direct assembly references to provider-specific packages.
    /// </remarks>
    private static readonly HashSet<string> TransientDatabaseExceptionTypeNames = new(StringComparer.Ordinal)
    {
        // SQL Server (Microsoft.Data.SqlClient / System.Data.SqlClient)
        "SqlException",

        // PostgreSQL (Npgsql)
        "NpgsqlException",
        "PostgresException",

        // MySQL (MySqlConnector / MySql.Data)
        "MySqlException",

        // MongoDB (MongoDB.Driver)
        "MongoException",
        "MongoConnectionException",
        "MongoCommandException",

        // SQLite (Microsoft.Data.Sqlite)
        "SqliteException",

        // Generic ADO.NET
        "DbException"
    };

    /// <summary>
    /// Known timeout-related exception type names.
    /// </summary>
    private static readonly HashSet<string> TimeoutExceptionTypeNames = new(StringComparer.Ordinal)
    {
        "TimeoutException",
        "TaskCanceledException",
        "OperationCanceledException"
    };

    /// <summary>
    /// Known connection failure exception type names.
    /// </summary>
    private static readonly HashSet<string> ConnectionFailureExceptionTypeNames = new(StringComparer.Ordinal)
    {
        "SocketException",
        "IOException",
        "HttpRequestException"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseTransientErrorPredicate"/> class.
    /// </summary>
    /// <param name="options">Circuit breaker options controlling which exception types are considered transient.</param>
    public DatabaseTransientErrorPredicate(DatabaseCircuitBreakerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Determines whether the specified exception represents a transient database error.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method walks the exception type hierarchy to match against known transient
    /// database exception types. It also checks inner exceptions for cases where
    /// provider exceptions are wrapped.
    /// </para>
    /// <para>
    /// Timeout and connection failure exceptions are included or excluded based on
    /// the <see cref="DatabaseCircuitBreakerOptions.IncludeTimeouts"/> and
    /// <see cref="DatabaseCircuitBreakerOptions.IncludeConnectionFailures"/> settings.
    /// </para>
    /// </remarks>
    /// <param name="exception">The exception to evaluate.</param>
    /// <returns><c>true</c> if the exception is a transient database error; otherwise, <c>false</c>.</returns>
    public bool IsTransient(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return IsTransientCore(exception) || HasTransientInnerException(exception);
    }

    private bool IsTransientCore(Exception exception)
    {
        var typeName = exception.GetType().Name;

        // Check database-specific transient exceptions
        if (TransientDatabaseExceptionTypeNames.Contains(typeName))
        {
            return true;
        }

        // Check timeout exceptions (configurable)
        if (_options.IncludeTimeouts && TimeoutExceptionTypeNames.Contains(typeName))
        {
            return true;
        }

        // Check connection failure exceptions (configurable)
        if (_options.IncludeConnectionFailures && ConnectionFailureExceptionTypeNames.Contains(typeName))
        {
            return true;
        }

        // Walk the type hierarchy for base class matches
        var baseType = exception.GetType().BaseType;
        while (baseType is not null && baseType != typeof(Exception))
        {
            if (TransientDatabaseExceptionTypeNames.Contains(baseType.Name))
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private bool HasTransientInnerException(Exception exception)
    {
        var inner = exception.InnerException;
        while (inner is not null)
        {
            if (IsTransientCore(inner))
            {
                return true;
            }

            inner = inner.InnerException;
        }

        return false;
    }
}

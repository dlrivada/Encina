using Encina.DomainModeling;
using LanguageExt;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace Encina.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Entity Framework Core implementation of <see cref="IBulkOperations{TEntity}"/> that automatically
/// selects the appropriate provider-specific implementation based on the database connection type.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This implementation automatically detects the database provider from the DbContext connection
/// and delegates to the appropriate provider-specific implementation:
/// </para>
/// <list type="bullet">
/// <item><description>SQL Server: Uses <see cref="BulkOperationsEFSqlServer{TEntity}"/> with SqlBulkCopy and TVPs</description></item>
/// <item><description>PostgreSQL: Uses <see cref="BulkOperationsEFPostgreSql{TEntity}"/> with batched inserts and ON CONFLICT</description></item>
/// <item><description>MySQL: Uses <see cref="BulkOperationsEFMySql{TEntity}"/> with batched inserts and ON DUPLICATE KEY UPDATE</description></item>
/// <item><description>SQLite: Uses <see cref="BulkOperationsEFSqlite{TEntity}"/> with batched inserts and INSERT OR REPLACE</description></item>
/// <item><description>Oracle: Uses <see cref="BulkOperationsEFOracle{TEntity}"/> with INSERT ALL and MERGE</description></item>
/// </list>
/// <para>
/// Each provider implementation is optimized for its specific database engine, using native
/// bulk copy mechanisms where available (SQL Server) or batched operations for others.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via DI
/// services.AddEncinaBulkOperations&lt;Order&gt;();
///
/// // Use in service - provider is detected automatically
/// var bulkOps = serviceProvider.GetRequiredService&lt;IBulkOperations&lt;Order&gt;&gt;();
/// var result = await bulkOps.BulkInsertAsync(orders, BulkConfig.Default with { BatchSize = 5000 });
/// </code>
/// </example>
public sealed class BulkOperationsEF<TEntity> : IBulkOperations<TEntity>
    where TEntity : class, new()
{
    private readonly IBulkOperations<TEntity> _innerImplementation;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationsEF{TEntity}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when dbContext is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the database provider is not supported.</exception>
    public BulkOperationsEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _innerImplementation = CreateProviderImplementation(dbContext);
    }

    /// <inheritdoc/>
    public Task<Either<EncinaError, int>> BulkInsertAsync(
        IEnumerable<TEntity> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default)
        => _innerImplementation.BulkInsertAsync(entities, config, cancellationToken);

    /// <inheritdoc/>
    public Task<Either<EncinaError, int>> BulkUpdateAsync(
        IEnumerable<TEntity> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default)
        => _innerImplementation.BulkUpdateAsync(entities, config, cancellationToken);

    /// <inheritdoc/>
    public Task<Either<EncinaError, int>> BulkDeleteAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
        => _innerImplementation.BulkDeleteAsync(entities, cancellationToken);

    /// <inheritdoc/>
    public Task<Either<EncinaError, int>> BulkMergeAsync(
        IEnumerable<TEntity> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default)
        => _innerImplementation.BulkMergeAsync(entities, config, cancellationToken);

    /// <inheritdoc/>
    public Task<Either<EncinaError, IReadOnlyList<TEntity>>> BulkReadAsync(
        IEnumerable<object> ids,
        CancellationToken cancellationToken = default)
        => _innerImplementation.BulkReadAsync(ids, cancellationToken);

    private static IBulkOperations<TEntity> CreateProviderImplementation(DbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();

        return connection switch
        {
            SqlConnection => new BulkOperationsEFSqlServer<TEntity>(dbContext),
            NpgsqlConnection => new BulkOperationsEFPostgreSql<TEntity>(dbContext),
            MySqlConnection => new BulkOperationsEFMySql<TEntity>(dbContext),
            SqliteConnection => new BulkOperationsEFSqlite<TEntity>(dbContext),
            OracleConnection => new BulkOperationsEFOracle<TEntity>(dbContext),
            _ => throw new NotSupportedException(
                $"Database provider '{connection.GetType().Name}' is not supported for bulk operations. " +
                $"Supported providers: SQL Server, PostgreSQL, MySQL, SQLite, Oracle.")
        };
    }
}

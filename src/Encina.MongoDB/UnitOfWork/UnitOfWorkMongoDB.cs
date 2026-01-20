using System.Collections.Concurrent;
using Encina;
using Encina.DomainModeling;
using Encina.MongoDB.Repository;
using LanguageExt;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.UnitOfWork;

/// <summary>
/// MongoDB implementation of <see cref="IUnitOfWork"/> using client sessions.
/// </summary>
/// <remarks>
/// <para>
/// This implementation manages MongoDB transactions for coordinating changes across
/// multiple repositories using client sessions. Each repository operation executes
/// within the session context when a transaction is active.
/// </para>
/// <para>
/// <strong>Important:</strong> MongoDB transactions require a replica set deployment.
/// Standalone MongoDB servers do not support multi-document transactions.
/// For development, consider using a single-node replica set.
/// </para>
/// <para>
/// The implementation:
/// <list type="bullet">
/// <item><description>Creates client sessions for transaction management</description></item>
/// <item><description>Caches repository instances by entity type for reuse</description></item>
/// <item><description>Automatically aborts uncommitted transactions on dispose</description></item>
/// <item><description>Returns Railway Oriented Programming results for all operations</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class TransferFundsHandler(IUnitOfWork unitOfWork)
/// {
///     public async Task&lt;Either&lt;EncinaError, Unit&gt;&gt; HandleAsync(TransferCommand cmd, CancellationToken ct)
///     {
///         var beginResult = await unitOfWork.BeginTransactionAsync(ct);
///         if (beginResult.IsLeft) return beginResult;
///
///         var accounts = unitOfWork.Repository&lt;Account, AccountId&gt;();
///
///         // Operations execute within the session context
///         var sourceResult = await accounts.GetByIdAsync(cmd.SourceId, ct);
///         if (sourceResult.IsLeft)
///         {
///             await unitOfWork.RollbackAsync(ct);
///             return sourceResult.Map(_ =&gt; Unit.Default);
///         }
///
///         // ... perform transfer ...
///
///         return await unitOfWork.CommitAsync(ct);
///     }
/// }
/// </code>
/// </example>
public sealed class UnitOfWorkMongoDB : IUnitOfWork
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoDatabase _database;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private IClientSessionHandle? _session;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options containing database configuration.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="mongoClient"/>, <paramref name="options"/>,
    /// or <paramref name="serviceProvider"/> is null.
    /// </exception>
    public UnitOfWorkMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _mongoClient = mongoClient;
        _database = mongoClient.GetDatabase(options.Value.DatabaseName);
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public bool HasActiveTransaction => _session?.IsInTransaction == true;

    /// <summary>
    /// Gets the current client session, if one is active.
    /// </summary>
    /// <remarks>
    /// This property is used internally by repositories to participate in the transaction.
    /// Returns null when no session is active.
    /// </remarks>
    internal IClientSessionHandle? CurrentSession => _session;

    /// <summary>
    /// Gets the MongoDB database.
    /// </summary>
    /// <remarks>
    /// This property is used internally by repositories to access collections.
    /// </remarks>
    internal IMongoDatabase Database => _database;

    /// <inheritdoc/>
    public IFunctionalRepository<TEntity, TId> Repository<TEntity, TId>()
        where TEntity : class
        where TId : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entityType = typeof(TEntity);

        return (IFunctionalRepository<TEntity, TId>)_repositories.GetOrAdd(
            entityType,
            _ =>
            {
                // Resolve the repository options from DI
                var options = _serviceProvider.GetService(typeof(MongoDbRepositoryOptions<TEntity, TId>))
                    as MongoDbRepositoryOptions<TEntity, TId>
                    ?? throw new InvalidOperationException(
                        $"No MongoDB repository options found for {typeof(TEntity).Name}. " +
                        $"Register options using services.AddEncinaRepository<{typeof(TEntity).Name}, {typeof(TId).Name}>(...) " +
                        $"with UseUnitOfWork enabled.");

                var collectionName = options.GetEffectiveCollectionName();
                var collection = _database.GetCollection<TEntity>(collectionName);
                var idProperty = options.IdProperty
                    ?? throw new InvalidOperationException(
                        $"IdProperty not configured for {typeof(TEntity).Name}.");

                return new UnitOfWorkRepositoryMongoDB<TEntity, TId>(
                    collection,
                    idProperty,
                    this);
            });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// For MongoDB, changes are executed immediately within the session.
    /// This method is provided for API consistency but always returns 0
    /// since MongoDB does not track changes like EF Core.
    /// </remarks>
    public Task<Either<EncinaError, int>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // MongoDB executes changes immediately - no tracking
        // Return 0 to indicate no pending changes were saved
        return Task.FromResult(Right<EncinaError, int>(0));
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_session?.IsInTransaction == true)
        {
            return Left<EncinaError, Unit>(UnitOfWorkErrors.TransactionAlreadyActive());
        }

        try
        {
            // Start a new session if we don't have one
            _session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            _session.StartTransaction();

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (NotSupportedException ex) when (ex.Message.Contains("replica set", StringComparison.OrdinalIgnoreCase))
        {
            // MongoDB standalone servers don't support transactions
            return Left<EncinaError, Unit>(UnitOfWorkErrors.TransactionStartFailed(
                new InvalidOperationException(
                    "MongoDB transactions require a replica set deployment. " +
                    "Standalone MongoDB servers do not support multi-document transactions.",
                    ex)));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(UnitOfWorkErrors.TransactionStartFailed(ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_session is null || !_session.IsInTransaction)
        {
            return Left<EncinaError, Unit>(UnitOfWorkErrors.NoActiveTransaction());
        }

        try
        {
            await _session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            await DisposeSessionAsync().ConfigureAwait(false);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            await AbortInternalAsync().ConfigureAwait(false);
            return Left<EncinaError, Unit>(UnitOfWorkErrors.CommitFailed(ex));
        }
    }

    /// <inheritdoc/>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await AbortInternalAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Auto-abort uncommitted transaction
        await AbortInternalAsync().ConfigureAwait(false);

        _repositories.Clear();

        GC.SuppressFinalize(this);
    }

    private async Task AbortInternalAsync()
    {
        if (_session is null)
        {
            return;
        }

        try
        {
            if (_session.IsInTransaction)
            {
                await _session.AbortTransactionAsync().ConfigureAwait(false);
            }
        }
        catch
        {
            // Swallow abort exceptions - we're already in an error/cleanup path
        }
        finally
        {
            await DisposeSessionAsync().ConfigureAwait(false);
        }
    }

    private ValueTask DisposeSessionAsync()
    {
        if (_session is not null)
        {
            _session.Dispose();
            _session = null;
        }

        return ValueTask.CompletedTask;
    }
}

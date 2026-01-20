using Encina;
using LanguageExt;

namespace Encina.DomainModeling;

/// <summary>
/// Represents a Unit of Work pattern abstraction for coordinating changes across multiple repositories.
/// </summary>
/// <remarks>
/// <para>
/// <b>This interface is completely optional.</b> Most applications work perfectly fine using
/// <c>DbContext</c> directly. The <c>DbContext</c> is already a Unit of Work.
/// </para>
/// <para>
/// Consider using this interface only when you need:
/// </para>
/// <list type="bullet">
/// <item><description>Multiple DbContexts or databases in the same transaction</description></item>
/// <item><description>Explicit transaction control with Railway Oriented Programming error handling</description></item>
/// <item><description>Coordination between different repository implementations</description></item>
/// <item><description>Switching between data access providers (EF Core, Dapper, ADO.NET)</description></item>
/// </list>
/// <para>
/// For simple CRUD operations where all changes occur in a single DbContext, using
/// <c>DbContext.SaveChangesAsync()</c> directly is simpler and sufficient.
/// </para>
/// <para>
/// All operations return <see cref="Either{EncinaError, T}"/> for explicit error handling
/// following the Railway Oriented Programming pattern.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Option 1: Use DbContext directly (recommended for most cases)
/// public class TransferHandler(AppDbContext dbContext)
/// {
///     public async Task&lt;Either&lt;EncinaError, Unit&gt;&gt; HandleAsync(TransferCommand cmd, CancellationToken ct)
///     {
///         var source = await dbContext.Accounts.FindAsync(cmd.SourceId);
///         var target = await dbContext.Accounts.FindAsync(cmd.TargetId);
///
///         source.Debit(cmd.Amount);
///         target.Credit(cmd.Amount);
///
///         await dbContext.SaveChangesAsync(ct); // All changes are atomic
///         return Unit.Default;
///     }
/// }
///
/// // Option 2: Use UnitOfWork when you need explicit transaction control
/// public class TransferHandler(IUnitOfWork unitOfWork)
/// {
///     public async Task&lt;Either&lt;EncinaError, Unit&gt;&gt; HandleAsync(TransferCommand cmd, CancellationToken ct)
///     {
///         var accounts = unitOfWork.Repository&lt;Account, AccountId&gt;();
///
///         var beginResult = await unitOfWork.BeginTransactionAsync(ct);
///         if (beginResult.IsLeft) return beginResult;
///
///         // ... perform operations
///
///         var saveResult = await unitOfWork.SaveChangesAsync(ct);
///         if (saveResult.IsLeft)
///         {
///             await unitOfWork.RollbackAsync(ct);
///             return saveResult.Map(_ =&gt; Unit.Default);
///         }
///
///         return await unitOfWork.CommitAsync(ct);
///     }
/// }
/// </code>
/// </example>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Gets a repository for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <returns>
    /// A repository instance that shares the same transaction context.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Repositories obtained from the Unit of Work share the same transaction context.
    /// Changes tracked by the repository are not persisted until <see cref="SaveChangesAsync"/>
    /// is called.
    /// </para>
    /// <para>
    /// The same repository instance is returned for the same entity type within a Unit of Work scope.
    /// </para>
    /// </remarks>
    IFunctionalRepository<TEntity, TId> Repository<TEntity, TId>()
        where TEntity : class
        where TId : notnull;

    /// <summary>
    /// Saves all changes made in this Unit of Work to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the number of state entries written to the database;
    /// Left with <see cref="UnitOfWorkErrors.SaveChangesFailed(Exception)"/> if the operation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This operation persists all tracked changes across all repositories obtained from this Unit of Work.
    /// If a transaction is active, the changes are saved within that transaction context.
    /// </para>
    /// <para>
    /// If no explicit transaction was started, this operation is atomic. If a transaction is active,
    /// call <see cref="CommitAsync"/> to finalize the transaction after saving.
    /// </para>
    /// </remarks>
    Task<Either<EncinaError, int>> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with <see cref="UnitOfWorkErrors.TransactionAlreadyActive"/> if a transaction is already active;
    /// Left with <see cref="UnitOfWorkErrors.TransactionStartFailed"/> if the transaction cannot be started.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Only one transaction can be active at a time within a Unit of Work.
    /// The transaction encompasses all operations until <see cref="CommitAsync"/> or <see cref="RollbackAsync"/> is called.
    /// </para>
    /// <para>
    /// If the Unit of Work is disposed with an active transaction, the transaction is automatically rolled back.
    /// </para>
    /// </remarks>
    Task<Either<EncinaError, Unit>> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with <see cref="UnitOfWorkErrors.NoActiveTransaction"/> if no transaction is active;
    /// Left with <see cref="UnitOfWorkErrors.CommitFailed"/> if the commit fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This finalizes all changes made within the current transaction.
    /// Call <see cref="SaveChangesAsync"/> before committing to persist tracked entity changes.
    /// </para>
    /// <para>
    /// After commit, the transaction is disposed and <see cref="HasActiveTransaction"/> becomes false.
    /// </para>
    /// </remarks>
    Task<Either<EncinaError, Unit>> CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This discards all changes made within the current transaction.
    /// This operation is safe to call even if no transaction is active (it will be a no-op).
    /// </para>
    /// <para>
    /// After rollback, the transaction is disposed and <see cref="HasActiveTransaction"/> becomes false.
    /// Entity changes tracked by the DbContext/repositories are also discarded.
    /// </para>
    /// </remarks>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether there is an active transaction.
    /// </summary>
    /// <value>
    /// <c>true</c> if a transaction has been started and not yet committed or rolled back;
    /// otherwise, <c>false</c>.
    /// </value>
    bool HasActiveTransaction { get; }
}

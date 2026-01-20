using System.Collections.Immutable;
using Encina;

namespace Encina.DomainModeling;

/// <summary>
/// Factory for Unit of Work-specific errors that integrate with the <see cref="EncinaError"/> system.
/// </summary>
/// <remarks>
/// <para>
/// This class provides factory methods for creating consistent, well-structured errors
/// for Unit of Work operations. All errors follow the Encina error handling patterns
/// and can be used with Railway Oriented Programming.
/// </para>
/// <para>
/// Error codes follow the pattern: <c>UnitOfWork.{Category}</c> for easy categorization
/// and filtering in logging and monitoring systems.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a Unit of Work implementation:
/// public async Task&lt;Either&lt;EncinaError, Unit&gt;&gt; BeginTransactionAsync(CancellationToken ct)
/// {
///     if (HasActiveTransaction)
///         return Left(UnitOfWorkErrors.TransactionAlreadyActive());
///
///     try
///     {
///         _transaction = await _dbContext.Database.BeginTransactionAsync(ct);
///         return Right(Unit.Default);
///     }
///     catch (Exception ex)
///     {
///         return Left(UnitOfWorkErrors.TransactionStartFailed(ex));
///     }
/// }
/// </code>
/// </example>
public static class UnitOfWorkErrors
{
    private const string TransactionAlreadyActiveCode = "UnitOfWork.TransactionAlreadyActive";
    private const string NoActiveTransactionCode = "UnitOfWork.NoActiveTransaction";
    private const string SaveChangesFailedCode = "UnitOfWork.SaveChangesFailed";
    private const string CommitFailedCode = "UnitOfWork.CommitFailed";
    private const string TransactionStartFailedCode = "UnitOfWork.TransactionStartFailed";

    /// <summary>
    /// Creates a TransactionAlreadyActive error when attempting to start a transaction
    /// while one is already in progress.
    /// </summary>
    /// <returns>An <see cref="EncinaError"/> representing the transaction already active error.</returns>
    public static EncinaError TransactionAlreadyActive()
    {
        const string message = "A transaction is already active. Commit or rollback the existing transaction before starting a new one.";

        return EncinaErrors.Create(TransactionAlreadyActiveCode, message);
    }

    /// <summary>
    /// Creates a NoActiveTransaction error when attempting to commit or operate on
    /// a transaction that doesn't exist.
    /// </summary>
    /// <param name="operation">The operation that was attempted (e.g., "Commit", "Rollback").</param>
    /// <returns>An <see cref="EncinaError"/> representing the no active transaction error.</returns>
    public static EncinaError NoActiveTransaction(string operation = "Commit")
    {
        var message = $"Cannot {operation}: No active transaction. Call BeginTransactionAsync first.";

        var details = new Dictionary<string, object?>
        {
            ["Operation"] = operation
        }.ToImmutableDictionary();

        return EncinaErrors.Create(NoActiveTransactionCode, message, details: details);
    }

    /// <summary>
    /// Creates a SaveChangesFailed error when persisting changes to the database fails.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the save changes failure.</returns>
    public static EncinaError SaveChangesFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var message = $"Failed to save changes to the database: {exception.Message}";

        var details = new Dictionary<string, object?>
        {
            ["ExceptionType"] = exception.GetType().FullName
        }.ToImmutableDictionary();

        return EncinaErrors.Create(SaveChangesFailedCode, message, exception, details);
    }

    /// <summary>
    /// Creates a SaveChangesFailed error when persisting changes fails with concurrency conflict.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <param name="conflictingEntities">Optional list of entity types involved in the conflict.</param>
    /// <returns>An <see cref="EncinaError"/> representing the save changes failure due to concurrency.</returns>
    public static EncinaError SaveChangesFailed(Exception exception, IEnumerable<string>? conflictingEntities)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var entityList = conflictingEntities?.ToList() ?? [];
        var entityInfo = entityList.Count > 0
            ? $" Conflicting entities: {string.Join(", ", entityList)}."
            : string.Empty;

        var message = $"Failed to save changes due to concurrency conflict.{entityInfo}";

        var details = new Dictionary<string, object?>
        {
            ["ExceptionType"] = exception.GetType().FullName,
            ["ConflictingEntities"] = entityList
        }.ToImmutableDictionary();

        return EncinaErrors.Create(SaveChangesFailedCode, message, exception, details);
    }

    /// <summary>
    /// Creates a CommitFailed error when committing a transaction fails.
    /// </summary>
    /// <param name="exception">The exception that caused the commit to fail.</param>
    /// <returns>An <see cref="EncinaError"/> representing the commit failure.</returns>
    public static EncinaError CommitFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var message = $"Failed to commit transaction: {exception.Message}";

        var details = new Dictionary<string, object?>
        {
            ["ExceptionType"] = exception.GetType().FullName
        }.ToImmutableDictionary();

        return EncinaErrors.Create(CommitFailedCode, message, exception, details);
    }

    /// <summary>
    /// Creates a TransactionStartFailed error when beginning a new transaction fails.
    /// </summary>
    /// <param name="exception">The exception that caused the transaction start to fail.</param>
    /// <returns>An <see cref="EncinaError"/> representing the transaction start failure.</returns>
    public static EncinaError TransactionStartFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var message = $"Failed to start database transaction: {exception.Message}";

        var details = new Dictionary<string, object?>
        {
            ["ExceptionType"] = exception.GetType().FullName
        }.ToImmutableDictionary();

        return EncinaErrors.Create(TransactionStartFailedCode, message, exception, details);
    }

    /// <summary>
    /// Gets the error code for a Transaction Already Active error.
    /// </summary>
    public static string TransactionAlreadyActiveErrorCode => TransactionAlreadyActiveCode;

    /// <summary>
    /// Gets the error code for a No Active Transaction error.
    /// </summary>
    public static string NoActiveTransactionErrorCode => NoActiveTransactionCode;

    /// <summary>
    /// Gets the error code for a Save Changes Failed error.
    /// </summary>
    public static string SaveChangesFailedErrorCode => SaveChangesFailedCode;

    /// <summary>
    /// Gets the error code for a Commit Failed error.
    /// </summary>
    public static string CommitFailedErrorCode => CommitFailedCode;

    /// <summary>
    /// Gets the error code for a Transaction Start Failed error.
    /// </summary>
    public static string TransactionStartFailedErrorCode => TransactionStartFailedCode;
}

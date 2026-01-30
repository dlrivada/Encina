using System.Collections.Immutable;
using Encina;

namespace Encina.DomainModeling;

/// <summary>
/// Factory for repository-specific errors that integrate with the <see cref="EncinaError"/> system.
/// </summary>
/// <remarks>
/// <para>
/// This class provides factory methods for creating consistent, well-structured errors
/// for repository operations. All errors follow the Encina error handling patterns
/// and can be used with Railway Oriented Programming.
/// </para>
/// <para>
/// Error codes follow the pattern: <c>Repository.{Category}</c> for easy categorization
/// and filtering in logging and monitoring systems.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a repository implementation:
/// public async Task&lt;Either&lt;EncinaError, Order&gt;&gt; GetByIdAsync(OrderId id, CancellationToken ct)
/// {
///     var order = await _context.Orders.FindAsync([id.Value], ct);
///     return order is not null
///         ? Right(order)
///         : Left(RepositoryErrors.NotFound&lt;Order, OrderId&gt;(id));
/// }
/// </code>
/// </example>
public static class RepositoryErrors
{
    private const string NotFoundCode = "Repository.NotFound";
    private const string ConcurrencyConflictCode = "Repository.ConcurrencyConflict";
    private const string ValidationFailedCode = "Repository.ValidationFailed";
    private const string PersistenceErrorCode = "Repository.PersistenceError";
    private const string AlreadyExistsCode = "Repository.AlreadyExists";
    private const string InvalidOperationCode = "Repository.InvalidOperation";
    private const string BulkInsertFailedCode = "Repository.BulkInsertFailed";
    private const string BulkUpdateFailedCode = "Repository.BulkUpdateFailed";
    private const string BulkDeleteFailedCode = "Repository.BulkDeleteFailed";
    private const string BulkMergeFailedCode = "Repository.BulkMergeFailed";
    private const string BulkReadFailedCode = "Repository.BulkReadFailed";
    private const string EntityNotTrackedCode = "Repository.EntityNotTracked";
    private const string UpdateFailedCode = "Repository.UpdateFailed";
    private const string OperationNotSupportedCode = "Repository.OperationNotSupported";

    /// <summary>
    /// Creates a NotFound error when an entity with the specified ID is not found.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="id">The ID that was not found.</param>
    /// <returns>An <see cref="EncinaError"/> representing the not found error.</returns>
    public static EncinaError NotFound<TEntity, TId>(TId id)
        where TEntity : class
        where TId : notnull
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Entity of type '{entityTypeName}' with ID '{id}' was not found.";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["EntityId"] = id.ToString()
        }.ToImmutableDictionary();

        return EncinaErrors.Create(NotFoundCode, message, details: details);
    }

    /// <summary>
    /// Creates a NotFound error when an entity matching criteria is not found.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="criteria">A description of the search criteria.</param>
    /// <returns>An <see cref="EncinaError"/> representing the not found error.</returns>
    public static EncinaError NotFound<TEntity>(string criteria)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Entity of type '{entityTypeName}' matching criteria '{criteria}' was not found.";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["Criteria"] = criteria
        }.ToImmutableDictionary();

        return EncinaErrors.Create(NotFoundCode, message, details: details);
    }

    /// <summary>
    /// Creates a ConcurrencyConflict error when an optimistic concurrency conflict occurs.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="id">The ID of the entity with the conflict.</param>
    /// <param name="innerException">Optional inner exception with details about the conflict.</param>
    /// <returns>An <see cref="EncinaError"/> representing the concurrency conflict error.</returns>
    public static EncinaError ConcurrencyConflict<TEntity, TId>(TId id, Exception? innerException = null)
        where TEntity : class
        where TId : notnull
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Concurrency conflict occurred for entity of type '{entityTypeName}' with ID '{id}'. " +
                      "The entity has been modified by another process.";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["EntityId"] = id.ToString()
        }.ToImmutableDictionary();

        return EncinaErrors.Create(ConcurrencyConflictCode, message, innerException, details);
    }

    /// <summary>
    /// Creates a ConcurrencyConflict error when an optimistic concurrency conflict occurs.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="innerException">Optional inner exception with details about the conflict.</param>
    /// <returns>An <see cref="EncinaError"/> representing the concurrency conflict error.</returns>
    public static EncinaError ConcurrencyConflict<TEntity>(Exception? innerException = null)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Concurrency conflict occurred for entity of type '{entityTypeName}'. " +
                      "The entity has been modified by another process.";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName
        }.ToImmutableDictionary();

        return EncinaErrors.Create(ConcurrencyConflictCode, message, innerException, details);
    }

    /// <summary>
    /// Creates a ValidationFailed error when entity validation fails before persistence.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="validationErrors">The validation error messages.</param>
    /// <returns>An <see cref="EncinaError"/> representing the validation failure.</returns>
    public static EncinaError ValidationFailed<TEntity>(IEnumerable<string> validationErrors)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var errorList = validationErrors.ToList();
        var message = $"Validation failed for entity of type '{entityTypeName}': {string.Join("; ", errorList)}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["ValidationErrors"] = errorList
        }.ToImmutableDictionary();

        return EncinaErrors.Create(ValidationFailedCode, message, details: details);
    }

    /// <summary>
    /// Creates a ValidationFailed error with a single validation message.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="validationError">The validation error message.</param>
    /// <returns>An <see cref="EncinaError"/> representing the validation failure.</returns>
    public static EncinaError ValidationFailed<TEntity>(string validationError)
        where TEntity : class
    {
        return ValidationFailed<TEntity>([validationError]);
    }

    /// <summary>
    /// Creates a PersistenceError when a database operation fails.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="operation">The operation that failed (e.g., "Insert", "Update", "Delete").</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the persistence error.</returns>
    public static EncinaError PersistenceError<TEntity>(string operation, Exception exception)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(exception);

        var entityTypeName = typeof(TEntity).Name;
        var message = $"Database operation '{operation}' failed for entity of type '{entityTypeName}': {exception.Message}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["Operation"] = operation,
            ["ExceptionType"] = exception.GetType().FullName
        }.ToImmutableDictionary();

        return EncinaErrors.Create(PersistenceErrorCode, message, exception, details);
    }

    /// <summary>
    /// Creates a PersistenceError for a specific entity ID.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="id">The entity ID.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the persistence error.</returns>
    public static EncinaError PersistenceError<TEntity, TId>(TId id, string operation, Exception exception)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(exception);

        var entityTypeName = typeof(TEntity).Name;
        var message = $"Database operation '{operation}' failed for entity of type '{entityTypeName}' " +
                      $"with ID '{id}': {exception.Message}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["EntityId"] = id.ToString(),
            ["Operation"] = operation,
            ["ExceptionType"] = exception.GetType().FullName
        }.ToImmutableDictionary();

        return EncinaErrors.Create(PersistenceErrorCode, message, exception, details);
    }

    /// <summary>
    /// Creates an AlreadyExists error when trying to add a duplicate entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="id">The ID of the existing entity.</param>
    /// <returns>An <see cref="EncinaError"/> representing the already exists error.</returns>
    public static EncinaError AlreadyExists<TEntity, TId>(TId id)
        where TEntity : class
        where TId : notnull
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Entity of type '{entityTypeName}' with ID '{id}' already exists.";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["EntityId"] = id.ToString()
        }.ToImmutableDictionary();

        return EncinaErrors.Create(AlreadyExistsCode, message, details: details);
    }

    /// <summary>
    /// Creates an AlreadyExists error with a custom message.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="reason">The reason why the entity is considered a duplicate.</param>
    /// <returns>An <see cref="EncinaError"/> representing the already exists error.</returns>
    public static EncinaError AlreadyExists<TEntity>(string reason)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Entity of type '{entityTypeName}' already exists: {reason}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["Reason"] = reason
        }.ToImmutableDictionary();

        return EncinaErrors.Create(AlreadyExistsCode, message, details: details);
    }

    /// <summary>
    /// Creates an InvalidOperation error for operations that cannot be performed.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="operation">The operation that was attempted.</param>
    /// <param name="reason">The reason why the operation is invalid.</param>
    /// <returns>An <see cref="EncinaError"/> representing the invalid operation error.</returns>
    public static EncinaError InvalidOperation<TEntity>(string operation, string reason)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Invalid operation '{operation}' for entity of type '{entityTypeName}': {reason}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["Operation"] = operation,
            ["Reason"] = reason
        }.ToImmutableDictionary();

        return EncinaErrors.Create(InvalidOperationCode, message, details: details);
    }

    /// <summary>
    /// Gets the error code for a Not Found error.
    /// </summary>
    public static string NotFoundErrorCode => NotFoundCode;

    /// <summary>
    /// Gets the error code for a Concurrency Conflict error.
    /// </summary>
    public static string ConcurrencyConflictErrorCode => ConcurrencyConflictCode;

    /// <summary>
    /// Gets the error code for a Validation Failed error.
    /// </summary>
    public static string ValidationFailedErrorCode => ValidationFailedCode;

    /// <summary>
    /// Gets the error code for a Persistence Error.
    /// </summary>
    public static string PersistenceErrorErrorCode => PersistenceErrorCode;

    /// <summary>
    /// Gets the error code for an Already Exists error.
    /// </summary>
    public static string AlreadyExistsErrorCode => AlreadyExistsCode;

    /// <summary>
    /// Gets the error code for an Invalid Operation error.
    /// </summary>
    public static string InvalidOperationErrorCode => InvalidOperationCode;

    /// <summary>
    /// Gets the error code for a Bulk Insert Failed error.
    /// </summary>
    public static string BulkInsertFailedErrorCode => BulkInsertFailedCode;

    /// <summary>
    /// Gets the error code for a Bulk Update Failed error.
    /// </summary>
    public static string BulkUpdateFailedErrorCode => BulkUpdateFailedCode;

    /// <summary>
    /// Gets the error code for a Bulk Delete Failed error.
    /// </summary>
    public static string BulkDeleteFailedErrorCode => BulkDeleteFailedCode;

    /// <summary>
    /// Gets the error code for a Bulk Merge Failed error.
    /// </summary>
    public static string BulkMergeFailedErrorCode => BulkMergeFailedCode;

    /// <summary>
    /// Gets the error code for a Bulk Read Failed error.
    /// </summary>
    public static string BulkReadFailedErrorCode => BulkReadFailedCode;

    /// <summary>
    /// Creates a BulkInsertFailed error when a bulk insert operation fails.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entityCount">The total number of entities in the batch.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="failedIndex">Optional index of the entity that caused the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the bulk insert failure.</returns>
    public static EncinaError BulkInsertFailed<TEntity>(
        int entityCount,
        Exception exception,
        int? failedIndex = null)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(exception);

        var entityTypeName = typeof(TEntity).Name;
        var indexInfo = failedIndex.HasValue ? $" at index {failedIndex.Value}" : string.Empty;
        var message = $"Bulk insert failed for {entityCount} entities of type '{entityTypeName}'{indexInfo}: {exception.Message}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["EntityCount"] = entityCount,
            ["FailedIndex"] = failedIndex,
            ["ExceptionType"] = exception.GetType().FullName
        }.ToImmutableDictionary();

        return EncinaErrors.Create(BulkInsertFailedCode, message, exception, details);
    }

    /// <summary>
    /// Creates a BulkInsertFailed error with a custom message.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="reason">The reason for the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the bulk insert failure.</returns>
    public static EncinaError BulkInsertFailed<TEntity>(string reason)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Bulk insert failed for entity type '{entityTypeName}': {reason}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["Reason"] = reason
        }.ToImmutableDictionary();

        return EncinaErrors.Create(BulkInsertFailedCode, message, details: details);
    }

    /// <summary>
    /// Creates a BulkUpdateFailed error when a bulk update operation fails.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entityCount">The total number of entities in the batch.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="failedIndex">Optional index of the entity that caused the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the bulk update failure.</returns>
    public static EncinaError BulkUpdateFailed<TEntity>(
        int entityCount,
        Exception exception,
        int? failedIndex = null)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(exception);

        var entityTypeName = typeof(TEntity).Name;
        var indexInfo = failedIndex.HasValue ? $" at index {failedIndex.Value}" : string.Empty;
        var message = $"Bulk update failed for {entityCount} entities of type '{entityTypeName}'{indexInfo}: {exception.Message}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["EntityCount"] = entityCount,
            ["FailedIndex"] = failedIndex,
            ["ExceptionType"] = exception.GetType().FullName
        }.ToImmutableDictionary();

        return EncinaErrors.Create(BulkUpdateFailedCode, message, exception, details);
    }

    /// <summary>
    /// Creates a BulkUpdateFailed error with a custom message.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="reason">The reason for the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the bulk update failure.</returns>
    public static EncinaError BulkUpdateFailed<TEntity>(string reason)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Bulk update failed for entity type '{entityTypeName}': {reason}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["Reason"] = reason
        }.ToImmutableDictionary();

        return EncinaErrors.Create(BulkUpdateFailedCode, message, details: details);
    }

    /// <summary>
    /// Creates a BulkDeleteFailed error when a bulk delete operation fails.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entityCount">The total number of entities in the batch.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="failedIndex">Optional index of the entity that caused the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the bulk delete failure.</returns>
    public static EncinaError BulkDeleteFailed<TEntity>(
        int entityCount,
        Exception exception,
        int? failedIndex = null)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(exception);

        var entityTypeName = typeof(TEntity).Name;
        var indexInfo = failedIndex.HasValue ? $" at index {failedIndex.Value}" : string.Empty;
        var message = $"Bulk delete failed for {entityCount} entities of type '{entityTypeName}'{indexInfo}: {exception.Message}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["EntityCount"] = entityCount,
            ["FailedIndex"] = failedIndex,
            ["ExceptionType"] = exception.GetType().FullName
        }.ToImmutableDictionary();

        return EncinaErrors.Create(BulkDeleteFailedCode, message, exception, details);
    }

    /// <summary>
    /// Creates a BulkDeleteFailed error with a custom message.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="reason">The reason for the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the bulk delete failure.</returns>
    public static EncinaError BulkDeleteFailed<TEntity>(string reason)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Bulk delete failed for entity type '{entityTypeName}': {reason}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["Reason"] = reason
        }.ToImmutableDictionary();

        return EncinaErrors.Create(BulkDeleteFailedCode, message, details: details);
    }

    /// <summary>
    /// Creates a BulkMergeFailed error when a bulk merge (upsert) operation fails.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entityCount">The total number of entities in the batch.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="failedIndex">Optional index of the entity that caused the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the bulk merge failure.</returns>
    public static EncinaError BulkMergeFailed<TEntity>(
        int entityCount,
        Exception exception,
        int? failedIndex = null)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(exception);

        var entityTypeName = typeof(TEntity).Name;
        var indexInfo = failedIndex.HasValue ? $" at index {failedIndex.Value}" : string.Empty;
        var message = $"Bulk merge failed for {entityCount} entities of type '{entityTypeName}'{indexInfo}: {exception.Message}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["EntityCount"] = entityCount,
            ["FailedIndex"] = failedIndex,
            ["ExceptionType"] = exception.GetType().FullName
        }.ToImmutableDictionary();

        return EncinaErrors.Create(BulkMergeFailedCode, message, exception, details);
    }

    /// <summary>
    /// Creates a BulkMergeFailed error with a custom message.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="reason">The reason for the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the bulk merge failure.</returns>
    public static EncinaError BulkMergeFailed<TEntity>(string reason)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Bulk merge failed for entity type '{entityTypeName}': {reason}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["Reason"] = reason
        }.ToImmutableDictionary();

        return EncinaErrors.Create(BulkMergeFailedCode, message, details: details);
    }

    /// <summary>
    /// Creates a BulkReadFailed error when a bulk read operation fails.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="idCount">The number of IDs requested.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the bulk read failure.</returns>
    public static EncinaError BulkReadFailed<TEntity>(int idCount, Exception exception)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(exception);

        var entityTypeName = typeof(TEntity).Name;
        var message = $"Bulk read failed for {idCount} IDs of type '{entityTypeName}': {exception.Message}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["IdCount"] = idCount,
            ["ExceptionType"] = exception.GetType().FullName
        }.ToImmutableDictionary();

        return EncinaErrors.Create(BulkReadFailedCode, message, exception, details);
    }

    /// <summary>
    /// Creates a BulkReadFailed error with a custom message.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="reason">The reason for the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the bulk read failure.</returns>
    public static EncinaError BulkReadFailed<TEntity>(string reason)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Bulk read failed for entity type '{entityTypeName}': {reason}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["Reason"] = reason
        }.ToImmutableDictionary();

        return EncinaErrors.Create(BulkReadFailedCode, message, details: details);
    }

    /// <summary>
    /// Creates an EntityNotTracked error when an entity is not being tracked by the change tracker.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>An <see cref="EncinaError"/> representing the entity not tracked error.</returns>
    /// <remarks>
    /// <para>
    /// This error is typically used when performing immutable entity updates where the original
    /// entity must be tracked before it can be replaced with a modified version.
    /// </para>
    /// </remarks>
    public static EncinaError EntityNotTracked<TEntity>()
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Entity of type '{entityTypeName}' is not being tracked by the change tracker. " +
                      "The entity must be loaded/tracked before it can be updated.";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName
        }.ToImmutableDictionary();

        return EncinaErrors.Create(EntityNotTrackedCode, message, details: details);
    }

    /// <summary>
    /// Creates an EntityNotTracked error with a specific entity ID.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="id">The ID of the entity that is not tracked.</param>
    /// <returns>An <see cref="EncinaError"/> representing the entity not tracked error.</returns>
    public static EncinaError EntityNotTracked<TEntity, TId>(TId id)
        where TEntity : class
        where TId : notnull
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Entity of type '{entityTypeName}' with ID '{id}' is not being tracked by the change tracker. " +
                      "The entity must be loaded/tracked before it can be updated.";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["EntityId"] = id.ToString()
        }.ToImmutableDictionary();

        return EncinaErrors.Create(EntityNotTrackedCode, message, details: details);
    }

    /// <summary>
    /// Creates an UpdateFailed error when an entity update operation fails.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="exception">Optional exception that caused the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the update failure.</returns>
    /// <remarks>
    /// <para>
    /// This error is typically used for immutable entity updates when the detach/attach
    /// operation fails, or when the entity state cannot be properly modified.
    /// </para>
    /// </remarks>
    public static EncinaError UpdateFailed<TEntity>(string reason, Exception? exception = null)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Update failed for entity of type '{entityTypeName}': {reason}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["Reason"] = reason
        }.ToImmutableDictionary();

        return EncinaErrors.Create(UpdateFailedCode, message, exception, details);
    }

    /// <summary>
    /// Creates an UpdateFailed error for a specific entity ID.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="id">The ID of the entity that failed to update.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="exception">Optional exception that caused the failure.</param>
    /// <returns>An <see cref="EncinaError"/> representing the update failure.</returns>
    public static EncinaError UpdateFailed<TEntity, TId>(TId id, string reason, Exception? exception = null)
        where TEntity : class
        where TId : notnull
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Update failed for entity of type '{entityTypeName}' with ID '{id}': {reason}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["EntityId"] = id.ToString(),
            ["Reason"] = reason
        }.ToImmutableDictionary();

        return EncinaErrors.Create(UpdateFailedCode, message, exception, details);
    }

    /// <summary>
    /// Gets the error code for an Entity Not Tracked error.
    /// </summary>
    public static string EntityNotTrackedErrorCode => EntityNotTrackedCode;

    /// <summary>
    /// Gets the error code for an Update Failed error.
    /// </summary>
    public static string UpdateFailedErrorCode => UpdateFailedCode;

    /// <summary>
    /// Gets the error code for an Operation Not Supported error.
    /// </summary>
    public static string OperationNotSupportedErrorCode => OperationNotSupportedCode;

    /// <summary>
    /// Creates an OperationNotSupported error when an operation requires change tracking that the provider doesn't support.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="operationName">The name of the operation that is not supported (e.g., "UpdateImmutable").</param>
    /// <returns>An <see cref="EncinaError"/> representing the operation not supported error.</returns>
    /// <remarks>
    /// <para>
    /// This error is returned by non-EF Core providers (Dapper, ADO.NET, MongoDB) when an operation
    /// requires change tracking capabilities. The error message provides guidance on using
    /// <see cref="ImmutableAggregateHelper.PrepareForUpdate{TAggregate}"/> as an alternative.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a Dapper repository implementation:
    /// public Task&lt;Either&lt;EncinaError, Unit&gt;&gt; UpdateImmutableAsync(Order modified, CancellationToken ct)
    /// {
    ///     // Dapper doesn't have change tracking, return error with guidance
    ///     return Task.FromResult(
    ///         Either&lt;EncinaError, Unit&gt;.Left(
    ///             RepositoryErrors.OperationNotSupported&lt;Order&gt;("UpdateImmutableAsync")));
    /// }
    /// </code>
    /// </example>
    public static EncinaError OperationNotSupported<TEntity>(string operationName)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Operation '{operationName}' is not supported for entity type '{entityTypeName}' " +
                      "because this provider does not have change tracking capabilities. " +
                      "Use ImmutableAggregateHelper.PrepareForUpdate() followed by the standard UpdateAsync() method instead.";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["OperationName"] = operationName,
            ["RecommendedAlternative"] = "ImmutableAggregateHelper.PrepareForUpdate()"
        }.ToImmutableDictionary();

        return EncinaErrors.Create(OperationNotSupportedCode, message, details: details);
    }

    /// <summary>
    /// Creates an OperationNotSupported error with a custom reason.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="operationName">The name of the operation that is not supported.</param>
    /// <param name="reason">A custom reason explaining why the operation is not supported.</param>
    /// <returns>An <see cref="EncinaError"/> representing the operation not supported error.</returns>
    public static EncinaError OperationNotSupported<TEntity>(string operationName, string reason)
        where TEntity : class
    {
        var entityTypeName = typeof(TEntity).Name;
        var message = $"Operation '{operationName}' is not supported for entity type '{entityTypeName}': {reason}";

        var details = new Dictionary<string, object?>
        {
            ["EntityType"] = entityTypeName,
            ["OperationName"] = operationName,
            ["Reason"] = reason
        }.ToImmutableDictionary();

        return EncinaErrors.Create(OperationNotSupportedCode, message, details: details);
    }
}

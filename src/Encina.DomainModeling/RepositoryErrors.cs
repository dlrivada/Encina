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
}

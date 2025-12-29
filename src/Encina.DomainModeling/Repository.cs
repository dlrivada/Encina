using System.Linq.Expressions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling;

/// <summary>
/// Read-only repository interface for querying entities.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The ID type.</typeparam>
/// <remarks>
/// <para>
/// This interface follows the Repository pattern from Domain-Driven Design,
/// providing a collection-like interface for accessing domain objects.
/// </para>
/// <para>
/// The read-only variant is useful for CQRS query handlers that should not modify state.
/// </para>
/// </remarks>
public interface IReadOnlyRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The entity if found, None otherwise.</returns>
    Task<Option<TEntity>> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>All entities.</returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities matching the given specification.
    /// </summary>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Entities matching the specification.</returns>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a single entity matching the specification.
    /// </summary>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The entity if found, None otherwise.</returns>
    Task<Option<TEntity>> FindOneAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities matching the given predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Entities matching the predicate.</returns>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the specification.
    /// </summary>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if any entity matches, false otherwise.</returns>
    Task<bool> AnyAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if any entity matches, false otherwise.</returns>
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the specification.
    /// </summary>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The count of matching entities.</returns>
    Task<int> CountAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all entities.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The total count of entities.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged result of entities.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A paged result.</returns>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged result of entities matching the specification.
    /// </summary>
    /// <param name="specification">The specification to match.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A paged result.</returns>
    Task<PagedResult<TEntity>> GetPagedAsync(
        Specification<TEntity> specification,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Full repository interface with read and write operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The ID type.</typeparam>
/// <remarks>
/// <para>
/// This interface extends <see cref="IReadOnlyRepository{TEntity, TId}"/> with write operations.
/// It follows the Repository pattern from Domain-Driven Design, providing a collection-like
/// interface for accessing and persisting domain objects.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderRepository : IRepository&lt;Order, OrderId&gt;
/// {
///     private readonly DbContext _context;
///
///     public async Task&lt;Option&lt;Order&gt;&gt; GetByIdAsync(OrderId id, CancellationToken ct)
///     {
///         var order = await _context.Orders.FindAsync([id], ct);
///         return order is not null ? Some(order) : None;
///     }
///
///     public async Task AddAsync(Order entity, CancellationToken ct)
///     {
///         await _context.Orders.AddAsync(entity, ct);
///     }
///
///     // ... other implementations
/// }
/// </code>
/// </example>
public interface IRepository<TEntity, TId> : IReadOnlyRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities to the repository.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Updates multiple entities.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(TEntity entity);

    /// <summary>
    /// Removes multiple entities from the repository.
    /// </summary>
    /// <param name="entities">The entities to remove.</param>
    void RemoveRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Removes an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the entity was found and removed, false otherwise.</returns>
    Task<bool> RemoveByIdAsync(TId id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for aggregate roots with domain event support.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type.</typeparam>
/// <typeparam name="TId">The ID type.</typeparam>
public interface IAggregateRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : notnull
{
    /// <summary>
    /// Saves the aggregate and publishes its domain events.
    /// </summary>
    /// <param name="aggregate">The aggregate to save.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a paged result of entities.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <param name="Items">The items in the current page.</param>
/// <param name="PageNumber">The current page number (1-based).</param>
/// <param name="PageSize">The page size.</param>
/// <param name="TotalCount">The total count of items across all pages.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    /// <summary>
    /// Gets whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Gets whether the result is empty.
    /// </summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>An empty paged result.</returns>
    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 10) =>
        new([], pageNumber, pageSize, 0);

    /// <summary>
    /// Maps the items to a new type.
    /// </summary>
    /// <typeparam name="TResult">The target type.</typeparam>
    /// <param name="selector">The mapping function.</param>
    /// <returns>A new paged result with mapped items.</returns>
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return new PagedResult<TResult>(
            Items.Select(selector).ToList(),
            PageNumber,
            PageSize,
            TotalCount);
    }
}

/// <summary>
/// Error types for repository operations.
/// </summary>
/// <param name="Message">Description of the error.</param>
/// <param name="ErrorCode">Machine-readable error code.</param>
/// <param name="EntityType">The type of entity involved.</param>
/// <param name="EntityId">The ID of the entity involved (if applicable).</param>
/// <param name="InnerException">Optional inner exception.</param>
public sealed record RepositoryError(
    string Message,
    string ErrorCode,
    Type EntityType,
    object? EntityId = null,
    Exception? InnerException = null)
{
    /// <summary>
    /// Creates an error for when an entity is not found.
    /// </summary>
    public static RepositoryError NotFound<TEntity, TId>(TId id)
        where TEntity : class =>
        new(
            $"Entity of type '{typeof(TEntity).Name}' with ID '{id}' was not found",
            "REPOSITORY_NOT_FOUND",
            typeof(TEntity),
            id);

    /// <summary>
    /// Creates an error for when an entity already exists.
    /// </summary>
    public static RepositoryError AlreadyExists<TEntity, TId>(TId id)
        where TEntity : class =>
        new(
            $"Entity of type '{typeof(TEntity).Name}' with ID '{id}' already exists",
            "REPOSITORY_ALREADY_EXISTS",
            typeof(TEntity),
            id);

    /// <summary>
    /// Creates an error for a concurrency conflict.
    /// </summary>
    public static RepositoryError ConcurrencyConflict<TEntity, TId>(TId id)
        where TEntity : class =>
        new(
            $"Concurrency conflict for entity of type '{typeof(TEntity).Name}' with ID '{id}'",
            "REPOSITORY_CONCURRENCY_CONFLICT",
            typeof(TEntity),
            id);

    /// <summary>
    /// Creates an error for a database operation failure.
    /// </summary>
    public static RepositoryError OperationFailed<TEntity>(string operation, Exception exception)
        where TEntity : class =>
        new(
            $"Database operation '{operation}' failed for entity type '{typeof(TEntity).Name}': {exception.Message}",
            "REPOSITORY_OPERATION_FAILED",
            typeof(TEntity),
            InnerException: exception);

    /// <summary>
    /// Creates an error for invalid pagination parameters.
    /// </summary>
    public static RepositoryError InvalidPagination<TEntity>(int pageNumber, int pageSize)
        where TEntity : class =>
        new(
            $"Invalid pagination parameters: pageNumber={pageNumber}, pageSize={pageSize}",
            "REPOSITORY_INVALID_PAGINATION",
            typeof(TEntity));
}

/// <summary>
/// Extension methods for repository operations.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Gets an entity by ID, returning Left with NotFound error if not found.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="repository">The repository.</param>
    /// <param name="id">The entity ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either the entity or a NotFound error.</returns>
    public static async Task<Either<RepositoryError, TEntity>> GetByIdOrErrorAsync<TEntity, TId>(
        this IReadOnlyRepository<TEntity, TId> repository,
        TId id,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TId>
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);

        var result = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return result.Match<Either<RepositoryError, TEntity>>(
            Some: entity => Right(entity),
            None: () => Left(RepositoryError.NotFound<TEntity, TId>(id)));
    }

    /// <summary>
    /// Gets an entity by ID or throws if not found.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="repository">The repository.</param>
    /// <param name="id">The entity ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when the entity is not found.</exception>
    public static async Task<TEntity> GetByIdOrThrowAsync<TEntity, TId>(
        this IReadOnlyRepository<TEntity, TId> repository,
        TId id,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TId>
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);

        var result = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return result.Match(
            Some: entity => entity,
            None: () => throw new EntityNotFoundException(typeof(TEntity), id?.ToString()));
    }

    /// <summary>
    /// Adds an entity if it doesn't exist, returning an error if it already exists.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="repository">The repository.</param>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either Unit on success or AlreadyExists error.</returns>
    public static async Task<Either<RepositoryError, Unit>> AddIfNotExistsAsync<TEntity, TId>(
        this IRepository<TEntity, TId> repository,
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TId>
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(entity);

        var existing = await repository.GetByIdAsync(entity.Id, cancellationToken).ConfigureAwait(false);

        if (existing.IsSome)
        {
            return Left(RepositoryError.AlreadyExists<TEntity, TId>(entity.Id));
        }

        await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return Right(unit);
    }

    /// <summary>
    /// Updates an entity if it exists, returning an error if not found.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="repository">The repository.</param>
    /// <param name="id">The entity ID.</param>
    /// <param name="updateAction">The update action to apply.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either the updated entity or NotFound error.</returns>
    public static async Task<Either<RepositoryError, TEntity>> UpdateIfExistsAsync<TEntity, TId>(
        this IRepository<TEntity, TId> repository,
        TId id,
        Action<TEntity> updateAction,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TId>
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(updateAction);

        var result = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return result.Match<Either<RepositoryError, TEntity>>(
            Some: entity =>
            {
                updateAction(entity);
                repository.Update(entity);
                return Right(entity);
            },
            None: () => Left(RepositoryError.NotFound<TEntity, TId>(id)));
    }

    /// <summary>
    /// Gets entities matching the specification or returns empty list if none found.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="repository">The repository.</param>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The matching entities.</returns>
    public static async Task<IReadOnlyList<TEntity>> FindOrEmptyAsync<TEntity, TId>(
        this IReadOnlyRepository<TEntity, TId> repository,
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TId>
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            return await repository.FindAsync(specification, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return [];
        }
    }
}

/// <summary>
/// Exception thrown when an entity is not found.
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    /// <summary>
    /// Gets the type of entity that was not found.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// Gets the ID of the entity that was not found.
    /// </summary>
    public string? EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="entityType">The type of entity.</param>
    /// <param name="entityId">The entity ID.</param>
    public EntityNotFoundException(Type entityType, string? entityId)
        : base($"Entity of type '{entityType.Name}' with ID '{entityId}' was not found")
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="entityType">The type of entity.</param>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="innerException">The inner exception.</param>
    public EntityNotFoundException(Type entityType, string? entityId, Exception innerException)
        : base($"Entity of type '{entityType.Name}' with ID '{entityId}' was not found", innerException)
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

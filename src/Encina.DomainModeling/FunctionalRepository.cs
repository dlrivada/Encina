using Encina;
using LanguageExt;

namespace Encina.DomainModeling;

/// <summary>
/// Read-only repository interface with functional error handling using Railway Oriented Programming.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The ID type.</typeparam>
/// <remarks>
/// <para>
/// <b>This interface is completely optional.</b> Most applications work perfectly fine using
/// <c>DbContext</c> directly. Consider using this interface only when you need:
/// </para>
/// <list type="bullet">
/// <item><description>Multiple DbContexts or databases in the same operation</description></item>
/// <item><description>Easy unit testing with mocks (avoiding EF InMemory provider)</description></item>
/// <item><description>Switching between data access providers (EF Core, Dapper, ADO.NET, MongoDB)</description></item>
/// <item><description>Domain-Driven Design with aggregate repositories</description></item>
/// </list>
/// <para>
/// For simple CRUD operations or complex LINQ queries, using <c>DbContext</c> directly is simpler
/// and sufficient. The <c>DbContext</c> is already a Unit of Work and <c>DbSet&lt;T&gt;</c> is already a repository.
/// </para>
/// <para>
/// This interface provides a read-only view of the repository, suitable for CQRS query handlers
/// that should not modify state. All operations return <see cref="Either{EncinaError, T}"/>
/// for explicit error handling.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Option 1: Use DbContext directly (recommended for most cases)
/// public class GetOrderQueryHandler(AppDbContext dbContext)
/// {
///     public async Task&lt;Either&lt;EncinaError, OrderDto&gt;&gt; HandleAsync(GetOrderQuery query, CancellationToken ct)
///     {
///         var order = await dbContext.Orders.FindAsync(query.OrderId, ct);
///         return order is null
///             ? EncinaError.NotFound("Order not found")
///             : new OrderDto(order.Id, order.Total);
///     }
/// }
///
/// // Option 2: Use repository when you need abstraction
/// public class GetOrderQueryHandler(IFunctionalReadRepository&lt;Order, OrderId&gt; repository)
/// {
///     public async Task&lt;Either&lt;EncinaError, OrderDto&gt;&gt; HandleAsync(GetOrderQuery query, CancellationToken ct)
///     {
///         return await repository.GetByIdAsync(query.OrderId, ct)
///             .MapAsync(order =&gt; new OrderDto(order.Id, order.Total));
///     }
/// }
/// </code>
/// </example>
public interface IFunctionalReadRepository<TEntity, in TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the entity if found; Left with <see cref="RepositoryErrors.NotFound{TEntity, TId}(TId)"/> otherwise.
    /// </returns>
    Task<Either<EncinaError, TEntity>> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with all entities; Left with error if the operation fails.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities matching the given specification.
    /// </summary>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with matching entities; Left with error if the operation fails.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a single entity matching the specification.
    /// </summary>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the entity if found; Left with <see cref="RepositoryErrors.NotFound{TEntity}(string)"/> otherwise.
    /// </returns>
    Task<Either<EncinaError, TEntity>> FirstOrDefaultAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all entities.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the count; Left with error if the operation fails.
    /// </returns>
    Task<Either<EncinaError, int>> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the given specification.
    /// </summary>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the count; Left with error if the operation fails.
    /// </returns>
    Task<Either<EncinaError, int>> CountAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the specification.
    /// </summary>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with true if any entity matches, false otherwise; Left with error if the operation fails.
    /// </returns>
    Task<Either<EncinaError, bool>> AnyAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entities exist.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with true if any entity exists, false otherwise; Left with error if the operation fails.
    /// </returns>
    Task<Either<EncinaError, bool>> AnyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Full repository interface with functional error handling using Railway Oriented Programming.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The ID type.</typeparam>
/// <remarks>
/// <para>
/// <b>This interface is completely optional.</b> Most applications work perfectly fine using
/// <c>DbContext</c> directly. See <see cref="IFunctionalReadRepository{TEntity, TId}"/> for guidance
/// on when to use repositories vs direct ORM access.
/// </para>
/// <para>
/// This interface extends <see cref="IFunctionalReadRepository{TEntity, TId}"/> with write operations.
/// All operations return <see cref="Either{EncinaError, T}"/> for explicit error handling
/// following the Railway Oriented Programming pattern.
/// </para>
/// <para>
/// Use this interface for command handlers that need to read and modify entities
/// <b>only when you need the abstraction benefits</b> (provider switching, easy mocking, multi-database scenarios).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Option 1: Use DbContext directly (recommended for most cases)
/// public class CreateOrderHandler(AppDbContext dbContext)
/// {
///     public async Task&lt;Either&lt;EncinaError, OrderId&gt;&gt; HandleAsync(CreateOrderCommand cmd, CancellationToken ct)
///     {
///         var order = new Order(OrderId.New(), cmd.CustomerId, cmd.Items);
///         dbContext.Orders.Add(order);
///         await dbContext.SaveChangesAsync(ct);
///         return order.Id;
///     }
/// }
///
/// // Option 2: Use repository when you need abstraction
/// public class CreateOrderHandler(IFunctionalRepository&lt;Order, OrderId&gt; repository)
/// {
///     public async Task&lt;Either&lt;EncinaError, OrderId&gt;&gt; HandleAsync(CreateOrderCommand cmd, CancellationToken ct)
///     {
///         var order = new Order(OrderId.New(), cmd.CustomerId, cmd.Items);
///         return await repository.AddAsync(order, ct)
///             .MapAsync(added =&gt; added.Id);
///     }
/// }
/// </code>
/// </example>
public interface IFunctionalRepository<TEntity, in TId> : IFunctionalReadRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the added entity; Left with error if the operation fails
    /// (e.g., duplicate key, validation error).
    /// </returns>
    Task<Either<EncinaError, TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the updated entity; Left with error if the operation fails
    /// (e.g., not found, concurrency conflict).
    /// </returns>
    Task<Either<EncinaError, TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with Unit on success; Left with <see cref="RepositoryErrors.NotFound{TEntity, TId}(TId)"/>
    /// if not found, or other error if the operation fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> DeleteAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with Unit on success; Left with error if the operation fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities to the repository.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the added entities; Left with error if the operation fails.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyList<TEntity>>> AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with Unit on success; Left with error if the operation fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities matching the specification.
    /// </summary>
    /// <param name="specification">The specification for entities to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the number of deleted entities; Left with error if the operation fails.
    /// </returns>
    Task<Either<EncinaError, int>> DeleteRangeAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Extension methods for functional repository operations.
/// </summary>
public static class FunctionalRepositoryExtensions
{
    /// <summary>
    /// Gets an entity by ID and applies a transformation if found.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="repository">The repository.</param>
    /// <param name="id">The entity ID.</param>
    /// <param name="selector">The transformation to apply.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either the transformed result or an error.</returns>
    public static async Task<Either<EncinaError, TResult>> GetByIdAndMapAsync<TEntity, TId, TResult>(
        this IFunctionalReadRepository<TEntity, TId> repository,
        TId id,
        Func<TEntity, TResult> selector,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(selector);

        var result = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return result.Map(selector);
    }

    /// <summary>
    /// Gets an entity by ID and applies an async transformation if found.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="repository">The repository.</param>
    /// <param name="id">The entity ID.</param>
    /// <param name="selector">The async transformation to apply.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either the transformed result or an error.</returns>
    public static async Task<Either<EncinaError, TResult>> GetByIdAndMapAsync<TEntity, TId, TResult>(
        this IFunctionalReadRepository<TEntity, TId> repository,
        TId id,
        Func<TEntity, Task<TResult>> selector,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(selector);

        var result = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return await result.MatchAsync(
            RightAsync: async entity => Either<EncinaError, TResult>.Right(await selector(entity).ConfigureAwait(false)),
            Left: error => Either<EncinaError, TResult>.Left(error)).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an entity by ID, applies an update, and saves it.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="repository">The repository.</param>
    /// <param name="id">The entity ID.</param>
    /// <param name="updateAction">The action to apply to the entity.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either the updated entity or an error.</returns>
    public static async Task<Either<EncinaError, TEntity>> GetAndUpdateAsync<TEntity, TId>(
        this IFunctionalRepository<TEntity, TId> repository,
        TId id,
        Action<TEntity> updateAction,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(updateAction);

        var result = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return await result.MatchAsync(
            RightAsync: async entity =>
            {
                updateAction(entity);
                return await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
            },
            Left: error => Either<EncinaError, TEntity>.Left(error)).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds an entity only if it doesn't already exist (by checking specification).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The ID type.</typeparam>
    /// <param name="repository">The repository.</param>
    /// <param name="entity">The entity to add.</param>
    /// <param name="existsSpecification">Specification to check if entity already exists.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either the added entity or an error if it already exists.</returns>
    public static async Task<Either<EncinaError, TEntity>> AddIfNotExistsAsync<TEntity, TId>(
        this IFunctionalRepository<TEntity, TId> repository,
        TEntity entity,
        Specification<TEntity> existsSpecification,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(existsSpecification);

        var existsResult = await repository.AnyAsync(existsSpecification, cancellationToken).ConfigureAwait(false);

        return await existsResult.MatchAsync(
            RightAsync: async exists =>
            {
                if (exists)
                {
                    return Either<EncinaError, TEntity>.Left(
                        RepositoryErrors.AlreadyExists<TEntity>("Entity matching specification already exists"));
                }

                return await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            },
            Left: error => Either<EncinaError, TEntity>.Left(error)).ConfigureAwait(false);
    }
}

using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.ReadWriteSeparation;

/// <summary>
/// Factory for creating <see cref="DbContext"/> instances connected to either the primary (write)
/// database or a read replica based on the current routing context.
/// </summary>
/// <typeparam name="TContext">The type of <see cref="DbContext"/> to create.</typeparam>
/// <remarks>
/// <para>
/// This factory is the main integration point for read/write database separation in EF Core.
/// It creates DbContext instances with the appropriate connection string based on the current
/// <see cref="Encina.Messaging.ReadWriteSeparation.DatabaseRoutingContext"/>.
/// </para>
/// <para>
/// <b>Usage Scenarios:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       Use <see cref="CreateWriteContext"/> for explicit write operations
///     </description>
///   </item>
///   <item>
///     <description>
///       Use <see cref="CreateReadContext"/> for explicit read operations on replicas
///     </description>
///   </item>
///   <item>
///     <description>
///       Use <see cref="CreateContext"/> for automatic routing based on current context
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Automatic Routing:</b>
/// When using the <see cref="ReadWriteRoutingPipelineBehavior{TRequest,TResponse}"/>,
/// the routing context is automatically set based on whether the request is a query or command.
/// The factory then creates the appropriate context via <see cref="CreateContext"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ProductQueryHandler : IQueryHandler&lt;GetProductsQuery, IReadOnlyList&lt;Product&gt;&gt;
/// {
///     private readonly IReadWriteDbContextFactory&lt;AppDbContext&gt; _factory;
///
///     public ProductQueryHandler(IReadWriteDbContextFactory&lt;AppDbContext&gt; factory)
///     {
///         _factory = factory;
///     }
///
///     public async Task&lt;Either&lt;EncinaError, IReadOnlyList&lt;Product&gt;&gt;&gt; Handle(
///         GetProductsQuery query,
///         CancellationToken ct)
///     {
///         // Automatically uses read replica when routing context is set
///         await using var context = _factory.CreateContext();
///         var products = await context.Products.ToListAsync(ct);
///         return products;
///     }
/// }
/// </code>
/// </example>
public interface IReadWriteDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// Creates a <see cref="DbContext"/> connected to the primary (write) database.
    /// </summary>
    /// <returns>A new <typeparamref name="TContext"/> instance connected to the primary database.</returns>
    /// <remarks>
    /// Use this method when you need explicit write operations regardless of the current routing context.
    /// This is useful for scenarios where you need to ensure data is written to the primary database
    /// even if the routing context indicates a read operation.
    /// </remarks>
    Either<EncinaError, TContext> CreateWriteContext();

    /// <summary>
    /// Creates a <see cref="DbContext"/> connected to a read replica database.
    /// </summary>
    /// <returns>
    /// <c>Right</c> with a new <typeparamref name="TContext"/> instance connected to a read replica,
    /// or <c>Left</c> with an <see cref="EncinaError"/> if the connection string is not configured.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this method when you explicitly want to read from a replica, regardless of the current
    /// routing context. The specific replica is selected based on the configured
    /// <see cref="Encina.Messaging.ReadWriteSeparation.ReplicaStrategy"/>.
    /// </para>
    /// <para>
    /// If no read replicas are configured, this method falls back to the primary database.
    /// </para>
    /// </remarks>
    Either<EncinaError, TContext> CreateReadContext();

    /// <summary>
    /// Creates a <see cref="DbContext"/> based on the current routing context.
    /// </summary>
    /// <returns>
    /// <c>Right</c> with a new <typeparamref name="TContext"/> instance connected to the appropriate database,
    /// or <c>Left</c> with an <see cref="EncinaError"/> if the connection string is not configured.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the recommended method for most scenarios. It automatically routes to the appropriate
    /// database based on the current intent:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="Encina.Messaging.ReadWriteSeparation.DatabaseIntent.Read"/>: Uses a read replica
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="Encina.Messaging.ReadWriteSeparation.DatabaseIntent.Write"/>: Uses the primary database
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="Encina.Messaging.ReadWriteSeparation.DatabaseIntent.ForceWrite"/>: Uses the primary database
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// If routing is not enabled or no context is set, defaults to the primary database.
    /// </para>
    /// </remarks>
    Either<EncinaError, TContext> CreateContext();

    /// <summary>
    /// Asynchronously creates a <see cref="DbContext"/> connected to the primary (write) database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask<Either<EncinaError, TContext>> CreateWriteContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously creates a <see cref="DbContext"/> connected to a read replica database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask<Either<EncinaError, TContext>> CreateReadContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously creates a <see cref="DbContext"/> based on the current routing context.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask<Either<EncinaError, TContext>> CreateContextAsync(CancellationToken cancellationToken = default);
}

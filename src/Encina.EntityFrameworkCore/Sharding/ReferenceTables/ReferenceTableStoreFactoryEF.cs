using Encina.Sharding.ReferenceTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.EntityFrameworkCore.Sharding.ReferenceTables;

/// <summary>
/// EF Core factory that creates <see cref="ReferenceTableStoreEF"/> instances
/// bound to a specific shard connection string.
/// </summary>
/// <typeparam name="TContext">The <see cref="DbContext"/> type.</typeparam>
/// <remarks>
/// <para>
/// The factory uses a provider-specific <c>configureProvider</c> delegate to configure
/// the <see cref="DbContextOptionsBuilder{TContext}"/> for each shard. This matches the
/// pattern used by <see cref="ShardedDbContextFactory{TContext}"/>.
/// </para>
/// <para>
/// The <see cref="DbContext"/> is activated via
/// <see cref="ActivatorUtilities.CreateInstance(IServiceProvider, Type, object[])"/>,
/// which resolves constructor dependencies from DI and passes the options explicitly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // SQL Server
/// var factory = new ReferenceTableStoreFactoryEF&lt;AppDbContext&gt;(
///     serviceProvider,
///     (builder, cs) =&gt; builder.UseSqlServer(cs));
///
/// // PostgreSQL
/// var factory = new ReferenceTableStoreFactoryEF&lt;AppDbContext&gt;(
///     serviceProvider,
///     (builder, cs) =&gt; builder.UseNpgsql(cs));
/// </code>
/// </example>
public sealed class ReferenceTableStoreFactoryEF<TContext>(
    IServiceProvider serviceProvider,
    Action<DbContextOptionsBuilder<TContext>, string> configureProvider) : IReferenceTableStoreFactory
    where TContext : DbContext
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly Action<DbContextOptionsBuilder<TContext>, string> _configureProvider = configureProvider ?? throw new ArgumentNullException(nameof(configureProvider));

    /// <inheritdoc />
    public IReferenceTableStore CreateForShard(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        _configureProvider(optionsBuilder, connectionString);

        var context = ActivatorUtilities.CreateInstance<TContext>(
            _serviceProvider,
            optionsBuilder.Options);

        return new ReferenceTableStoreEF(context);
    }
}

using Encina.Messaging.ReadWriteSeparation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.EntityFrameworkCore.ReadWriteSeparation;

/// <summary>
/// Default implementation of <see cref="IReadWriteDbContextFactory{TContext}"/> that creates
/// DbContext instances with the appropriate connection string based on routing context.
/// </summary>
/// <typeparam name="TContext">The type of <see cref="DbContext"/> to create.</typeparam>
/// <remarks>
/// <para>
/// This factory uses <see cref="ActivatorUtilities.CreateInstance{T}(IServiceProvider, object[])"/>
/// to create DbContext instances, passing a configured <see cref="DbContextOptions{TContext}"/>
/// with the appropriate connection string.
/// </para>
/// <para>
/// <b>Connection Selection:</b>
/// The factory uses <see cref="IReadWriteConnectionSelector"/> to determine which connection
/// string to use based on the current <see cref="DatabaseRoutingContext"/>.
/// </para>
/// <para>
/// <b>DbContext Configuration:</b>
/// The factory clones the base <see cref="DbContextOptions{TContext}"/> and replaces the
/// connection string, preserving all other configuration (logging, interceptors, etc.).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration (automatic via AddEncinaEntityFrameworkCore)
/// services.AddScoped&lt;IReadWriteDbContextFactory&lt;AppDbContext&gt;, ReadWriteDbContextFactory&lt;AppDbContext&gt;&gt;();
///
/// // Usage in a handler
/// public class MyHandler
/// {
///     private readonly IReadWriteDbContextFactory&lt;AppDbContext&gt; _factory;
///
///     public async Task Handle()
///     {
///         // Create context based on current routing context
///         await using var context = _factory.CreateContext();
///
///         // Or explicitly create for write/read
///         await using var writeContext = _factory.CreateWriteContext();
///         await using var readContext = _factory.CreateReadContext();
///     }
/// }
/// </code>
/// </example>
public sealed class ReadWriteDbContextFactory<TContext> : IReadWriteDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadWriteConnectionSelector _connectionSelector;
    private readonly DbContextOptions<TContext> _baseOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadWriteDbContextFactory{TContext}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for creating DbContext instances.</param>
    /// <param name="connectionSelector">The connection selector for determining connection strings.</param>
    /// <param name="baseOptions">The base DbContext options to use as a template.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <see langword="null"/>.
    /// </exception>
    public ReadWriteDbContextFactory(
        IServiceProvider serviceProvider,
        IReadWriteConnectionSelector connectionSelector,
        DbContextOptions<TContext> baseOptions)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(connectionSelector);
        ArgumentNullException.ThrowIfNull(baseOptions);

        _serviceProvider = serviceProvider;
        _connectionSelector = connectionSelector;
        _baseOptions = baseOptions;
    }

    /// <inheritdoc />
    public TContext CreateWriteContext()
    {
        var connectionString = _connectionSelector.GetWriteConnectionString();
        return CreateContextWithConnectionString(connectionString);
    }

    /// <inheritdoc />
    public TContext CreateReadContext()
    {
        var connectionString = _connectionSelector.GetReadConnectionString();
        return CreateContextWithConnectionString(connectionString);
    }

    /// <inheritdoc />
    public TContext CreateContext()
    {
        var connectionString = _connectionSelector.GetConnectionString();
        return CreateContextWithConnectionString(connectionString);
    }

    /// <inheritdoc />
    public ValueTask<TContext> CreateWriteContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(CreateWriteContext());
    }

    /// <inheritdoc />
    public ValueTask<TContext> CreateReadContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(CreateReadContext());
    }

    /// <inheritdoc />
    public ValueTask<TContext> CreateContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(CreateContext());
    }

    private TContext CreateContextWithConnectionString(string connectionString)
    {
        // Create new options with the specified connection string
        var optionsBuilder = new DbContextOptionsBuilder<TContext>(_baseOptions);

        // Get the relational extension from base options to determine the provider
        var relationalExtension = _baseOptions.Extensions
            .OfType<Microsoft.EntityFrameworkCore.Infrastructure.RelationalOptionsExtension>()
            .FirstOrDefault();

        if (relationalExtension is not null)
        {
            // Create a new extension with the updated connection string
            var newExtension = relationalExtension.WithConnectionString(connectionString);

            // Replace the extension in the builder
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(newExtension);
        }

        var options = optionsBuilder.Options;

        // Create the context using ActivatorUtilities
        return ActivatorUtilities.CreateInstance<TContext>(_serviceProvider, options);
    }
}

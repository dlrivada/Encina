using Encina.Messaging.Health;
using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Marten.Health;

/// <summary>
/// Health check for Marten (PostgreSQL) event store connectivity.
/// </summary>
public sealed class MartenHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the Marten health check.
    /// </summary>
    public const string DefaultName = "encina-marten";

    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve IDocumentStore from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public MartenHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "eventsourcing", "marten", "postgresql", "ready"])
    {
        _serviceProvider = serviceProvider;
        _options = options ?? new ProviderHealthCheckOptions();
    }

    /// <inheritdoc/>
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var store = _serviceProvider.GetRequiredService<IDocumentStore>();

        // Use Marten's built-in QuerySession which creates a connection automatically
        await using var session = store.QuerySession();

        // Execute a simple SQL query to verify the connection works
        var result = await session.QueryAsync<int>("SELECT 1", cancellationToken).ConfigureAwait(false);

        return HealthCheckResult.Healthy($"{Name} is connected");
    }
}

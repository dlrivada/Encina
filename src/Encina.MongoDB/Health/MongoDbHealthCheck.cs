using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Encina.MongoDB.Health;

/// <summary>
/// Health check for MongoDB database connectivity.
/// </summary>
public sealed class MongoDbHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the MongoDB health check.
    /// </summary>
    public const string DefaultName = "encina-mongodb";

    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve IMongoClient from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public MongoDbHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "database", "mongodb", "ready"])
    {
        _serviceProvider = serviceProvider;
        _options = options ?? new ProviderHealthCheckOptions();
    }

    /// <inheritdoc/>
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(_options.Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var mongoClient = _serviceProvider.GetRequiredService<IMongoClient>();

            // Run a simple ping command to check connectivity
            var adminDb = mongoClient.GetDatabase("admin");
            var pingCommand = new BsonDocument("ping", 1);
            await adminDb.RunCommandAsync<BsonDocument>(pingCommand, cancellationToken: linkedCts.Token).ConfigureAwait(false);

            return HealthCheckResult.Healthy($"{Name} is reachable");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"{Name} health check timed out after {_options.Timeout.TotalSeconds}s");
        }
        catch (MongoException ex)
        {
            return HealthCheckResult.Unhealthy($"{Name} health check failed: {ex.Message}");
        }
    }
}

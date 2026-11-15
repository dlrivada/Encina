using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.SignalR;

/// <summary>
/// Freezes the <see cref="EncinaHub"/> allow-list when the host starts, logs each entry
/// registered with <c>allowAnonymous: true</c> once, and warns when an <see cref="EncinaHub"/> is
/// mapped while the allow-list is empty.
/// </summary>
/// <remarks>
/// The work runs in <see cref="StartedAsync"/>, after every hosted service (including the web
/// server that maps the hub endpoints) has started, so the mapped hubs are known.
/// </remarks>
internal sealed class SignalRAllowListStartupCheck : IHostedLifecycleService
{
    private readonly IOptions<SignalROptions> _options;
    private readonly ILogger<SignalRAllowListStartupCheck> _logger;
    private readonly IEnumerable<EndpointDataSource> _endpointDataSources;

    public SignalRAllowListStartupCheck(
        IOptions<SignalROptions> options,
        ILogger<SignalRAllowListStartupCheck> logger,
        IEnumerable<EndpointDataSource> endpointDataSources)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(endpointDataSources);

        _options = options;
        _logger = logger;
        _endpointDataSources = endpointDataSources;
    }

    /// <inheritdoc />
    public Task StartedAsync(CancellationToken cancellationToken)
    {
        var allowList = _options.Value.GetAllowList();
        foreach (var registration in allowList.Values.Where(entry => entry.AllowAnonymous))
        {
            Log.AnonymousHubTypeAllowed(_logger, registration.Kind, registration.Type.FullName!);
        }

        if (allowList.Count == 0 && IsEncinaHubMapped())
        {
            Log.EmptyAllowListWithMappedHub(_logger);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>Freezes the allow-list before the server accepts connections, so a duplicate registration fails startup.</remarks>
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        _options.Value.GetAllowList();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private bool IsEncinaHubMapped()
        => _endpointDataSources
            .SelectMany(source => source.Endpoints)
            .Any(endpoint => endpoint.Metadata.GetMetadata<HubMetadata>()?.HubType.IsAssignableTo(typeof(EncinaHub)) == true);
}

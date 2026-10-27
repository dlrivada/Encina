using Encina.Audit.Marten.Diagnostics;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Audit.Marten.Projections;

/// <summary>
/// Fails the host at startup when Marten's store-wide projection error flags would let the audit projections skip an
/// event they cannot project (issue #2770, D14, D22).
/// </summary>
/// <remarks>
/// <para>
/// The check inspects the built <see cref="StoreOptions"/> of the <see cref="IDocumentStore"/>, so it sees the final
/// flags after the application's own <c>AddMarten</c> configuration and every <c>IConfigureMarten</c> ran, whatever
/// the registration order. <see cref="IHostedLifecycleService.StartingAsync"/> runs before any hosted service starts,
/// so the host stops before Marten's async daemon processes an event.
/// </para>
/// <para>
/// Encina never changes these flags: they are store-wide and also govern the application's own projections, so the
/// application sets them (or opts out with <see cref="MartenOperationAuditOptions.AllowSkippedProjectionErrors"/>,
/// which is logged).
/// </para>
/// </remarks>
internal sealed class AuditProjectionErrorPolicyStartupGuard : IHostedLifecycleService
{
    /// <summary>The fixed message of the startup failure; the offending flag names follow it.</summary>
    internal const string FailureMessage =
        "The Marten audit projections cannot start: Marten's projection error flags skip events that fail to project. "
        + "An audit event that cannot be read must pause the projection shard so an operator resolves it, never be silently skipped. "
        + "Set StoreOptions.Projections.Errors and StoreOptions.Projections.RebuildErrors SkipApplyErrors and SkipSerializationErrors to false in the application's AddMarten configuration. "
        + "These flags are store-wide: they also apply to every other projection of the application's store. "
        + "To start with skipping enabled anyway, set MartenOperationAuditOptions.AllowSkippedProjectionErrors to true; that opt-out is logged at startup. "
        + "Offending flags: ";

    /// <summary>The message when the store options are not Marten's <see cref="StoreOptions"/> and cannot be verified.</summary>
    internal const string UnverifiableMessage =
        "The Marten audit projections cannot start: the store's projection error flags cannot be read, so the fail-closed check cannot run.";

    private readonly IDocumentStore _store;
    private readonly MartenOperationAuditOptions _options;
    private readonly ILogger<AuditProjectionErrorPolicyStartupGuard> _logger;

    public AuditProjectionErrorPolicyStartupGuard(
        IDocumentStore store,
        IOptions<MartenOperationAuditOptions> options,
        ILogger<AuditProjectionErrorPolicyStartupGuard> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Returns the names of the flags that skip failing events, in a fixed order.</summary>
    internal static IReadOnlyList<string> SkippingFlags(StoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var flags = new List<string>(4);
        AddIf(flags, options.Projections.Errors.SkipApplyErrors, "Errors.SkipApplyErrors");
        AddIf(flags, options.Projections.Errors.SkipSerializationErrors, "Errors.SkipSerializationErrors");
        AddIf(flags, options.Projections.RebuildErrors.SkipApplyErrors, "RebuildErrors.SkipApplyErrors");
        AddIf(flags, options.Projections.RebuildErrors.SkipSerializationErrors, "RebuildErrors.SkipSerializationErrors");
        return flags;
    }

    private static void AddIf(List<string> flags, bool enabled, string name)
    {
        if (enabled)
        {
            flags.Add(name);
        }
    }

    /// <inheritdoc />
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        if (_store.Options is not StoreOptions storeOptions)
        {
            // Fail closed: the flags cannot be read, so they cannot be verified.
            MartenAuditLog.ProjectionErrorPolicyViolated(_logger, 0);
            throw new InvalidOperationException(UnverifiableMessage);
        }

        var skipping = SkippingFlags(storeOptions);
        if (skipping.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (_options.AllowSkippedProjectionErrors)
        {
            MartenAuditLog.ProjectionErrorPolicyCheckSkipped(
                _logger, nameof(MartenOperationAuditOptions.AllowSkippedProjectionErrors), skipping.Count);
            return Task.CompletedTask;
        }

        MartenAuditLog.ProjectionErrorPolicyViolated(_logger, skipping.Count);
        throw new InvalidOperationException(FailureMessage + string.Join(", ", skipping) + ".");
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

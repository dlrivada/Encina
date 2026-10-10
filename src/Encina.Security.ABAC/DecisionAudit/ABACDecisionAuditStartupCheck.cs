using Encina.Security.ABAC.Diagnostics;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// Checks the prerequisites of the decision audit when the host starts, so an enabled audit can never
/// silently skip the audit: no <see cref="IOperationAuditStore"/> is a Critical log (EventId 9087) and a
/// failed start, an in-memory store is a Warning (9086) and <c>BestEffort</c> is a Warning (9084).
/// </summary>
/// <remarks>
/// <para>
/// It reads the final <see cref="IOptions{TOptions}"/> when the host starts, not the options instance
/// seen by <see cref="ServiceCollectionExtensions.AddEncinaABAC"/>. When
/// <see cref="ABACDecisionAuditOptions.Enabled"/> is <c>false</c> it returns at once and never resolves
/// the store, so an application without one starts. The store is resolved from a scope of its own
/// because database stores are scoped.
/// </para>
/// </remarks>
internal sealed class ABACDecisionAuditStartupCheck : IHostedService
{
    private readonly IOptions<ABACOptions> _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ABACDecisionAuditStartupCheck> _logger;

    public ABACDecisionAuditStartupCheck(
        IOptions<ABACOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ABACDecisionAuditStartupCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var audit = _options.Value.DecisionAudit;
        if (!audit.Enabled)
        {
            return Task.CompletedTask;
        }

        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetService<IOperationAuditStore>();
        if (store is null)
        {
            ABACLogMessages.DecisionAuditStoreMissing(_logger);
            throw new InvalidOperationException(
                "ABACOptions.DecisionAudit.Enabled is true but no IOperationAuditStore is registered. " +
                "Register an operation audit store (for example through a provider package) or disable the decision audit.");
        }

        if (store is InMemoryOperationAuditStore)
        {
            ABACLogMessages.DecisionAuditInMemoryStore(_logger);
        }

        if (audit.FailureMode == ABACDecisionAuditFailureMode.BestEffort)
        {
            ABACLogMessages.DecisionAuditBestEffort(_logger);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

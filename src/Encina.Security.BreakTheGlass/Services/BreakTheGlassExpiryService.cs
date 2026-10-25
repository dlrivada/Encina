using Encina.Diagnostics;
using Encina.Security.Audit;
using Encina.Security.BreakTheGlass.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Closes expired elevations and writes the <c>break_glass.expired</c> audit entry of each one.
/// </summary>
/// <remarks>
/// <para>
/// Per cycle the sweeper lists the rows that are Active past their expiry or Expired without a recorded
/// expiry (<see cref="IBreakTheGlassStore.GetExpiredUnrecordedAsync"/>). An Active row is closed with the
/// conditional <see cref="IBreakTheGlassStore.TryCloseAsync"/>: only the winner continues, so several
/// hosts can run the sweeper without a distributed lock. An Expired-unrecorded row (closed by a host
/// that crashed before auditing) goes straight to the audit entry. After the entry is stored the row is
/// marked recorded. A duplicate entry after a crash is possible and over-reports, never under-reports.
/// </para>
/// <para>
/// A failure is never reported as success: a per-row audit failure is logged and the batch continues, a
/// store failure stops the batch, and in both cases the cycle is not marked completed (so the health
/// check turns Degraded after three intervals) and the failed rows are retried next cycle.
/// </para>
/// <para>
/// Each cycle runs under the built-in service identity <see cref="ServiceIdentityName"/>: its subject is
/// the actor of every expiry entry and its scope supplies the correlation id. The store is resolved from
/// a scope of its own per cycle because database stores are scoped and this service is a singleton. The
/// sweeper stays on when <see cref="BreakTheGlassOptions.Enabled"/> is <see langword="false"/>, so
/// elevations that were open when the feature was switched off still expire and are audited.
/// </para>
/// </remarks>
internal sealed class BreakTheGlassExpiryService : BackgroundService
{
    /// <summary>The built-in service identity the sweep runs under.</summary>
    internal const string ServiceIdentityName = "encina.break-glass.expiry";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IInternalRequestContextScopeFactory _scopes;
    private readonly IOptions<BreakTheGlassOptions> _options;
    private readonly BreakTheGlassSweepState _state;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BreakTheGlassExpiryService> _logger;

    public BreakTheGlassExpiryService(
        IServiceScopeFactory scopeFactory,
        IInternalRequestContextScopeFactory scopes,
        IOptions<BreakTheGlassOptions> options,
        BreakTheGlassSweepState state,
        TimeProvider timeProvider,
        ILogger<BreakTheGlassExpiryService> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _scopes = scopes;
        _options = options;
        _state = state;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _state.RecordStarted(_timeProvider.GetUtcNow());
        using var timer = new PeriodicTimer(_options.Value.ExpirySweepInterval, _timeProvider);

        try
        {
            do
            {
                await SweepOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping.
        }
    }

    /// <summary>
    /// Runs one sweep cycle.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the cycle.</param>
    /// <returns><see langword="true"/> when the cycle completed with every row handled; <see langword="false"/> when it failed.</returns>
    internal async Task<bool> SweepOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var ran = await _scopes.RunAsBuiltInAsync(ServiceIdentityName, SweepRowsAsync, cancellationToken).ConfigureAwait(false);
            return ran.Match(Right: completed => completed, Left: ScopeRefused);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            BreakTheGlassLogMessages.ExpirySweepThrew(_logger, ex.ForLogging());
            BreakTheGlassDiagnostics.RecordStoreFailure(BreakTheGlassDiagnostics.OperationExpirySweep);
            return false;
        }
    }

    private bool ScopeRefused(EncinaError error)
    {
        BreakTheGlassLogMessages.ExpirySweepFailed(_logger, error.GetCode().IfNone("encina.unknown"));
        BreakTheGlassDiagnostics.RecordStoreFailure(BreakTheGlassDiagnostics.OperationExpirySweep);
        return false;
    }

    // The work of one cycle; Right(true) when the cycle completed, Right(false) when it failed (logged).
    private async Task<Either<EncinaError, bool>> SweepRowsAsync(IRequestContext context, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IBreakTheGlassStore>();
        var auditStore = scope.ServiceProvider.GetRequiredService<IOperationAuditStore>();

        var listed = await store.GetExpiredUnrecordedAsync(now, _options.Value.ExpirySweepBatchSize, cancellationToken).ConfigureAwait(false);
        var rows = listed.Match<IReadOnlyList<BreakTheGlassElevation>?>(Right: found => found, Left: error => { StoreFailed(error); return null; });
        if (rows is null)
        {
            return false;
        }

        var failure = await HandleRowsAsync(rows, store, auditStore, context, now, cancellationToken).ConfigureAwait(false);
        return Complete(failure, rows.Count);
    }

    private bool Complete(SweepFailure failure, int rows)
    {
        if (failure != SweepFailure.None)
        {
            return false;
        }

        _state.RecordCompleted(_timeProvider.GetUtcNow());
        BreakTheGlassLogMessages.ExpirySweepCompleted(_logger, rows);
        return true;
    }

    // A per-row audit failure continues with the next row; a store failure stops the batch. Either way
    // the cycle is not completed.
    private async Task<SweepFailure> HandleRowsAsync(
        IReadOnlyList<BreakTheGlassElevation> rows,
        IBreakTheGlassStore store,
        IOperationAuditStore auditStore,
        IRequestContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var failure = SweepFailure.None;
        foreach (var row in rows)
        {
            var result = await HandleRowAsync(row, store, auditStore, context, now, cancellationToken).ConfigureAwait(false);
            if (result == SweepFailure.Store)
            {
                return SweepFailure.Store;
            }

            if (result == SweepFailure.Audit)
            {
                failure = SweepFailure.Audit;
            }
        }

        return failure;
    }

    private async Task<SweepFailure> HandleRowAsync(
        BreakTheGlassElevation row,
        IBreakTheGlassStore store,
        IOperationAuditStore auditStore,
        IRequestContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (row.Status == BreakTheGlassStatus.Active)
        {
            var closed = await store.TryCloseAsync(row.TenantId, row.ElevationId, BreakTheGlassStatus.Expired, "expired", now, cancellationToken).ConfigureAwait(false);
            var won = closed.Match(Right: closedHere => closedHere, Left: error => { StoreFailed(error); return (bool?)null; });
            if (won is null)
            {
                return SweepFailure.Store;
            }

            if (!won.Value)
            {
                // Another host closed it and records the expiry.
                return SweepFailure.None;
            }
        }

        var entry = BreakTheGlassAudit.Create(
            BreakTheGlassAudit.ActionExpired,
            AuditOutcome.Success,
            row.ElevationId,
            context.CorrelationId,
            context.Identity.UserId,
            row.TenantId,
            now,
            new Dictionary<string, object?> { ["scope"] = row.Scope, ["reason"] = "expired" });
        var audited = await BreakTheGlassAudit.WriteAsync(auditStore, entry, cancellationToken).ConfigureAwait(false);
        if (audited.IsLeft)
        {
            BreakTheGlassLogMessages.ExpiryAuditFailed(_logger, BreakTheGlassErrors.AuditFailedCode);
            BreakTheGlassDiagnostics.RecordStoreFailure(BreakTheGlassDiagnostics.OperationExpirySweep);
            return SweepFailure.Audit;
        }

        var marked = await store.MarkExpiryRecordedAsync(row.TenantId, row.ElevationId, now, cancellationToken).ConfigureAwait(false);
        return marked.Match(
            Right: _ =>
            {
                BreakTheGlassLogMessages.ExpiryRecorded(_logger, row.Scope);
                return SweepFailure.None;
            },
            Left: error =>
            {
                StoreFailed(error);
                return SweepFailure.Store;
            });
    }

    private void StoreFailed(EncinaError error)
    {
        BreakTheGlassLogMessages.ExpirySweepFailed(_logger, error.GetCode().IfNone("encina.unknown"));
        BreakTheGlassDiagnostics.RecordStoreFailure(BreakTheGlassDiagnostics.OperationExpirySweep);
    }

    private enum SweepFailure
    {
        None,
        Audit,
        Store
    }
}

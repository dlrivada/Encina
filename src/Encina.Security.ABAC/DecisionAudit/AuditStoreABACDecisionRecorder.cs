using System.Transactions;

using Encina.Security.ABAC.Diagnostics;
using Encina.Security.Audit;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// The default <see cref="IABACDecisionRecorder"/>: writes each decision record as an
/// <see cref="OperationAuditEntry"/> through the application's <see cref="IOperationAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Isolation</b>: every write runs in its own DI scope with ambient transactions suppressed, so
/// the record never joins the request's unit of work. A denied request rolls its transaction back;
/// its decision record must survive that rollback.
/// </para>
/// <para>
/// <b>Bounded, never linked to the client</b>: the write gets a token of its own, cancelled at
/// <see cref="ABACDecisionAuditOptions.WriteTimeout"/>, and is also raced against that bound, so a
/// store that ignores its token cannot hold the request. The client's token is never linked: a
/// disconnect must not erase the evidence of a denied attempt.
/// </para>
/// <para>
/// <b>Idempotent outcome</b>: the entry id is the decision id. When the write fails, throws or times
/// out, the recorder looks the entry up by correlation id (bounded by the same timeout) and treats a
/// committed entry with that id as written. On stores whose reads lag behind writes (Marten's
/// asynchronous projection) the entry may not be visible yet; the failure then stands, so the result
/// is never a false success.
/// </para>
/// <para>
/// <b>Failures</b>: a store <c>Left</c> is returned as is (the Policy Enforcement Point keeps only
/// its code); an exception, including <see cref="TimeoutException"/> for a write over the bound, is
/// rethrown for the Policy Enforcement Point to log through its redacted form. No
/// <see cref="IOperationAuditStore"/> registered gives <see cref="ABACErrors.DecisionAuditStoreUnavailable"/>.
/// </para>
/// </remarks>
public sealed class AuditStoreABACDecisionRecorder : IABACDecisionRecorder
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ABACOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuditStoreABACDecisionRecorder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditStoreABACDecisionRecorder"/> class.
    /// </summary>
    /// <param name="scopeFactory">Opens the scope each write resolves the operation audit store from.</param>
    /// <param name="options">The ABAC options; <see cref="ABACDecisionAuditOptions.WriteTimeout"/> bounds each write.</param>
    /// <param name="timeProvider">The clock of the write bound.</param>
    /// <param name="logger">The logger.</param>
    public AuditStoreABACDecisionRecorder(
        IServiceScopeFactory scopeFactory,
        IOptions<ABACOptions> options,
        TimeProvider timeProvider,
        ILogger<AuditStoreABACDecisionRecorder> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <paramref name="cancellationToken"/> is checked once before the write starts and is never
    /// linked to the write itself: <see cref="ABACDecisionAuditOptions.WriteTimeout"/> is its only bound.
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ABACDecisionRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(record);
        var timeout = _options.Value.DecisionAudit.WriteTimeout;

        await using var scope = _scopeFactory.CreateAsyncScope();
        using var isolation = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);

        var store = scope.ServiceProvider.GetService<IOperationAuditStore>();
        if (store is null)
        {
            return ABACErrors.DecisionAuditStoreUnavailable();
        }

        var result = await WriteAsync(store, entry, timeout).ConfigureAwait(false);
        isolation.Complete();
        return result;
    }

    private async ValueTask<Either<EncinaError, Unit>> WriteAsync(
        IOperationAuditStore store, OperationAuditEntry entry, TimeSpan timeout)
    {
        Either<EncinaError, Unit> written;

        try
        {
            written = await TimeBoundedCall.RunAsync(
                bound => store.RecordAsync(entry, bound), timeout, _timeProvider).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (await IsStoredAsync(store, entry, timeout, ex.GetType().Name).ConfigureAwait(false))
            {
                return Unit.Default;
            }

            throw;
        }

        return written.IsRight || !await IsStoredAsync(store, entry, timeout, CodeOf(written)).ConfigureAwait(false)
            ? written
            : Unit.Default;
    }

    // An ambiguous failure (the store reported one, threw or timed out after committing) is settled
    // by looking for the entry with the decision id. A failed lookup confirms nothing.
    private async ValueTask<bool> IsStoredAsync(
        IOperationAuditStore store, OperationAuditEntry entry, TimeSpan timeout, string failureCode)
    {
        try
        {
            var found = await TimeBoundedCall.RunAsync(
                bound => store.GetByCorrelationIdAsync(entry.CorrelationId, bound), timeout, _timeProvider).ConfigureAwait(false);
            var stored = found.Match(Right: entries => entries.Any(candidate => candidate.Id == entry.Id), Left: _ => false);
            LogIfStored(stored, entry.Id, failureCode);
            return stored;
        }
        catch (Exception)
        {
            // The lookup is a best-effort confirmation; the original failure is what gets reported.
            return false;
        }
    }

    private void LogIfStored(bool stored, Guid decisionId, string failureCode)
    {
        if (stored)
        {
            ABACLogMessages.DecisionAlreadyStored(_logger, decisionId, failureCode);
        }
    }

    private static string CodeOf(Either<EncinaError, Unit> result) =>
        result.Match(Right: _ => string.Empty, Left: error => error.GetCode().IfNone("encina.unknown"));
}

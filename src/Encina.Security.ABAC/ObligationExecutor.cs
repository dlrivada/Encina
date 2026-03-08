using System.Diagnostics;
using System.Globalization;

using Encina.Security.ABAC.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Security.ABAC;

/// <summary>
/// Executes obligations and advice expressions returned by the PDP, delegating to
/// registered <see cref="IObligationHandler"/> implementations.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.18 — Obligations are mandatory: if a handler for an obligation
/// cannot be found or fails, the PEP <b>must deny access</b> regardless of the PDP decision.
/// </para>
/// <para>
/// Advice expressions are best-effort: failures are logged but do not affect the
/// authorization decision.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var executor = new ObligationExecutor(handlers, logger);
/// var result = await executor.ExecuteObligationsAsync(
///     decision.Obligations, evaluationContext, cancellationToken);
///
/// result.Match(
///     Left: error => /* obligation failed — deny access */,
///     Right: _ => /* all obligations fulfilled — proceed */);
/// </code>
/// </example>
public sealed class ObligationExecutor
{
    private readonly IEnumerable<IObligationHandler> _handlers;
    private readonly ILogger<ObligationExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObligationExecutor"/> class.
    /// </summary>
    /// <param name="handlers">The registered obligation handlers resolved from DI.</param>
    /// <param name="logger">Logger for obligation execution tracing.</param>
    public ObligationExecutor(
        IEnumerable<IObligationHandler> handlers,
        ILogger<ObligationExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(logger);

        _handlers = handlers;
        _logger = logger;
    }

    /// <summary>
    /// Executes all obligations, ensuring each has a registered handler.
    /// </summary>
    /// <param name="obligations">The obligations to execute.</param>
    /// <param name="context">The policy evaluation context for handler use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success; an <see cref="EncinaError"/> if any obligation
    /// handler is missing or fails. Per XACML 3.0 §7.18, any failure means access must be denied.
    /// </returns>
    public async ValueTask<Either<EncinaError, Unit>> ExecuteObligationsAsync(
        IReadOnlyList<Obligation> obligations,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(obligations);
        ArgumentNullException.ThrowIfNull(context);

        if (obligations.Count == 0)
        {
            return unit;
        }

        foreach (var obligation in obligations)
        {
            var handler = FindHandler(obligation.Id);

            if (handler is null)
            {
                ABACLogMessages.ObligationNoHandler(_logger, obligation.Id);

                ABACDiagnostics.ObligationNoHandler.Add(1,
                    new KeyValuePair<string, object?>(ABACDiagnostics.TagObligationId, obligation.Id));

                ABACDiagnostics.ObligationFailed.Add(1,
                    new KeyValuePair<string, object?>(ABACDiagnostics.TagObligationId, obligation.Id));

                return ABACErrors.ObligationFailed(
                    obligation.Id,
                    string.Format(CultureInfo.InvariantCulture,
                        "No handler registered for obligation '{0}'.", obligation.Id));
            }

            var handlerStartTimestamp = Stopwatch.GetTimestamp();

            var result = await handler.HandleAsync(obligation, context, cancellationToken)
                .ConfigureAwait(false);

            var handlerElapsed = Stopwatch.GetElapsedTime(handlerStartTimestamp);
            ABACDiagnostics.ObligationDuration.Record(handlerElapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>(ABACDiagnostics.TagObligationId, obligation.Id));

            var failed = result.Match(
                Left: error =>
                {
                    ABACLogMessages.ObligationHandlerFailed(_logger,
                        obligation.Id,
                        error.Message);

                    ABACDiagnostics.ObligationFailed.Add(1,
                        new KeyValuePair<string, object?>(ABACDiagnostics.TagObligationId, obligation.Id));

                    return true;
                },
                Right: _ =>
                {
                    ABACLogMessages.ObligationExecuted(_logger, obligation.Id);

                    ABACDiagnostics.ObligationExecuted.Add(1,
                        new KeyValuePair<string, object?>(ABACDiagnostics.TagObligationId, obligation.Id));

                    return false;
                });

            if (failed)
            {
                return ABACErrors.ObligationFailed(
                    obligation.Id,
                    string.Format(CultureInfo.InvariantCulture,
                        "Handler for obligation '{0}' returned an error.", obligation.Id));
            }
        }

        ABACLogMessages.AllObligationsExecuted(_logger, obligations.Count);

        return unit;
    }

    /// <summary>
    /// Executes advice expressions on a best-effort basis.
    /// </summary>
    /// <param name="adviceExpressions">The advice expressions to execute.</param>
    /// <param name="context">The policy evaluation context for handler use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Advice is non-mandatory per XACML 3.0. Failures are logged as warnings
    /// but do not affect the authorization decision.
    /// </remarks>
    public async ValueTask ExecuteAdviceAsync(
        IReadOnlyList<AdviceExpression> adviceExpressions,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(adviceExpressions);
        ArgumentNullException.ThrowIfNull(context);

        if (adviceExpressions.Count == 0)
        {
            return;
        }

        foreach (var advice in adviceExpressions)
        {
            var handler = FindHandler(advice.Id);

            if (handler is null)
            {
                ABACLogMessages.AdviceNoHandler(_logger, advice.Id);
                continue;
            }

            // Advice uses the same handler mechanism as obligations,
            // but we wrap it in an Obligation-shaped call for handler compatibility
            var syntheticObligation = new Obligation
            {
                Id = advice.Id,
                FulfillOn = advice.AppliesTo,
                AttributeAssignments = advice.AttributeAssignments
            };

            var adviceStartTimestamp = Stopwatch.GetTimestamp();

            var result = await handler.HandleAsync(syntheticObligation, context, cancellationToken)
                .ConfigureAwait(false);

            var adviceElapsed = Stopwatch.GetElapsedTime(adviceStartTimestamp);
            ABACDiagnostics.ObligationDuration.Record(adviceElapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>(ABACDiagnostics.TagAdviceId, advice.Id));

            result.Match(
                Left: error => ABACLogMessages.AdviceHandlerFailed(_logger,
                    advice.Id,
                    error.Message),
                Right: _ =>
                {
                    ABACLogMessages.AdviceExecuted(_logger, advice.Id);

                    ABACDiagnostics.AdviceExecuted.Add(1,
                        new KeyValuePair<string, object?>(ABACDiagnostics.TagAdviceId, advice.Id));
                });
        }
    }

    // ── Private Helpers ─────────────────────────────────────────────

    private IObligationHandler? FindHandler(string obligationId)
    {
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(obligationId))
            {
                return handler;
            }
        }

        return null;
    }
}

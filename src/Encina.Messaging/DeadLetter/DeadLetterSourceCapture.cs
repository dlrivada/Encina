using Encina.Diagnostics;
using Encina.Messaging.Diagnostics;
using Encina.Messaging.Recoverability;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Captures the terminal failures of the built-in sources (recoverability, outbox, inbox, scheduling and
/// sagas) into the dead letter queue, each one only while its <c>DeadLetterOptions.IntegrateWith*</c> flag is on.
/// </summary>
/// <remarks>
/// <para>
/// Registered by every dead letter queue registration (<c>UseDeadLetterQueue</c>, <c>AddEncinaDeadLetterQueue</c>).
/// The sources take it as an optional dependency, so without the dead letter queue nothing is captured.
/// </para>
/// <para>
/// Every capture runs in a scope of its own: on EF Core the store shares the scoped <c>DbContext</c>, and a
/// capture in the source's scope would save (or be broken by) the source's own tracked changes.
/// </para>
/// <para>
/// A capture is idempotent on <c>(SourcePattern, SourceMessageId)</c>: a repeated terminal event of the same
/// source message keeps the one dead letter. Every method returns <c>Right</c> when the message was captured,
/// was already captured, or the source's flag is off. It returns <c>Left</c> when the capture failed: the store
/// failed, or the capture threw (<see cref="DeadLetterErrorCodes.CaptureFailed"/>), both worth trying again; or the
/// dead letter queue cannot store the message at all (<see cref="DeadLetterErrorCodes.CaptureRejected"/>: its input
/// rules reject an identity value or instant, or the source pattern has no <c>IntegrateWith*</c> flag), which
/// <see cref="IsRetryable"/> reports as not retryable so a source records its terminal state instead of looping.
/// The source decides what a <c>Left</c> means for its message; it never reports success for it. Only the
/// error code or the exception type is logged.
/// </para>
/// </remarks>
public sealed class DeadLetterSourceCapture
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DeadLetterOptions _options;
    private readonly ILogger<DeadLetterSourceCapture> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterSourceCapture"/> class.
    /// </summary>
    /// <param name="scopeFactory">Creates the scope each capture runs in.</param>
    /// <param name="options">The dead letter queue options (the <c>IntegrateWith*</c> flags).</param>
    /// <param name="logger">The logger.</param>
    public DeadLetterSourceCapture(
        IServiceScopeFactory scopeFactory,
        DeadLetterOptions options,
        ILogger<DeadLetterSourceCapture> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Gets whether the terminal failures of <paramref name="sourcePattern"/> are captured.
    /// </summary>
    /// <param name="sourcePattern">One of <see cref="DeadLetterSourcePatterns"/>.</param>
    /// <returns>
    /// The <c>IntegrateWith*</c> flag of a built-in source; <see langword="false"/> for any other pattern.
    /// </returns>
    public bool IsEnabledFor(string sourcePattern) => FlagOf(sourcePattern) ?? false;

    /// <summary>
    /// Gets whether a failed capture can succeed when it is tried again.
    /// </summary>
    /// <param name="error">The <c>Left</c> a capture method returned.</param>
    /// <returns>
    /// <see langword="false"/> for <see cref="DeadLetterErrorCodes.CaptureRejected"/> (the dead letter queue cannot
    /// store this message, so a source records its terminal state instead of trying again);
    /// <see langword="true"/> for any other error, such as a store failure.
    /// </returns>
    public static bool IsRetryable(EncinaError error)
        => !string.Equals(error.GetCode().IfNone(string.Empty), DeadLetterErrorCodes.CaptureRejected, StringComparison.Ordinal);

    // The IntegrateWith* flag of a built-in source; null for any other pattern.
    // crap-exempt: single-question switch — maps a source pattern to its IntegrateWith* flag.
    private bool? FlagOf(string sourcePattern) => sourcePattern switch
    {
        DeadLetterSourcePatterns.Recoverability => _options.IntegrateWithRecoverability,
        DeadLetterSourcePatterns.Outbox => _options.IntegrateWithOutbox,
        DeadLetterSourcePatterns.Inbox => _options.IntegrateWithInbox,
        DeadLetterSourcePatterns.Scheduling => _options.IntegrateWithScheduling,
        DeadLetterSourcePatterns.Saga => _options.IntegrateWithSagas,
        _ => null
    };

    /// <summary>
    /// Captures a failed request object (the request is serialized through <c>IMessageSerializer</c>).
    /// </summary>
    /// <param name="request">The failed request.</param>
    /// <param name="context">The failure; its <see cref="DeadLetterContext.SourcePattern"/> selects the flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>Right</c> when captured, already captured or not enabled; <c>Left</c> when the capture failed.</returns>
    public Task<Either<EncinaError, Unit>> CaptureAsync(
        object request,
        DeadLetterContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        return CaptureInOwnScopeAsync(
            context.SourcePattern,
            orchestrator => orchestrator.AddAsync(request, context, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Captures a failed message from its stored type name and its stored, already serialized content.
    /// </summary>
    /// <param name="requestType">The stored type name of the message.</param>
    /// <param name="requestContent">The stored content, written by <c>IMessageSerializer</c>; kept as is.</param>
    /// <param name="context">The failure; its <see cref="DeadLetterContext.SourcePattern"/> selects the flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>Right</c> when captured, already captured or not enabled; <c>Left</c> when the capture failed.</returns>
    public Task<Either<EncinaError, Unit>> CaptureSerializedAsync(
        string requestType,
        string requestContent,
        DeadLetterContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(requestType);
        ArgumentNullException.ThrowIfNull(requestContent);
        ArgumentNullException.ThrowIfNull(context);

        return CaptureInOwnScopeAsync(
            context.SourcePattern,
            orchestrator => orchestrator.AddSerializedAsync(requestType, requestContent, context, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Captures a message that the recoverability pipeline failed permanently, keyed by
    /// <see cref="FailedMessage.Id"/> (one id per retry chain).
    /// </summary>
    /// <param name="failedMessage">The permanently failed message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>Right</c> when captured, already captured or not enabled; <c>Left</c> when the capture failed.</returns>
    public Task<Either<EncinaError, Unit>> CaptureFailedMessageAsync(
        FailedMessage failedMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(failedMessage);

        return CaptureInOwnScopeAsync(
            DeadLetterSourcePatterns.Recoverability,
            orchestrator => orchestrator.AddFromFailedMessageAsync(
                failedMessage,
                DeadLetterSourcePatterns.Recoverability,
                cancellationToken),
            cancellationToken);
    }

    private async Task<Either<EncinaError, Unit>> CaptureInOwnScopeAsync(
        string sourcePattern,
        Func<DeadLetterOrchestrator, Task<Either<EncinaError, IDeadLetterMessage>>> capture,
        CancellationToken cancellationToken)
    {
        // A pattern with no IntegrateWith* flag is never reported as captured.
        if (FlagOf(sourcePattern) is not { } enabled)
            return Rejected(sourcePattern, exception: null, "No IntegrateWith* flag exists for this source pattern");

        if (!enabled)
            return Unit.Default;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<DeadLetterOrchestrator>();
            var captured = await capture(orchestrator).ConfigureAwait(false);

            return captured.Match(
                Right: _ => Either<EncinaError, Unit>.Right(Unit.Default),
                Left: error => Failed(sourcePattern, error));
        }
        catch (ArgumentException ex)
        {
            // The dead letter queue's input rules reject this message (an identity value too long or with edge
            // white space, a non-UTC instant): trying again cannot succeed.
            return Rejected(sourcePattern, ex, $"Capturing the dead letter was rejected: {ex.GetType().FullName}");
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            // Only the exception type: its message can carry personal data.
            DeadLetterLog.SourceCaptureThrew(_logger, ex.ForLogging(), sourcePattern);
            DeadLetterMetrics.RecordStoreFailure(CaptureOperation, DeadLetterErrorCodes.CaptureFailed);
            return EncinaErrors.Create(
                DeadLetterErrorCodes.CaptureFailed,
                $"Capturing the dead letter threw {ex.GetType().FullName}");
        }
    }

    // The operation name a capture failure is counted under in encina.dlq.store_failures_total; a store Left is
    // already counted by the orchestrator under its own operation.
    private const string CaptureOperation = "capture";

    private Either<EncinaError, Unit> Rejected(string sourcePattern, Exception? exception, string text)
    {
        DeadLetterLog.SourceCaptureRejected(_logger, exception?.ForLogging(), sourcePattern, DeadLetterErrorCodes.CaptureRejected);
        DeadLetterMetrics.RecordStoreFailure(CaptureOperation, DeadLetterErrorCodes.CaptureRejected);
        return EncinaErrors.Create(DeadLetterErrorCodes.CaptureRejected, text);
    }

    private Either<EncinaError, Unit> Failed(string sourcePattern, EncinaError error)
    {
        DeadLetterLog.SourceCaptureFailed(_logger, sourcePattern, error.GetCode().IfNone("encina.unknown"));
        return error;
    }
}

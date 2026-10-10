using Encina.Diagnostics;
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
/// was already captured, or the source's flag is off; it returns <c>Left</c> when the capture failed (the
/// store failed, or the capture threw, which is reported as <see cref="DeadLetterErrorCodes.CaptureFailed"/>).
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
    // crap-exempt: single-question switch — maps a source pattern to its IntegrateWith* flag.
    public bool IsEnabledFor(string sourcePattern) => sourcePattern switch
    {
        DeadLetterSourcePatterns.Recoverability => _options.IntegrateWithRecoverability,
        DeadLetterSourcePatterns.Outbox => _options.IntegrateWithOutbox,
        DeadLetterSourcePatterns.Inbox => _options.IntegrateWithInbox,
        DeadLetterSourcePatterns.Scheduling => _options.IntegrateWithScheduling,
        DeadLetterSourcePatterns.Saga => _options.IntegrateWithSagas,
        _ => false
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
        if (!IsEnabledFor(sourcePattern))
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
        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            // Only the exception type: its message can carry personal data.
            DeadLetterLog.SourceCaptureThrew(_logger, ex.ForLogging(), sourcePattern);
            return EncinaErrors.Create(
                DeadLetterErrorCodes.CaptureFailed,
                $"Capturing the dead letter threw {ex.GetType().FullName}");
        }
    }

    private Either<EncinaError, Unit> Failed(string sourcePattern, EncinaError error)
    {
        DeadLetterLog.SourceCaptureFailed(_logger, sourcePattern, error.GetCode().IfNone("encina.unknown"));
        return error;
    }
}

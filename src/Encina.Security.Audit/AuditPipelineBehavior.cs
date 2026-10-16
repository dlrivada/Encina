using System.Collections.Concurrent;
using System.Reflection;
using System.Transactions;

using Encina.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.Audit;

/// <summary>
/// Pipeline behavior that automatically records audit entries for CQRS requests.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
/// <remarks>
/// <para>
/// This behavior intercepts requests and records audit entries after execution completes.
/// The write is awaited before the behavior returns and runs in a child scope of its own, so it
/// never touches the request's DbContext or connection and survives a request rollback.
/// </para>
/// <para>
/// <b>Failure mode:</b> with <see cref="OperationAuditFailureMode.FailClosed"/> (the default) a request that
/// succeeded returns <see cref="OperationAuditErrors.WriteFailedCode"/> when its audit write fails, or
/// <see cref="OperationAuditErrors.MaskingFailedCode"/> when its payload could not be masked; with
/// <see cref="OperationAuditFailureMode.BestEffort"/> the failure is only logged at Warning. The handler's own
/// <c>Left</c> or exception is never replaced.
/// </para>
/// <para>
/// <b>Auditing Rules:</b>
/// <list type="bullet">
/// <item>Commands are audited by default when <c>OperationAuditOptions.AuditAllCommands</c> is <c>true</c></item>
/// <item>Queries are NOT audited by default unless <c>OperationAuditOptions.AuditAllQueries</c> is <c>true</c></item>
/// <item>Queries with <c>[Auditable]</c> attribute are always audited</item>
/// <item>Requests with <c>[Auditable(Skip = true)]</c> are never audited</item>
/// <item>Requests in the excluded types collection are never audited</item>
/// </list>
/// </para>
/// </remarks>
public sealed partial class AuditPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Cache for ShouldAudit results per request type
    private static readonly ConcurrentDictionary<Type, bool> ShouldAuditCache = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOperationAuditEntryFactory _entryFactory;
    private readonly OperationAuditOptions _options;
    private readonly ILogger<AuditPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="scopeFactory">
    /// Creates the child scope in which each audit write resolves its <see cref="IOperationAuditStore"/>,
    /// so database stores never share the request's DbContext or connection.
    /// </param>
    /// <param name="entryFactory">The factory for creating audit entries.</param>
    /// <param name="options">The audit configuration options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">Optional time provider for the entry timestamps. Defaults to system time.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any required parameter is null.
    /// </exception>
    public AuditPipelineBehavior(
        IServiceScopeFactory scopeFactory,
        IOperationAuditEntryFactory entryFactory,
        IOptions<OperationAuditOptions> options,
        ILogger<AuditPipelineBehavior<TRequest, TResponse>> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(entryFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _entryFactory = entryFactory;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Check if this request should be audited
        if (!ShouldAudit(typeof(TRequest)))
        {
            return await nextStep().ConfigureAwait(false);
        }

        // Capture start time
        var startedAtUtc = _timeProvider.GetUtcNow();

        Either<EncinaError, TResponse> result;

        try
        {
            result = await nextStep().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The handler's cancellation is rethrown unchanged; a failed audit write is only logged.
            _ = await RecordAuditEntryAsync(
                request, default, context, AuditOutcome.Error, "Operation was cancelled", startedAtUtc).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            // The handler's exception is rethrown unchanged; a failed audit write is only logged.
            _ = await RecordAuditEntryAsync(
                request, default, context, AuditOutcome.Error, ex.GetType().Name, startedAtUtc).ConfigureAwait(false);
            throw;
        }

        var (outcome, response, errorCode) = Classify(result);
        var written = await RecordAuditEntryAsync(
            request, response, context, outcome, errorCode, startedAtUtc).ConfigureAwait(false);

        return Reconcile(result, written);
    }

    private static (AuditOutcome Outcome, TResponse? Response, string? ErrorCode) Classify(
        Either<EncinaError, TResponse> result) =>
        result.Match(
            Right: value => (AuditOutcome.Success, (TResponse?)value, (string?)null),
            Left: error => (MapErrorToOutcome(error), default(TResponse), (string?)error.GetCode().IfNone("encina.unknown")));

    /// <summary>
    /// Applies the failure mode: only a request that succeeded is replaced by the audit error, and only
    /// under <see cref="OperationAuditFailureMode.FailClosed"/>; the handler's own Left is never replaced.
    /// </summary>
    private Either<EncinaError, TResponse> Reconcile(
        Either<EncinaError, TResponse> result,
        Either<EncinaError, LanguageExt.Unit> written)
    {
        if (_options.FailureMode != OperationAuditFailureMode.FailClosed || result.IsLeft)
        {
            return result;
        }

        return written.Match<Either<EncinaError, TResponse>>(
            Right: _ => result,
            Left: error => error);
    }

    private bool ShouldAudit(Type requestType)
    {
        return ShouldAuditCache.GetOrAdd(requestType, type => EvaluateShouldAudit(type));
    }

    private bool EvaluateShouldAudit(Type requestType)
    {
        // Check for [Auditable(Skip = true)]
        var attribute = requestType.GetCustomAttribute<AuditableAttribute>();
        if (attribute?.Skip == true)
        {
            return false;
        }

        // Check if type is in excluded types
        if (_options.IsExcluded(requestType))
        {
            return false;
        }

        // Check if it's a command
        if (IsCommand(requestType))
        {
            return _options.AuditAllCommands;
        }

        // Check if it's a query
        if (IsQuery(requestType))
        {
            // Audit if: AuditAllQueries is true, OR has [Auditable] attribute, OR is explicitly included
            return _options.AuditAllQueries ||
                   attribute is not null ||
                   _options.IsQueryIncluded(requestType);
        }

        // Default: audit commands, don't audit other requests
        return false;
    }

    private static bool IsCommand(Type type)
    {
        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
    }

    private static bool IsQuery(Type type)
    {
        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));
    }

    private static AuditOutcome MapErrorToOutcome(EncinaError error)
    {
        // Check message for authorization-related patterns
        var message = error.Message;
        if (message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("not authorized", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("permission denied", StringComparison.OrdinalIgnoreCase))
        {
            return AuditOutcome.Denied;
        }

        // Check message for validation-related patterns
        if (message.Contains("validation", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("required", StringComparison.OrdinalIgnoreCase))
        {
            return AuditOutcome.Failure;
        }

        // Default to Failure for business logic errors (Either.Left typically means business rule violation)
        return AuditOutcome.Failure;
    }

    /// <summary>
    /// Builds and writes the entry, awaited, in a child scope of its own so the store never shares or
    /// outlives the request's DbContext or connection and the write survives a request rollback. The
    /// write deliberately ignores the request's cancellation token: a cancelled request is still audited.
    /// </summary>
    /// <remarks>
    /// The write uses <see cref="CancellationToken.None"/> by design; the store owns its timeout. An ambient
    /// <see cref="System.Transactions.Transaction"/> is suppressed so the entry never enlists in the request's transaction.
    /// </remarks>
    /// <returns>
    /// Right when written; otherwise Left (already logged) with <see cref="OperationAuditErrors.MaskingFailedCode"/>
    /// when the payload could not be masked, or <see cref="OperationAuditErrors.WriteFailedCode"/>.
    /// </returns>
    private async Task<Either<EncinaError, LanguageExt.Unit>> RecordAuditEntryAsync(
        TRequest request,
        TResponse? response,
        IRequestContext context,
        AuditOutcome outcome,
        string? errorMessage,
        DateTimeOffset startedAtUtc)
    {
        var requestType = typeof(TRequest).Name;

        try
        {
            var built = _entryFactory.Create(
                request,
                response,
                context,
                outcome,
                errorMessage,
                startedAtUtc,
                _timeProvider.GetUtcNow());

            // A Left from the factory (a masking failure) means no entry exists: nothing is stored and the
            // factory's error, which carries a code and never a message, is the result.
            return await built.Match(
                Left: error => Task.FromResult(LogFactoryFailure(requestType, error)),
                Right: entry => WriteEntryAsync(requestType, entry)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogAuditRecordingException(_logger, requestType, _options.FailureMode, ex.ForLogging());
            return OperationAuditErrors.WriteFailed(requestType, ex.GetType().Name);
        }
    }

    private Either<EncinaError, LanguageExt.Unit> LogFactoryFailure(string requestType, EncinaError error)
    {
        LogAuditRecordingFailed(_logger, requestType, error.GetCode().IfNone("encina.unknown"), _options.FailureMode);
        return error;
    }

    private async Task<Either<EncinaError, LanguageExt.Unit>> WriteEntryAsync(string requestType, OperationAuditEntry entry)
    {
        // Suppress any ambient System.Transactions scope of the request: the audit row must not
        // enlist in it (it would roll back with the request or escalate to a distributed transaction).
        using var isolation = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IOperationAuditStore>();
        var result = await store.RecordAsync(entry, CancellationToken.None).ConfigureAwait(false);

        return result.Match<Either<EncinaError, LanguageExt.Unit>>(
            Right: unit => unit,
            Left: error =>
            {
                var code = error.GetCode().IfNone("encina.unknown");
                LogAuditRecordingFailed(_logger, requestType, code, _options.FailureMode);
                return OperationAuditErrors.WriteFailed(requestType, code);
            });
    }

    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Warning,
        Message = "Failed to record audit entry for {RequestType}: {ErrorCode} (failure mode {FailureMode})")]
    private static partial void LogAuditRecordingFailed(
        ILogger logger,
        string requestType,
        string errorCode,
        OperationAuditFailureMode failureMode);

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Warning,
        Message = "Exception while recording audit entry for {RequestType} (failure mode {FailureMode})")]
    private static partial void LogAuditRecordingException(
        ILogger logger,
        string requestType,
        OperationAuditFailureMode failureMode,
        Exception exception);

    /// <summary>
    /// Clears the ShouldAudit cache. Primarily for testing.
    /// </summary>
    internal static void ClearCache()
    {
        ShouldAuditCache.Clear();
    }
}

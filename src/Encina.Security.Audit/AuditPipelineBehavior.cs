using System.Collections.Concurrent;
using System.Reflection;
using LanguageExt;
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
/// Audit recording is fire-and-forget to avoid affecting request processing.
/// </para>
/// <para>
/// <b>Auditing Rules:</b>
/// <list type="bullet">
/// <item>Commands are audited by default when <c>AuditOptions.AuditAllCommands</c> is <c>true</c></item>
/// <item>Queries are NOT audited by default unless <c>AuditOptions.AuditAllQueries</c> is <c>true</c></item>
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

    private readonly IAuditStore _auditStore;
    private readonly IAuditEntryFactory _entryFactory;
    private readonly AuditOptions _options;
    private readonly ILogger<AuditPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="auditStore">The store for recording audit entries.</param>
    /// <param name="entryFactory">The factory for creating audit entries.</param>
    /// <param name="options">The audit configuration options.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public AuditPipelineBehavior(
        IAuditStore auditStore,
        IAuditEntryFactory entryFactory,
        IOptions<AuditOptions> options,
        ILogger<AuditPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(entryFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _auditStore = auditStore;
        _entryFactory = entryFactory;
        _options = options.Value;
        _logger = logger;
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

        Either<EncinaError, TResponse> result;
        AuditOutcome outcome = AuditOutcome.Success;
        string? errorMessage = null;

        try
        {
            result = await nextStep().ConfigureAwait(false);

            // Map Either result to audit outcome
            _ = result.Match(
                Right: _ =>
                {
                    outcome = AuditOutcome.Success;
                    return LanguageExt.Unit.Default;
                },
                Left: error =>
                {
                    outcome = MapErrorToOutcome(error);
                    errorMessage = error.Message;
                    return LanguageExt.Unit.Default;
                });
        }
        catch (OperationCanceledException)
        {
            outcome = AuditOutcome.Error;
            errorMessage = "Operation was cancelled";

            // Record cancellation and re-throw
            _ = RecordAuditEntryAsync(request, context, outcome, errorMessage);
            throw;
        }
        catch (Exception ex)
        {
            outcome = AuditOutcome.Error;
            errorMessage = ex.Message;

            // Record exception and re-throw
            _ = RecordAuditEntryAsync(request, context, outcome, errorMessage);
            throw;
        }

        // Fire-and-forget audit recording
        _ = RecordAuditEntryAsync(request, context, outcome, errorMessage);

        return result;
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

    private async Task RecordAuditEntryAsync(
        TRequest request,
        IRequestContext context,
        AuditOutcome outcome,
        string? errorMessage)
    {
        try
        {
            var entry = _entryFactory.Create(request, context, outcome, errorMessage);

            var result = await _auditStore.RecordAsync(entry).ConfigureAwait(false);

            result.Match(
                Right: _ => { }, // Success - nothing to do
                Left: error => LogAuditRecordingFailed(_logger, typeof(TRequest).Name, error.Message));
        }
        catch (Exception ex)
        {
            // Log warning but don't fail the request
            LogAuditRecordingException(_logger, typeof(TRequest).Name, ex);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Failed to record audit entry for {RequestType}: {ErrorMessage}")]
    private static partial void LogAuditRecordingFailed(
        ILogger logger,
        string requestType,
        string errorMessage);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Exception while recording audit entry for {RequestType}")]
    private static partial void LogAuditRecordingException(
        ILogger logger,
        string requestType,
        Exception exception);

    /// <summary>
    /// Clears the ShouldAudit cache. Primarily for testing.
    /// </summary>
    internal static void ClearCache()
    {
        ShouldAuditCache.Clear();
    }
}

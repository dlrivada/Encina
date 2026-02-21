using System.Diagnostics;
using System.Diagnostics.Metrics;
using Encina.Security.Sanitization.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.Sanitization;

/// <summary>
/// Pipeline behavior that sanitizes incoming request properties before handler execution.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior operates as a pre-handler phase:
/// <list type="number">
/// <item><description>Discovers properties decorated with sanitization attributes
/// (e.g., <c>[SanitizeHtml]</c>, <c>[SanitizeSql]</c>, <c>[Sanitize]</c>).</description></item>
/// <item><description>Sanitizes each property value according to its attribute type
/// using the <see cref="SanitizationOrchestrator"/>.</description></item>
/// <item><description>If <see cref="SanitizationOptions.SanitizeAllStringInputs"/> is enabled,
/// also sanitizes all unannotated string properties using the default profile.</description></item>
/// </list>
/// </para>
/// <para>
/// The behavior short-circuits with an error if sanitization fails for a property.
/// </para>
/// <para>
/// <b>Observability</b>: Emits OpenTelemetry traces via <c>Encina.Security.Sanitization</c> ActivitySource
/// and metrics via <c>Encina.Security.Sanitization</c> Meter when enabled via <see cref="SanitizationOptions"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Request with sanitized properties
/// public sealed record CreateArticleCommand(
///     [property: SanitizeHtml] string Title,
///     [property: Sanitize(Profile = "BlogPost")] string Content,
///     [property: SanitizeSql] string SearchTerm
/// ) : ICommand&lt;ArticleId&gt;;
/// </code>
/// </example>
internal sealed class InputSanitizationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly SanitizationOrchestrator _orchestrator;
    private readonly SanitizationOptions _options;
    private readonly ILogger<InputSanitizationPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputSanitizationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="orchestrator">The sanitization orchestrator for property-level operations.</param>
    /// <param name="options">The sanitization configuration options.</param>
    /// <param name="logger">The logger for structured logging.</param>
    public InputSanitizationPipelineBehavior(
        SanitizationOrchestrator orchestrator,
        IOptions<SanitizationOptions> options,
        ILogger<InputSanitizationPipelineBehavior<TRequest, TResponse>> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var requestType = typeof(TRequest);
        var requestTypeName = requestType.Name;

        // Check if there's anything to sanitize
        var attributeProperties = SanitizationPropertyCache.GetProperties(requestType);
        var hasAttributeProperties = attributeProperties.Length > 0;
        var hasAutoSanitize = _options.SanitizeAllStringInputs;

        if (!hasAttributeProperties && !hasAutoSanitize)
        {
            SanitizationLogMessages.InputSanitizationSkipped(_logger, requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        var startedAt = Stopwatch.GetTimestamp();

        // Start tracing if enabled
        using var activity = _options.EnableTracing
            ? SanitizationDiagnostics.StartInputSanitization(requestTypeName)
            : null;

        var propertyCount = hasAutoSanitize
            ? SanitizationPropertyCache.GetStringProperties(requestType).Length
            : attributeProperties.Length;

        if (hasAutoSanitize)
        {
            SanitizationLogMessages.AutoSanitizationStarted(_logger, requestTypeName, propertyCount);
        }
        else
        {
            SanitizationLogMessages.InputSanitizationStarted(_logger, requestTypeName, propertyCount);
        }

        // Perform sanitization
        var sanitizationResult = _orchestrator.Sanitize(request);

        if (sanitizationResult.IsLeft)
        {
            var errorMessage = sanitizationResult.Match(
                Right: _ => string.Empty,
                Left: e => e.Message);

            RecordFailure(activity, startedAt, requestTypeName, errorMessage);
            return sanitizationResult.Match<Either<EncinaError, TResponse>>(
                Right: _ => default!,
                Left: e => e);
        }

        RecordSuccess(activity, startedAt, requestTypeName, propertyCount);

        return await nextStep().ConfigureAwait(false);
    }

    /// <summary>
    /// Records a successful pipeline completion with tracing and metrics.
    /// </summary>
    private void RecordSuccess(Activity? activity, long startedAt, string requestTypeName, int propertyCount)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);

        SanitizationLogMessages.InputSanitizationCompleted(_logger, requestTypeName, elapsed.TotalMilliseconds);

        if (_options.EnableTracing)
        {
            SanitizationDiagnostics.RecordOperationEvent(activity, "Sanitize", propertyCount);
            SanitizationDiagnostics.RecordSuccess(activity);
        }

        if (_options.EnableMetrics)
        {
            var tags = new TagList
            {
                { SanitizationDiagnostics.TagRequestType, requestTypeName },
                { SanitizationDiagnostics.TagOperation, "sanitize" },
                { SanitizationDiagnostics.TagOutcome, "success" }
            };

            SanitizationDiagnostics.OperationsTotal.Add(1, tags);
            SanitizationDiagnostics.PropertiesProcessed.Add(propertyCount, tags);
            SanitizationDiagnostics.OperationDuration.Record(elapsed.TotalMilliseconds, tags);
        }
    }

    /// <summary>
    /// Records a failed pipeline operation with tracing and metrics.
    /// </summary>
    private void RecordFailure(Activity? activity, long startedAt, string requestTypeName, string errorMessage)
    {
        if (_options.EnableTracing)
        {
            SanitizationDiagnostics.RecordFailure(activity, "sanitize", errorMessage);
        }

        if (_options.EnableMetrics)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var tags = new TagList
            {
                { SanitizationDiagnostics.TagRequestType, requestTypeName },
                { SanitizationDiagnostics.TagOperation, "sanitize" },
                { SanitizationDiagnostics.TagOutcome, "failure" }
            };

            SanitizationDiagnostics.OperationsTotal.Add(1, tags);
            SanitizationDiagnostics.FailuresTotal.Add(1, tags);
            SanitizationDiagnostics.OperationDuration.Record(elapsed.TotalMilliseconds, tags);
        }

        _logger.LogWarning("Input sanitization pipeline failed for {RequestType}: {ErrorMessage}",
            requestTypeName, errorMessage);
    }
}

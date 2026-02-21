using System.Diagnostics;
using System.Diagnostics.Metrics;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Attributes;
using Encina.Security.Sanitization.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.Sanitization;

/// <summary>
/// Pipeline behavior that encodes outgoing response properties after handler execution.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior operates as a post-handler phase:
/// <list type="number">
/// <item><description>Invokes the next step in the pipeline to get the response.</description></item>
/// <item><description>Discovers properties on the response decorated with encoding attributes
/// (e.g., <c>[EncodeForHtml]</c>, <c>[EncodeForJavaScript]</c>, <c>[EncodeForUrl]</c>).</description></item>
/// <item><description>Encodes each property value according to its attribute type
/// using the <see cref="IOutputEncoder"/>.</description></item>
/// <item><description>If <see cref="SanitizationOptions.EncodeAllOutputs"/> is enabled,
/// also HTML-encodes all unannotated string properties.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Observability</b>: Emits OpenTelemetry traces via <c>Encina.Security.Sanitization</c> ActivitySource
/// and metrics via <c>Encina.Security.Sanitization</c> Meter when enabled via <see cref="SanitizationOptions"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Response with encoded properties
/// public sealed record ArticleResponse(
///     [property: EncodeForHtml] string Title,
///     [property: EncodeForHtml] string Content,
///     [property: EncodeForJavaScript] string JsonData
/// );
/// </code>
/// </example>
internal sealed class OutputEncodingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IOutputEncoder _encoder;
    private readonly SanitizationOptions _options;
    private readonly ILogger<OutputEncodingPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputEncodingPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="encoder">The output encoder for context-aware encoding operations.</param>
    /// <param name="options">The sanitization configuration options.</param>
    /// <param name="logger">The logger for structured logging.</param>
    public OutputEncodingPipelineBehavior(
        IOutputEncoder encoder,
        IOptions<SanitizationOptions> options,
        ILogger<OutputEncodingPipelineBehavior<TRequest, TResponse>> logger)
    {
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
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
        // Execute the handler first — encoding is a post-handler operation
        var response = await nextStep().ConfigureAwait(false);

        if (response.IsLeft)
        {
            return response;
        }

        var responseType = typeof(TResponse);
        var responseTypeName = responseType.Name;

        // Check if there's anything to encode
        var attributeProperties = EncodingPropertyCache.GetProperties(responseType);
        var hasAttributeProperties = attributeProperties.Length > 0;
        var hasAutoEncode = _options.EncodeAllOutputs;

        if (!hasAttributeProperties && !hasAutoEncode)
        {
            SanitizationLogMessages.OutputEncodingSkipped(_logger, responseTypeName);
            return response;
        }

        var startedAt = Stopwatch.GetTimestamp();

        // Start tracing if enabled
        using var activity = _options.EnableTracing
            ? SanitizationDiagnostics.StartOutputEncoding(responseTypeName)
            : null;

        return response.Match(
            Right: responseValue =>
            {
                var propertyCount = hasAutoEncode
                    ? EncodingPropertyCache.GetStringProperties(responseType).Length
                    : attributeProperties.Length;

                SanitizationLogMessages.OutputEncodingStarted(_logger, responseTypeName, propertyCount);

                var encodingResult = EncodeResponse(responseValue, responseType, responseTypeName);

                if (encodingResult.IsLeft)
                {
                    var errorMessage = encodingResult.Match(
                        Right: _ => string.Empty,
                        Left: e => e.Message);

                    RecordFailure(activity, startedAt, responseTypeName, errorMessage);
                    return encodingResult.Match<Either<EncinaError, TResponse>>(
                        Right: _ => default!,
                        Left: e => e);
                }

                RecordSuccess(activity, startedAt, responseTypeName, propertyCount);
                return (Either<EncinaError, TResponse>)responseValue;
            },
            Left: e => (Either<EncinaError, TResponse>)e);
    }

    /// <summary>
    /// Encodes all applicable properties on the response object.
    /// </summary>
    private Either<EncinaError, Unit> EncodeResponse(
        TResponse response, Type responseType, string responseTypeName)
    {
        if (response is null)
        {
            return LanguageExt.Prelude.Right<EncinaError, Unit>(Unit.Default);
        }

        // Attribute-based encoding
        var properties = EncodingPropertyCache.GetProperties(responseType);

        foreach (var prop in properties)
        {
            var value = prop.Getter(response) as string;
            if (value is null)
            {
                continue;
            }

            try
            {
                var encoded = EncodeProperty(value, prop.Attribute);
                prop.Setter(response, encoded);
            }
            catch (Exception ex)
            {
                SanitizationLogMessages.OutputEncodingPropertyFailed(
                    _logger, prop.Property.Name, responseTypeName, ex.Message);

                return SanitizationErrors.PropertyError(prop.Property.Name, ex);
            }
        }

        // Auto-encode mode: encode all remaining string properties as HTML
        if (_options.EncodeAllOutputs)
        {
            var attributePropertyNames = new System.Collections.Generic.HashSet<string>(
                properties.Select(p => p.Property.Name),
                StringComparer.Ordinal);

            var stringProperties = EncodingPropertyCache.GetStringProperties(responseType);

            foreach (var prop in stringProperties)
            {
                if (attributePropertyNames.Contains(prop.Name))
                {
                    continue;
                }

                var value = prop.GetValue(response) as string;
                if (value is null)
                {
                    continue;
                }

                try
                {
                    var encoded = _encoder.EncodeForHtml(value);
                    prop.SetValue(response, encoded);
                }
                catch (Exception ex)
                {
                    SanitizationLogMessages.OutputEncodingPropertyFailed(
                        _logger, prop.Name, responseTypeName, ex.Message);

                    return SanitizationErrors.PropertyError(prop.Name, ex);
                }
            }
        }

        return LanguageExt.Prelude.Right<EncinaError, Unit>(Unit.Default);
    }

    /// <summary>
    /// Encodes a single property value based on the attribute's encoding context.
    /// </summary>
    private string EncodeProperty(string value, EncodingAttribute attribute)
    {
        return attribute.EncodingContext switch
        {
            EncodingContext.Html => _encoder.EncodeForHtml(value),
            EncodingContext.JavaScript => _encoder.EncodeForJavaScript(value),
            EncodingContext.Url => _encoder.EncodeForUrl(value),
            _ => value
        };
    }

    /// <summary>
    /// Records a successful pipeline completion with tracing and metrics.
    /// </summary>
    private void RecordSuccess(Activity? activity, long startedAt, string responseTypeName, int propertyCount)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);

        SanitizationLogMessages.OutputEncodingCompleted(_logger, responseTypeName, elapsed.TotalMilliseconds);

        if (_options.EnableTracing)
        {
            SanitizationDiagnostics.RecordOperationEvent(activity, "Encode", propertyCount);
            SanitizationDiagnostics.RecordSuccess(activity);
        }

        if (_options.EnableMetrics)
        {
            var tags = new TagList
            {
                { SanitizationDiagnostics.TagRequestType, responseTypeName },
                { SanitizationDiagnostics.TagOperation, "encode" },
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
    private void RecordFailure(Activity? activity, long startedAt, string responseTypeName, string errorMessage)
    {
        if (_options.EnableTracing)
        {
            SanitizationDiagnostics.RecordFailure(activity, "encode", errorMessage);
        }

        if (_options.EnableMetrics)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var tags = new TagList
            {
                { SanitizationDiagnostics.TagRequestType, responseTypeName },
                { SanitizationDiagnostics.TagOperation, "encode" },
                { SanitizationDiagnostics.TagOutcome, "failure" }
            };

            SanitizationDiagnostics.OperationsTotal.Add(1, tags);
            SanitizationDiagnostics.FailuresTotal.Add(1, tags);
            SanitizationDiagnostics.OperationDuration.Record(elapsed.TotalMilliseconds, tags);
        }

        _logger.LogWarning("Output encoding pipeline failed for {ResponseType}: {ErrorMessage}",
            responseTypeName, errorMessage);
    }
}

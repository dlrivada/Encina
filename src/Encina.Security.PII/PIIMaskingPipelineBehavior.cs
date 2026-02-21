using System.Collections.Concurrent;
using System.Reflection;
using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.PII;

/// <summary>
/// Pipeline behavior that masks PII in handler responses before they are returned
/// to the caller.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior operates as a <b>post-handler</b> phase:
/// <list type="number">
/// <item><description>Executes the next handler in the pipeline via <c>nextStep()</c>.</description></item>
/// <item><description>On success (Right), applies PII masking to the response using
/// <see cref="IPIIMasker.MaskObject{T}"/>.</description></item>
/// <item><description>On error (Left), passes the error through unchanged.</description></item>
/// </list>
/// </para>
/// <para>
/// Masking is only applied when <see cref="PIIOptions.MaskInResponses"/> is <c>true</c> (default).
/// When disabled, responses pass through unmasked.
/// </para>
/// <para>
/// If masking fails due to serialization or other errors, the original unmasked response
/// is returned with a warning logged — masking failures never cause request failures.
/// </para>
/// <para>
/// <b>Generic constraint handling:</b>
/// Since <typeparamref name="TResponse"/> does not have a <c>class</c> constraint but
/// <see cref="IPIIMasker.MaskObject{T}"/> requires one, the behavior uses cached reflection
/// to invoke <c>MaskObject</c> at runtime after verifying the response type is a reference type.
/// The <see cref="MethodInfo"/> is cached per response type for amortized zero-allocation lookups.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Response with PII-decorated properties will be masked automatically
/// public sealed record GetUserResponse(
///     string Id,
///     [property: PII(PIIType.Email)] string Email,
///     [property: PII(PIIType.Phone)] string PhoneNumber,
///     [property: PII(PIIType.Name)] string FullName
/// );
///
/// // Pipeline registration ensures masking happens transparently
/// services.AddEncinaPII(options =>
/// {
///     options.MaskInResponses = true; // default
/// });
/// </code>
/// </example>
public sealed class PIIMaskingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> MaskObjectMethods = new();

    private readonly IPIIMasker _masker;
    private readonly PIIOptions _options;
    private readonly ILogger<PIIMaskingPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PIIMaskingPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="masker">The PII masker for response masking.</param>
    /// <param name="options">The PII configuration options.</param>
    /// <param name="logger">The logger for structured logging.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="masker"/>, <paramref name="options"/>, or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public PIIMaskingPipelineBehavior(
        IPIIMasker masker,
        IOptions<PIIOptions> options,
        ILogger<PIIMaskingPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(masker);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _masker = masker;
        _options = options.Value;
        _logger = logger;
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

        var responseTypeName = typeof(TResponse).Name;

        // Execute the next handler in the pipeline
        var result = await nextStep().ConfigureAwait(false);

        // Skip masking if disabled, if the result is an error, or if response is a value type
        if (!_options.MaskInResponses)
        {
            PIILogMessages.PipelineMaskingSkipped(_logger, responseTypeName, "MaskInResponses is disabled");
            return result;
        }

        if (result.IsLeft)
        {
            PIILogMessages.PipelineMaskingSkipped(_logger, responseTypeName, "Result is error");
            return result;
        }

        if (!typeof(TResponse).IsClass)
        {
            PIILogMessages.PipelineMaskingSkipped(_logger, responseTypeName, "Response is value type");
            return result;
        }

        // Apply PII masking to the successful response
        return result.Match(
            Right: response => MaskResponse(response),
            Left: error => (Either<EncinaError, TResponse>)error);
    }

    /// <summary>
    /// Masks PII in the response object, returning the original on failure.
    /// </summary>
    private Either<EncinaError, TResponse> MaskResponse(TResponse response)
    {
        if (response is null)
        {
            return response!;
        }

        var responseTypeName = typeof(TResponse).Name;

        try
        {
            // IPIIMasker.MaskObject<T> requires 'where T : class' constraint,
            // but TResponse does not carry this constraint from IPipelineBehavior.
            // Since we've verified typeof(TResponse).IsClass above, we use cached
            // reflection to invoke MaskObject<TResponse> safely at runtime.
            var method = MaskObjectMethods.GetOrAdd(typeof(TResponse), static type =>
                typeof(IPIIMasker).GetMethod(nameof(IPIIMasker.MaskObject))!
                    .MakeGenericMethod(type));

            var maskedResponse = (TResponse)method.Invoke(_masker, [response])!;

            // Structured log and metrics for successful pipeline masking
            PIILogMessages.PipelineMaskingApplied(_logger, responseTypeName);

            if (_options.EnableMetrics)
            {
                PIIDiagnostics.RecordPipelineMetrics(responseTypeName, "success");
            }

            return maskedResponse;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not OutOfMemoryException)
        {
            // Masking failures should never cause request failures.
            // Log warning and return the original response.
            PIILogMessages.PipelineMaskingFailed(_logger, ex.InnerException!, responseTypeName);

            if (_options.EnableMetrics)
            {
                PIIDiagnostics.RecordPipelineMetrics(responseTypeName, "failure");
                PIIDiagnostics.RecordErrorMetric(ex.InnerException!.GetType().Name);
            }

            return response;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            PIILogMessages.PipelineMaskingFailed(_logger, ex, responseTypeName);

            if (_options.EnableMetrics)
            {
                PIIDiagnostics.RecordPipelineMetrics(responseTypeName, "failure");
                PIIDiagnostics.RecordErrorMetric(ex.GetType().Name);
            }

            return response;
        }
    }
}

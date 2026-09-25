using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using Encina.Compliance.Consent.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Consent;

/// <summary>
/// Pipeline behavior that enforces consent requirements declared via <see cref="RequireConsentAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior inspects the request type for the <see cref="RequireConsentAttribute"/> and
/// enforces consent validation before the request handler executes:
/// </para>
/// <list type="number">
/// <item><description>Detects <see cref="RequireConsentAttribute"/> (cached per request type).</description></item>
/// <item><description>Extracts the data subject identifier from the request or context.</description></item>
/// <item><description>Validates consent via <see cref="IConsentValidator"/> for all required purposes.</description></item>
/// <item><description>Blocks, warns, or skips based on <see cref="ConsentOptions.EnforcementMode"/>.</description></item>
/// </list>
/// <para>
/// The behavior supports three enforcement modes via <see cref="ConsentEnforcementMode"/>:
/// <see cref="ConsentEnforcementMode.Block"/> returns an error for missing consent,
/// <see cref="ConsentEnforcementMode.Warn"/> logs warnings but allows processing,
/// <see cref="ConsentEnforcementMode.Disabled"/> skips validation entirely.
/// </para>
/// <para>
/// <b>Observability:</b> Emits OpenTelemetry traces via <c>Encina.Compliance.Consent</c> ActivitySource,
/// metrics via <c>Encina.Compliance.Consent</c> Meter, and structured log messages via <see cref="ILogger"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequireConsent(ConsentPurposes.Marketing)]
/// public sealed record SendNewsletterCommand(string UserId) : ICommand&lt;Unit&gt;;
///
/// [RequireConsent(ConsentPurposes.Analytics, ConsentPurposes.Personalization,
///     SubjectIdProperty = "CustomerId")]
/// public sealed record TrackBehaviorCommand(string CustomerId) : ICommand&lt;Unit&gt;;
/// </code>
/// </example>
public sealed class ConsentRequiredPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, RequireConsentAttribute?> AttributeCache = new();
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> PropertyCache = new();

    private readonly IConsentValidator _validator;
    private readonly ConsentOptions _options;
    private readonly ILogger<ConsentRequiredPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentRequiredPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validator">The consent validator for checking consent status.</param>
    /// <param name="options">Consent configuration options.</param>
    /// <param name="logger">Logger for structured consent compliance logging.</param>
    public ConsentRequiredPipelineBehavior(
        IConsentValidator validator,
        IOptions<ConsentOptions> options,
        ILogger<ConsentRequiredPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _validator = validator;
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
        ArgumentNullException.ThrowIfNull(nextStep);

        var requestType = typeof(TRequest);
        var requestTypeName = requestType.Name;

        // Step 1: Check enforcement mode — if disabled, skip entirely
        if (_options.EnforcementMode == ConsentEnforcementMode.Disabled)
        {
            _logger.ConsentEnforcementDisabled(requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        // Step 2: Check for [RequireConsent] attribute (cached)
        var attribute = AttributeCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttribute<RequireConsentAttribute>());

        // No attribute — skip entirely
        if (attribute is null)
        {
            _logger.ConsentCheckSkipped(requestTypeName);
            ConsentDiagnostics.ConsentCheckSkipped.Add(1, new TagList
            {
                { ConsentDiagnostics.TagRequestType, requestTypeName }
            });
            return await nextStep().ConfigureAwait(false);
        }

        // Step 3: Extract subject ID
        var subjectId = ExtractSubjectId(request, requestType, attribute, context);

        if (string.IsNullOrEmpty(subjectId))
        {
            var error = ConsentErrors.MissingConsent(subjectId ?? "unknown", string.Join(", ", attribute.Purposes));
            _logger.ConsentCheckFailed(requestTypeName, "Subject ID could not be resolved");
            return Left<EncinaError, TResponse>(error);
        }

        // Step 4: Start tracing and logging. Neither the activity, the logs nor the metrics below
        // carry the resolved subjectId — it is the data subject's own identifier and must never
        // reach an observability sink in plain text (#1314).
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = ConsentDiagnostics.StartConsentCheck(requestTypeName);
        activity?.SetTag(ConsentDiagnostics.TagPurpose, string.Join(",", attribute.Purposes));
        activity?.SetTag(ConsentDiagnostics.TagEnforcementMode, _options.EnforcementMode.ToString());
        _logger.ConsentCheckStarted(requestTypeName);

        // Step 5: Validate consent for all required purposes
        var validationResult = await _validator
            .ValidateAsync(subjectId, attribute.Purposes, cancellationToken)
            .ConfigureAwait(false);

        // Handle validator infrastructure errors
        if (validationResult.IsLeft)
        {
            var validatorError = (EncinaError)validationResult;
            RecordFailed(activity, startedAt, requestTypeName, "validation_error");
            return Left<EncinaError, TResponse>(validatorError);
        }

        var result = (ConsentValidationResult)validationResult;

        // Step 6: Handle invalid consent
        if (!result.IsValid)
        {
            // Log each missing purpose
            foreach (var purpose in result.MissingPurposes)
            {
                _logger.ConsentMissing(purpose, requestTypeName);
            }

            if (_options.EnforcementMode == ConsentEnforcementMode.Block)
            {
                var errorMessage = attribute.ErrorMessage
                    ?? $"Consent missing for required purpose(s): {string.Join(", ", result.MissingPurposes)}";

                _logger.ConsentCheckFailed(requestTypeName, errorMessage);

                var error = ConsentErrors.MissingConsent(subjectId, string.Join(", ", result.MissingPurposes));
                RecordFailed(activity, startedAt, requestTypeName, ConsentErrors.MissingConsentCode);
                return Left<EncinaError, TResponse>(error);
            }

            // Warn mode — log but proceed
            foreach (var errorMsg in result.Errors)
            {
                _logger.ConsentWarning(requestTypeName, errorMsg);
            }
        }

        // Step 7: Log warnings from valid-with-warnings result
        foreach (var warning in result.Warnings)
        {
            _logger.ConsentWarning(requestTypeName, warning);
        }

        // Step 8: Record success and proceed
        RecordPassed(activity, startedAt, requestTypeName);
        _logger.ConsentCheckPassed(requestTypeName);
        return await nextStep().ConfigureAwait(false);
    }

    private static string? ExtractSubjectId(
        TRequest request,
        Type requestType,
        RequireConsentAttribute attribute,
        IRequestContext context)
    {
        // If SubjectIdProperty is specified, use cached reflection. The property value is converted
        // by SubjectIdConversion (string, Guid, integer ids, IFormattable strongly-typed ids, or
        // wrappers exposing a primitive 'Value'); an unsupported type throws. A property whose value
        // is null, Guid.Empty or empty is a missing subject (the check below fails closed), never a
        // reason to use the caller's id (project history: #1149). A configured SubjectIdProperty
        // that names no public instance property is a configuration error and throws.
        if (attribute.SubjectIdProperty is not null)
        {
            var cacheKey = (requestType, attribute.SubjectIdProperty);
            var property = PropertyCache.GetOrAdd(cacheKey, static key =>
                key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance));

            if (property is null)
            {
                throw new InvalidOperationException(
                    $"[RequireConsent(SubjectIdProperty = \"{attribute.SubjectIdProperty}\")] on '{requestType.FullName}' " +
                    $"names a property that does not exist. Declare a public instance property " +
                    $"'{attribute.SubjectIdProperty}' on the request or fix the attribute.");
            }

            return SubjectIdConversion.ToInvariantString(property.GetValue(request), property);
        }

        // No SubjectIdProperty configured: use the authenticated caller
        return context.UserId;
    }

    // Neither tag list below carries the subject id: metric tags are high-cardinality and would
    // multiply per data subject, and both are personal data that must never reach an
    // observability sink (#1314).
    private static void RecordPassed(Activity? activity, long startedAt, string requestTypeName)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var tags = new TagList
        {
            { ConsentDiagnostics.TagRequestType, requestTypeName }
        };

        ConsentDiagnostics.ConsentCheckTotal.Add(1, tags);
        ConsentDiagnostics.ConsentCheckPassed.Add(1, tags);
        ConsentDiagnostics.ConsentCheckDuration.Record(elapsed.TotalMilliseconds, tags);
        ConsentDiagnostics.RecordPassed(activity);
    }

    private static void RecordFailed(Activity? activity, long startedAt, string requestTypeName, string failureReason)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var tags = new TagList
        {
            { ConsentDiagnostics.TagRequestType, requestTypeName },
            { ConsentDiagnostics.TagFailureReason, failureReason }
        };

        ConsentDiagnostics.ConsentCheckTotal.Add(1, tags);
        ConsentDiagnostics.ConsentCheckFailed.Add(1, tags);
        ConsentDiagnostics.ConsentCheckDuration.Record(elapsed.TotalMilliseconds, tags);
        ConsentDiagnostics.RecordFailed(activity, failureReason);
    }
}

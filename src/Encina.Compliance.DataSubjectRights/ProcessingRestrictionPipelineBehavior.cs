using System.Diagnostics;
using System.Reflection;

using Encina.Compliance.DataSubjectRights.Diagnostics;
using Encina.Compliance.GDPR;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Pipeline behavior that checks for active processing restrictions (Article 18) before allowing
/// requests that process personal data to proceed.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// Per GDPR Article 18(2), while a processing restriction is active for a data subject,
/// personal data may only be stored — not processed — except with the data subject's consent,
/// for legal claims, for protecting rights of another person, or for reasons of important
/// public interest.
/// </para>
/// <para>
/// This behavior detects requests that process personal data by checking for one of:
/// <see cref="RestrictProcessingAttribute"/>, <see cref="ProcessesPersonalDataAttribute"/>,
/// or <see cref="ProcessingActivityAttribute"/> on the request type.
/// </para>
/// <para>
/// The enforcement mode is controlled by <see cref="DataSubjectRightsOptions.RestrictionEnforcementMode"/>:
/// <see cref="DSREnforcementMode.Block"/> returns an error for restricted subjects,
/// <see cref="DSREnforcementMode.Warn"/> logs a warning but allows processing,
/// <see cref="DSREnforcementMode.Disabled"/> skips restriction checks entirely.
/// </para>
/// <para>
/// <b>Attribute resolution:</b> Each closed generic type resolves its attribute info exactly once
/// via a <c>static readonly</c> field. <see cref="RestrictProcessingAttribute"/> provides the
/// most specific subject ID extraction configuration, but any of the three attributes
/// triggers restriction checking.
/// </para>
/// <para>
/// <b>Subject ID extraction:</b> The behavior first attempts to read the property specified by
/// <see cref="RestrictProcessingAttribute.SubjectIdProperty"/> via reflection. If no property
/// is specified or the attribute is not <see cref="RestrictProcessingAttribute"/>, the behavior
/// falls back to the registered <see cref="IDataSubjectIdExtractor"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Request with explicit subject ID property
/// [RestrictProcessing(SubjectIdProperty = nameof(CustomerId))]
/// public record UpdateCustomerProfileCommand(string CustomerId, string NewEmail) : ICommand;
///
/// // Request using default subject ID extraction
/// [ProcessesPersonalData]
/// public record SendMarketingEmailCommand(string SubjectId) : ICommand;
///
/// // Request with full processing activity declaration
/// [ProcessingActivity(
///     Purpose = "Order fulfillment",
///     LawfulBasis = LawfulBasis.Contract,
///     DataCategories = new[] { "Name", "Email" },
///     DataSubjects = new[] { "Customers" },
///     RetentionDays = 2555)]
/// public record ProcessOrderCommand(string CustomerId) : ICommand&lt;OrderId&gt;;
/// </code>
/// </example>
public sealed class ProcessingRestrictionPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Static per-generic-type attribute info. Each closed generic type (e.g.,
    /// <c>ProcessingRestrictionPipelineBehavior&lt;UpdateCustomerCommand, Unit&gt;</c>)
    /// resolves its own attribute info exactly once via the CLR's static field guarantee.
    /// </summary>
    private static readonly RestrictionAttributeInfo? CachedAttributeInfo = ResolveAttributeInfo();

    private readonly IDSRRequestStore _requestStore;
    private readonly IDataSubjectIdExtractor _subjectIdExtractor;
    private readonly DataSubjectRightsOptions _options;
    private readonly ILogger<ProcessingRestrictionPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessingRestrictionPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="requestStore">The DSR request store for checking active restrictions.</param>
    /// <param name="subjectIdExtractor">The subject ID extractor for identifying the data subject.</param>
    /// <param name="options">DSR configuration options controlling enforcement mode.</param>
    /// <param name="logger">Logger for structured restriction check logging.</param>
    public ProcessingRestrictionPipelineBehavior(
        IDSRRequestStore requestStore,
        IDataSubjectIdExtractor subjectIdExtractor,
        IOptions<DataSubjectRightsOptions> options,
        ILogger<ProcessingRestrictionPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(requestStore);
        ArgumentNullException.ThrowIfNull(subjectIdExtractor);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _requestStore = requestStore;
        _subjectIdExtractor = subjectIdExtractor;
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

        var requestTypeName = typeof(TRequest).Name;

        // Step 1: Disabled mode — no-op, no logging, no metrics
        if (_options.RestrictionEnforcementMode == DSREnforcementMode.Disabled)
        {
            _logger.RestrictionCheckDisabled(requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        var attrInfo = CachedAttributeInfo;

        // Step 2: No personal data attributes at all — skip entirely
        if (attrInfo is null)
        {
            _logger.RestrictionCheckNoAttributes(requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        using var activity = DataSubjectRightsDiagnostics.StartRestrictionCheck(requestTypeName);

        // Step 3: Extract subject ID
        var subjectId = ExtractSubjectId(request, context, attrInfo);

        // Step 4: If no subject ID — can't check restriction, proceed
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            _logger.SubjectIdNotExtracted(requestTypeName);
            DataSubjectRightsDiagnostics.RecordSkipped(activity);
            DataSubjectRightsDiagnostics.RestrictionChecksTotal.Add(1,
                new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagOutcome, "skipped"));
            return await nextStep().ConfigureAwait(false);
        }

        // Step 5: Check for active restriction
        var restrictionResult = await _requestStore
            .HasActiveRestrictionAsync(subjectId, cancellationToken)
            .ConfigureAwait(false);

        return await restrictionResult.MatchAsync(
            RightAsync: async hasRestriction =>
            {
                if (!hasRestriction)
                {
                    // Step 8: Not restricted — proceed normally
                    DataSubjectRightsDiagnostics.RecordCompleted(activity);
                    DataSubjectRightsDiagnostics.RestrictionChecksTotal.Add(1,
                        new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagOutcome, "allowed"));
                    return await nextStep().ConfigureAwait(false);
                }

                // Step 6/7: Active restriction found — apply enforcement
                var error = DSRErrors.RestrictionActive(subjectId);
                var enforcementResult = ApplyEnforcement(activity, error, subjectId);

                if (enforcementResult.IsLeft)
                {
                    return Left<EncinaError, TResponse>((EncinaError)enforcementResult);
                }

                // Warn mode — continue despite restriction
                return await nextStep().ConfigureAwait(false);
            },
            LeftAsync: async error =>
            {
                // Store error — log and proceed (fail-open to avoid blocking all requests)
                _logger.RestrictionCheckStoreError(subjectId, requestTypeName, error.Message);
                DataSubjectRightsDiagnostics.RecordFailed(activity, error.Message);
                DataSubjectRightsDiagnostics.RestrictionChecksTotal.Add(1,
                    new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagOutcome, "error"));
                return await nextStep().ConfigureAwait(false);
            }).ConfigureAwait(false);
    }

    // ================================================================
    // Subject ID extraction
    // ================================================================

    private string? ExtractSubjectId(TRequest request, IRequestContext context, RestrictionAttributeInfo attrInfo)
    {
        // Priority 1: Explicit SubjectIdProperty from [RestrictProcessing] attribute
        if (attrInfo.SubjectIdProperty is { Length: > 0 } propertyName)
        {
            var property = typeof(TRequest).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property is not null)
            {
                var value = property.GetValue(request);
                if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                {
                    return stringValue;
                }
            }
        }

        // Priority 2: Fallback to registered IDataSubjectIdExtractor
        return _subjectIdExtractor.ExtractSubjectId(request, context);
    }

    // ================================================================
    // Enforcement
    // ================================================================

    private Either<EncinaError, bool> ApplyEnforcement(Activity? activity, EncinaError error, string subjectId)
    {
        var requestTypeName = typeof(TRequest).Name;

        if (_options.RestrictionEnforcementMode == DSREnforcementMode.Block)
        {
            // Step 6: Block mode — return error
            _logger.RestrictionBlocked(subjectId, requestTypeName);
            DataSubjectRightsDiagnostics.RecordBlocked(activity, subjectId);
            DataSubjectRightsDiagnostics.RestrictionChecksTotal.Add(1,
                new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagOutcome, "blocked"));
            return Left<EncinaError, bool>(error);
        }

        // Step 7: Warn mode — log warning but allow processing
        _logger.RestrictionWarned(subjectId, requestTypeName);
        DataSubjectRightsDiagnostics.RecordWarned(activity, subjectId);
        DataSubjectRightsDiagnostics.RestrictionChecksTotal.Add(1,
            new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagOutcome, "warned"));
        return Right<EncinaError, bool>(true);
    }

    // ================================================================
    // Attribute resolution
    // ================================================================

    private static RestrictionAttributeInfo? ResolveAttributeInfo()
    {
        var requestType = typeof(TRequest);
        var restrictAttr = requestType.GetCustomAttribute<RestrictProcessingAttribute>();
        var processesPersonalData = requestType.GetCustomAttribute<ProcessesPersonalDataAttribute>() is not null;
        var processingActivity = requestType.GetCustomAttribute<ProcessingActivityAttribute>() is not null;

        // No DSR or GDPR attributes at all — null signals "skip entirely"
        if (restrictAttr is null && !processesPersonalData && !processingActivity)
        {
            return null;
        }

        // Determine the source for diagnostics
        var source = restrictAttr is not null
            ? "RestrictProcessingAttribute"
            : processesPersonalData
                ? "ProcessesPersonalDataAttribute"
                : "ProcessingActivityAttribute";

        // SubjectIdProperty is only available from [RestrictProcessing]
        var subjectIdProperty = restrictAttr?.SubjectIdProperty;

        return new RestrictionAttributeInfo(subjectIdProperty, source);
    }

    /// <summary>
    /// Cached attribute information for a request type's restriction-related declarations.
    /// </summary>
    /// <param name="SubjectIdProperty">
    /// The name of the property on the request that contains the data subject identifier,
    /// or <c>null</c> when the <see cref="IDataSubjectIdExtractor"/> should be used.
    /// Only populated from <see cref="RestrictProcessingAttribute.SubjectIdProperty"/>.
    /// </param>
    /// <param name="Source">
    /// Describes which attribute triggered the restriction check (for diagnostics).
    /// </param>
    private sealed record RestrictionAttributeInfo(string? SubjectIdProperty, string Source);
}

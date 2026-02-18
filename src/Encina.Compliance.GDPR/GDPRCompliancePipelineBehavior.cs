using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using Encina.Compliance.GDPR.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Pipeline behavior that enforces GDPR compliance requirements declared via attributes.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior inspects the request type for GDPR-related attributes and enforces
/// compliance checks before the request is handled:
/// </para>
/// <list type="number">
/// <item><description>Detects <see cref="ProcessingActivityAttribute"/> and <see cref="ProcessesPersonalDataAttribute"/>.</description></item>
/// <item><description>Verifies the processing activity is registered in the <see cref="IProcessingActivityRegistry"/> (Article 30 RoPA).</description></item>
/// <item><description>Validates that a lawful basis is declared (Article 6(1)).</description></item>
/// <item><description>Runs the <see cref="IGDPRComplianceValidator"/> for additional compliance checks.</description></item>
/// <item><description>Logs processing activity for accountability (Article 5(2)).</description></item>
/// </list>
/// <para>
/// The behavior supports two enforcement modes via <see cref="GDPROptions.EnforcementMode"/>:
/// <see cref="GDPREnforcementMode.Enforce"/> blocks non-compliant requests,
/// <see cref="GDPREnforcementMode.WarnOnly"/> logs warnings but allows processing to continue.
/// </para>
/// <para>
/// <b>Observability:</b> Emits OpenTelemetry traces via <c>Encina.Compliance.GDPR</c> ActivitySource,
/// metrics via <c>Encina.Compliance.GDPR</c> Meter, and structured log messages via <see cref="ILogger"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Request with GDPR processing activity declaration
/// [ProcessingActivity(
///     Purpose = "Order fulfillment",
///     LawfulBasis = LawfulBasis.Contract,
///     DataCategories = new[] { "Name", "Email", "Address" },
///     DataSubjects = new[] { "Customers" },
///     RetentionDays = 2555)]
/// public sealed record CreateOrderCommand(OrderData Data) : ICommand&lt;OrderId&gt;;
///
/// // Simple PII marker (requires registered activity)
/// [ProcessesPersonalData]
/// public sealed record UpdateProfileCommand(ProfileData Data) : ICommand;
/// </code>
/// </example>
public sealed class GDPRCompliancePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, GDPRAttributeInfo?> AttributeCache = new();

    private readonly IProcessingActivityRegistry _registry;
    private readonly IGDPRComplianceValidator _validator;
    private readonly GDPROptions _options;
    private readonly ILogger<GDPRCompliancePipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDPRCompliancePipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="registry">The processing activity registry for RoPA lookups.</param>
    /// <param name="validator">The compliance validator for additional checks.</param>
    /// <param name="options">GDPR configuration options.</param>
    /// <param name="logger">Logger for structured GDPR compliance logging.</param>
    public GDPRCompliancePipelineBehavior(
        IProcessingActivityRegistry registry,
        IGDPRComplianceValidator validator,
        IOptions<GDPROptions> options,
        ILogger<GDPRCompliancePipelineBehavior<TRequest, TResponse>> logger)
    {
        _registry = registry;
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

        var requestType = typeof(TRequest);
        var requestTypeName = requestType.Name;

        // Step 1: Check for GDPR attributes (cached)
        var attrInfo = AttributeCache.GetOrAdd(requestType, static type =>
        {
            var processingAttr = type.GetCustomAttribute<ProcessingActivityAttribute>();
            var personalDataAttr = type.GetCustomAttribute<ProcessesPersonalDataAttribute>();

            if (processingAttr is null && personalDataAttr is null)
            {
                return null;
            }

            return new GDPRAttributeInfo(processingAttr, personalDataAttr is not null);
        });

        // No GDPR attributes — skip entirely
        if (attrInfo is null)
        {
            _logger.ComplianceCheckSkipped(requestTypeName);
            GDPRDiagnostics.ComplianceCheckSkipped.Add(1, new TagList
            {
                { GDPRDiagnostics.TagRequestType, requestTypeName }
            });
            return await nextStep().ConfigureAwait(false);
        }

        // Step 2: Start tracing and logging
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = GDPRDiagnostics.StartComplianceCheck(requestTypeName);
        _logger.ComplianceCheckStarted(requestTypeName);

        // Step 3: Look up the processing activity in the registry
        var lookupResult = await _registry
            .GetActivityByRequestTypeAsync(requestType, cancellationToken)
            .ConfigureAwait(false);

        // Handle registry errors
        if (lookupResult.IsLeft)
        {
            var registryError = GDPRErrors.RegistryLookupFailed(requestType, (EncinaError)lookupResult);
            RecordFailed(activity, startedAt, requestTypeName, GDPRErrors.RegistryLookupFailedCode);
            return Left<EncinaError, TResponse>(registryError);
        }

        var activityOption = (Option<ProcessingActivity>)lookupResult;

        // Step 4: Check if the processing activity is registered
        if (activityOption.IsNone)
        {
            _logger.UnregisteredActivity(requestTypeName);

            if (_options.BlockUnregisteredProcessing)
            {
                var error = GDPRErrors.UnregisteredActivity(requestType);
                RecordFailed(activity, startedAt, requestTypeName, GDPRErrors.UnregisteredActivityCode);
                return Left<EncinaError, TResponse>(error);
            }
        }

        // Step 5: Set lawful basis tag for observability
        activityOption.IfSome(pa => GDPRDiagnostics.SetLawfulBasis(activity, pa.LawfulBasis));

        // Step 6: Run compliance validator
        var validationResult = await _validator
            .ValidateAsync(request, context, cancellationToken)
            .ConfigureAwait(false);

        // Handle validator errors
        if (validationResult.IsLeft)
        {
            var validatorError = (EncinaError)validationResult;
            RecordFailed(activity, startedAt, requestTypeName, GDPRErrors.ComplianceValidationFailedCode);
            return Left<EncinaError, TResponse>(validatorError);
        }

        var complianceResult = (ComplianceResult)validationResult;

        // Step 7: Handle non-compliant result
        if (!complianceResult.IsCompliant)
        {
            _logger.ComplianceCheckFailed(requestTypeName, string.Join("; ", complianceResult.Errors));

            if (_options.EnforcementMode == GDPREnforcementMode.Enforce)
            {
                var error = GDPRErrors.ComplianceValidationFailed(requestType, complianceResult.Errors);
                RecordFailed(activity, startedAt, requestTypeName, GDPRErrors.ComplianceValidationFailedCode);
                return Left<EncinaError, TResponse>(error);
            }

            // WarnOnly mode — log but proceed
            foreach (var errorMsg in complianceResult.Errors)
            {
                _logger.ComplianceWarning(requestTypeName, errorMsg);
            }
        }

        // Step 8: Log warnings from compliant-with-warnings result
        foreach (var warning in complianceResult.Warnings)
        {
            _logger.ComplianceWarning(requestTypeName, warning);
        }

        // Step 9: Log processing for accountability (Article 5(2))
        activityOption.IfSome(pa => _logger.ProcessingActivityLogged(
            requestTypeName,
            pa.Purpose,
            pa.LawfulBasis.ToString()));

        // Step 10: Record success and proceed
        RecordPassed(activity, startedAt, requestTypeName);
        return await nextStep().ConfigureAwait(false);
    }

    private void RecordPassed(Activity? activity, long startedAt, string requestTypeName)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var tags = new TagList
        {
            { GDPRDiagnostics.TagRequestType, requestTypeName }
        };

        GDPRDiagnostics.ComplianceCheckTotal.Add(1, tags);
        GDPRDiagnostics.ComplianceCheckPassed.Add(1, tags);
        GDPRDiagnostics.ComplianceCheckDuration.Record(elapsed.TotalMilliseconds, tags);
        GDPRDiagnostics.RecordPassed(activity);
        _logger.ComplianceCheckPassed(requestTypeName, "Passed");
    }

    private void RecordFailed(Activity? activity, long startedAt, string requestTypeName, string failureReason)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var tags = new TagList
        {
            { GDPRDiagnostics.TagRequestType, requestTypeName },
            { GDPRDiagnostics.TagFailureReason, failureReason }
        };

        GDPRDiagnostics.ComplianceCheckTotal.Add(1, tags);
        GDPRDiagnostics.ComplianceCheckFailed.Add(1, tags);
        GDPRDiagnostics.ComplianceCheckDuration.Record(elapsed.TotalMilliseconds, tags);
        GDPRDiagnostics.RecordFailed(activity, failureReason);
        _logger.ComplianceCheckFailed(requestTypeName, failureReason);
    }

    /// <summary>
    /// Cached GDPR attribute information for a request type.
    /// </summary>
    private sealed record GDPRAttributeInfo(
        ProcessingActivityAttribute? ProcessingActivity,
        bool HasProcessesPersonalData);
}

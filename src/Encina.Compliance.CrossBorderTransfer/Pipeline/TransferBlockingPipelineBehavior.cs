using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Attributes;
using Encina.Compliance.CrossBorderTransfer.Diagnostics;
using Encina.Compliance.CrossBorderTransfer.Errors;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Modules.Isolation;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.CrossBorderTransfer.Pipeline;

/// <summary>
/// Pipeline behavior that enforces cross-border transfer compliance declared via
/// <see cref="RequiresCrossBorderTransferAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior inspects the request type for the <see cref="RequiresCrossBorderTransferAttribute"/>
/// and enforces transfer validation before the request handler executes:
/// </para>
/// <list type="number">
/// <item><description>Checks enforcement mode — if disabled, skips entirely.</description></item>
/// <item><description>Detects <see cref="RequiresCrossBorderTransferAttribute"/> (cached per request type).</description></item>
/// <item><description>Extracts source and destination country codes from the attribute or request properties.</description></item>
/// <item><description>Validates the transfer via <see cref="ITransferValidator"/>.</description></item>
/// <item><description>Blocks, warns, or allows based on <see cref="CrossBorderTransferOptions.EnforcementMode"/>.</description></item>
/// </list>
/// <para>
/// The behavior supports three enforcement modes via <see cref="CrossBorderTransferEnforcementMode"/>:
/// <see cref="CrossBorderTransferEnforcementMode.Block"/> returns an error for non-compliant transfers,
/// <see cref="CrossBorderTransferEnforcementMode.Warn"/> logs warnings but allows processing,
/// <see cref="CrossBorderTransferEnforcementMode.Disabled"/> skips validation entirely.
/// </para>
/// <para>
/// <b>Observability:</b> Emits OpenTelemetry traces via <c>Encina.Compliance.CrossBorderTransfer</c>
/// ActivitySource, metrics via <c>Encina.Compliance.CrossBorderTransfer</c> Meter, and structured
/// log messages via <see cref="ILogger"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequiresCrossBorderTransfer(Destination = "US", DataCategory = "personal-data")]
/// public sealed record SyncToUSCommand : ICommand&lt;Unit&gt;;
///
/// [RequiresCrossBorderTransfer(
///     DestinationProperty = "TargetCountry",
///     DataCategory = "health-data")]
/// public sealed record TransferRecordsCommand(string TargetCountry) : ICommand&lt;Unit&gt;;
/// </code>
/// </example>
public sealed class TransferBlockingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, RequiresCrossBorderTransferAttribute?> AttributeCache = new();
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> PropertyCache = new();

    private readonly ITransferValidator _validator;
    private readonly CrossBorderTransferOptions _options;
    private readonly ILogger<TransferBlockingPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IModuleExecutionContext? _moduleContext;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="TransferBlockingPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validator">The transfer validator for checking GDPR Chapter V compliance.</param>
    /// <param name="options">Cross-border transfer configuration options.</param>
    /// <param name="logger">Logger for structured transfer compliance logging.</param>
    /// <param name="serviceProvider">Service provider for resolving optional cross-cutting dependencies.</param>
    public TransferBlockingPipelineBehavior(
        ITransferValidator validator,
        IOptions<CrossBorderTransferOptions> options,
        ILogger<TransferBlockingPipelineBehavior<TRequest, TResponse>> logger,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _validator = validator;
        _options = options.Value;
        _logger = logger;
        _moduleContext = serviceProvider.GetService<IModuleExecutionContext>();
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
        if (_options.EnforcementMode == CrossBorderTransferEnforcementMode.Disabled)
        {
            _logger.TransferEnforcementDisabled(requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        // Step 2: Check for [RequiresCrossBorderTransfer] attribute (cached)
        var attribute = AttributeCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttribute<RequiresCrossBorderTransferAttribute>());

        // No attribute — skip entirely
        if (attribute is null)
        {
            CrossBorderTransferDiagnostics.TransferCheckSkipped.Add(1, new TagList
            {
                { CrossBorderTransferDiagnostics.TagRequestType, requestTypeName }
            });
            return await nextStep().ConfigureAwait(false);
        }

        // Step 3: Extract source and destination
        var destination = ExtractProperty(request, requestType, attribute.DestinationProperty) ?? attribute.Destination;
        var source = ExtractProperty(request, requestType, attribute.SourceProperty);

        if (string.IsNullOrEmpty(destination))
        {
            _logger.TransferDestinationUnresolved(requestTypeName);

            var error = CrossBorderTransferErrors.TransferBlocked(
                $"Destination country could not be resolved for request '{requestTypeName}'.");
            return Left<EncinaError, TResponse>(error);
        }

        // Step 4: Start tracing and build transfer request
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = CrossBorderTransferDiagnostics.StartTransferCheck(requestTypeName);
        activity?.SetTag(CrossBorderTransferDiagnostics.TagDestination, destination);
        activity?.SetTag(CrossBorderTransferDiagnostics.TagDataCategory, attribute.DataCategory);
        activity?.SetTag(CrossBorderTransferDiagnostics.TagEnforcementMode, _options.EnforcementMode.ToString());

        if (source is not null)
        {
            activity?.SetTag(CrossBorderTransferDiagnostics.TagSource, source);
        }

        _logger.TransferValidationStarted(requestTypeName, source ?? "(default)", destination);

        var moduleId = _moduleContext?.CurrentModule;
        var tenantId = context.TenantId;

        var transferRequest = new TransferRequest
        {
            SourceCountryCode = source ?? _options.DefaultSourceCountryCode,
            DestinationCountryCode = destination,
            DataCategory = attribute.DataCategory,
            TenantId = tenantId,
            ModuleId = moduleId
        };

        if (tenantId is not null)
        {
            activity?.SetTag("encina.tenant_id", tenantId);
        }

        if (moduleId is not null)
        {
            activity?.SetTag("encina.module_id", moduleId);
        }

        // Step 5: Validate transfer
        var validationResult = await _validator
            .ValidateAsync(transferRequest, cancellationToken)
            .ConfigureAwait(false);

        // Handle validator infrastructure errors
        if (validationResult.IsLeft)
        {
            var validatorError = (EncinaError)validationResult;
            RecordBlocked(activity, startedAt, requestTypeName, destination, "validation_error");
            return Left<EncinaError, TResponse>(validatorError);
        }

        var outcome = (TransferValidationOutcome)validationResult;

        // Step 6: Handle blocked transfer
        if (!outcome.IsAllowed)
        {
            var reason = outcome.BlockReason ?? "Transfer blocked — no valid GDPR Chapter V mechanism.";

            if (_options.EnforcementMode == CrossBorderTransferEnforcementMode.Block)
            {
                _logger.TransferBlockedByPolicy(requestTypeName, source ?? "(default)", destination, reason);

                RecordBlocked(activity, startedAt, requestTypeName, destination, reason);
                return Left<EncinaError, TResponse>(CrossBorderTransferErrors.TransferBlocked(reason));
            }

            // Warn mode — log but proceed
            _logger.TransferWarned(requestTypeName, source ?? "(default)", destination, reason);

            RecordWarned(activity, startedAt, requestTypeName, destination, reason);
            return await nextStep().ConfigureAwait(false);
        }

        // Step 7: Log warnings from allowed transfer
        foreach (var warning in outcome.Warnings)
        {
            _logger.TransferOutcomeWarning(requestTypeName, warning);
        }

        // Step 8: Record success and proceed
        RecordPassed(activity, startedAt, requestTypeName, destination, outcome.Basis.ToString());
        _logger.TransferValidationAllowed(requestTypeName, source ?? "(default)", destination, outcome.Basis.ToString());

        return await nextStep().ConfigureAwait(false);
    }

    private static string? ExtractProperty(TRequest request, Type requestType, string? propertyName)
    {
        if (propertyName is null)
        {
            return null;
        }

        var cacheKey = (requestType, propertyName);
        var property = PropertyCache.GetOrAdd(cacheKey, static key =>
            key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance));

        return property?.GetValue(request) as string;
    }

    private static void RecordPassed(Activity? activity, long startedAt, string requestTypeName, string destination, string basis)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var tags = new TagList
        {
            { CrossBorderTransferDiagnostics.TagRequestType, requestTypeName },
            { CrossBorderTransferDiagnostics.TagDestination, destination },
            { CrossBorderTransferDiagnostics.TagBasis, basis }
        };

        CrossBorderTransferDiagnostics.TransferCheckTotal.Add(1, tags);
        CrossBorderTransferDiagnostics.TransferCheckPassed.Add(1, tags);
        CrossBorderTransferDiagnostics.TransferCheckDuration.Record(elapsed.TotalMilliseconds, tags);
        CrossBorderTransferDiagnostics.RecordPassed(activity, basis);
    }

    private static void RecordBlocked(Activity? activity, long startedAt, string requestTypeName, string destination, string reason)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var tags = new TagList
        {
            { CrossBorderTransferDiagnostics.TagRequestType, requestTypeName },
            { CrossBorderTransferDiagnostics.TagDestination, destination },
            { CrossBorderTransferDiagnostics.TagFailureReason, reason }
        };

        CrossBorderTransferDiagnostics.TransferCheckTotal.Add(1, tags);
        CrossBorderTransferDiagnostics.TransferCheckBlocked.Add(1, tags);
        CrossBorderTransferDiagnostics.TransferCheckDuration.Record(elapsed.TotalMilliseconds, tags);
        CrossBorderTransferDiagnostics.RecordBlocked(activity, reason);
    }

    private static void RecordWarned(Activity? activity, long startedAt, string requestTypeName, string destination, string reason)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var tags = new TagList
        {
            { CrossBorderTransferDiagnostics.TagRequestType, requestTypeName },
            { CrossBorderTransferDiagnostics.TagDestination, destination },
            { CrossBorderTransferDiagnostics.TagFailureReason, reason }
        };

        CrossBorderTransferDiagnostics.TransferCheckTotal.Add(1, tags);
        CrossBorderTransferDiagnostics.TransferCheckWarned.Add(1, tags);
        CrossBorderTransferDiagnostics.TransferCheckDuration.Record(elapsed.TotalMilliseconds, tags);
        CrossBorderTransferDiagnostics.RecordWarned(activity, reason);
    }
}

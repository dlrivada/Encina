using System.Diagnostics;
using System.Reflection;

using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Diagnostics;
using Encina.Compliance.Retention.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Retention;

/// <summary>
/// Pipeline behavior that automatically tracks retention records for response
/// properties or types decorated with the <see cref="RetentionPeriodAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type whose class or properties may be decorated with retention attributes.</typeparam>
/// <remarks>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), personal data shall be kept for no longer
/// than is necessary for the purposes for which it is processed. This behavior intercepts
/// responses after the handler has processed the request and automatically registers retention
/// records to ensure data lifecycle tracking begins at creation time.
/// </para>
/// <para>
/// The behavior scans <typeparamref name="TResponse"/> for <see cref="RetentionPeriodAttribute"/>
/// applied at class level or on individual properties. When found, a retention record is tracked
/// via <see cref="IRetentionRecordService.TrackEntityAsync"/> with a retention period resolved
/// either from the attribute or from <see cref="IRetentionPolicyService.GetRetentionPeriodAsync"/>.
/// </para>
/// <para>
/// <b>Attribute resolution:</b> Each closed generic type resolves its attribute info exactly once
/// via a <c>static readonly</c> field. This ensures zero reflection overhead on subsequent calls
/// for the same <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/> pair.
/// </para>
/// <para>
/// <b>Entity ID resolution:</b> The behavior attempts to resolve the entity identifier from the
/// response by looking for a public property named <c>Id</c> or <c>EntityId</c> (case-insensitive).
/// If no suitable property is found, the behavior reports an error or warning depending on the
/// <see cref="RetentionOptions.EnforcementMode"/>.
/// </para>
/// <para>
/// The enforcement mode is controlled by <see cref="RetentionOptions.EnforcementMode"/>:
/// <see cref="RetentionEnforcementMode.Block"/> returns an error if record creation fails,
/// <see cref="RetentionEnforcementMode.Warn"/> logs a warning but returns the response,
/// <see cref="RetentionEnforcementMode.Disabled"/> skips all retention tracking entirely.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Response type with declarative retention tracking
/// [RetentionPeriod(Years = 7, DataCategory = "financial-records",
///     Reason = "German tax law (AO section 147)")]
/// public sealed record CreateInvoiceResponse
/// {
///     public string Id { get; init; } = string.Empty;
///     public decimal Amount { get; init; }
/// }
///
/// // The pipeline behavior automatically creates a retention record
/// // when the handler returns a successful CreateInvoiceResponse.
/// </code>
/// </example>
public sealed class RetentionValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Static per-generic-type attribute info. Each closed generic type (e.g.,
    /// <c>RetentionValidationPipelineBehavior&lt;CreateInvoiceCommand, CreateInvoiceResponse&gt;</c>)
    /// resolves its own attribute info exactly once via the CLR's static field guarantee.
    /// </summary>
    private static readonly RetentionAttributeInfo? CachedAttributeInfo = ResolveAttributeInfo();

    private readonly IRetentionRecordService _recordService;
    private readonly IRetentionPolicyService _policyService;
    private readonly RetentionOptions _options;
    private readonly ILogger<RetentionValidationPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionValidationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="recordService">Service for tracking retention records via event-sourced aggregates.</param>
    /// <param name="policyService">Service for resolving retention periods from policies.</param>
    /// <param name="options">Retention configuration options controlling enforcement mode.</param>
    /// <param name="logger">Logger for structured retention pipeline logging.</param>
    public RetentionValidationPipelineBehavior(
        IRetentionRecordService recordService,
        IRetentionPolicyService policyService,
        IOptions<RetentionOptions> options,
        ILogger<RetentionValidationPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(recordService);
        ArgumentNullException.ThrowIfNull(policyService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _recordService = recordService;
        _policyService = policyService;
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
        var responseTypeName = typeof(TResponse).Name;

        // Step 1: Disabled mode — no-op, no logging, no metrics
        if (_options.EnforcementMode == RetentionEnforcementMode.Disabled)
        {
            _logger.RetentionPipelineDisabled(requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        var attrInfo = CachedAttributeInfo;

        // Step 2: No retention attributes on response type — skip entirely
        if (attrInfo is null)
        {
            return await nextStep().ConfigureAwait(false);
        }

        using var activity = RetentionDiagnostics.StartPipelineExecution(requestTypeName, responseTypeName);
        var startTimestamp = Stopwatch.GetTimestamp();

        // Step 3: Execute the handler to get the response
        var result = await nextStep().ConfigureAwait(false);

        // Step 4: If the handler returned an error, pass it through
        if (result.IsLeft)
        {
            _logger.RetentionPipelineHandlerError(requestTypeName);
            RetentionDiagnostics.RecordSkipped(activity);
            RetentionDiagnostics.PipelineDuration.Record(
                Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
            return result;
        }

        // Step 5: Create retention records for decorated fields/type
        var response = (TResponse)result;

        _logger.RetentionPipelineStarted(requestTypeName, responseTypeName, attrInfo.Fields.Length);

        try
        {
            foreach (var field in attrInfo.Fields)
            {
                var recordResult = await TrackRetentionRecordAsync(
                    response!, field, responseTypeName, cancellationToken).ConfigureAwait(false);

                if (recordResult.IsLeft)
                {
                    // Block mode — return error immediately
                    RetentionDiagnostics.RecordFailed(activity, "record_creation_blocked");
                    RetentionDiagnostics.PipelineExecutionsTotal.Add(1,
                        new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "blocked"));
                    RetentionDiagnostics.PipelineDuration.Record(
                        Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
                    return Left<EncinaError, TResponse>((EncinaError)recordResult);
                }
            }

            _logger.RetentionPipelineCompleted(requestTypeName, responseTypeName, attrInfo.Fields.Length);
            RetentionDiagnostics.RecordCompleted(activity, attrInfo.Fields.Length);
            RetentionDiagnostics.PipelineExecutionsTotal.Add(1,
                new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "completed"));
            RetentionDiagnostics.PipelineDuration.Record(
                Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);

            return Right<EncinaError, TResponse>(response!);
        }
        catch (Exception ex)
        {
            _logger.RetentionPipelineError(requestTypeName, responseTypeName, ex);
            RetentionDiagnostics.RecordFailed(activity, ex.Message);
            RetentionDiagnostics.PipelineExecutionsTotal.Add(1,
                new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "failed"));
            RetentionDiagnostics.PipelineDuration.Record(
                Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);

            if (_options.EnforcementMode == RetentionEnforcementMode.Block)
            {
                return Left<EncinaError, TResponse>(
                    RetentionErrors.PipelineRecordCreationFailed("(pipeline)", ex.Message, ex));
            }

            // Warn mode — log and allow through
            return Right<EncinaError, TResponse>(response!);
        }
    }

    // ================================================================
    // Retention record tracking via ES services
    // ================================================================

    private async ValueTask<Either<EncinaError, Unit>> TrackRetentionRecordAsync(
        TResponse response,
        RetentionFieldInfo field,
        string responseTypeName,
        CancellationToken cancellationToken)
    {
        // Resolve entity ID from the response
        var entityId = ResolveEntityId(response, field);

        if (string.IsNullOrWhiteSpace(entityId))
        {
            var error = RetentionErrors.PipelineEntityIdNotFound(responseTypeName);

            if (_options.EnforcementMode == RetentionEnforcementMode.Block)
            {
                _logger.RetentionRecordCreationBlocked(field.DataCategory ?? responseTypeName, responseTypeName, "Entity ID not found");
                return Left<EncinaError, Unit>(error);
            }

            _logger.RetentionEntityIdNotFound(responseTypeName);
            return Right<EncinaError, Unit>(unit);
        }

        var dataCategory = field.DataCategory ?? responseTypeName;
        var retentionPeriod = field.RetentionPeriod;

        // If no retention period on attribute, resolve from policy service
        if (retentionPeriod <= TimeSpan.Zero)
        {
            var periodResult = await _policyService
                .GetRetentionPeriodAsync(dataCategory, cancellationToken)
                .ConfigureAwait(false);

            if (periodResult.IsLeft)
            {
                var periodError = (EncinaError)periodResult;

                if (_options.EnforcementMode == RetentionEnforcementMode.Block)
                {
                    _logger.RetentionRecordCreationBlocked(dataCategory, responseTypeName, periodError.Message);
                    return Left<EncinaError, Unit>(periodError);
                }

                _logger.RetentionRecordCreationWarned(dataCategory, responseTypeName, periodError.Message);
                return Right<EncinaError, Unit>(unit);
            }

            retentionPeriod = (TimeSpan)periodResult;
        }

        // Track entity via the event-sourced record service (policyId = Guid.Empty for attribute-based)
        var trackResult = await _recordService
            .TrackEntityAsync(entityId, dataCategory, Guid.Empty, retentionPeriod, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return trackResult.Match(
            Right: _ =>
            {
                _logger.RetentionRecordCreated(entityId, dataCategory, DateTimeOffset.UtcNow + retentionPeriod, retentionPeriod);
                RetentionDiagnostics.RecordsCreatedTotal.Add(1,
                    new KeyValuePair<string, object?>(RetentionDiagnostics.TagDataCategory, dataCategory));
                return Right<EncinaError, Unit>(unit);
            },
            Left: error =>
            {
                if (_options.EnforcementMode == RetentionEnforcementMode.Block)
                {
                    _logger.RetentionRecordCreationBlocked(dataCategory, responseTypeName, error.Message);
                    return Left<EncinaError, Unit>(
                        RetentionErrors.PipelineRecordCreationFailed(dataCategory, error.Message));
                }

                _logger.RetentionRecordCreationWarned(dataCategory, responseTypeName, error.Message);
                return Right<EncinaError, Unit>(unit);
            });
    }

    // ================================================================
    // Entity ID resolution
    // ================================================================

    private static string? ResolveEntityId(TResponse response, RetentionFieldInfo field)
    {
        if (response is null)
        {
            return null;
        }

        // If the field has an EntityIdProperty resolved during attribute scanning, use it
        if (field.EntityIdProperty is not null)
        {
            var value = field.EntityIdProperty.GetValue(response);
            return value?.ToString();
        }

        return null;
    }

    // ================================================================
    // Attribute resolution (runs once per closed generic type)
    // ================================================================

    private static RetentionAttributeInfo? ResolveAttributeInfo()
    {
        var responseType = typeof(TResponse);
        var fields = new List<RetentionFieldInfo>();

        // Resolve entity ID property (Id or EntityId) from the response type
        var entityIdProperty = ResolveEntityIdProperty(responseType);

        // Check for class-level [RetentionPeriod] attribute
        var classAttr = responseType.GetCustomAttribute<RetentionPeriodAttribute>();
        if (classAttr is not null && classAttr.RetentionPeriod > TimeSpan.Zero)
        {
            fields.Add(new RetentionFieldInfo(
                RetentionPeriod: classAttr.RetentionPeriod,
                DataCategory: classAttr.DataCategory,
                Reason: classAttr.Reason,
                AutoDelete: classAttr.AutoDelete,
                EntityIdProperty: entityIdProperty));
        }

        // Check for property-level [RetentionPeriod] attributes
        foreach (var property in responseType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead)
            {
                continue;
            }

            var propAttr = property.GetCustomAttribute<RetentionPeriodAttribute>();
            if (propAttr is not null && propAttr.RetentionPeriod > TimeSpan.Zero)
            {
                fields.Add(new RetentionFieldInfo(
                    RetentionPeriod: propAttr.RetentionPeriod,
                    DataCategory: propAttr.DataCategory ?? property.Name,
                    Reason: propAttr.Reason,
                    AutoDelete: propAttr.AutoDelete,
                    EntityIdProperty: entityIdProperty));
            }
        }

        // No decorated fields at all — null signals "skip entirely"
        if (fields.Count == 0)
        {
            return null;
        }

        return new RetentionAttributeInfo([.. fields]);
    }

    private static PropertyInfo? ResolveEntityIdProperty(Type responseType)
    {
        // Look for "EntityId" first (more specific), then "Id" (more common)
        var properties = responseType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var entityIdProp = System.Array.Find(properties,
            p => string.Equals(p.Name, "EntityId", StringComparison.OrdinalIgnoreCase) && p.CanRead);

        if (entityIdProp is not null)
        {
            return entityIdProp;
        }

        var idProp = System.Array.Find(properties,
            p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase) && p.CanRead);

        return idProp;
    }

    // ================================================================
    // Nested types
    // ================================================================

    /// <summary>
    /// Cached attribute information for a response type's retention-related decorations.
    /// </summary>
    /// <param name="Fields">
    /// The list of retention-tracked fields/class decorations that need record creation.
    /// </param>
    private sealed record RetentionAttributeInfo(RetentionFieldInfo[] Fields);

    /// <summary>
    /// Metadata for a single retention-tracked field or class decoration.
    /// </summary>
    /// <param name="RetentionPeriod">The computed retention period from the attribute.</param>
    /// <param name="DataCategory">The data category for retention policy resolution (null uses type name).</param>
    /// <param name="Reason">The documented reason for the retention period.</param>
    /// <param name="AutoDelete">Whether data should be automatically deleted when the retention period expires.</param>
    /// <param name="EntityIdProperty">The property used to resolve the entity identifier from the response.</param>
    private sealed record RetentionFieldInfo(
        TimeSpan RetentionPeriod,
        string? DataCategory,
        string? Reason,
        bool AutoDelete,
        PropertyInfo? EntityIdProperty);
}

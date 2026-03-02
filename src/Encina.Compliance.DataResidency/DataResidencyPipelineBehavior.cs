using System.Reflection;

using Encina.Compliance.DataResidency.Attributes;
using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Pipeline behavior that enforces data residency policies for requests decorated with
/// <see cref="DataResidencyAttribute"/> or <see cref="NoCrossBorderTransferAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// Per GDPR Chapter V (Articles 44-49), personal data may only be transferred to regions
/// that provide an adequate level of data protection. This behavior intercepts requests
/// before the handler executes to validate that the current processing region complies
/// with the declared residency policy.
/// </para>
/// <para>
/// <b>Attribute resolution:</b> Each closed generic type resolves its attribute info exactly once
/// via <c>static readonly</c> fields. This ensures zero reflection overhead on subsequent calls
/// for the same <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/> pair.
/// </para>
/// <para>
/// The enforcement mode is controlled by <see cref="DataResidencyOptions.EnforcementMode"/>:
/// <see cref="DataResidencyEnforcementMode.Block"/> returns an error if the region is not allowed,
/// <see cref="DataResidencyEnforcementMode.Warn"/> logs a warning but allows the request through,
/// <see cref="DataResidencyEnforcementMode.Disabled"/> skips all residency checks entirely.
/// </para>
/// <para>
/// After successful handler execution, the behavior optionally records:
/// <list type="bullet">
/// <item><description>Data locations via <see cref="IDataLocationStore"/> (when <see cref="DataResidencyOptions.TrackDataLocations"/> is <c>true</c>)</description></item>
/// <item><description>Audit entries via <see cref="IResidencyAuditStore"/> (when <see cref="DataResidencyOptions.TrackAuditTrail"/> is <c>true</c>)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Request with data residency constraint
/// [DataResidency("DE", "FR", DataCategory = "healthcare-data")]
/// public record CreatePatientCommand(string PatientId) : ICommand&lt;PatientId&gt;;
///
/// // Request with no cross-border transfer constraint
/// [NoCrossBorderTransfer(DataCategory = "classified-data")]
/// public record ProcessClassifiedCommand(string DocumentId) : ICommand;
/// </code>
/// </example>
public sealed class DataResidencyPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Static per-generic-type attribute info for <see cref="DataResidencyAttribute"/>.
    /// Each closed generic type resolves its own attribute info exactly once via the
    /// CLR's static field guarantee.
    /// </summary>
    private static readonly DataResidencyAttributeInfo? CachedResidencyInfo = ResolveResidencyAttribute();

    /// <summary>
    /// Static per-generic-type attribute info for <see cref="NoCrossBorderTransferAttribute"/>.
    /// Each closed generic type resolves its own attribute info exactly once via the
    /// CLR's static field guarantee.
    /// </summary>
    private static readonly NoCrossBorderTransferInfo? CachedNoCrossInfo = ResolveNoCrossAttribute();

    /// <summary>
    /// Cached entity ID property from the response type for data location tracking.
    /// Looks for <c>EntityId</c> (preferred) then <c>Id</c> (fallback) on the response type.
    /// </summary>
    private static readonly PropertyInfo? CachedEntityIdProperty = ResolveEntityIdProperty();

    private readonly IRegionContextProvider _regionContextProvider;
    private readonly IDataResidencyPolicy _residencyPolicy;
    private readonly ICrossBorderTransferValidator _transferValidator;
    private readonly IDataLocationStore _dataLocationStore;
    private readonly IResidencyAuditStore _auditStore;
    private readonly DataResidencyOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DataResidencyPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataResidencyPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="regionContextProvider">Provider for resolving the current processing region.</param>
    /// <param name="residencyPolicy">Service for evaluating data residency policies.</param>
    /// <param name="transferValidator">Validator for cross-border data transfers.</param>
    /// <param name="dataLocationStore">Store for recording data locations.</param>
    /// <param name="auditStore">Store for recording residency audit entries.</param>
    /// <param name="options">Data residency configuration options.</param>
    /// <param name="timeProvider">Time provider for deterministic timestamps.</param>
    /// <param name="logger">Logger for structured diagnostic messages.</param>
    public DataResidencyPipelineBehavior(
        IRegionContextProvider regionContextProvider,
        IDataResidencyPolicy residencyPolicy,
        ICrossBorderTransferValidator transferValidator,
        IDataLocationStore dataLocationStore,
        IResidencyAuditStore auditStore,
        IOptions<DataResidencyOptions> options,
        TimeProvider timeProvider,
        ILogger<DataResidencyPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(regionContextProvider);
        ArgumentNullException.ThrowIfNull(residencyPolicy);
        ArgumentNullException.ThrowIfNull(transferValidator);
        ArgumentNullException.ThrowIfNull(dataLocationStore);
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _regionContextProvider = regionContextProvider;
        _residencyPolicy = residencyPolicy;
        _transferValidator = transferValidator;
        _dataLocationStore = dataLocationStore;
        _auditStore = auditStore;
        _options = options.Value;
        _timeProvider = timeProvider;
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
        if (_options.EnforcementMode == DataResidencyEnforcementMode.Disabled)
        {
            _logger.LogDebug(
                "Data residency enforcement disabled for '{RequestType}'", requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        var residencyInfo = CachedResidencyInfo;
        var noCrossInfo = CachedNoCrossInfo;

        // Step 2: No attributes → skip entirely
        if (residencyInfo is null && noCrossInfo is null)
        {
            return await nextStep().ConfigureAwait(false);
        }

        var dataCategory = residencyInfo?.DataCategory
            ?? noCrossInfo?.DataCategory
            ?? requestTypeName;

        // Step 3: Get current region from IRegionContextProvider
        var regionResult = await _regionContextProvider.GetCurrentRegionAsync(cancellationToken)
            .ConfigureAwait(false);

        if (regionResult.IsLeft)
        {
            var regionError = (EncinaError)regionResult;

            _logger.LogWarning(
                "Cannot resolve current region for request '{RequestType}': {ErrorMessage}",
                requestTypeName, regionError.Message);

            if (_options.EnforcementMode == DataResidencyEnforcementMode.Block)
            {
                await TryRecordAuditAsync(
                    dataCategory, sourceRegion: "unknown",
                    ResidencyAction.PolicyCheck, ResidencyOutcome.Blocked,
                    requestType: typeof(TRequest).FullName,
                    details: $"Region resolution failed: {regionError.Message}",
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return Left<EncinaError, TResponse>(regionError);
            }

            // Warn mode — proceed without region validation
            return await nextStep().ConfigureAwait(false);
        }

        var currentRegion = (Region)regionResult;

        // Step 4: [DataResidency] validation — check allowed regions via IDataResidencyPolicy
        if (residencyInfo is not null)
        {
            var policyResult = await ValidateResidencyPolicyAsync(
                residencyInfo, currentRegion, dataCategory, requestTypeName, cancellationToken)
                .ConfigureAwait(false);

            if (policyResult.IsLeft)
            {
                return Left<EncinaError, TResponse>((EncinaError)policyResult);
            }
        }

        // Step 5: [NoCrossBorderTransfer] — record constraint and validate no movement
        if (noCrossInfo is not null)
        {
            _logger.LogDebug(
                "No cross-border transfer constraint active for '{RequestType}' in region '{RegionCode}'",
                requestTypeName, currentRegion.Code);

            await TryRecordAuditAsync(
                dataCategory, sourceRegion: currentRegion.Code,
                ResidencyAction.CrossBorderTransfer, ResidencyOutcome.Allowed,
                requestType: typeof(TRequest).FullName,
                details: $"No cross-border transfer constraint enforced; data must remain in region '{currentRegion.Code}'."
                    + (noCrossInfo.Reason is not null ? $" Reason: {noCrossInfo.Reason}" : string.Empty),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        // Step 6: Call next handler
        var result = await nextStep().ConfigureAwait(false);

        // Step 7: Record data location (on success, if tracking enabled)
        if (result.IsRight && _options.TrackDataLocations)
        {
            await TryRecordDataLocationAsync(
                (TResponse)result, currentRegion, dataCategory, cancellationToken)
                .ConfigureAwait(false);
        }

        // Step 8: Record outcome audit entry
        await TryRecordAuditAsync(
            dataCategory, sourceRegion: currentRegion.Code,
            ResidencyAction.PolicyCheck,
            outcome: result.IsRight ? ResidencyOutcome.Allowed : ResidencyOutcome.Blocked,
            entityId: result.IsRight ? ResolveEntityId((TResponse)result) : null,
            requestType: typeof(TRequest).FullName,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }

    // ================================================================
    // Residency policy validation
    // ================================================================

    private async ValueTask<Either<EncinaError, Unit>> ValidateResidencyPolicyAsync(
        DataResidencyAttributeInfo info,
        Region currentRegion,
        string dataCategory,
        string requestTypeName,
        CancellationToken cancellationToken)
    {
        // Check if current region is allowed for this data category
        var isAllowedResult = await _residencyPolicy.IsAllowedAsync(
            dataCategory, currentRegion, cancellationToken).ConfigureAwait(false);

        if (isAllowedResult.IsLeft)
        {
            var policyError = (EncinaError)isAllowedResult;

            _logger.LogWarning(
                "Residency policy check failed for data category '{DataCategory}': {ErrorMessage}",
                dataCategory, policyError.Message);

            if (_options.EnforcementMode == DataResidencyEnforcementMode.Block)
            {
                await TryRecordAuditAsync(
                    dataCategory, sourceRegion: currentRegion.Code,
                    ResidencyAction.PolicyCheck, ResidencyOutcome.Blocked,
                    requestType: typeof(TRequest).FullName,
                    details: policyError.Message,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return Left<EncinaError, Unit>(policyError);
            }

            // Warn mode — proceed despite policy lookup failure
            return Right<EncinaError, Unit>(unit);
        }

        var isAllowed = isAllowedResult.Match(Right: r => r, Left: _ => false);

        if (!isAllowed)
        {
            var error = DataResidencyErrors.RegionNotAllowed(dataCategory, currentRegion.Code);

            _logger.LogWarning(
                "Region '{RegionCode}' is not allowed for data category '{DataCategory}' on request '{RequestType}'",
                currentRegion.Code, dataCategory, requestTypeName);

            var outcome = _options.EnforcementMode == DataResidencyEnforcementMode.Block
                ? ResidencyOutcome.Blocked
                : ResidencyOutcome.Warning;

            await TryRecordAuditAsync(
                dataCategory, sourceRegion: currentRegion.Code,
                ResidencyAction.Violation, outcome,
                requestType: typeof(TRequest).FullName,
                details: error.Message,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (_options.EnforcementMode == DataResidencyEnforcementMode.Block)
            {
                return Left<EncinaError, Unit>(error);
            }
        }

        // Check if an EU adequacy decision is required for the current region
        if (info.RequireAdequacyDecision && !currentRegion.HasAdequacyDecision)
        {
            var error = DataResidencyErrors.CrossBorderTransferDenied(
                currentRegion.Code, currentRegion.Code,
                $"Adequacy decision required for data category '{dataCategory}' but region '{currentRegion.Code}' does not have one.");

            _logger.LogWarning(
                "Region '{RegionCode}' lacks adequacy decision required for data category '{DataCategory}'",
                currentRegion.Code, dataCategory);

            var outcome = _options.EnforcementMode == DataResidencyEnforcementMode.Block
                ? ResidencyOutcome.Blocked
                : ResidencyOutcome.Warning;

            await TryRecordAuditAsync(
                dataCategory, sourceRegion: currentRegion.Code,
                ResidencyAction.Violation, outcome,
                requestType: typeof(TRequest).FullName,
                details: error.Message,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (_options.EnforcementMode == DataResidencyEnforcementMode.Block)
            {
                return Left<EncinaError, Unit>(error);
            }
        }

        return Right<EncinaError, Unit>(unit);
    }

    // ================================================================
    // Data location recording
    // ================================================================

    private async ValueTask TryRecordDataLocationAsync(
        TResponse response,
        Region region,
        string dataCategory,
        CancellationToken cancellationToken)
    {
        var entityId = ResolveEntityId(response);

        if (string.IsNullOrWhiteSpace(entityId))
        {
            return;
        }

        var location = new DataLocation
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = entityId,
            DataCategory = dataCategory,
            Region = region,
            StorageType = StorageType.Primary,
            StoredAtUtc = _timeProvider.GetUtcNow()
        };

        var recordResult = await _dataLocationStore.RecordAsync(location, cancellationToken)
            .ConfigureAwait(false);

        if (recordResult.IsLeft)
        {
            _logger.LogWarning(
                "Failed to record data location for entity '{EntityId}': {ErrorMessage}",
                entityId, ((EncinaError)recordResult).Message);
        }
    }

    // ================================================================
    // Audit trail recording
    // ================================================================

    private async ValueTask TryRecordAuditAsync(
        string dataCategory,
        string sourceRegion,
        ResidencyAction action,
        ResidencyOutcome outcome,
        string? entityId = null,
        string? targetRegion = null,
        string? legalBasis = null,
        string? requestType = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.TrackAuditTrail)
        {
            return;
        }

        var entry = new ResidencyAuditEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            DataCategory = dataCategory,
            SourceRegion = sourceRegion,
            Action = action,
            Outcome = outcome,
            EntityId = entityId,
            TargetRegion = targetRegion,
            LegalBasis = legalBasis,
            RequestType = requestType,
            TimestampUtc = _timeProvider.GetUtcNow(),
            Details = details
        };

        var recordResult = await _auditStore.RecordAsync(entry, cancellationToken)
            .ConfigureAwait(false);

        if (recordResult.IsLeft)
        {
            _logger.LogWarning(
                "Failed to record residency audit entry: {ErrorMessage}",
                ((EncinaError)recordResult).Message);
        }
    }

    // ================================================================
    // Entity ID resolution
    // ================================================================

    private static string? ResolveEntityId(TResponse response)
    {
        if (response is null || CachedEntityIdProperty is null)
        {
            return null;
        }

        return CachedEntityIdProperty.GetValue(response)?.ToString();
    }

    // ================================================================
    // Attribute resolution (runs once per closed generic type)
    // ================================================================

    private static DataResidencyAttributeInfo? ResolveResidencyAttribute()
    {
        var attr = typeof(TRequest).GetCustomAttribute<DataResidencyAttribute>();

        if (attr is null)
        {
            return null;
        }

        return new DataResidencyAttributeInfo(
            AllowedRegionCodes: attr.AllowedRegionCodes,
            DataCategory: attr.DataCategory,
            RequireAdequacyDecision: attr.RequireAdequacyDecision);
    }

    private static NoCrossBorderTransferInfo? ResolveNoCrossAttribute()
    {
        var attr = typeof(TRequest).GetCustomAttribute<NoCrossBorderTransferAttribute>();

        if (attr is null)
        {
            return null;
        }

        return new NoCrossBorderTransferInfo(
            DataCategory: attr.DataCategory,
            Reason: attr.Reason);
    }

    private static PropertyInfo? ResolveEntityIdProperty()
    {
        var responseType = typeof(TResponse);
        var properties = responseType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Look for "EntityId" first (more specific), then "Id" (more common)
        // Use System.Array explicitly to avoid conflict with LanguageExt.Prelude.Array<T>()
        var entityIdProp = System.Array.Find(properties,
            p => string.Equals(p.Name, "EntityId", StringComparison.OrdinalIgnoreCase) && p.CanRead);

        if (entityIdProp is not null)
        {
            return entityIdProp;
        }

        return System.Array.Find(properties,
            p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase) && p.CanRead);
    }
}

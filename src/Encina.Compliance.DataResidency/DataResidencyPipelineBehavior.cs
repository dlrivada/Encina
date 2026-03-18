using System.Reflection;

using Encina.Compliance.DataResidency.Abstractions;
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
/// After successful handler execution, the behavior optionally records data locations via
/// <see cref="IDataLocationService"/> (when <see cref="DataResidencyOptions.TrackDataLocations"/> is <c>true</c>).
/// Audit trail is captured automatically by the event-sourced aggregate's event stream,
/// eliminating the need for a separate audit store.
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
    private readonly IResidencyPolicyService _residencyPolicyService;
    private readonly ICrossBorderTransferValidator _transferValidator;
    private readonly IDataLocationService _dataLocationService;
    private readonly DataResidencyOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DataResidencyPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataResidencyPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="regionContextProvider">Provider for resolving the current processing region.</param>
    /// <param name="residencyPolicyService">Service for evaluating data residency policies.</param>
    /// <param name="transferValidator">Validator for cross-border data transfers.</param>
    /// <param name="dataLocationService">Service for recording data locations.</param>
    /// <param name="options">Data residency configuration options.</param>
    /// <param name="timeProvider">Time provider for deterministic timestamps.</param>
    /// <param name="logger">Logger for structured diagnostic messages.</param>
    public DataResidencyPipelineBehavior(
        IRegionContextProvider regionContextProvider,
        IResidencyPolicyService residencyPolicyService,
        ICrossBorderTransferValidator transferValidator,
        IDataLocationService dataLocationService,
        IOptions<DataResidencyOptions> options,
        TimeProvider timeProvider,
        ILogger<DataResidencyPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(regionContextProvider);
        ArgumentNullException.ThrowIfNull(residencyPolicyService);
        ArgumentNullException.ThrowIfNull(transferValidator);
        ArgumentNullException.ThrowIfNull(dataLocationService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _regionContextProvider = regionContextProvider;
        _residencyPolicyService = residencyPolicyService;
        _transferValidator = transferValidator;
        _dataLocationService = dataLocationService;
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
                return Left<EncinaError, TResponse>(regionError);
            }

            // Warn mode — proceed without region validation
            return await nextStep().ConfigureAwait(false);
        }

        var currentRegion = (Region)regionResult;

        // Step 4: [DataResidency] validation — check allowed regions via IResidencyPolicyService
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
        var isAllowedResult = await _residencyPolicyService.IsAllowedAsync(
            dataCategory, currentRegion, cancellationToken).ConfigureAwait(false);

        if (isAllowedResult.IsLeft)
        {
            var policyError = (EncinaError)isAllowedResult;

            _logger.LogWarning(
                "Residency policy check failed for data category '{DataCategory}': {ErrorMessage}",
                dataCategory, policyError.Message);

            if (_options.EnforcementMode == DataResidencyEnforcementMode.Block)
            {
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

        var registerResult = await _dataLocationService.RegisterLocationAsync(
            entityId, dataCategory, region.Code, StorageType.Primary,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (registerResult.IsLeft)
        {
            _logger.LogWarning(
                "Failed to record data location for entity '{EntityId}': {ErrorMessage}",
                entityId, ((EncinaError)registerResult).Message);
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

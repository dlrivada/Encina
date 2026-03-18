using Microsoft.Extensions.Logging;

namespace Encina.Compliance.DataResidency.Diagnostics;

/// <summary>
/// High-performance structured log messages for the Data Residency module.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 8600-8699 range to avoid collisions with other
/// Encina subsystems (GDPR uses 8100-8199, Consent uses 8200-8299, DSR uses 8300-8399,
/// Anonymization uses 8400-8499, Retention uses 8500-8599).
/// </para>
/// <para>
/// Allocation blocks:
/// <list type="table">
/// <item><term>8600-8609</term><description>Pipeline behavior</description></item>
/// <item><term>8610-8619</term><description>Transfer validation</description></item>
/// <item><term>8620-8629</term><description>Auto-registration</description></item>
/// <item><term>8630-8639</term><description>Health check</description></item>
/// <item><term>8640-8649</term><description>Policy management</description></item>
/// <item><term>8650-8659</term><description>Location tracking</description></item>
/// <item><term>8660-8669</term><description>Audit trail</description></item>
/// <item><term>8670-8679</term><description>Region resolution</description></item>
/// <item><term>8680-8699</term><description>Reserved</description></item>
/// </list>
/// </para>
/// </remarks>
internal static partial class DataResidencyLogMessages
{
    // ========================================================================
    // Pipeline behavior log messages (8600-8609)
    // ========================================================================

    /// <summary>Data residency pipeline skipped because enforcement mode is Disabled.</summary>
    [LoggerMessage(
        EventId = 8600,
        Level = LogLevel.Trace,
        Message = "Data residency pipeline skipped (enforcement disabled). RequestType={RequestType}")]
    internal static partial void ResidencyPipelineDisabled(this ILogger logger, string requestType);

    /// <summary>Data residency pipeline skipped because no [DataResidency] attributes found on response type.</summary>
    [LoggerMessage(
        EventId = 8601,
        Level = LogLevel.Trace,
        Message = "Data residency pipeline skipped (no [DataResidency] attributes on response). RequestType={RequestType}, ResponseType={ResponseType}")]
    internal static partial void ResidencyPipelineNoAttributes(this ILogger logger, string requestType, string responseType);

    /// <summary>Data residency check passed — region is allowed.</summary>
    [LoggerMessage(
        EventId = 8602,
        Level = LogLevel.Debug,
        Message = "Data residency check passed. RequestType={RequestType}, Region={Region}, DataCategory={DataCategory}")]
    internal static partial void ResidencyCheckPassed(this ILogger logger, string requestType, string region, string dataCategory);

    /// <summary>Data residency check blocked — region not allowed in Block enforcement mode.</summary>
    [LoggerMessage(
        EventId = 8603,
        Level = LogLevel.Warning,
        Message = "Data residency check blocked. RequestType={RequestType}, Region={Region}, DataCategory={DataCategory}, Reason={Reason}")]
    internal static partial void ResidencyCheckBlocked(this ILogger logger, string requestType, string region, string dataCategory, string reason);

    /// <summary>Data residency check warning — region not allowed but enforcement is in Warn mode.</summary>
    [LoggerMessage(
        EventId = 8604,
        Level = LogLevel.Warning,
        Message = "Data residency check warning (proceeding in Warn mode). RequestType={RequestType}, Region={Region}, DataCategory={DataCategory}, Reason={Reason}")]
    internal static partial void ResidencyCheckWarning(this ILogger logger, string requestType, string region, string dataCategory, string reason);

    /// <summary>Region resolution failed — could not determine the current data region.</summary>
    [LoggerMessage(
        EventId = 8605,
        Level = LogLevel.Error,
        Message = "Region resolution failed. RequestType={RequestType}, ErrorMessage={ErrorMessage}")]
    internal static partial void RegionResolutionFailed(this ILogger logger, string requestType, string errorMessage);

    /// <summary>Data residency pipeline started processing a request.</summary>
    [LoggerMessage(
        EventId = 8606,
        Level = LogLevel.Debug,
        Message = "Data residency pipeline started. RequestType={RequestType}, ResponseType={ResponseType}, AttributeCount={AttributeCount}")]
    internal static partial void ResidencyPipelineStarted(this ILogger logger, string requestType, string responseType, int attributeCount);

    /// <summary>Data residency pipeline completed all checks.</summary>
    [LoggerMessage(
        EventId = 8607,
        Level = LogLevel.Debug,
        Message = "Data residency pipeline completed. RequestType={RequestType}, ResponseType={ResponseType}, Outcome={Outcome}")]
    internal static partial void ResidencyPipelineCompleted(this ILogger logger, string requestType, string responseType, string outcome);

    /// <summary>Data residency pipeline received a handler error and is passing it through.</summary>
    [LoggerMessage(
        EventId = 8608,
        Level = LogLevel.Debug,
        Message = "Data residency pipeline passing through handler error. RequestType={RequestType}")]
    internal static partial void ResidencyPipelineHandlerError(this ILogger logger, string requestType);

    /// <summary>Exception occurred in the data residency pipeline.</summary>
    [LoggerMessage(
        EventId = 8609,
        Level = LogLevel.Error,
        Message = "Data residency pipeline error. RequestType={RequestType}, ResponseType={ResponseType}")]
    internal static partial void ResidencyPipelineError(this ILogger logger, string requestType, string responseType, Exception exception);

    // ========================================================================
    // Transfer validation log messages (8610-8619)
    // ========================================================================

    /// <summary>Cross-border transfer validated and allowed.</summary>
    [LoggerMessage(
        EventId = 8610,
        Level = LogLevel.Debug,
        Message = "Cross-border transfer allowed. SourceRegion={SourceRegion}, TargetRegion={TargetRegion}, LegalBasis={LegalBasis}")]
    internal static partial void TransferAllowed(this ILogger logger, string sourceRegion, string targetRegion, string legalBasis);

    /// <summary>Cross-border transfer denied.</summary>
    [LoggerMessage(
        EventId = 8611,
        Level = LogLevel.Warning,
        Message = "Cross-border transfer denied. SourceRegion={SourceRegion}, TargetRegion={TargetRegion}, Reason={Reason}")]
    internal static partial void TransferDenied(this ILogger logger, string sourceRegion, string targetRegion, string reason);

    /// <summary>Adequacy decision check performed for cross-border transfer.</summary>
    [LoggerMessage(
        EventId = 8612,
        Level = LogLevel.Debug,
        Message = "Adequacy decision check. TargetRegion={TargetRegion}, HasAdequacy={HasAdequacy}")]
    internal static partial void TransferAdequacyCheck(this ILogger logger, string targetRegion, bool hasAdequacy);

    /// <summary>Cross-border transfer validation started.</summary>
    [LoggerMessage(
        EventId = 8613,
        Level = LogLevel.Debug,
        Message = "Transfer validation started. SourceRegion={SourceRegion}, TargetRegion={TargetRegion}")]
    internal static partial void TransferValidationStarted(this ILogger logger, string sourceRegion, string targetRegion);

    /// <summary>Cross-border transfer skipped — same region (no border crossed).</summary>
    [LoggerMessage(
        EventId = 8614,
        Level = LogLevel.Trace,
        Message = "Transfer validation skipped (same region). Region={Region}")]
    internal static partial void TransferValidationSameRegion(this ILogger logger, string region);

    /// <summary>EU/EEA free movement applied — no transfer restriction.</summary>
    [LoggerMessage(
        EventId = 8615,
        Level = LogLevel.Debug,
        Message = "EU/EEA free movement applies. SourceRegion={SourceRegion}, TargetRegion={TargetRegion}")]
    internal static partial void TransferEuFreeMovement(this ILogger logger, string sourceRegion, string targetRegion);

    // ========================================================================
    // Auto-registration log messages (8620-8629)
    // ========================================================================

    /// <summary>Data residency auto-registration started scanning assemblies.</summary>
    [LoggerMessage(
        EventId = 8620,
        Level = LogLevel.Information,
        Message = "Data residency auto-registration started. AssembliesToScan={AssembliesToScan}")]
    internal static partial void ResidencyAutoRegistrationStarted(this ILogger logger, int assembliesToScan);

    /// <summary>Data residency auto-registration completed.</summary>
    [LoggerMessage(
        EventId = 8621,
        Level = LogLevel.Information,
        Message = "Data residency auto-registration completed. PoliciesCreated={PoliciesCreated}, AssembliesScanned={AssembliesScanned}")]
    internal static partial void ResidencyAutoRegistrationCompleted(this ILogger logger, int policiesCreated, int assembliesScanned);

    /// <summary>Data residency auto-registration skipped (no assemblies configured or disabled).</summary>
    [LoggerMessage(
        EventId = 8622,
        Level = LogLevel.Debug,
        Message = "Data residency auto-registration skipped (no assemblies configured or auto-registration disabled)")]
    internal static partial void ResidencyAutoRegistrationSkipped(this ILogger logger);

    /// <summary>Data residency policy discovered via [DataResidency] attribute.</summary>
    [LoggerMessage(
        EventId = 8623,
        Level = LogLevel.Debug,
        Message = "Data residency policy discovered. EntityType={EntityType}, DataCategory={DataCategory}, AllowedRegions={AllowedRegions}")]
    internal static partial void ResidencyPolicyDiscovered(this ILogger logger, string entityType, string dataCategory, string allowedRegions);

    /// <summary>Data residency policy auto-creation skipped because a policy already exists.</summary>
    [LoggerMessage(
        EventId = 8624,
        Level = LogLevel.Debug,
        Message = "Data residency policy already exists for category. DataCategory={DataCategory}, skipping auto-creation")]
    internal static partial void ResidencyPolicyAlreadyExists(this ILogger logger, string dataCategory);

    /// <summary>Data residency auto-registration failed to create policy.</summary>
    [LoggerMessage(
        EventId = 8625,
        Level = LogLevel.Warning,
        Message = "Failed to auto-register data residency policy. DataCategory={DataCategory}")]
    internal static partial void ResidencyAutoRegistrationPolicyFailed(this ILogger logger, string dataCategory, Exception exception);

    // ========================================================================
    // Health check log messages (8630-8639)
    // ========================================================================

    /// <summary>Data residency health check completed.</summary>
    [LoggerMessage(
        EventId = 8630,
        Level = LogLevel.Debug,
        Message = "Data residency health check completed. Status={Status}, StoresVerified={StoresVerified}")]
    internal static partial void ResidencyHealthCheckCompleted(this ILogger logger, string status, int storesVerified);

    // ========================================================================
    // Policy management log messages (8640-8649)
    // ========================================================================

    /// <summary>New data residency policy registered.</summary>
    [LoggerMessage(
        EventId = 8640,
        Level = LogLevel.Information,
        Message = "Data residency policy created. DataCategory={DataCategory}, AllowedRegions={AllowedRegions}")]
    internal static partial void ResidencyPolicyCreated(this ILogger logger, string dataCategory, string allowedRegions);

    /// <summary>No data residency policy found for the specified category.</summary>
    [LoggerMessage(
        EventId = 8641,
        Level = LogLevel.Debug,
        Message = "No data residency policy found for category. DataCategory={DataCategory}")]
    internal static partial void ResidencyPolicyNotFound(this ILogger logger, string dataCategory);

    // ========================================================================
    // Location tracking log messages (8650-8659)
    // ========================================================================

    /// <summary>Data location recorded successfully.</summary>
    [LoggerMessage(
        EventId = 8650,
        Level = LogLevel.Debug,
        Message = "Data location recorded. EntityId={EntityId}, Region={Region}, DataCategory={DataCategory}")]
    internal static partial void LocationRecorded(this ILogger logger, string entityId, string region, string dataCategory);

    /// <summary>Failed to record data location.</summary>
    [LoggerMessage(
        EventId = 8651,
        Level = LogLevel.Warning,
        Message = "Failed to record data location. EntityId={EntityId}, Region={Region}, ErrorMessage={ErrorMessage}")]
    internal static partial void LocationRecordFailed(this ILogger logger, string entityId, string region, string errorMessage);

    /// <summary>Exception while recording data location.</summary>
    [LoggerMessage(
        EventId = 8652,
        Level = LogLevel.Error,
        Message = "Exception while recording data location. EntityId={EntityId}, Region={Region}")]
    internal static partial void LocationRecordException(this ILogger logger, string entityId, string region, Exception exception);

    // ========================================================================
    // Audit trail log messages (8660-8669)
    // ========================================================================

    /// <summary>Residency audit entry recorded successfully.</summary>
    [LoggerMessage(
        EventId = 8660,
        Level = LogLevel.Debug,
        Message = "Residency audit entry recorded. Action={Action}, EntityId={EntityId}, Region={Region}")]
    internal static partial void ResidencyAuditEntryRecorded(this ILogger logger, string action, string? entityId, string? region);

    /// <summary>Failed to record residency audit entry.</summary>
    [LoggerMessage(
        EventId = 8661,
        Level = LogLevel.Warning,
        Message = "Failed to record residency audit entry. Action={Action}")]
    internal static partial void ResidencyAuditEntryFailed(this ILogger logger, string action, Exception exception);

    // ========================================================================
    // Region resolution log messages (8670-8679)
    // ========================================================================

    /// <summary>Region resolved from HTTP X-Data-Region header.</summary>
    [LoggerMessage(
        EventId = 8670,
        Level = LogLevel.Debug,
        Message = "Region resolved from header. RegionCode={RegionCode}, HeaderName={HeaderName}")]
    internal static partial void RegionResolvedFromHeader(this ILogger logger, string regionCode, string headerName);

    /// <summary>Region resolved from tenant configuration.</summary>
    [LoggerMessage(
        EventId = 8671,
        Level = LogLevel.Debug,
        Message = "Region resolved from tenant configuration. RegionCode={RegionCode}, TenantId={TenantId}")]
    internal static partial void RegionResolvedFromTenant(this ILogger logger, string regionCode, string? tenantId);

    /// <summary>Region resolved from default configuration.</summary>
    [LoggerMessage(
        EventId = 8672,
        Level = LogLevel.Debug,
        Message = "Region resolved from default configuration. RegionCode={RegionCode}")]
    internal static partial void RegionResolvedFromDefault(this ILogger logger, string regionCode);

    /// <summary>Region could not be resolved from any source.</summary>
    [LoggerMessage(
        EventId = 8673,
        Level = LogLevel.Warning,
        Message = "Region could not be resolved from any source (header, tenant, default)")]
    internal static partial void RegionResolutionExhausted(this ILogger logger);

    /// <summary>Custom region code detected — creating ad-hoc region definition.</summary>
    [LoggerMessage(
        EventId = 8674,
        Level = LogLevel.Debug,
        Message = "Custom region code detected, creating ad-hoc region. RegionCode={RegionCode}")]
    internal static partial void RegionCustomCodeDetected(this ILogger logger, string regionCode);

    // ========================================================================
    // Event-sourced service log messages (8680-8699)
    // ========================================================================

    /// <summary>Residency policy created via event-sourced aggregate.</summary>
    [LoggerMessage(
        EventId = 8680,
        Level = LogLevel.Information,
        Message = "Residency policy created (ES). PolicyId={PolicyId}, DataCategory={DataCategory}")]
    internal static partial void ResidencyPolicyCreatedES(this ILogger logger, Guid policyId, string dataCategory);

    /// <summary>Residency policy updated via event-sourced aggregate.</summary>
    [LoggerMessage(
        EventId = 8681,
        Level = LogLevel.Information,
        Message = "Residency policy updated (ES). PolicyId={PolicyId}")]
    internal static partial void ResidencyPolicyUpdatedES(this ILogger logger, Guid policyId);

    /// <summary>Residency policy deleted via event-sourced aggregate.</summary>
    [LoggerMessage(
        EventId = 8682,
        Level = LogLevel.Information,
        Message = "Residency policy deleted (ES). PolicyId={PolicyId}, Reason={Reason}")]
    internal static partial void ResidencyPolicyDeletedES(this ILogger logger, Guid policyId, string reason);

    /// <summary>Data location registered via event-sourced aggregate.</summary>
    [LoggerMessage(
        EventId = 8683,
        Level = LogLevel.Information,
        Message = "Data location registered (ES). LocationId={LocationId}, EntityId={EntityId}, Region={Region}")]
    internal static partial void DataLocationRegisteredES(this ILogger logger, Guid locationId, string entityId, string region);

    /// <summary>Data location migrated via event-sourced aggregate.</summary>
    [LoggerMessage(
        EventId = 8684,
        Level = LogLevel.Information,
        Message = "Data location migrated (ES). LocationId={LocationId}, NewRegion={NewRegion}")]
    internal static partial void DataLocationMigratedES(this ILogger logger, Guid locationId, string newRegion);

    /// <summary>Data location verified via event-sourced aggregate.</summary>
    [LoggerMessage(
        EventId = 8685,
        Level = LogLevel.Debug,
        Message = "Data location verified (ES). LocationId={LocationId}")]
    internal static partial void DataLocationVerifiedES(this ILogger logger, Guid locationId);

    /// <summary>Data location removed via event-sourced aggregate.</summary>
    [LoggerMessage(
        EventId = 8686,
        Level = LogLevel.Information,
        Message = "Data location removed (ES). LocationId={LocationId}, Reason={Reason}")]
    internal static partial void DataLocationRemovedES(this ILogger logger, Guid locationId, string reason);

    /// <summary>Sovereignty violation detected on data location.</summary>
    [LoggerMessage(
        EventId = 8687,
        Level = LogLevel.Warning,
        Message = "Sovereignty violation detected (ES). LocationId={LocationId}, DataCategory={DataCategory}, ViolatingRegion={ViolatingRegion}")]
    internal static partial void SovereigntyViolationDetectedES(this ILogger logger, Guid locationId, string dataCategory, string violatingRegion);

    /// <summary>Sovereignty violation resolved on data location.</summary>
    [LoggerMessage(
        EventId = 8688,
        Level = LogLevel.Information,
        Message = "Sovereignty violation resolved (ES). LocationId={LocationId}, Resolution={Resolution}")]
    internal static partial void SovereigntyViolationResolvedES(this ILogger logger, Guid locationId, string resolution);

    /// <summary>Invalid state transition attempted on an aggregate.</summary>
    [LoggerMessage(
        EventId = 8689,
        Level = LogLevel.Warning,
        Message = "Invalid state transition on residency aggregate. AggregateId={AggregateId}, Operation={Operation}")]
    internal static partial void ResidencyInvalidStateTransition(this ILogger logger, Guid aggregateId, string operation);

    /// <summary>Service operation failed with an unexpected exception.</summary>
    [LoggerMessage(
        EventId = 8690,
        Level = LogLevel.Error,
        Message = "Residency service error. Operation={Operation}")]
    internal static partial void ResidencyServiceError(this ILogger logger, string operation, Exception exception);

    /// <summary>Cache hit for a residency read model.</summary>
    [LoggerMessage(
        EventId = 8691,
        Level = LogLevel.Debug,
        Message = "Residency cache hit. CacheKey={CacheKey}")]
    internal static partial void ResidencyCacheHit(this ILogger logger, string cacheKey);

    /// <summary>Cache invalidated for a residency read model.</summary>
    [LoggerMessage(
        EventId = 8692,
        Level = LogLevel.Debug,
        Message = "Residency cache invalidated. CacheKey={CacheKey}")]
    internal static partial void ResidencyCacheInvalidated(this ILogger logger, string cacheKey);
}

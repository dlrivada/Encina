using Microsoft.Extensions.Logging;

namespace Encina.Compliance.DataSubjectRights.Diagnostics;

/// <summary>
/// High-performance structured log messages for the Data Subject Rights module.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 8300-8399 range to avoid collisions with other
/// Encina subsystems (GDPR uses 8100-8199, Consent uses 8200-8299).
/// </para>
/// </remarks>
internal static partial class DSRLogMessages
{
    // ========================================================================
    // Auto-registration log messages (8300-8309)
    // ========================================================================

    /// <summary>DSR auto-registration completed.</summary>
    [LoggerMessage(
        EventId = 8300,
        Level = LogLevel.Information,
        Message = "DSR auto-registration completed. PersonalDataFieldsDiscovered={FieldsDiscovered}, AssembliesScanned={AssembliesScanned}")]
    internal static partial void DSRAutoRegistrationCompleted(this ILogger logger, int fieldsDiscovered, int assembliesScanned);

    /// <summary>DSR auto-registration skipped.</summary>
    [LoggerMessage(
        EventId = 8301,
        Level = LogLevel.Debug,
        Message = "DSR auto-registration skipped (no assemblies configured or auto-registration disabled)")]
    internal static partial void DSRAutoRegistrationSkipped(this ILogger logger);

    /// <summary>Personal data field discovered during auto-registration.</summary>
    [LoggerMessage(
        EventId = 8302,
        Level = LogLevel.Debug,
        Message = "Personal data field discovered. EntityType={EntityType}, FieldName={FieldName}, Category={Category}")]
    internal static partial void PersonalDataFieldDiscovered(this ILogger logger, string entityType, string fieldName, string category);

    /// <summary>Entity type with personal data discovered during auto-registration.</summary>
    [LoggerMessage(
        EventId = 8303,
        Level = LogLevel.Debug,
        Message = "Entity with personal data discovered. EntityType={EntityType}, FieldCount={FieldCount}")]
    internal static partial void PersonalDataEntityDiscovered(this ILogger logger, string entityType, int fieldCount);

    // ========================================================================
    // Health check log messages (8310-8319)
    // ========================================================================

    /// <summary>DSR health check completed.</summary>
    [LoggerMessage(
        EventId = 8310,
        Level = LogLevel.Debug,
        Message = "DSR health check completed. Status={Status}, OverdueRequestCount={OverdueRequestCount}")]
    internal static partial void DSRHealthCheckCompleted(this ILogger logger, string status, int overdueRequestCount);

    // ========================================================================
    // Handler lifecycle (8320-8329)
    // ========================================================================

    /// <summary>DSR request started for a specific right type.</summary>
    [LoggerMessage(
        EventId = 8320,
        Level = LogLevel.Information,
        Message = "DSR request started. RightType={RightType}, SubjectId={SubjectId}")]
    internal static partial void DSRRequestStarted(this ILogger logger, string rightType, string subjectId);

    /// <summary>DSR request completed successfully.</summary>
    [LoggerMessage(
        EventId = 8321,
        Level = LogLevel.Information,
        Message = "DSR request completed. RightType={RightType}, SubjectId={SubjectId}")]
    internal static partial void DSRRequestCompleted(this ILogger logger, string rightType, string subjectId);

    /// <summary>DSR request failed.</summary>
    [LoggerMessage(
        EventId = 8322,
        Level = LogLevel.Warning,
        Message = "DSR request failed. RightType={RightType}, SubjectId={SubjectId}, Reason={Reason}")]
    internal static partial void DSRRequestFailed(this ILogger logger, string rightType, string subjectId, string reason);

    // ========================================================================
    // Access (8323-8324)
    // ========================================================================

    /// <summary>Access request completed with data location and activity counts.</summary>
    [LoggerMessage(
        EventId = 8323,
        Level = LogLevel.Information,
        Message = "Access request completed. SubjectId={SubjectId}, DataLocations={DataLocations}, ProcessingActivities={ProcessingActivities}")]
    internal static partial void AccessRequestCompleted(this ILogger logger, string subjectId, int dataLocations, int processingActivities);

    /// <summary>Failed to retrieve processing activities for access response.</summary>
    [LoggerMessage(
        EventId = 8324,
        Level = LogLevel.Warning,
        Message = "Failed to retrieve processing activities for access response. ErrorMessage={ErrorMessage}")]
    internal static partial void AccessProcessingActivitiesFailed(this ILogger logger, string errorMessage);

    // ========================================================================
    // Erasure (8325-8329)
    // ========================================================================

    /// <summary>Erasure operation started for a subject.</summary>
    [LoggerMessage(
        EventId = 8325,
        Level = LogLevel.Information,
        Message = "Erasure started. SubjectId={SubjectId}, Reason={Reason}")]
    internal static partial void ErasureStarted(this ILogger logger, string subjectId, string reason);

    /// <summary>Erasure operation completed.</summary>
    [LoggerMessage(
        EventId = 8326,
        Level = LogLevel.Information,
        Message = "Erasure completed. SubjectId={SubjectId}, FieldsErased={FieldsErased}, FieldsRetained={FieldsRetained}, FieldsFailed={FieldsFailed}")]
    internal static partial void ErasureCompleted(this ILogger logger, string subjectId, int fieldsErased, int fieldsRetained, int fieldsFailed);

    /// <summary>Erasure operation failed.</summary>
    [LoggerMessage(
        EventId = 8327,
        Level = LogLevel.Warning,
        Message = "Erasure failed. SubjectId={SubjectId}, Reason={Reason}")]
    internal static partial void ErasureFailed(this ILogger logger, string subjectId, string reason);

    /// <summary>Individual field erased successfully.</summary>
    [LoggerMessage(
        EventId = 8328,
        Level = LogLevel.Debug,
        Message = "Field erased. FieldName={FieldName}, EntityType={EntityType}, EntityId={EntityId}")]
    internal static partial void ErasureFieldErased(this ILogger logger, string fieldName, string entityType, string entityId);

    /// <summary>Individual field erasure failed.</summary>
    [LoggerMessage(
        EventId = 8329,
        Level = LogLevel.Warning,
        Message = "Field erasure failed. FieldName={FieldName}, EntityType={EntityType}, EntityId={EntityId}, ErrorMessage={ErrorMessage}")]
    internal static partial void ErasureFieldFailed(this ILogger logger, string fieldName, string entityType, string entityId, string errorMessage);

    // ========================================================================
    // Portability (8330-8333)
    // ========================================================================

    /// <summary>Portability export started.</summary>
    [LoggerMessage(
        EventId = 8330,
        Level = LogLevel.Information,
        Message = "Portability export started. SubjectId={SubjectId}, Format={Format}")]
    internal static partial void PortabilityExportStarted(this ILogger logger, string subjectId, string format);

    /// <summary>Portability export completed.</summary>
    [LoggerMessage(
        EventId = 8331,
        Level = LogLevel.Information,
        Message = "Portability export completed. SubjectId={SubjectId}, Format={Format}, PortableFields={PortableFields}, TotalFields={TotalFields}")]
    internal static partial void PortabilityExportCompleted(this ILogger logger, string subjectId, string format, int portableFields, int totalFields);

    /// <summary>Portability export failed.</summary>
    [LoggerMessage(
        EventId = 8332,
        Level = LogLevel.Warning,
        Message = "Portability export failed. SubjectId={SubjectId}, Format={Format}, Reason={Reason}")]
    internal static partial void PortabilityExportFailed(this ILogger logger, string subjectId, string format, string reason);

    /// <summary>Requested export format is not supported.</summary>
    [LoggerMessage(
        EventId = 8333,
        Level = LogLevel.Warning,
        Message = "Export format not supported. Format={Format}")]
    internal static partial void ExportFormatNotSupported(this ILogger logger, string format);

    // ========================================================================
    // Rectification (8334)
    // ========================================================================

    /// <summary>Rectification completed for a subject's field.</summary>
    [LoggerMessage(
        EventId = 8334,
        Level = LogLevel.Information,
        Message = "Rectification completed. SubjectId={SubjectId}, FieldName={FieldName}")]
    internal static partial void RectificationCompleted(this ILogger logger, string subjectId, string fieldName);

    // ========================================================================
    // Restriction (8335-8337)
    // ========================================================================

    /// <summary>Processing restriction applied for a subject.</summary>
    [LoggerMessage(
        EventId = 8335,
        Level = LogLevel.Information,
        Message = "Processing restriction applied. SubjectId={SubjectId}, Reason={Reason}")]
    internal static partial void RestrictionApplied(this ILogger logger, string subjectId, string reason);

    /// <summary>Processing blocked due to active restriction (Block mode).</summary>
    [LoggerMessage(
        EventId = 8336,
        Level = LogLevel.Warning,
        Message = "Processing blocked for restricted data subject. SubjectId={SubjectId}, RequestType={RequestType}")]
    internal static partial void RestrictionBlocked(this ILogger logger, string subjectId, string requestType);

    /// <summary>Active restriction detected but processing allowed (Warn mode).</summary>
    [LoggerMessage(
        EventId = 8337,
        Level = LogLevel.Warning,
        Message = "Processing restriction active — proceeding in Warn mode. SubjectId={SubjectId}, RequestType={RequestType}")]
    internal static partial void RestrictionWarned(this ILogger logger, string subjectId, string requestType);

    // ========================================================================
    // Objection (8338)
    // ========================================================================

    /// <summary>Objection recorded for a subject against a processing purpose.</summary>
    [LoggerMessage(
        EventId = 8338,
        Level = LogLevel.Information,
        Message = "Objection recorded. SubjectId={SubjectId}, ProcessingPurpose={ProcessingPurpose}")]
    internal static partial void ObjectionRecorded(this ILogger logger, string subjectId, string processingPurpose);

    // ========================================================================
    // Pipeline / restriction check (8339-8341)
    // ========================================================================

    /// <summary>Restriction check skipped because enforcement mode is Disabled.</summary>
    [LoggerMessage(
        EventId = 8339,
        Level = LogLevel.Trace,
        Message = "Restriction check skipped (enforcement disabled). RequestType={RequestType}")]
    internal static partial void RestrictionCheckDisabled(this ILogger logger, string requestType);

    /// <summary>Restriction check skipped because no personal data attributes found.</summary>
    [LoggerMessage(
        EventId = 8340,
        Level = LogLevel.Trace,
        Message = "Restriction check skipped (no personal data attributes). RequestType={RequestType}")]
    internal static partial void RestrictionCheckNoAttributes(this ILogger logger, string requestType);

    /// <summary>Subject ID could not be extracted from request — restriction check skipped.</summary>
    [LoggerMessage(
        EventId = 8341,
        Level = LogLevel.Debug,
        Message = "Subject ID not extracted — skipping restriction check. RequestType={RequestType}")]
    internal static partial void SubjectIdNotExtracted(this ILogger logger, string requestType);

    // ========================================================================
    // Notification (8342-8343)
    // ========================================================================

    /// <summary>Article 19 notification published successfully.</summary>
    [LoggerMessage(
        EventId = 8342,
        Level = LogLevel.Debug,
        Message = "Notification published. NotificationType={NotificationType}")]
    internal static partial void NotificationPublished(this ILogger logger, string notificationType);

    /// <summary>Failed to publish Article 19 notification.</summary>
    [LoggerMessage(
        EventId = 8343,
        Level = LogLevel.Warning,
        Message = "Notification publish failed. NotificationType={NotificationType}, ErrorMessage={ErrorMessage}")]
    internal static partial void NotificationPublishFailed(this ILogger logger, string notificationType, string errorMessage);

    // ========================================================================
    // Audit trail (8344-8345)
    // ========================================================================

    /// <summary>Audit entry record failed.</summary>
    [LoggerMessage(
        EventId = 8344,
        Level = LogLevel.Warning,
        Message = "Failed to record audit entry. DSRRequestId={DSRRequestId}, ErrorMessage={ErrorMessage}")]
    internal static partial void AuditEntryFailed(this ILogger logger, string dsrRequestId, string errorMessage);

    /// <summary>No personal data found for subject during erasure.</summary>
    [LoggerMessage(
        EventId = 8345,
        Level = LogLevel.Information,
        Message = "No personal data found for subject during erasure. SubjectId={SubjectId}")]
    internal static partial void ErasureNoDataFound(this ILogger logger, string subjectId);

    // ========================================================================
    // Restriction store error (8346)
    // ========================================================================

    /// <summary>Failed to check restriction status — proceeding without check (fail-open).</summary>
    [LoggerMessage(
        EventId = 8346,
        Level = LogLevel.Warning,
        Message = "Restriction check store error — proceeding without check. SubjectId={SubjectId}, RequestType={RequestType}, ErrorMessage={ErrorMessage}")]
    internal static partial void RestrictionCheckStoreError(this ILogger logger, string subjectId, string requestType, string errorMessage);
}

namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Type of security event that may indicate a potential data breach.
/// </summary>
/// <remarks>
/// <para>
/// Security events are the inputs to the breach detection engine. Applications create
/// <see cref="SecurityEvent"/> instances with the appropriate type from their security
/// infrastructure (SIEM, application logs, monitoring tools) and submit them for
/// evaluation against registered <c>IBreachDetectionRule</c> implementations.
/// </para>
/// <para>
/// Per EDPB Guidelines 9/2022, controllers should implement appropriate technical measures
/// to detect breaches promptly. Automated detection complements manual reporting and
/// helps meet the 72-hour notification deadline under Art. 33(1).
/// </para>
/// </remarks>
public enum SecurityEventType
{
    /// <summary>
    /// Unauthorized access to personal data (failed or successful).
    /// </summary>
    /// <remarks>
    /// Includes failed authentication attempts, brute-force attacks,
    /// unauthorized API access, and credential stuffing.
    /// </remarks>
    UnauthorizedAccess = 0,

    /// <summary>
    /// Large-volume or unusual data extraction from systems containing personal data.
    /// </summary>
    /// <remarks>
    /// Includes bulk downloads, mass API queries, database dumps,
    /// and unusual export activity.
    /// </remarks>
    DataExfiltration = 1,

    /// <summary>
    /// Unauthorized elevation of user privileges in systems processing personal data.
    /// </summary>
    /// <remarks>
    /// Includes role escalation, admin access acquisition,
    /// and unauthorized permission changes.
    /// </remarks>
    PrivilegeEscalation = 2,

    /// <summary>
    /// Unusual or anomalous database query patterns targeting personal data.
    /// </summary>
    /// <remarks>
    /// Includes SQL injection attempts, unusual query volumes,
    /// queries on sensitive tables, and abnormal access patterns.
    /// </remarks>
    AnomalousQuery = 3,

    /// <summary>
    /// Unauthorized modification of personal data.
    /// </summary>
    /// <remarks>
    /// Includes unauthorized record updates, data tampering,
    /// and integrity violations.
    /// </remarks>
    DataModification = 4,

    /// <summary>
    /// System intrusion or compromise affecting data processing infrastructure.
    /// </summary>
    /// <remarks>
    /// Includes network intrusions, server compromises,
    /// and infrastructure-level attacks.
    /// </remarks>
    SystemIntrusion = 5,

    /// <summary>
    /// Malware detected on systems that process or store personal data.
    /// </summary>
    /// <remarks>
    /// Includes ransomware, spyware, keyloggers,
    /// and data-exfiltrating malware.
    /// </remarks>
    MalwareDetected = 6,

    /// <summary>
    /// Suspicious activity from an authorized internal user.
    /// </summary>
    /// <remarks>
    /// Includes unusual data access patterns by employees,
    /// access outside business hours, and policy violations.
    /// </remarks>
    InsiderThreat = 7,

    /// <summary>
    /// Custom security event type defined by the application.
    /// </summary>
    /// <remarks>
    /// Use this type for application-specific security events that don't
    /// fit the predefined categories. Custom detection rules can evaluate
    /// these events based on the <see cref="SecurityEvent.Metadata"/> dictionary.
    /// </remarks>
    Custom = 8
}

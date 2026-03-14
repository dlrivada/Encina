namespace Encina.Compliance.PrivacyByDesign.Model;

/// <summary>
/// Represents a single Privacy by Design violation detected during request validation.
/// </summary>
/// <remarks>
/// <para>
/// A violation is produced when a request field fails data minimization, purpose limitation,
/// or default privacy checks. Violations are collected in <see cref="PrivacyValidationResult"/>
/// and used by the pipeline behavior to decide whether to block, warn, or allow the request.
/// </para>
/// <para>
/// Per GDPR Article 25, the controller must implement appropriate measures to ensure data
/// protection principles are effectively implemented. Each violation identifies a specific
/// field and the principle that was breached.
/// </para>
/// </remarks>
/// <param name="FieldName">The name of the property that caused the violation.</param>
/// <param name="ViolationType">The category of Privacy by Design principle that was violated.</param>
/// <param name="Message">A human-readable description of the violation.</param>
/// <param name="Severity">The severity level of the violation.</param>
public sealed record PrivacyViolation(
    string FieldName,
    PrivacyViolationType ViolationType,
    string Message,
    MinimizationSeverity Severity);

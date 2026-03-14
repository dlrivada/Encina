namespace Encina.Compliance.PrivacyByDesign.Model;

/// <summary>
/// Describes a field that has been identified as not strictly necessary for the declared
/// processing purpose.
/// </summary>
/// <remarks>
/// <para>
/// A field is "unnecessary" when it is decorated with <c>[NotStrictlyNecessary]</c>.
/// The violation is only reported when the field has a non-default value in the request,
/// indicating that unnecessary data is being actively collected.
/// </para>
/// <para>
/// Per GDPR Article 25(2), personal data that are not necessary for the specific purpose
/// should not be processed by default. This record provides the details needed for
/// compliance reporting and remediation.
/// </para>
/// </remarks>
/// <param name="FieldName">The name of the property on the request type.</param>
/// <param name="Reason">
/// The reason this field is not strictly necessary, as declared in the
/// <c>[NotStrictlyNecessary(Reason = "...")]</c> attribute.
/// </param>
/// <param name="HasValue">
/// Whether the field has a non-default value in the current request instance.
/// A violation is only reported when this is <see langword="true"/>.
/// </param>
/// <param name="Severity">The severity level of the minimization finding.</param>
public sealed record UnnecessaryFieldInfo(
    string FieldName,
    string Reason,
    bool HasValue,
    MinimizationSeverity Severity);

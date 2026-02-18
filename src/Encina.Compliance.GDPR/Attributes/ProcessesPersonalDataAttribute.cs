namespace Encina.Compliance.GDPR;

/// <summary>
/// Marker attribute indicating that a request type processes personal data subject to GDPR.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute on requests that access or process personal data but do not yet have
/// a full <see cref="ProcessingActivityAttribute"/> declaration. This serves as:
/// </para>
/// <list type="bullet">
/// <item><b>Discovery aid</b>: Helps identify all request types that touch personal data</item>
/// <item><b>Compliance check</b>: The <c>GDPRCompliancePipelineBehavior</c> will warn (or block,
/// depending on <c>GDPROptions.BlockUnregisteredProcessing</c>) when a request marked with
/// this attribute does not have a corresponding registered processing activity</item>
/// <item><b>Audit trail</b>: Processing of marked requests is logged for accountability (Article 5(2))</item>
/// </list>
/// <para>
/// For full compliance, consider upgrading to <see cref="ProcessingActivityAttribute"/> which
/// provides complete Article 30 documentation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple marker for requests that read personal data
/// [ProcessesPersonalData]
/// public record GetUserProfileQuery(Guid UserId) : IQuery&lt;UserProfile&gt;;
///
/// // For full compliance, prefer ProcessingActivityAttribute instead
/// [ProcessingActivity(
///     Purpose = "Display user profile",
///     LawfulBasis = LawfulBasis.Contract,
///     DataCategories = new[] { "Name", "Email" },
///     DataSubjects = new[] { "Users" },
///     RetentionDays = 365)]
/// public record GetUserProfileQuery(Guid UserId) : IQuery&lt;UserProfile&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ProcessesPersonalDataAttribute : Attribute;

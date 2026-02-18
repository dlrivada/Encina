namespace Encina.Compliance.GDPR;

/// <summary>
/// Default implementation of <see cref="IDataProtectionOfficer"/> providing DPO contact information.
/// </summary>
/// <remarks>
/// <para>
/// This immutable record stores the DPO contact details required by GDPR Articles 37-39.
/// It is typically configured once at application startup via <c>GDPROptions</c> and used
/// across the application for RoPA exports and compliance reporting.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var dpo = new DataProtectionOfficer("Jane Smith", "dpo@company.com", "+34 600 000 000");
///
/// // Or without phone
/// var dpo = new DataProtectionOfficer("Jane Smith", "dpo@company.com");
/// </code>
/// </example>
/// <param name="Name">Full name of the Data Protection Officer.</param>
/// <param name="Email">Email address of the Data Protection Officer.</param>
/// <param name="Phone">Optional phone number of the Data Protection Officer.</param>
public sealed record DataProtectionOfficer(string Name, string Email, string? Phone = null) : IDataProtectionOfficer;

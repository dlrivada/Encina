namespace Encina.Compliance.GDPR.Export;

/// <summary>
/// Metadata included in RoPA exports for audit trail and identification.
/// </summary>
/// <remarks>
/// Captures the controller identity (Article 30(1)(a)), DPO contact (Article 37(7)),
/// and the export timestamp for document versioning.
/// </remarks>
/// <param name="ControllerName">Name of the data controller (organization).</param>
/// <param name="ControllerEmail">Contact email of the data controller.</param>
/// <param name="ExportedAtUtc">UTC timestamp when the export was generated.</param>
/// <param name="DataProtectionOfficer">DPO contact information, or <c>null</c> if not designated.</param>
public sealed record RoPAExportMetadata(
    string ControllerName,
    string ControllerEmail,
    DateTimeOffset ExportedAtUtc,
    IDataProtectionOfficer? DataProtectionOfficer = null);

using System.Reflection;

namespace Encina.Diagnostics;

/// <summary>
/// Centralized EventId range allocation for all Encina packages.
/// Each package MUST register its range here before using EventIds.
/// Validated at test time by architecture tests in <c>Encina.Testing.Architecture</c>.
/// </summary>
/// <remarks>
/// <para>
/// Allocation policy:
/// <list type="bullet">
/// <item>Each package gets a contiguous, non-overlapping range</item>
/// <item>Ranges are grouped by functional area (core, messaging, security, compliance)</item>
/// <item>New packages must register a free range via PR before using EventIds</item>
/// <item>Use <see cref="GetAllRanges"/> to discover all registered allocations</item>
/// </list>
/// </para>
/// </remarks>
public static class EventIdRanges
{
    // ═══════════════════════════════════════════════════════════════════════
    // Core packages (1-99)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Security.Sanitization — input sanitization pipeline.</summary>
    public static readonly (int Min, int Max) Sanitization = (1, 99);

    // ═══════════════════════════════════════════════════════════════════════
    // DomainModeling (1100-1699)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.DomainModeling — repository operations.</summary>
    public static readonly (int Min, int Max) DomainModelingRepository = (1100, 1199);

    /// <summary>Encina.DomainModeling — unit of work / transaction operations.</summary>
    public static readonly (int Min, int Max) DomainModelingUnitOfWork = (1200, 1299);

    /// <summary>Encina.DomainModeling — bulk operations.</summary>
    public static readonly (int Min, int Max) DomainModelingBulkOperations = (1300, 1399);

    /// <summary>Encina.DomainModeling — specification evaluation.</summary>
    public static readonly (int Min, int Max) DomainModelingSpecification = (1400, 1499);

    /// <summary>Encina.EntityFrameworkCore — soft delete operations.</summary>
    public static readonly (int Min, int Max) EntityFrameworkCoreSoftDelete = (1500, 1599);

    /// <summary>Encina.DomainModeling — audit trail operations.</summary>
    public static readonly (int Min, int Max) DomainModelingAudit = (1600, 1699);

    // ═══════════════════════════════════════════════════════════════════════
    // Security Audit (1700-1799)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Security.Audit — read audit operations.</summary>
    public static readonly (int Min, int Max) SecurityAuditRead = (1700, 1799);

    // ═══════════════════════════════════════════════════════════════════════
    // Infrastructure (1800-1999)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Tenancy — multi-tenancy operations.</summary>
    public static readonly (int Min, int Max) Tenancy = (1800, 1899);

    /// <summary>Encina — module isolation operations.</summary>
    public static readonly (int Min, int Max) ModuleIsolation = (1900, 1999);

    // ═══════════════════════════════════════════════════════════════════════
    // Messaging (2000-2499)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Messaging — outbox store operations.</summary>
    public static readonly (int Min, int Max) MessagingOutbox = (2000, 2099);

    /// <summary>Encina.Messaging — inbox store operations.</summary>
    public static readonly (int Min, int Max) MessagingInbox = (2100, 2199);

    /// <summary>Encina.Messaging — saga store operations.</summary>
    public static readonly (int Min, int Max) MessagingSaga = (2200, 2299);

    /// <summary>Encina.Messaging — scheduled message store operations.</summary>
    public static readonly (int Min, int Max) MessagingScheduling = (2300, 2399);

    /// <summary>Encina.EntityFrameworkCore — query cache operations.</summary>
    public static readonly (int Min, int Max) EntityFrameworkCoreQueryCache = (2400, 2449);

    /// <summary>Encina.Messaging.Encryption — message encryption/decryption.</summary>
    public static readonly (int Min, int Max) MessagingEncryption = (2450, 2499);

    // ═══════════════════════════════════════════════════════════════════════
    // Domain Events & Event Sourcing (2500-2699)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.DomainModeling — domain event operations.</summary>
    public static readonly (int Min, int Max) DomainModelingDomainEvents = (2500, 2549);

    /// <summary>Encina.Audit.Marten — event-sourced audit store.</summary>
    public static readonly (int Min, int Max) AuditMarten = (2550, 2599);

    // ═══════════════════════════════════════════════════════════════════════
    // Security packages (8000-8099)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Security — core authorization pipeline.</summary>
    public static readonly (int Min, int Max) Security = (8000, 8009);

    /// <summary>Encina.Security.PII — PII masking operations.</summary>
    public static readonly (int Min, int Max) SecurityPII = (8010, 8029);

    /// <summary>Encina.IdGeneration — distributed ID generation.</summary>
    public static readonly (int Min, int Max) IdGeneration = (8030, 8099);

    // ═══════════════════════════════════════════════════════════════════════
    // Compliance packages (8100-8999)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Compliance.GDPR — GDPR compliance pipeline.</summary>
    public static readonly (int Min, int Max) ComplianceGDPR = (8100, 8199);

    /// <summary>Encina.Compliance.Consent — consent management.</summary>
    public static readonly (int Min, int Max) ComplianceConsent = (8200, 8299);

    /// <summary>Encina.Compliance.DataSubjectRights — data subject rights (Art. 15-22).</summary>
    public static readonly (int Min, int Max) ComplianceDSR = (8300, 8349);

    /// <summary>Encina.Compliance.LawfulBasis — lawful basis validation (Art. 6).</summary>
    public static readonly (int Min, int Max) ComplianceLawfulBasis = (8350, 8399);

    /// <summary>Encina.Compliance.Anonymization — data anonymization.</summary>
    public static readonly (int Min, int Max) ComplianceAnonymization = (8400, 8449);

    /// <summary>Encina.Marten.GDPR — crypto-shredding for GDPR.</summary>
    public static readonly (int Min, int Max) MartenGDPRCryptoShredding = (8450, 8499);

    /// <summary>Encina.Compliance.Retention — data retention enforcement.</summary>
    public static readonly (int Min, int Max) ComplianceRetention = (8500, 8599);

    /// <summary>Encina.Compliance.DataResidency — data residency compliance.</summary>
    public static readonly (int Min, int Max) ComplianceDataResidency = (8600, 8699);

    /// <summary>Encina.Compliance.BreachNotification — breach notification (Art. 33-34).</summary>
    public static readonly (int Min, int Max) ComplianceBreachNotification = (8700, 8799);

    /// <summary>Encina.Compliance.DPIA — Data Protection Impact Assessment (Art. 35).</summary>
    public static readonly (int Min, int Max) ComplianceDPIA = (8800, 8899);

    /// <summary>Encina.Compliance.PrivacyByDesign — privacy by design (Art. 25).</summary>
    public static readonly (int Min, int Max) CompliancePrivacyByDesign = (8900, 8949);

    /// <summary>Encina.Security.Secrets — distributed cache operations. Event IDs: 8950-8999.</summary>
    public static readonly (int Min, int Max) SecuritySecrets = (8950, 8999);

    // ═══════════════════════════════════════════════════════════════════════
    // Security extensions (9000-9199)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Security.ABAC — attribute-based access control.</summary>
    public static readonly (int Min, int Max) SecurityABAC = (9000, 9099);

    /// <summary>Encina.Security.AntiTampering — signature validation.</summary>
    public static readonly (int Min, int Max) SecurityAntiTampering = (9100, 9199);

    // ═══════════════════════════════════════════════════════════════════════
    // Compliance extensions (9200-9499)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Compliance.NIS2 — NIS2 Directive cybersecurity compliance.</summary>
    public static readonly (int Min, int Max) ComplianceNIS2 = (9200, 9299);

    /// <summary>Encina.Compliance.CrossBorderTransfer — cross-border transfer (Ch. V).</summary>
    public static readonly (int Min, int Max) ComplianceCrossBorderTransfer = (9300, 9399);

    /// <summary>Encina.Compliance.ProcessorAgreements — processor agreement management (Art. 28).</summary>
    public static readonly (int Min, int Max) ComplianceProcessorAgreements = (9400, 9499);

    // ═══════════════════════════════════════════════════════════════════════
    // AI Act & future compliance modules (9500-9999)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Compliance.AIAct — EU AI Act compliance pipeline.</summary>
    public static readonly (int Min, int Max) ComplianceAIAct = (9500, 9599);

    /// <summary>Encina.Compliance.Attestation — tamper-evident audit attestation.</summary>
    public static readonly (int Min, int Max) ComplianceAttestation = (9600, 9699);

    /// <summary>
    /// Returns all registered ranges for validation and diagnostics.
    /// </summary>
    /// <returns>A list of tuples containing range name, min, and max EventId.</returns>
    public static IReadOnlyList<(string Name, int Min, int Max)> GetAllRanges()
    {
        return typeof(EventIdRanges)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof((int, int)))
            .Select(f =>
            {
                var range = ((int Min, int Max))f.GetValue(null)!;
                return (f.Name, range.Min, range.Max);
            })
            .OrderBy(r => r.Min)
            .ToList()
            .AsReadOnly();
    }
}

namespace Encina.Compliance.CrossBorderTransfer.Model;

/// <summary>
/// Represents a request to validate an international data transfer for GDPR Chapter V compliance.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="TransferRequest"/> captures the essential information needed to evaluate
/// whether a proposed data transfer between two countries is compliant with GDPR Articles 44-49
/// and the Schrems II judgment requirements.
/// </para>
/// <para>
/// The request is evaluated by <c>ITransferValidator</c> which checks adequacy decisions,
/// approved transfers, SCC agreements, and TIA requirements to produce a
/// <see cref="TransferValidationOutcome"/>.
/// </para>
/// <para>
/// <see cref="TenantId"/> and <see cref="ModuleId"/> support multi-tenancy and module isolation
/// in the pipeline context, ensuring transfer validation is scoped to the correct tenant
/// and module boundaries.
/// </para>
/// </remarks>
public sealed record TransferRequest
{
    /// <summary>
    /// ISO 3166-1 alpha-2 country code of the data source (exporter) location.
    /// </summary>
    /// <example>"DE" (Germany), "FR" (France), "ES" (Spain)</example>
    public required string SourceCountryCode { get; init; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code of the data destination (importer) location.
    /// </summary>
    /// <example>"US" (United States), "IN" (India), "CN" (China)</example>
    public required string DestinationCountryCode { get; init; }

    /// <summary>
    /// Category of personal data being transferred.
    /// </summary>
    /// <remarks>
    /// Used to determine the level of protection required and whether special category
    /// data (Art. 9) or criminal conviction data (Art. 10) is involved, which may impose
    /// additional transfer restrictions.
    /// </remarks>
    /// <example>"personal-data", "sensitive-data", "health-data", "financial-data"</example>
    public required string DataCategory { get; init; }

    /// <summary>
    /// Identifier of the data processor or sub-processor involved in the transfer.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for controller-to-controller transfers. When specified, used to look up
    /// the applicable SCC agreement and determine the correct <see cref="SCCModule"/>.
    /// </remarks>
    public string? ProcessorId { get; init; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    /// <remarks>
    /// When set, transfer validation is scoped to approved transfers and SCC agreements
    /// belonging to this tenant. Supports the Encina multi-tenancy cross-cutting function.
    /// </remarks>
    public string? TenantId { get; init; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    /// <remarks>
    /// When set, transfer validation is scoped to the specified module boundary.
    /// Supports the Encina module isolation cross-cutting function.
    /// </remarks>
    public string? ModuleId { get; init; }
}

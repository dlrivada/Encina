namespace Encina.Compliance.DataResidency.Attributes;

/// <summary>
/// Declares data residency requirements for a request type.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a request type, the <see cref="DataResidencyPipelineBehavior{TRequest, TResponse}"/>
/// automatically validates that the current processing region complies with the declared
/// residency policy before the request handler is invoked.
/// </para>
/// <para>
/// Per GDPR Article 44 (general principle for transfers), any transfer of personal data
/// to a third country or international organisation shall take place only if the conditions
/// laid down in Chapter V are complied with by the controller and processor.
/// This attribute enables declarative enforcement of those conditions.
/// </para>
/// <para>
/// The <see cref="AllowedRegionCodes"/> specify which regions are permitted for the data
/// category. These codes correspond to <see cref="Model.Region.Code"/> values
/// (ISO 3166-1 alpha-2 or custom identifiers).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Only allow processing in Germany and France
/// [DataResidency("DE", "FR", DataCategory = "healthcare-data")]
/// public record CreatePatientRecordCommand(string PatientId) : ICommand&lt;PatientId&gt;;
///
/// // EU-only with adequacy decision requirement
/// [DataResidency("DE", "FR", "NL", "BE",
///     DataCategory = "financial-records",
///     RequireAdequacyDecision = true)]
/// public record CreateInvoiceCommand(string CustomerId) : ICommand&lt;InvoiceId&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class DataResidencyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataResidencyAttribute"/> class
    /// with the specified allowed region codes.
    /// </summary>
    /// <param name="allowedRegionCodes">
    /// The region codes where data of this type is permitted to be processed.
    /// Empty array means all regions are allowed (no restriction).
    /// </param>
    public DataResidencyAttribute(params string[] allowedRegionCodes)
    {
        AllowedRegionCodes = allowedRegionCodes;
    }

    /// <summary>
    /// Gets the region codes where data processing is permitted.
    /// </summary>
    /// <remarks>
    /// Each code should match a <see cref="Model.Region.Code"/> value
    /// (e.g., "DE", "FR", "US", "EU", or a custom identifier like "AZURE-WESTEU").
    /// An empty array means no geographic restrictions are imposed.
    /// </remarks>
    public string[] AllowedRegionCodes { get; }

    /// <summary>
    /// Gets or sets the data category for residency policy resolution.
    /// </summary>
    /// <remarks>
    /// Maps to the <c>DataCategory</c> in the policy store.
    /// If not set, the pipeline behavior derives the category from the request type name.
    /// </remarks>
    public string? DataCategory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an EU adequacy decision is required
    /// for the processing region.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the pipeline behavior additionally validates that the
    /// current region has an EU adequacy decision (<see cref="Model.Region.HasAdequacyDecision"/>).
    /// This provides a stricter level of compliance enforcement per GDPR Article 45.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool RequireAdequacyDecision { get; set; }
}

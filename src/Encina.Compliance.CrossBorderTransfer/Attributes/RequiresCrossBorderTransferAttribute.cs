namespace Encina.Compliance.CrossBorderTransfer.Attributes;

/// <summary>
/// Declares that a request type involves an international data transfer that must be validated
/// against GDPR Chapter V requirements before execution.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used by
/// <see cref="Pipeline.TransferBlockingPipelineBehavior{TRequest, TResponse}"/>
/// to enforce cross-border transfer validation in the Encina pipeline. When a request
/// decorated with this attribute is dispatched, the pipeline verifies that the transfer
/// has a valid legal basis (adequacy decision, SCC agreement, TIA, etc.) before the
/// handler executes.
/// </para>
/// <para>
/// The destination country can be specified statically via <see cref="Destination"/> or
/// dynamically via <see cref="DestinationProperty"/> (using cached reflection). The source
/// country can likewise be specified via <see cref="SourceProperty"/> or defaults to the
/// system's configured source country.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Static destination
/// [RequiresCrossBorderTransfer(Destination = "US", DataCategory = "personal-data")]
/// public sealed record SyncToUSCommand : ICommand&lt;Unit&gt;;
///
/// // Dynamic destination from request property
/// [RequiresCrossBorderTransfer(
///     DestinationProperty = "TargetCountryCode",
///     DataCategory = "health-data")]
/// public sealed record TransferPatientRecordsCommand(string TargetCountryCode) : ICommand&lt;Unit&gt;;
///
/// // Dynamic source and destination
/// [RequiresCrossBorderTransfer(
///     SourceProperty = "FromCountry",
///     DestinationProperty = "ToCountry",
///     DataCategory = "financial-data")]
/// public sealed record ReplicateDataCommand(string FromCountry, string ToCountry) : ICommand&lt;Unit&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequiresCrossBorderTransferAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 country code of the data destination (importer).
    /// </summary>
    /// <remarks>
    /// Use this for statically known destinations. When the destination varies per request,
    /// use <see cref="DestinationProperty"/> instead.
    /// </remarks>
    /// <example>"US", "CN", "IN"</example>
    public string? Destination { get; set; }

    /// <summary>
    /// Gets or sets the category of personal data being transferred.
    /// </summary>
    /// <remarks>
    /// Used to determine the level of protection required and whether special category
    /// data (Art. 9) or criminal conviction data (Art. 10) is involved, which may impose
    /// additional transfer restrictions.
    /// </remarks>
    /// <example>"personal-data", "sensitive-data", "health-data", "financial-data"</example>
    public string DataCategory { get; set; } = "personal-data";

    /// <summary>
    /// Gets or sets the name of the request property that contains the source country code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When specified, the pipeline behavior uses cached reflection to extract the source
    /// country from the request instance. When <c>null</c>, the system uses the configured
    /// default source country.
    /// </para>
    /// <para>
    /// The property must be a public readable property that returns a <see cref="string"/>.
    /// </para>
    /// </remarks>
    /// <example>"SourceCountryCode", "FromCountry"</example>
    public string? SourceProperty { get; set; }

    /// <summary>
    /// Gets or sets the name of the request property that contains the destination country code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When specified, the pipeline behavior uses cached reflection to extract the destination
    /// country from the request instance at runtime. This takes precedence over <see cref="Destination"/>.
    /// </para>
    /// <para>
    /// The property must be a public readable property that returns a <see cref="string"/>.
    /// </para>
    /// </remarks>
    /// <example>"DestinationCountryCode", "TargetCountry", "ToCountry"</example>
    public string? DestinationProperty { get; set; }
}

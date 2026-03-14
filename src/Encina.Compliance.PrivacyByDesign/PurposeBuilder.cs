using Encina.Compliance.PrivacyByDesign.Model;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Fluent builder for constructing <see cref="PurposeDefinition"/> instances
/// during configuration.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="PrivacyByDesignOptions.AddPurpose(string, Action{PurposeBuilder})"/>
/// and <see cref="PrivacyByDesignOptions.AddPurpose(string, string, Action{PurposeBuilder})"/>
/// to declaratively define processing purposes at DI registration time.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(b), personal data shall be "collected for specified, explicit and
/// legitimate purposes." Each builder instance produces a <see cref="PurposeDefinition"/>
/// that formalizes one such purpose.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// options.AddPurpose("Order Processing", purpose =>
/// {
///     purpose.Description = "Processing personal data for order fulfillment.";
///     purpose.LegalBasis = "Contract";
///     purpose.AllowedFields.AddRange(["ProductId", "Quantity", "ShippingAddress"]);
///     purpose.ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(2);
/// });
/// </code>
/// </example>
public sealed class PurposeBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PurposeBuilder"/> class.
    /// </summary>
    /// <param name="name">The purpose name.</param>
    public PurposeBuilder(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <summary>
    /// Gets the purpose name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the module identifier for module-scoped purposes,
    /// or <see langword="null"/> for global scope.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// Gets or sets a detailed description of why personal data is being processed
    /// under this purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legal basis for processing under this purpose
    /// (e.g., "Consent", "Contract", "Legitimate Interest").
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 6(1), processing is lawful only if at least one legal basis applies.
    /// </remarks>
    public string LegalBasis { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of fields that are allowed to be processed under this purpose.
    /// </summary>
    /// <remarks>
    /// Fields not in this list that appear in a request with this declared purpose
    /// are flagged as purpose limitation violations.
    /// </remarks>
    public List<string> AllowedFields { get; } = [];

    /// <summary>
    /// Gets or sets the UTC timestamp when this purpose definition expires,
    /// or <see langword="null"/> if it does not expire.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Builds a <see cref="PurposeDefinition"/> from this builder's configuration.
    /// </summary>
    /// <param name="timeProvider">Time provider for the creation timestamp.</param>
    /// <returns>A fully constructed <see cref="PurposeDefinition"/>.</returns>
    internal PurposeDefinition Build(TimeProvider timeProvider) => new()
    {
        PurposeId = Guid.NewGuid().ToString("N"),
        Name = Name,
        Description = Description,
        LegalBasis = LegalBasis,
        AllowedFields = AllowedFields,
        ModuleId = ModuleId,
        CreatedAtUtc = timeProvider.GetUtcNow(),
        ExpiresAtUtc = ExpiresAtUtc
    };
}

namespace Encina.Compliance.PrivacyByDesign.Model;

/// <summary>
/// Defines a processing purpose with its allowed fields and metadata.
/// </summary>
/// <remarks>
/// <para>
/// A purpose definition declares the legal basis and scope for processing personal data.
/// It is used by <c>IPrivacyByDesignValidator</c> to validate that request fields comply
/// with the declared processing purpose.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(b), personal data shall be "collected for specified, explicit and
/// legitimate purposes and not further processed in a manner that is incompatible with those
/// purposes." Each <see cref="PurposeDefinition"/> represents one such specified purpose.
/// </para>
/// <para>
/// In modular monolith architectures, purposes can be scoped to a specific module via
/// <see cref="ModuleId"/>, ensuring that purpose definitions in different modules are
/// tracked independently.
/// </para>
/// </remarks>
public sealed record PurposeDefinition
{
    /// <summary>
    /// The unique identifier for this purpose definition.
    /// </summary>
    public required string PurposeId { get; init; }

    /// <summary>
    /// A human-readable name for this processing purpose.
    /// </summary>
    /// <example>"Order Processing", "Marketing Analytics", "Fraud Detection".</example>
    public required string Name { get; init; }

    /// <summary>
    /// A detailed description of why personal data is being processed under this purpose.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The legal basis for processing under this purpose (e.g., "Consent", "Contract",
    /// "Legitimate Interest").
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 6(1), processing is lawful only if at least one legal basis applies.
    /// This field documents which basis justifies the processing.
    /// </remarks>
    public required string LegalBasis { get; init; }

    /// <summary>
    /// The fields that are allowed to be processed under this purpose.
    /// </summary>
    /// <remarks>
    /// Fields not in this list that appear in a request with this declared purpose
    /// are flagged as purpose limitation violations.
    /// </remarks>
    public required IReadOnlyList<string> AllowedFields { get; init; }

    /// <summary>
    /// The tenant identifier for multi-tenancy support, or <see langword="null"/> when tenancy is not used.
    /// </summary>
    /// <remarks>
    /// When multi-tenancy is enabled, purpose definitions are scoped to a specific tenant.
    /// This is a soft dependency: Privacy by Design works identically with or without multi-tenancy.
    /// </remarks>
    public string? TenantId { get; init; }

    /// <summary>
    /// The module identifier for modular monolith isolation, or <see langword="null"/>
    /// when module isolation is not used.
    /// </summary>
    /// <remarks>
    /// In modular monolith architectures, purpose definitions can be scoped to a specific module.
    /// This ensures that the same purpose name in different modules is tracked independently.
    /// This is a soft dependency: Privacy by Design works identically with or without module isolation.
    /// </remarks>
    public string? ModuleId { get; init; }

    /// <summary>
    /// The UTC timestamp when this purpose definition was created.
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// The UTC timestamp when this purpose definition expires, or <see langword="null"/> if it does not expire.
    /// </summary>
    /// <remarks>
    /// Expired purpose definitions are no longer valid for justifying data processing.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>
    /// Determines whether this purpose definition is currently valid for justifying data processing.
    /// </summary>
    /// <param name="nowUtc">The current UTC time for comparison against <see cref="ExpiresAtUtc"/>.</param>
    /// <returns><see langword="true"/> if the purpose has not expired; otherwise, <see langword="false"/>.</returns>
    public bool IsCurrent(DateTimeOffset nowUtc) =>
        ExpiresAtUtc is null || ExpiresAtUtc > nowUtc;
}

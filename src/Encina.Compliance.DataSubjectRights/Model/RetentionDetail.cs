namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Describes why a specific personal data field was retained despite an erasure request.
/// </summary>
/// <remarks>
/// <para>
/// When a field cannot be erased due to legal retention requirements or Article 17(3)
/// exemptions, this record documents the field, the entity type it belongs to, and
/// the reason for retention. This information supports demonstrability and must be
/// communicated to the data subject.
/// </para>
/// </remarks>
public sealed record RetentionDetail
{
    /// <summary>
    /// The name of the field that was retained.
    /// </summary>
    /// <example>"TaxIdentificationNumber", "InvoiceNumber"</example>
    public required string FieldName { get; init; }

    /// <summary>
    /// The CLR type of the entity containing the retained field.
    /// </summary>
    public required Type EntityType { get; init; }

    /// <summary>
    /// Human-readable reason explaining why the field was retained.
    /// </summary>
    /// <example>"Required for tax compliance (7-year retention period)"</example>
    public required string Reason { get; init; }
}

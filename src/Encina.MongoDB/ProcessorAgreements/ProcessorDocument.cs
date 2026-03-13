using Encina.Compliance.ProcessorAgreements.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.ProcessorAgreements;

/// <summary>
/// MongoDB BSON document for <see cref="Processor"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps <see cref="Processor"/> domain records to a MongoDB-native document format
/// with BSON annotations for proper serialization and indexing.
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item><description><see cref="Processor.SubProcessorAuthorizationType"/> (enum) → <see cref="SubProcessorAuthorizationTypeValue"/> (int).</description></item>
/// <item><description>DateTimeOffset → DateTime (UTC) for MongoDB native date storage.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ProcessorDocument
{
    /// <summary>
    /// Unique identifier for this processor.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the processor.
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The country where the processor is established.
    /// </summary>
    [BsonElement("country")]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// The contact email address for the processor's data protection representative.
    /// </summary>
    [BsonElement("contact_email")]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// The identifier of the parent processor, or null for top-level processors.
    /// </summary>
    [BsonElement("parent_processor_id")]
    public string? ParentProcessorId { get; set; }

    /// <summary>
    /// The depth in the sub-processor hierarchy (0 = top-level).
    /// </summary>
    [BsonElement("depth")]
    public int Depth { get; set; }

    /// <summary>
    /// Integer value of the <see cref="SubProcessorAuthorizationType"/> enum.
    /// </summary>
    [BsonElement("sub_processor_authorization_type")]
    public int SubProcessorAuthorizationTypeValue { get; set; }

    /// <summary>
    /// The tenant identifier for multi-tenancy support.
    /// </summary>
    [BsonElement("tenant_id")]
    public string? TenantId { get; set; }

    /// <summary>
    /// The module identifier for modular monolith isolation.
    /// </summary>
    [BsonElement("module_id")]
    public string? ModuleId { get; set; }

    /// <summary>
    /// Timestamp when this processor was registered (UTC).
    /// </summary>
    [BsonElement("created_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this processor was last updated (UTC).
    /// </summary>
    [BsonElement("last_updated_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastUpdatedAtUtc { get; set; }

    /// <summary>
    /// Creates a document from a domain <see cref="Processor"/>.
    /// </summary>
    /// <param name="processor">The domain processor to convert.</param>
    /// <returns>A <see cref="ProcessorDocument"/> suitable for MongoDB persistence.</returns>
    public static ProcessorDocument FromProcessor(Processor processor)
    {
        ArgumentNullException.ThrowIfNull(processor);

        return new ProcessorDocument
        {
            Id = processor.Id,
            Name = processor.Name,
            Country = processor.Country,
            ContactEmail = processor.ContactEmail,
            ParentProcessorId = processor.ParentProcessorId,
            Depth = processor.Depth,
            SubProcessorAuthorizationTypeValue = (int)processor.SubProcessorAuthorizationType,
            TenantId = processor.TenantId,
            ModuleId = processor.ModuleId,
            CreatedAtUtc = processor.CreatedAtUtc.UtcDateTime,
            LastUpdatedAtUtc = processor.LastUpdatedAtUtc.UtcDateTime
        };
    }

    /// <summary>
    /// Converts this document back to a domain <see cref="Processor"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="Processor"/> if valid, or <c>null</c> if the document contains
    /// invalid values (undefined enum).
    /// </returns>
    public Processor? ToProcessor()
    {
        if (!Enum.IsDefined(typeof(SubProcessorAuthorizationType), SubProcessorAuthorizationTypeValue))
            return null;

        return new Processor
        {
            Id = Id,
            Name = Name,
            Country = Country,
            ContactEmail = ContactEmail,
            ParentProcessorId = ParentProcessorId,
            Depth = Depth,
            SubProcessorAuthorizationType = (SubProcessorAuthorizationType)SubProcessorAuthorizationTypeValue,
            TenantId = TenantId,
            ModuleId = ModuleId,
            CreatedAtUtc = new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero),
            LastUpdatedAtUtc = new DateTimeOffset(LastUpdatedAtUtc, TimeSpan.Zero)
        };
    }
}

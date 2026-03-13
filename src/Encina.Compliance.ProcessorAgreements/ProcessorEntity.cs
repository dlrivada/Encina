namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Persistence entity for <see cref="Model.Processor"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a processor,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Model.Processor.SubProcessorAuthorizationType"/>
/// (<see cref="Model.SubProcessorAuthorizationType"/>) is stored as
/// <see cref="SubProcessorAuthorizationTypeValue"/> (<see cref="int"/>)
/// for cross-provider compatibility.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="ProcessorMapper"/> to convert between this entity and
/// <see cref="Model.Processor"/>.
/// </para>
/// </remarks>
public sealed class ProcessorEntity
{
    /// <summary>
    /// Unique identifier for this processor.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The display name of the processor.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The country where the processor is established.
    /// </summary>
    public required string Country { get; set; }

    /// <summary>
    /// The contact email address for the processor's data protection representative.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// The identifier of the parent processor, or <see langword="null"/> for top-level processors.
    /// </summary>
    public string? ParentProcessorId { get; set; }

    /// <summary>
    /// The depth of this processor in the sub-processor hierarchy.
    /// </summary>
    public required int Depth { get; set; }

    /// <summary>
    /// Integer value of the <see cref="Model.SubProcessorAuthorizationType"/> enum.
    /// </summary>
    /// <remarks>
    /// Values: Specific=0, General=1.
    /// </remarks>
    public required int SubProcessorAuthorizationTypeValue { get; set; }

    /// <summary>
    /// The tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// The module identifier for modular monolith isolation.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// Timestamp when this processor was registered (UTC).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this processor was last updated (UTC).
    /// </summary>
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}

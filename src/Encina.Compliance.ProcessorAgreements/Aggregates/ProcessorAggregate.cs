using Encina.Compliance.ProcessorAgreements.Events;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.DomainModeling;

namespace Encina.Compliance.ProcessorAgreements.Aggregates;

/// <summary>
/// Event-sourced aggregate representing a data processor or sub-processor in the processing chain.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 28(1), the controller must use only processors providing sufficient guarantees
/// to implement appropriate technical and organisational measures. This aggregate captures the
/// processor's identity, hierarchical position, and authorization type for sub-processor engagement.
/// </para>
/// <para>
/// Processors form a bounded hierarchy using <see cref="ParentProcessorId"/> and <see cref="Depth"/>:
/// top-level processors have <see cref="ParentProcessorId"/> = <see langword="null"/> and
/// <see cref="Depth"/> = 0. The hierarchy is bounded by configuration to prevent unbounded
/// processing chains per Article 28(4).
/// </para>
/// <para>
/// This is a long-lived identity aggregate. Contractual state (agreements, terms, expiration)
/// is tracked separately via <see cref="DPAAggregate"/>.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail
/// for GDPR Art. 5(2) accountability requirements.
/// </para>
/// </remarks>
public sealed class ProcessorAggregate : AggregateBase
{
    /// <summary>
    /// The display name of the processor (e.g., "Stripe", "AWS").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The country where the processor is established (ISO 3166-1).
    /// </summary>
    /// <remarks>
    /// Relevant for determining whether Standard Contractual Clauses are required
    /// for cross-border data transfers per Articles 44-49.
    /// </remarks>
    public string Country { get; private set; } = string.Empty;

    /// <summary>
    /// The contact email for the processor's data protection representative,
    /// or <see langword="null"/> if not provided.
    /// </summary>
    public string? ContactEmail { get; private set; }

    /// <summary>
    /// The identifier of the parent processor, or <see langword="null"/> for top-level processors.
    /// </summary>
    /// <remarks>
    /// Per Article 28(2), the processor shall not engage another processor without prior
    /// written authorization. This field tracks the hierarchical relationship.
    /// </remarks>
    public Guid? ParentProcessorId { get; private set; }

    /// <summary>
    /// The depth of this processor in the sub-processor hierarchy.
    /// </summary>
    /// <remarks>
    /// <c>0</c> = top-level processor, <c>1</c> = direct sub-processor,
    /// <c>2</c> = sub-sub-processor, etc. Bounded by <c>MaxSubProcessorDepth</c> in configuration.
    /// </remarks>
    public int Depth { get; private set; }

    /// <summary>
    /// The type of written authorization granted for sub-processor engagement per Article 28(2).
    /// </summary>
    public SubProcessorAuthorizationType AuthorizationType { get; private set; }

    /// <summary>
    /// Whether this processor has been removed from the processing chain.
    /// </summary>
    public bool IsRemoved { get; private set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// The UTC timestamp when this processor was registered.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>
    /// The UTC timestamp when this processor was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAtUtc { get; private set; }

    /// <summary>
    /// Registers a new data processor in the processing chain.
    /// </summary>
    /// <param name="id">Unique identifier for the new processor.</param>
    /// <param name="name">The display name of the processor.</param>
    /// <param name="country">The country where the processor is established.</param>
    /// <param name="contactEmail">The contact email, or <see langword="null"/> if not provided.</param>
    /// <param name="parentProcessorId">The parent processor's identifier, or <see langword="null"/> for top-level processors.</param>
    /// <param name="depth">The depth in the sub-processor hierarchy (0 = top-level).</param>
    /// <param name="authorizationType">The type of written authorization for sub-processor engagement.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when the registration occurred.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="ProcessorAggregate"/> representing the registered processor.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> or <paramref name="country"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="depth"/> is negative.</exception>
    public static ProcessorAggregate Register(
        Guid id,
        string name,
        string country,
        string? contactEmail,
        Guid? parentProcessorId,
        int depth,
        SubProcessorAuthorizationType authorizationType,
        DateTimeOffset occurredAtUtc,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        ArgumentOutOfRangeException.ThrowIfNegative(depth);

        var aggregate = new ProcessorAggregate();
        aggregate.RaiseEvent(new ProcessorRegistered(
            id, name, country, contactEmail, parentProcessorId,
            depth, authorizationType, occurredAtUtc, tenantId, moduleId));
        return aggregate;
    }

    /// <summary>
    /// Updates the processor's identity information.
    /// </summary>
    /// <param name="name">The updated display name.</param>
    /// <param name="country">The updated country.</param>
    /// <param name="contactEmail">The updated contact email, or <see langword="null"/>.</param>
    /// <param name="authorizationType">The updated authorization type.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when the update occurred.</param>
    /// <exception cref="InvalidOperationException">Thrown when the processor has been removed.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> or <paramref name="country"/> is null or whitespace.</exception>
    public void Update(
        string name,
        string country,
        string? contactEmail,
        SubProcessorAuthorizationType authorizationType,
        DateTimeOffset occurredAtUtc)
    {
        if (IsRemoved)
        {
            throw new InvalidOperationException(
                $"Cannot update processor '{Id}' because it has been removed from the processing chain.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);

        RaiseEvent(new ProcessorUpdated(Id, name, country, contactEmail, authorizationType, occurredAtUtc));
    }

    /// <summary>
    /// Removes the processor from the processing chain.
    /// </summary>
    /// <param name="reason">The reason for removing the processor.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when the removal occurred.</param>
    /// <exception cref="InvalidOperationException">Thrown when the processor has already been removed.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null or whitespace.</exception>
    public void Remove(string reason, DateTimeOffset occurredAtUtc)
    {
        if (IsRemoved)
        {
            throw new InvalidOperationException(
                $"Processor '{Id}' has already been removed from the processing chain.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RaiseEvent(new ProcessorRemoved(Id, reason, occurredAtUtc));
    }

    /// <summary>
    /// Records that a sub-processor has been added to this processor's hierarchy.
    /// </summary>
    /// <param name="subProcessorId">The identifier of the sub-processor being added.</param>
    /// <param name="subProcessorName">The display name of the sub-processor.</param>
    /// <param name="depth">The depth of the sub-processor in the hierarchy.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when the sub-processor was added.</param>
    /// <exception cref="InvalidOperationException">Thrown when the processor has been removed.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="subProcessorName"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="depth"/> is not positive.</exception>
    public void AddSubProcessor(Guid subProcessorId, string subProcessorName, int depth, DateTimeOffset occurredAtUtc)
    {
        if (IsRemoved)
        {
            throw new InvalidOperationException(
                $"Cannot add sub-processor to processor '{Id}' because it has been removed from the processing chain.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(subProcessorName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(depth);

        RaiseEvent(new SubProcessorAdded(Id, subProcessorId, subProcessorName, depth, occurredAtUtc));
    }

    /// <summary>
    /// Records that a sub-processor has been removed from this processor's hierarchy.
    /// </summary>
    /// <param name="subProcessorId">The identifier of the sub-processor being removed.</param>
    /// <param name="reason">The reason for removing the sub-processor.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when the sub-processor was removed.</param>
    /// <exception cref="InvalidOperationException">Thrown when the processor has been removed.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null or whitespace.</exception>
    public void RemoveSubProcessor(Guid subProcessorId, string reason, DateTimeOffset occurredAtUtc)
    {
        if (IsRemoved)
        {
            throw new InvalidOperationException(
                $"Cannot remove sub-processor from processor '{Id}' because it has been removed from the processing chain.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RaiseEvent(new SubProcessorRemoved(Id, subProcessorId, reason, occurredAtUtc));
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case ProcessorRegistered e:
                Id = e.ProcessorId;
                Name = e.Name;
                Country = e.Country;
                ContactEmail = e.ContactEmail;
                ParentProcessorId = e.ParentProcessorId;
                Depth = e.Depth;
                AuthorizationType = e.AuthorizationType;
                IsRemoved = false;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                CreatedAtUtc = e.OccurredAtUtc;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case ProcessorUpdated e:
                Name = e.Name;
                Country = e.Country;
                ContactEmail = e.ContactEmail;
                AuthorizationType = e.AuthorizationType;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case ProcessorRemoved e:
                IsRemoved = true;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case SubProcessorAdded e:
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case SubProcessorRemoved e:
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;
        }
    }
}

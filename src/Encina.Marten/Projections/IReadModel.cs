namespace Encina.Marten.Projections;

/// <summary>
/// Marker interface for read models (projections).
/// </summary>
/// <remarks>
/// <para>
/// Read models are denormalized views of aggregate state, optimized for queries.
/// They are built by projecting domain events from the event stream.
/// </para>
/// <para>
/// <b>CQRS Pattern</b>: Read models form the "Query" side of Command Query
/// Responsibility Segregation. While aggregates handle commands and maintain
/// invariants, read models provide efficient, tailored views for queries.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class OrderSummary : IReadModel
/// {
///     public Guid Id { get; set; }
///     public string CustomerName { get; set; } = string.Empty;
///     public decimal TotalAmount { get; set; }
///     public int ItemCount { get; set; }
///     public string Status { get; set; } = string.Empty;
///     public DateTime CreatedAtUtc { get; set; }
/// }
/// </code>
/// </example>
public interface IReadModel
{
    /// <summary>
    /// Gets or sets the unique identifier for this read model.
    /// </summary>
    Guid Id { get; set; }
}

/// <summary>
/// Read model with strongly-typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public interface IReadModel<TId> : IReadModel
    where TId : notnull
{
    /// <summary>
    /// Gets or sets the strongly-typed unique identifier for this read model.
    /// </summary>
    new TId Id { get; set; }
}

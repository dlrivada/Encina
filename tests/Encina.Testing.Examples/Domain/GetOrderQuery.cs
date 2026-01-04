namespace Encina.Testing.Examples.Domain;

/// <summary>
/// Sample query for demonstrating test patterns.
/// </summary>
public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

/// <summary>
/// Sample DTO returned by GetOrderQuery.
/// </summary>
public sealed record OrderDto(
    Guid Id,
    string CustomerId,
    decimal Amount,
    DateTime CreatedAtUtc);

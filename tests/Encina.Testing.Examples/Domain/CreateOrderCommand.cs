namespace Encina.Testing.Examples.Domain;

/// <summary>
/// Sample command for demonstrating test patterns.
/// </summary>
public sealed record CreateOrderCommand : ICommand<Guid>
{
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public string? Notes { get; init; }
}

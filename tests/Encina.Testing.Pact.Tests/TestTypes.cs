using LanguageExt;

namespace Encina.Testing.Pact.Tests;

/// <summary>
/// Sample command for testing.
/// </summary>
public sealed record TestCreateOrderCommand(Guid OrderId, string CustomerName) : ICommand<TestOrderDto>;

/// <summary>
/// Sample query for testing.
/// </summary>
public sealed record TestGetOrderByIdQuery(Guid OrderId) : IQuery<TestOrderDto>;

/// <summary>
/// Sample notification for testing.
/// </summary>
public sealed record TestOrderCreatedNotification(Guid OrderId, string CustomerName) : INotification;

/// <summary>
/// Sample DTO for testing.
/// </summary>
public sealed record TestOrderDto
{
    public Guid Id { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string Status { get; init; } = "Created";
}

/// <summary>
/// Sample unit command for testing.
/// </summary>
public sealed record TestDeleteOrderCommand(Guid OrderId) : ICommand;

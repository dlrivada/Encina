namespace Encina.UnitTests.Testing.Architecture.TestTypes;

/// <summary>
/// A sealed notification for testing architecture rules.
/// </summary>
public sealed record OrderCreatedNotification
{
    public required Guid OrderId { get; init; }
}

/// <summary>
/// A sealed event for testing architecture rules.
/// </summary>
public sealed record OrderCompletedEvent
{
    public required Guid OrderId { get; init; }
}

/// <summary>
/// An unsealed notification (should fail the sealed notification rule).
/// </summary>
public record OrderUpdatedNotification
{
    public required Guid OrderId { get; init; }
}

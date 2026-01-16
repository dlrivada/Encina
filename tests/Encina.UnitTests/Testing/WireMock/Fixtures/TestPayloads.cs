namespace Encina.UnitTests.Testing.WireMock.Fixtures;

/// <summary>
/// Test payload for webhook body deserialization tests.
/// </summary>
public sealed record OrderWebhookPayload
{
    public required int OrderId { get; init; }
    public required string Status { get; init; }
}

namespace Encina.Testing.WireMock.Tests.Fixtures;

/// <summary>
/// Test payload for webhook body deserialization tests.
/// </summary>
public sealed record OrderWebhookPayload
{
    public required int OrderId { get; init; }
    public required string Status { get; init; }
}

namespace Encina.IntegrationTests.Cdc.Helpers;

/// <summary>
/// Simple entity used for integration testing of the CDC pipeline.
/// </summary>
internal sealed class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

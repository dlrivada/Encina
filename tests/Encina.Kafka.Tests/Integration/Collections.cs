using Encina.TestInfrastructure.Fixtures;

namespace Encina.Kafka.Tests.Integration;

/// <summary>
/// Collection definition for Kafka integration tests.
/// </summary>
/// <remarks>
/// This re-exports the collection definition from TestInfrastructure so that xUnit
/// can find the collection in the same assembly as the tests.
/// </remarks>
[CollectionDefinition(KafkaCollection.Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class KafkaCollection : ICollectionFixture<KafkaFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "Kafka";
}

using Encina.TestInfrastructure.Fixtures;

namespace Encina.RabbitMQ.Tests.Integration;

/// <summary>
/// Collection definition for RabbitMQ integration tests.
/// </summary>
/// <remarks>
/// This re-exports the collection definition from TestInfrastructure so that xUnit
/// can find the collection in the same assembly as the tests.
/// </remarks>
[CollectionDefinition(RabbitMqCollection.Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "RabbitMQ";
}

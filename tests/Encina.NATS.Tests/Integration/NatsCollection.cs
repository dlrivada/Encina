using Encina.TestInfrastructure.Fixtures;

namespace Encina.NATS.Tests.Integration;

/// <summary>
/// Collection definition for NATS integration tests.
/// This must be in the same assembly as the tests that use it.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class NatsCollection : ICollectionFixture<NatsFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "NATS";
}

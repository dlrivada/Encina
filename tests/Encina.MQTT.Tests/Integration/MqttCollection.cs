using Encina.TestInfrastructure.Fixtures;

namespace Encina.MQTT.Tests.Integration;

/// <summary>
/// Collection definition for MQTT integration tests.
/// This must be in the same assembly as the tests that use it.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class MqttCollection : ICollectionFixture<MqttFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "MQTT";
}

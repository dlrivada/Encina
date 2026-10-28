using Encina.TestInfrastructure.Fixtures;

namespace Encina.ContractTests.MessageBrokers.AzureServiceBus;

/// <summary>
/// Owns the single Azure Service Bus emulator container shared by every Service Bus test class of this assembly.
/// </summary>
[CollectionDefinition(ServiceBusEmulatorFixture.CollectionName)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed class ServiceBusEmulatorCollection : ICollectionFixture<ServiceBusEmulatorFixture>
#pragma warning restore CA1711
{
}

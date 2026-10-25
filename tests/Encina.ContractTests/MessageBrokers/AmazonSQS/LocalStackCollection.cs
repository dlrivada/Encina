using Encina.TestInfrastructure.Fixtures;

namespace Encina.ContractTests.MessageBrokers.AmazonSQS;

/// <summary>
/// Owns the single LocalStack container shared by every AWS emulator test class of this assembly.
/// </summary>
[CollectionDefinition(LocalStackFixture.CollectionName)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed class LocalStackCollection : ICollectionFixture<LocalStackFixture>
#pragma warning restore CA1711
{
}

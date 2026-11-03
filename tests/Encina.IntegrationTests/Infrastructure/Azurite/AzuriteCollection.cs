using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.Infrastructure.Azurite;

/// <summary>
/// Owns the single Azurite container shared by every Azure Storage emulator test class of this assembly.
/// </summary>
[CollectionDefinition(AzuriteFixture.CollectionName)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed class AzuriteCollection : ICollectionFixture<AzuriteFixture>
#pragma warning restore CA1711
{
}

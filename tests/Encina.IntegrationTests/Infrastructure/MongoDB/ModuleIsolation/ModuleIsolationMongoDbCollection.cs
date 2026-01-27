using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.ModuleIsolation;

/// <summary>
/// xUnit collection definition for MongoDB Module Isolation integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This collection uses <see cref="MongoDbFixture"/> which provides a throwaway MongoDB
/// instance for integration tests. Module isolation in MongoDB uses collection naming
/// conventions and access validation through <c>ModuleAwareMongoCollectionFactory</c>.
/// </para>
/// <para>
/// Test classes that need module isolation functionality should apply the
/// <c>[Collection(ModuleIsolationMongoDbCollection.Name)]</c> attribute to share the fixture.
/// </para>
/// <para>
/// Using a unique collection name enables parallel execution with other MongoDB test
/// collections while maintaining fixture isolation for module isolation tests.
/// </para>
/// </remarks>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class ModuleIsolationMongoDbCollection : ICollectionFixture<MongoDbFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection for MongoDB module isolation tests.
    /// </summary>
    public const string Name = "MongoDB-ModuleIsolation";
}

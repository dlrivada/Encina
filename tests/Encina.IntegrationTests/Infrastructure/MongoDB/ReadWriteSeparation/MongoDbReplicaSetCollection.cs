using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.ReadWriteSeparation;

/// <summary>
/// xUnit collection definition for MongoDB Read/Write Separation integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This collection uses <see cref="MongoDbReplicaSetFixture"/> which configures MongoDB
/// in single-node replica set mode. This is required for testing read preference semantics
/// and other replica set-dependent features.
/// </para>
/// <para>
/// Test classes that need replica set functionality should apply the
/// <c>[Collection(MongoDbReplicaSetCollection.Name)]</c> attribute to share the fixture.
/// </para>
/// </remarks>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class MongoDbReplicaSetCollection : ICollectionFixture<MongoDbReplicaSetFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection for MongoDB replica set tests.
    /// </summary>
    public const string Name = "MongoDB-ReplicaSet";
}

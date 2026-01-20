using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB;

/// <summary>
/// Collection definition for MongoDB integration tests in the IntegrationTests assembly.
/// This is required because xUnit collection definitions must be in the same assembly as the tests.
/// </summary>
[CollectionDefinition(Name)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xUnit convention requires 'Collection' suffix for collection definitions")]
public class MongoDbTestCollection : ICollectionFixture<MongoDbFixture>
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "MongoDB";
}

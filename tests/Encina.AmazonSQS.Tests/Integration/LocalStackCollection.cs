using Encina.TestInfrastructure.Fixtures;

namespace Encina.AmazonSQS.Tests.Integration;

/// <summary>
/// Collection definition for LocalStack integration tests.
/// This must be in the same assembly as the tests that use it.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class LocalStackCollection : ICollectionFixture<LocalStackFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "LocalStack";
}

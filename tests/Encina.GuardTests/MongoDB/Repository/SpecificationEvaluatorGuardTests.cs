using Encina.MongoDB.Repository;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Repository;

public class SpecificationEvaluatorGuardTests
{
    [Fact]
    public void Constructor_NullCollection_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SpecificationEvaluatorMongoDB<TestEntity>(null!));

    public class TestEntity { public Guid Id { get; set; } }
}

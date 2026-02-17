using Encina.Sharding;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="CompoundShardKeyExtractor"/>.
/// </summary>
public sealed class CompoundShardKeyExtractorGuardTests
{
    [Fact]
    public void Extract_NullEntity_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            CompoundShardKeyExtractor.Extract<TestEntity>(null!));
        ex.ParamName.ShouldBe("entity");
    }

    private sealed class TestEntity : IShardable
    {
        public string GetShardKey() => "key";
    }
}

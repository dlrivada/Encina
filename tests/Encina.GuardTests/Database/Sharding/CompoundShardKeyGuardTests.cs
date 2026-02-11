using Encina.Sharding;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="CompoundShardKey"/>.
/// </summary>
public sealed class CompoundShardKeyGuardTests
{
    [Fact]
    public void Constructor_Params_NullArray_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new CompoundShardKey((string[])null!));
        ex.ParamName.ShouldBe("components");
    }

    [Fact]
    public void Constructor_IReadOnlyList_NullList_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new CompoundShardKey((IReadOnlyList<string>)null!));
        ex.ParamName.ShouldBe("components");
    }

    [Fact]
    public void Constructor_Params_EmptyArray_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => new CompoundShardKey(Array.Empty<string>()));
        ex.ParamName.ShouldBe("components");
    }

    [Fact]
    public void Constructor_IReadOnlyList_EmptyList_ThrowsArgumentException()
    {
        IReadOnlyList<string> empty = [];
        var ex = Should.Throw<ArgumentException>(() => new CompoundShardKey(empty));
        ex.ParamName.ShouldBe("components");
    }
}

using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding;

/// <summary>
/// Unit tests for <see cref="CompoundShardKey"/>.
/// </summary>
public sealed class CompoundShardKeyTests
{
    // ────────────────────────────────────────────────────────────
    //  Construction
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_Params_SingleComponent_CreatesKey()
    {
        var key = new CompoundShardKey("region-1");

        key.ComponentCount.ShouldBe(1);
        key.PrimaryComponent.ShouldBe("region-1");
        key.HasSecondaryComponents.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_Params_MultipleComponents_CreatesKey()
    {
        var key = new CompoundShardKey("us-east", "customer-123");

        key.ComponentCount.ShouldBe(2);
        key.PrimaryComponent.ShouldBe("us-east");
        key.HasSecondaryComponents.ShouldBeTrue();
        key.Components[1].ShouldBe("customer-123");
    }

    [Fact]
    public void Constructor_Params_ThreeComponents_CreatesKey()
    {
        var key = new CompoundShardKey("us-east", "customer-123", "order-456");

        key.ComponentCount.ShouldBe(3);
        key.Components[0].ShouldBe("us-east");
        key.Components[1].ShouldBe("customer-123");
        key.Components[2].ShouldBe("order-456");
    }

    [Fact]
    public void Constructor_IReadOnlyList_CreatesKey()
    {
        IReadOnlyList<string> components = ["region-a", "tenant-b"];
        var key = new CompoundShardKey(components);

        key.ComponentCount.ShouldBe(2);
        key.PrimaryComponent.ShouldBe("region-a");
        key.Components[1].ShouldBe("tenant-b");
    }

    [Fact]
    public void Constructor_EmptyArray_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new CompoundShardKey(Array.Empty<string>()));
    }

    [Fact]
    public void Constructor_EmptyList_ThrowsArgumentException()
    {
        IReadOnlyList<string> empty = [];
        Should.Throw<ArgumentException>(() => new CompoundShardKey(empty));
    }

    [Fact]
    public void Constructor_NullArray_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new CompoundShardKey((string[])null!));
    }

    [Fact]
    public void Constructor_NullList_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new CompoundShardKey((IReadOnlyList<string>)null!));
    }

    // ────────────────────────────────────────────────────────────
    //  Implicit conversion
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ImplicitConversion_FromString_CreatesSingleComponentKey()
    {
        CompoundShardKey key = "customer-42";

        key.ComponentCount.ShouldBe(1);
        key.PrimaryComponent.ShouldBe("customer-42");
        key.HasSecondaryComponents.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  ToString
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_SingleComponent_ReturnsComponent()
    {
        var key = new CompoundShardKey("region-1");
        key.ToString().ShouldBe("region-1");
    }

    [Fact]
    public void ToString_MultipleComponents_ReturnsPipeDelimited()
    {
        var key = new CompoundShardKey("us-east", "customer-123");
        key.ToString().ShouldBe("us-east|customer-123");
    }

    [Fact]
    public void ToString_ThreeComponents_ReturnsPipeDelimited()
    {
        var key = new CompoundShardKey("us-east", "customer-123", "order-456");
        key.ToString().ShouldBe("us-east|customer-123|order-456");
    }

    // ────────────────────────────────────────────────────────────
    //  Equality (record semantics)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_SameComponents_AreEqual()
    {
        var key1 = new CompoundShardKey("us-east", "customer-123");
        var key2 = new CompoundShardKey("us-east", "customer-123");

        key1.ShouldBe(key2);
    }

    [Fact]
    public void Equality_DifferentComponents_AreNotEqual()
    {
        var key1 = new CompoundShardKey("us-east", "customer-123");
        var key2 = new CompoundShardKey("eu-west", "customer-123");

        key1.ShouldNotBe(key2);
    }
}

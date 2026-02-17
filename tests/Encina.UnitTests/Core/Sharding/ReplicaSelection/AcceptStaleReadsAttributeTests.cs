using Encina.Sharding.ReplicaSelection;

namespace Encina.UnitTests.Core.Sharding.ReplicaSelection;

public sealed class AcceptStaleReadsAttributeTests
{
    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidValue_SetsMaxLagMilliseconds()
    {
        var attr = new AcceptStaleReadsAttribute(5000);

        attr.MaxLagMilliseconds.ShouldBe(5000);
    }

    [Fact]
    public void Constructor_Zero_IsValid()
    {
        var attr = new AcceptStaleReadsAttribute(0);
        attr.MaxLagMilliseconds.ShouldBe(0);
    }

    [Fact]
    public void Constructor_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new AcceptStaleReadsAttribute(-1));
    }

    [Fact]
    public void Constructor_IntMaxValue_IsValid()
    {
        var attr = new AcceptStaleReadsAttribute(int.MaxValue);
        attr.MaxLagMilliseconds.ShouldBe(int.MaxValue);
    }

    // ────────────────────────────────────────────────────────────
    //  MaxLag (TimeSpan)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MaxLag_ConvertsMillisecondsToTimeSpan()
    {
        var attr = new AcceptStaleReadsAttribute(5000);

        attr.MaxLag.ShouldBe(TimeSpan.FromMilliseconds(5000));
    }

    [Fact]
    public void MaxLag_ZeroMilliseconds_ReturnsTimeSpanZero()
    {
        var attr = new AcceptStaleReadsAttribute(0);
        attr.MaxLag.ShouldBe(TimeSpan.Zero);
    }

    // ────────────────────────────────────────────────────────────
    //  Attribute Usage
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AttributeUsage_AllowsClassAndMethod()
    {
        var usage = typeof(AcceptStaleReadsAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        (usage.ValidOn & AttributeTargets.Class).ShouldNotBe((AttributeTargets)0);
        (usage.ValidOn & AttributeTargets.Method).ShouldNotBe((AttributeTargets)0);
    }

    [Fact]
    public void AttributeUsage_DoesNotAllowMultiple()
    {
        var usage = typeof(AcceptStaleReadsAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    public void AttributeUsage_IsInherited()
    {
        var usage = typeof(AcceptStaleReadsAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.Inherited.ShouldBeTrue();
    }
}

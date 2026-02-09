using Encina.Cdc;
using Shouldly;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="ChangeContext"/> record.
/// </summary>
public sealed class ChangeContextTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, "tx", "db", "dbo");
        using var cts = new CancellationTokenSource();

        var context = new ChangeContext("Orders", metadata, cts.Token);

        context.TableName.ShouldBe("Orders");
        context.Metadata.ShouldBe(metadata);
        context.CancellationToken.ShouldBe(cts.Token);
    }

    [Fact]
    public void Constructor_WithDefaultCancellationToken_UsesNone()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);

        var context = new ChangeContext("T", metadata, CancellationToken.None);

        context.CancellationToken.ShouldBe(CancellationToken.None);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        var token = CancellationToken.None;

        var c1 = new ChangeContext("T", metadata, token);
        var c2 = new ChangeContext("T", metadata, token);

        c1.ShouldBe(c2);
    }
}

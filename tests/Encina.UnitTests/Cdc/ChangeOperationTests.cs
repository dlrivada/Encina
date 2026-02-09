using Encina.Cdc;
using Shouldly;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="ChangeOperation"/> enum.
/// </summary>
public sealed class ChangeOperationTests
{
    [Fact]
    public void Insert_HasValue_Zero()
    {
        ((int)ChangeOperation.Insert).ShouldBe(0);
    }

    [Fact]
    public void Update_HasValue_One()
    {
        ((int)ChangeOperation.Update).ShouldBe(1);
    }

    [Fact]
    public void Delete_HasValue_Two()
    {
        ((int)ChangeOperation.Delete).ShouldBe(2);
    }

    [Fact]
    public void Snapshot_HasValue_Three()
    {
        ((int)ChangeOperation.Snapshot).ShouldBe(3);
    }

    [Fact]
    public void AllValues_AreFourDistinctValues()
    {
        var values = Enum.GetValues<ChangeOperation>();
        values.Length.ShouldBe(4);
        values.Distinct().Count().ShouldBe(4);
    }

    [Theory]
    [InlineData(ChangeOperation.Insert, "Insert")]
    [InlineData(ChangeOperation.Update, "Update")]
    [InlineData(ChangeOperation.Delete, "Delete")]
    [InlineData(ChangeOperation.Snapshot, "Snapshot")]
    public void ToString_ReturnsExpectedName(ChangeOperation operation, string expected)
    {
        operation.ToString().ShouldBe(expected);
    }
}

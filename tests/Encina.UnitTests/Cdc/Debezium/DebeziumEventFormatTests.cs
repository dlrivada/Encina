using Encina.Cdc.Debezium;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for <see cref="DebeziumEventFormat"/> enum.
/// </summary>
public sealed class DebeziumEventFormatTests
{
    [Fact]
    public void CloudEvents_ShouldHaveValueZero()
    {
        ((int)DebeziumEventFormat.CloudEvents).ShouldBe(0);
    }

    [Fact]
    public void Flat_ShouldHaveValueOne()
    {
        ((int)DebeziumEventFormat.Flat).ShouldBe(1);
    }

    [Fact]
    public void Enum_ShouldHaveTwoValues()
    {
        Enum.GetValues<DebeziumEventFormat>().Length.ShouldBe(2);
    }
}

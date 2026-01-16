using Encina.Testing.WireMock;
namespace Encina.UnitTests.Testing.WireMock;

/// <summary>
/// Unit tests for <see cref="FaultType"/>.
/// </summary>
public sealed class FaultTypeTests
{
    [Fact]
    public void FaultType_ShouldHaveExpectedValues()
    {
        // Assert enum values match expected constants
        ((int)FaultType.EmptyResponse).ShouldBe(0);
        ((int)FaultType.MalformedResponse).ShouldBe(1);
        ((int)FaultType.Timeout).ShouldBe(2);
    }

    [Fact]
    public void FaultType_ShouldHaveThreeValues()
    {
        var values = Enum.GetValues<FaultType>();
        values.Length.ShouldBe(3);
    }

    [Theory]
    [InlineData(FaultType.EmptyResponse, "EmptyResponse")]
    [InlineData(FaultType.MalformedResponse, "MalformedResponse")]
    [InlineData(FaultType.Timeout, "Timeout")]
    public void FaultType_ShouldHaveCorrectName(FaultType faultType, string expectedName)
    {
        faultType.ToString().ShouldBe(expectedName);
    }

    [Fact]
    public void FaultType_ShouldParseFromString()
    {
        Enum.Parse<FaultType>("EmptyResponse").ShouldBe(FaultType.EmptyResponse);
        Enum.Parse<FaultType>("MalformedResponse").ShouldBe(FaultType.MalformedResponse);
        Enum.Parse<FaultType>("Timeout").ShouldBe(FaultType.Timeout);
    }
}

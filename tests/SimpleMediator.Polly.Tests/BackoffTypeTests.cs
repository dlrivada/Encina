namespace SimpleMediator.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="BackoffType"/> enum.
/// Verifies enum values and behavior.
/// </summary>
public class BackoffTypeTests
{
    [Fact]
    public void BackoffType_ShouldHaveConstantValue()
    {
        // Assert
        BackoffType.Constant.Should().BeDefined();
        ((int)BackoffType.Constant).Should().Be(0);
    }

    [Fact]
    public void BackoffType_ShouldHaveLinearValue()
    {
        // Assert
        BackoffType.Linear.Should().BeDefined();
        ((int)BackoffType.Linear).Should().Be(1);
    }

    [Fact]
    public void BackoffType_ShouldHaveExponentialValue()
    {
        // Assert
        BackoffType.Exponential.Should().BeDefined();
        ((int)BackoffType.Exponential).Should().Be(2);
    }

    [Fact]
    public void BackoffType_ShouldHaveThreeValues()
    {
        // Act
        var values = Enum.GetValues<BackoffType>();

        // Assert
        values.Should().HaveCount(3, "should have exactly Constant, Linear, and Exponential");
    }

    [Fact]
    public void BackoffType_ToString_ShouldReturnCorrectNames()
    {
        // Assert
        BackoffType.Constant.ToString().Should().Be("Constant");
        BackoffType.Linear.ToString().Should().Be("Linear");
        BackoffType.Exponential.ToString().Should().Be("Exponential");
    }

    [Fact]
    public void BackoffType_Parse_ShouldWorkForAllValues()
    {
        // Assert
        Enum.Parse<BackoffType>("Constant").Should().Be(BackoffType.Constant);
        Enum.Parse<BackoffType>("Linear").Should().Be(BackoffType.Linear);
        Enum.Parse<BackoffType>("Exponential").Should().Be(BackoffType.Exponential);
    }
}

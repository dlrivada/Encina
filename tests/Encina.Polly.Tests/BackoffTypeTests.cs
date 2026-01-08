namespace Encina.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="BackoffType"/> enum.
/// Verifies enum values and behavior.
/// </summary>
public class BackoffTypeTests
{
    [Fact]
    public void BackoffType_ShouldHaveConstantValue()
    {
        // Act
        var value = BackoffType.Constant;

        // Assert
        ((int)value).ShouldBe(0);
    }

    [Fact]
    public void BackoffType_ShouldHaveLinearValue()
    {
        // Act
        var value = BackoffType.Linear;

        // Assert
        ((int)value).ShouldBe(1);
    }

    [Fact]
    public void BackoffType_ShouldHaveExponentialValue()
    {
        // Act
        var value = BackoffType.Exponential;

        // Assert
        ((int)value).ShouldBe(2);
    }

    [Fact]
    public void BackoffType_ShouldHaveThreeValues()
    {
        // Act
        var values = Enum.GetValues<BackoffType>();

        // Assert
        values.Length.ShouldBe(3, "should have exactly Constant, Linear, and Exponential");
    }

    [Fact]
    public void BackoffType_ToString_ShouldReturnCorrectNames()
    {
        // Assert
        BackoffType.Constant.ToString().ShouldBe("Constant");
        BackoffType.Linear.ToString().ShouldBe("Linear");
        BackoffType.Exponential.ToString().ShouldBe("Exponential");
    }

    [Fact]
    public void BackoffType_Parse_ShouldWorkForAllValues()
    {
        // Assert
        Enum.Parse<BackoffType>("Constant").ShouldBe(BackoffType.Constant);
        Enum.Parse<BackoffType>("Linear").ShouldBe(BackoffType.Linear);
        Enum.Parse<BackoffType>("Exponential").ShouldBe(BackoffType.Exponential);
    }
}

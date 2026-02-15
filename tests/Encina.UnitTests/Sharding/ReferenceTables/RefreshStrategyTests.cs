using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="RefreshStrategy"/>.
/// </summary>
public sealed class RefreshStrategyTests
{
    // ────────────────────────────────────────────────────────────
    //  Enum Values
    // ────────────────────────────────────────────────────────────

    #region Enum Values

    [Fact]
    public void Enum_HasThreeValues()
    {
        // Act
        var values = Enum.GetValues<RefreshStrategy>();

        // Assert
        values.Length.ShouldBe(3);
    }

    [Fact]
    public void CdcDriven_HasValue0()
    {
        // Act & Assert
        ((int)RefreshStrategy.CdcDriven).ShouldBe(0);
    }

    [Fact]
    public void Polling_HasValue1()
    {
        // Act & Assert
        ((int)RefreshStrategy.Polling).ShouldBe(1);
    }

    [Fact]
    public void Manual_HasValue2()
    {
        // Act & Assert
        ((int)RefreshStrategy.Manual).ShouldBe(2);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Enum Names
    // ────────────────────────────────────────────────────────────

    #region Enum Names

    [Fact]
    public void CdcDriven_HasCorrectName()
    {
        // Act & Assert
        RefreshStrategy.CdcDriven.ToString().ShouldBe("CdcDriven");
    }

    [Fact]
    public void Polling_HasCorrectName()
    {
        // Act & Assert
        RefreshStrategy.Polling.ToString().ShouldBe("Polling");
    }

    [Fact]
    public void Manual_HasCorrectName()
    {
        // Act & Assert
        RefreshStrategy.Manual.ToString().ShouldBe("Manual");
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Parse
    // ────────────────────────────────────────────────────────────

    #region Parse

    [Theory]
    [InlineData("CdcDriven", RefreshStrategy.CdcDriven)]
    [InlineData("Polling", RefreshStrategy.Polling)]
    [InlineData("Manual", RefreshStrategy.Manual)]
    public void Parse_ValidString_ReturnsCorrectValue(string input, RefreshStrategy expected)
    {
        // Act
        var result = Enum.Parse<RefreshStrategy>(input);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion
}

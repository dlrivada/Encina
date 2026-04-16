using Encina.Compliance.Consent;
using Shouldly;

namespace Encina.UnitTests.Compliance.Consent.Model;

/// <summary>
/// Unit tests for <see cref="ConsentStatus"/> enum.
/// </summary>
public class ConsentStatusTests
{
    [Fact]
    public void ConsentStatus_ShouldHaveFourValues()
    {
        // Assert
        Enum.GetValues<ConsentStatus>().Count.ShouldBe(4);
    }

    [Fact]
    public void ConsentStatus_Active_ShouldBeDefault()
    {
        // Arrange & Act
        var status = default(ConsentStatus);

        // Assert
        status.ShouldBe(ConsentStatus.Active);
    }

    [Theory]
    [InlineData(ConsentStatus.Active, 0)]
    [InlineData(ConsentStatus.Withdrawn, 1)]
    [InlineData(ConsentStatus.Expired, 2)]
    [InlineData(ConsentStatus.RequiresReconsent, 3)]
    public void ConsentStatus_ShouldHaveExpectedIntValues(ConsentStatus status, int expectedValue)
    {
        // Assert
        ((int)status).ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData(ConsentStatus.Active, "Active")]
    [InlineData(ConsentStatus.Withdrawn, "Withdrawn")]
    [InlineData(ConsentStatus.Expired, "Expired")]
    [InlineData(ConsentStatus.RequiresReconsent, "RequiresReconsent")]
    public void ConsentStatus_ToString_ShouldReturnExpectedName(ConsentStatus status, string expectedName)
    {
        // Assert
        status.ToString().ShouldBe(expectedName);
    }
}

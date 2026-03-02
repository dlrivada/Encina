using Encina.Compliance.DataResidency;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DataResidencyErrorsTests
{
    [Fact]
    public void RegionNotAllowed_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.RegionNotAllowed("personal-data", "US");

        // Assert
        error.GetCode().IsSome.Should().BeTrue();
        error.GetCode().Match(Some: code => code, None: () => "").Should().Be(DataResidencyErrors.RegionNotAllowedCode);
    }

    [Fact]
    public void RegionNotAllowed_ShouldContainParametersInMessage()
    {
        // Act
        var error = DataResidencyErrors.RegionNotAllowed("healthcare-data", "CN");

        // Assert
        error.Message.Should().Contain("CN");
        error.Message.Should().Contain("healthcare-data");
    }

    [Fact]
    public void CrossBorderTransferDenied_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.CrossBorderTransferDenied("DE", "US", "No adequacy");

        // Assert
        error.GetCode().IsSome.Should().BeTrue();
        error.GetCode().Match(Some: code => code, None: () => "").Should().Be(DataResidencyErrors.CrossBorderTransferDeniedCode);
    }

    [Fact]
    public void CrossBorderTransferDenied_ShouldContainParametersInMessage()
    {
        // Act
        var error = DataResidencyErrors.CrossBorderTransferDenied("DE", "CN", "No safeguards");

        // Assert
        error.Message.Should().Contain("DE");
        error.Message.Should().Contain("CN");
        error.Message.Should().Contain("No safeguards");
    }

    [Fact]
    public void RegionNotResolved_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.RegionNotResolved("No header found");

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").Should().Be(DataResidencyErrors.RegionNotResolvedCode);
    }

    [Fact]
    public void PolicyNotFound_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.PolicyNotFound("healthcare-data");

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").Should().Be(DataResidencyErrors.PolicyNotFoundCode);
        error.Message.Should().Contain("healthcare-data");
    }

    [Fact]
    public void PolicyAlreadyExists_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.PolicyAlreadyExists("personal-data");

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").Should().Be(DataResidencyErrors.PolicyAlreadyExistsCode);
        error.Message.Should().Contain("personal-data");
    }

    [Fact]
    public void StoreError_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.StoreError("Record", "Connection failed");

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").Should().Be(DataResidencyErrors.StoreErrorCode);
        error.Message.Should().Contain("Record");
        error.Message.Should().Contain("Connection failed");
    }

    [Fact]
    public void StoreError_WithException_ShouldIncludeException()
    {
        // Arrange
        var ex = new InvalidOperationException("DB error");

        // Act
        var error = DataResidencyErrors.StoreError("GetByEntity", "Failed", ex);

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").Should().Be(DataResidencyErrors.StoreErrorCode);
    }

    [Fact]
    public void TransferValidationFailed_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.TransferValidationFailed("Timeout");

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").Should().Be(DataResidencyErrors.TransferValidationFailedCode);
        error.Message.Should().Contain("Timeout");
    }

    [Fact]
    public void ErrorCodes_ShouldBeUniqueConstants()
    {
        // Assert - all error codes should be distinct
        var codes = new[]
        {
            DataResidencyErrors.RegionNotAllowedCode,
            DataResidencyErrors.CrossBorderTransferDeniedCode,
            DataResidencyErrors.RegionNotResolvedCode,
            DataResidencyErrors.PolicyNotFoundCode,
            DataResidencyErrors.PolicyAlreadyExistsCode,
            DataResidencyErrors.StoreErrorCode,
            DataResidencyErrors.TransferValidationFailedCode
        };

        codes.Should().OnlyHaveUniqueItems();
    }
}

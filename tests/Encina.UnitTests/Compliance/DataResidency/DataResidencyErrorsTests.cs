using Encina.Compliance.DataResidency;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DataResidencyErrorsTests
{
    [Fact]
    public void RegionNotAllowed_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.RegionNotAllowed("personal-data", "US");

        // Assert
        error.GetCode().IsSome.ShouldBeTrue();
        error.GetCode().Match(Some: code => code, None: () => "").ShouldBe(DataResidencyErrors.RegionNotAllowedCode);
    }

    [Fact]
    public void RegionNotAllowed_ShouldContainParametersInMessage()
    {
        // Act
        var error = DataResidencyErrors.RegionNotAllowed("healthcare-data", "CN");

        // Assert
        error.Message.ShouldContain("CN");
        error.Message.ShouldContain("healthcare-data");
    }

    [Fact]
    public void CrossBorderTransferDenied_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.CrossBorderTransferDenied("DE", "US", "No adequacy");

        // Assert
        error.GetCode().IsSome.ShouldBeTrue();
        error.GetCode().Match(Some: code => code, None: () => "").ShouldBe(DataResidencyErrors.CrossBorderTransferDeniedCode);
    }

    [Fact]
    public void CrossBorderTransferDenied_ShouldContainParametersInMessage()
    {
        // Act
        var error = DataResidencyErrors.CrossBorderTransferDenied("DE", "CN", "No safeguards");

        // Assert
        error.Message.ShouldContain("DE");
        error.Message.ShouldContain("CN");
        error.Message.ShouldContain("No safeguards");
    }

    [Fact]
    public void RegionNotResolved_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.RegionNotResolved("No header found");

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").ShouldBe(DataResidencyErrors.RegionNotResolvedCode);
    }

    [Fact]
    public void PolicyNotFound_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.PolicyNotFound("healthcare-data");

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").ShouldBe(DataResidencyErrors.PolicyNotFoundCode);
        error.Message.ShouldContain("healthcare-data");
    }

    [Fact]
    public void PolicyAlreadyExists_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.PolicyAlreadyExists("personal-data");

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").ShouldBe(DataResidencyErrors.PolicyAlreadyExistsCode);
        error.Message.ShouldContain("personal-data");
    }

    [Fact]
    public void StoreError_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.StoreError("Record", "Connection failed");

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").ShouldBe(DataResidencyErrors.StoreErrorCode);
        error.Message.ShouldContain("Record");
        error.Message.ShouldContain("Connection failed");
    }

    [Fact]
    public void StoreError_WithException_ShouldIncludeException()
    {
        // Arrange
        var ex = new InvalidOperationException("DB error");

        // Act
        var error = DataResidencyErrors.StoreError("GetByEntity", "Failed", ex);

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").ShouldBe(DataResidencyErrors.StoreErrorCode);
    }

    [Fact]
    public void TransferValidationFailed_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.TransferValidationFailed("Timeout");

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").ShouldBe(DataResidencyErrors.TransferValidationFailedCode);
        error.Message.ShouldContain("Timeout");
    }

    [Fact]
    public void LocationNotFound_ShouldHaveCorrectCode()
    {
        // Act
        var error = DataResidencyErrors.LocationNotFound("loc-1");

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").ShouldBe(DataResidencyErrors.LocationNotFoundCode);
        error.Message.ShouldContain("loc-1");
    }

    [Fact]
    public void InvalidStateTransition_ShouldHaveCorrectCode()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        // Act
        var error = DataResidencyErrors.InvalidStateTransition(aggregateId, "Update");

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").ShouldBe(DataResidencyErrors.InvalidStateTransitionCode);
        error.Message.ShouldContain(aggregateId.ToString());
        error.Message.ShouldContain("Update");
    }

    [Fact]
    public void ServiceError_ShouldHaveCorrectCode()
    {
        // Arrange
        var exception = new InvalidOperationException("fail");

        // Act
        var error = DataResidencyErrors.ServiceError("Create", exception);

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").ShouldBe(DataResidencyErrors.ServiceErrorCode);
        error.Message.ShouldContain("Create");
        error.Message.ShouldContain("fail");
    }

    [Fact]
    public void EventHistoryUnavailable_ShouldHaveCorrectCode()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        // Act
        var error = DataResidencyErrors.EventHistoryUnavailable(aggregateId);

        // Assert
        error.GetCode().Match(Some: code => code, None: () => "").ShouldBe(DataResidencyErrors.EventHistoryUnavailableCode);
        error.Message.ShouldContain(aggregateId.ToString());
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
            DataResidencyErrors.TransferValidationFailedCode,
            DataResidencyErrors.LocationNotFoundCode,
            DataResidencyErrors.InvalidStateTransitionCode,
            DataResidencyErrors.ServiceErrorCode,
            DataResidencyErrors.EventHistoryUnavailableCode
        };

        codes.ShouldBeUnique();
    }
}

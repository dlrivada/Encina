using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit.ReadAudit;

/// <summary>
/// Unit tests for <see cref="ReadAuditErrors"/> factory methods.
/// </summary>
public class ReadAuditErrorsTests
{
    #region Error Code Constants

    [Fact]
    public void StoreErrorCode_ShouldBeCorrect()
    {
        ReadAuditErrors.StoreErrorCode.ShouldBe("read_audit.store_error");
    }

    [Fact]
    public void NotFoundCode_ShouldBeCorrect()
    {
        ReadAuditErrors.NotFoundCode.ShouldBe("read_audit.not_found");
    }

    [Fact]
    public void InvalidQueryCode_ShouldBeCorrect()
    {
        ReadAuditErrors.InvalidQueryCode.ShouldBe("read_audit.invalid_query");
    }

    [Fact]
    public void PurgeFailedCode_ShouldBeCorrect()
    {
        ReadAuditErrors.PurgeFailedCode.ShouldBe("read_audit.purge_failed");
    }

    [Fact]
    public void PurposeRequiredCode_ShouldBeCorrect()
    {
        ReadAuditErrors.PurposeRequiredCode.ShouldBe("read_audit.purpose_required");
    }

    #endregion

    #region StoreError Tests

    [Fact]
    public void StoreError_ShouldIncludeOperationInMessage()
    {
        // Act
        var error = ReadAuditErrors.StoreError("LogRead", "Connection failed");

        // Assert
        error.Message.ShouldContain("LogRead");
        error.Message.ShouldContain("Connection failed");
    }

    [Fact]
    public void StoreError_WithException_ShouldIncludeException()
    {
        // Arrange
        var exception = new InvalidOperationException("test exception");

        // Act
        var error = ReadAuditErrors.StoreError("Query", "Failed", exception);

        // Assert
        error.Exception.IsSome.ShouldBeTrue();
    }

    #endregion

    #region NotFound Tests

    [Fact]
    public void NotFound_ShouldIncludeEntityDetails()
    {
        // Act
        var error = ReadAuditErrors.NotFound("Patient", "P-123");

        // Assert
        error.Message.ShouldContain("Patient");
        error.Message.ShouldContain("P-123");
    }

    #endregion

    #region InvalidQuery Tests

    [Fact]
    public void InvalidQuery_ShouldIncludeReason()
    {
        // Act
        var error = ReadAuditErrors.InvalidQuery("PageSize must be positive");

        // Assert
        error.Message.ShouldContain("PageSize must be positive");
    }

    #endregion

    #region PurgeFailed Tests

    [Fact]
    public void PurgeFailed_ShouldIncludeReason()
    {
        // Act
        var error = ReadAuditErrors.PurgeFailed("Timeout exceeded");

        // Assert
        error.Message.ShouldContain("Timeout exceeded");
    }

    [Fact]
    public void PurgeFailed_WithException_ShouldIncludeException()
    {
        // Arrange
        var exception = new TimeoutException("db timeout");

        // Act
        var error = ReadAuditErrors.PurgeFailed("Timeout", exception);

        // Assert
        error.Exception.IsSome.ShouldBeTrue();
    }

    #endregion

    #region PurposeRequired Tests

    [Fact]
    public void PurposeRequired_ShouldIncludeEntityTypeAndUserId()
    {
        // Act
        var error = ReadAuditErrors.PurposeRequired("Patient", "user-1");

        // Assert
        error.Message.ShouldContain("Patient");
    }

    [Fact]
    public void PurposeRequired_WithNullUserId_ShouldNotThrow()
    {
        // Act
        var act = () => ReadAuditErrors.PurposeRequired("Patient", null);

        // Assert
        Should.NotThrow(act);
    }

    #endregion
}

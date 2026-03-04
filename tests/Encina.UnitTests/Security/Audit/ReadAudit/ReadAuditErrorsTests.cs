using Encina.Security.Audit;
using FluentAssertions;

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
        ReadAuditErrors.StoreErrorCode.Should().Be("read_audit.store_error");
    }

    [Fact]
    public void NotFoundCode_ShouldBeCorrect()
    {
        ReadAuditErrors.NotFoundCode.Should().Be("read_audit.not_found");
    }

    [Fact]
    public void InvalidQueryCode_ShouldBeCorrect()
    {
        ReadAuditErrors.InvalidQueryCode.Should().Be("read_audit.invalid_query");
    }

    [Fact]
    public void PurgeFailedCode_ShouldBeCorrect()
    {
        ReadAuditErrors.PurgeFailedCode.Should().Be("read_audit.purge_failed");
    }

    [Fact]
    public void PurposeRequiredCode_ShouldBeCorrect()
    {
        ReadAuditErrors.PurposeRequiredCode.Should().Be("read_audit.purpose_required");
    }

    #endregion

    #region StoreError Tests

    [Fact]
    public void StoreError_ShouldIncludeOperationInMessage()
    {
        // Act
        var error = ReadAuditErrors.StoreError("LogRead", "Connection failed");

        // Assert
        error.Message.Should().Contain("LogRead");
        error.Message.Should().Contain("Connection failed");
    }

    [Fact]
    public void StoreError_WithException_ShouldIncludeException()
    {
        // Arrange
        var exception = new InvalidOperationException("test exception");

        // Act
        var error = ReadAuditErrors.StoreError("Query", "Failed", exception);

        // Assert
        error.Exception.IsSome.Should().BeTrue();
    }

    #endregion

    #region NotFound Tests

    [Fact]
    public void NotFound_ShouldIncludeEntityDetails()
    {
        // Act
        var error = ReadAuditErrors.NotFound("Patient", "P-123");

        // Assert
        error.Message.Should().Contain("Patient");
        error.Message.Should().Contain("P-123");
    }

    #endregion

    #region InvalidQuery Tests

    [Fact]
    public void InvalidQuery_ShouldIncludeReason()
    {
        // Act
        var error = ReadAuditErrors.InvalidQuery("PageSize must be positive");

        // Assert
        error.Message.Should().Contain("PageSize must be positive");
    }

    #endregion

    #region PurgeFailed Tests

    [Fact]
    public void PurgeFailed_ShouldIncludeReason()
    {
        // Act
        var error = ReadAuditErrors.PurgeFailed("Timeout exceeded");

        // Assert
        error.Message.Should().Contain("Timeout exceeded");
    }

    [Fact]
    public void PurgeFailed_WithException_ShouldIncludeException()
    {
        // Arrange
        var exception = new TimeoutException("db timeout");

        // Act
        var error = ReadAuditErrors.PurgeFailed("Timeout", exception);

        // Assert
        error.Exception.IsSome.Should().BeTrue();
    }

    #endregion

    #region PurposeRequired Tests

    [Fact]
    public void PurposeRequired_ShouldIncludeEntityTypeAndUserId()
    {
        // Act
        var error = ReadAuditErrors.PurposeRequired("Patient", "user-1");

        // Assert
        error.Message.Should().Contain("Patient");
    }

    [Fact]
    public void PurposeRequired_WithNullUserId_ShouldNotThrow()
    {
        // Act
        var act = () => ReadAuditErrors.PurposeRequired("Patient", null);

        // Assert
        act.Should().NotThrow();
    }

    #endregion
}

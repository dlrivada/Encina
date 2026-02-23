using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent.Model;

/// <summary>
/// Unit tests for <see cref="BulkOperationResult"/> and <see cref="BulkOperationError"/>.
/// </summary>
public class BulkOperationResultTests
{
    #region Success Factory

    [Fact]
    public void Success_ShouldCreateResultWithZeroFailures()
    {
        // Act
        var result = BulkOperationResult.Success(5);

        // Assert
        result.SuccessCount.Should().Be(5);
        result.FailureCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
        result.AllSucceeded.Should().BeTrue();
        result.HasFailures.Should().BeFalse();
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public void Success_WithZeroCount_ShouldBeAllSucceeded()
    {
        // Act
        var result = BulkOperationResult.Success(0);

        // Assert
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
        result.AllSucceeded.Should().BeTrue();
        result.HasFailures.Should().BeFalse();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region Partial Factory

    [Fact]
    public void Partial_ShouldCreateResultWithFailures()
    {
        // Arrange
        var errors = new List<BulkOperationError>
        {
            new("user-1:marketing", EncinaError.New("Failed")),
            new("user-2:analytics", EncinaError.New("Also failed"))
        };

        // Act
        var result = BulkOperationResult.Partial(3, errors);

        // Assert
        result.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(2);
        result.Errors.Should().HaveCount(2);
        result.AllSucceeded.Should().BeFalse();
        result.HasFailures.Should().BeTrue();
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public void Partial_WithAllFailures_ShouldHaveZeroSuccess()
    {
        // Arrange
        var errors = new List<BulkOperationError>
        {
            new("item-1", EncinaError.New("Error 1"))
        };

        // Act
        var result = BulkOperationResult.Partial(0, errors);

        // Assert
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(1);
        result.AllSucceeded.Should().BeFalse();
        result.HasFailures.Should().BeTrue();
        result.TotalCount.Should().Be(1);
    }

    #endregion

    #region BulkOperationError Tests

    [Fact]
    public void BulkOperationError_ShouldStoreIdentifierAndError()
    {
        // Arrange
        var error = EncinaError.New("Something went wrong");

        // Act
        var bulkError = new BulkOperationError("user-123:marketing", error);

        // Assert
        bulkError.Identifier.Should().Be("user-123:marketing");
        bulkError.Error.Message.Should().Be("Something went wrong");
    }

    [Fact]
    public void BulkOperationError_Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var error = EncinaError.New("Error");
        var e1 = new BulkOperationError("id-1", error);
        var e2 = new BulkOperationError("id-1", error);

        // Assert
        e1.Should().Be(e2);
    }

    #endregion
}

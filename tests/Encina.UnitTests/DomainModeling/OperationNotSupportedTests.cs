using Encina.DomainModeling;

namespace Encina.UnitTests.DomainModeling;

/// <summary>
/// Tests for OperationNotSupported error factory method used by non-EF Core providers.
/// </summary>
public class OperationNotSupportedTests
{
    private sealed class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #region OperationNotSupported Tests

    [Fact]
    public void OperationNotSupported_ShouldCreateErrorWithCorrectCode()
    {
        // Act
        var error = RepositoryErrors.OperationNotSupported<TestEntity>("UpdateImmutable");

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.OperationNotSupportedErrorCode));
    }

    [Fact]
    public void OperationNotSupported_ShouldContainEntityTypeInMessage()
    {
        // Act
        var error = RepositoryErrors.OperationNotSupported<TestEntity>("UpdateImmutable");

        // Assert
        error.Message.ShouldContain("TestEntity");
    }

    [Fact]
    public void OperationNotSupported_ShouldContainOperationNameInMessage()
    {
        // Arrange
        const string operationName = "UpdateImmutableAsync";

        // Act
        var error = RepositoryErrors.OperationNotSupported<TestEntity>(operationName);

        // Assert
        error.Message.ShouldContain(operationName);
    }

    [Fact]
    public void OperationNotSupported_ShouldContainEntityTypeInDetails()
    {
        // Act
        var error = RepositoryErrors.OperationNotSupported<TestEntity>("UpdateImmutable");

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("EntityType");
        details["EntityType"].ShouldBe("TestEntity");
    }

    [Fact]
    public void OperationNotSupported_ShouldContainOperationNameInDetails()
    {
        // Arrange
        const string operationName = "UpdateImmutableAsync";

        // Act
        var error = RepositoryErrors.OperationNotSupported<TestEntity>(operationName);

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("OperationName");
        details["OperationName"].ShouldBe(operationName);
    }

    [Fact]
    public void OperationNotSupported_ShouldMentionHelperInMessage()
    {
        // Act
        var error = RepositoryErrors.OperationNotSupported<TestEntity>("UpdateImmutable");

        // Assert - Should recommend ImmutableAggregateHelper
        error.Message.ShouldContain("ImmutableAggregateHelper");
    }

    [Fact]
    public void OperationNotSupported_ErrorCode_ShouldBeCorrectValue()
    {
        // Assert
        RepositoryErrors.OperationNotSupportedErrorCode.ShouldBe("Repository.OperationNotSupported");
    }

    #endregion

    #region Consistency Across Operations

    [Theory]
    [InlineData("UpdateImmutable")]
    [InlineData("UpdateImmutableAsync")]
    public void OperationNotSupported_DifferentOperations_ShouldHaveConsistentFormat(string operationName)
    {
        // Act
        var error = RepositoryErrors.OperationNotSupported<TestEntity>(operationName);

        // Assert - All should have same code
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.OperationNotSupportedErrorCode));

        // All should mention the entity type
        error.Message.ShouldContain("TestEntity");

        // All should mention the operation
        error.Message.ShouldContain(operationName);

        // All should recommend the helper
        error.Message.ShouldContain("ImmutableAggregateHelper");
    }

    #endregion
}

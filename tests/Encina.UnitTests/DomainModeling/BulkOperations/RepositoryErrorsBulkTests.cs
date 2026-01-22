using Encina.DomainModeling;

namespace Encina.UnitTests.DomainModeling.BulkOperations;

/// <summary>
/// Unit tests for <see cref="RepositoryErrors"/> bulk operation error factory methods.
/// </summary>
[Trait("Category", "Unit")]
public class RepositoryErrorsBulkTests
{
    private sealed class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #region Error Code Properties Tests

    [Fact]
    public void BulkErrorCodes_HaveCorrectValues()
    {
        // Assert
        RepositoryErrors.BulkInsertFailedErrorCode.ShouldBe("Repository.BulkInsertFailed");
        RepositoryErrors.BulkUpdateFailedErrorCode.ShouldBe("Repository.BulkUpdateFailed");
        RepositoryErrors.BulkDeleteFailedErrorCode.ShouldBe("Repository.BulkDeleteFailed");
        RepositoryErrors.BulkMergeFailedErrorCode.ShouldBe("Repository.BulkMergeFailed");
        RepositoryErrors.BulkReadFailedErrorCode.ShouldBe("Repository.BulkReadFailed");
    }

    #endregion

    #region BulkInsertFailed Tests

    [Fact]
    public void BulkInsertFailed_WithException_ReturnsErrorWithCorrectCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Bulk insert error");

        // Act
        var error = RepositoryErrors.BulkInsertFailed<TestEntity>(100, exception);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.BulkInsertFailedErrorCode));
    }

    [Fact]
    public void BulkInsertFailed_WithException_IncludesEntityCount()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkInsertFailed<TestEntity>(250, exception);

        // Assert
        error.Message.ShouldContain("250");
        var details = error.GetDetails();
        details.ShouldContainKey("EntityCount");
        details["EntityCount"].ShouldBe(250);
    }

    [Fact]
    public void BulkInsertFailed_WithException_IncludesEntityType()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkInsertFailed<TestEntity>(100, exception);

        // Assert
        error.Message.ShouldContain("TestEntity");
        var details = error.GetDetails();
        details.ShouldContainKey("EntityType");
        details["EntityType"].ShouldBe("TestEntity");
    }

    [Fact]
    public void BulkInsertFailed_WithException_IncludesExceptionMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Database timeout occurred");

        // Act
        var error = RepositoryErrors.BulkInsertFailed<TestEntity>(100, exception);

        // Assert
        error.Message.ShouldContain("Database timeout occurred");
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBe(exception));
    }

    [Fact]
    public void BulkInsertFailed_WithFailedIndex_IncludesIndexInMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkInsertFailed<TestEntity>(100, exception, failedIndex: 42);

        // Assert
        error.Message.ShouldContain("at index 42");
        var details = error.GetDetails();
        details.ShouldContainKey("FailedIndex");
        details["FailedIndex"].ShouldBe(42);
    }

    [Fact]
    public void BulkInsertFailed_WithoutFailedIndex_ExcludesIndexFromMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkInsertFailed<TestEntity>(100, exception);

        // Assert
        error.Message.ShouldNotContain("at index");
        var details = error.GetDetails();
        details["FailedIndex"].ShouldBeNull();
    }

    [Fact]
    public void BulkInsertFailed_WithException_IncludesExceptionType()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkInsertFailed<TestEntity>(100, exception);

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("ExceptionType");
        details["ExceptionType"].ShouldBe(typeof(InvalidOperationException).FullName);
    }

    [Fact]
    public void BulkInsertFailed_NullException_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            RepositoryErrors.BulkInsertFailed<TestEntity>(100, null!));
    }

    [Fact]
    public void BulkInsertFailed_WithReason_ReturnsErrorWithCorrectCode()
    {
        // Act
        var error = RepositoryErrors.BulkInsertFailed<TestEntity>("SqlBulkCopy requires SqlConnection");

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.BulkInsertFailedErrorCode));
    }

    [Fact]
    public void BulkInsertFailed_WithReason_IncludesReasonInMessage()
    {
        // Act
        var error = RepositoryErrors.BulkInsertFailed<TestEntity>("Connection is not a SqlConnection");

        // Assert
        error.Message.ShouldContain("Connection is not a SqlConnection");
        var details = error.GetDetails();
        details.ShouldContainKey("Reason");
        details["Reason"].ShouldBe("Connection is not a SqlConnection");
    }

    #endregion

    #region BulkUpdateFailed Tests

    [Fact]
    public void BulkUpdateFailed_WithException_ReturnsErrorWithCorrectCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Update error");

        // Act
        var error = RepositoryErrors.BulkUpdateFailed<TestEntity>(50, exception);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.BulkUpdateFailedErrorCode));
    }

    [Fact]
    public void BulkUpdateFailed_WithException_IncludesEntityCountAndType()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkUpdateFailed<TestEntity>(75, exception);

        // Assert
        error.Message.ShouldContain("75");
        error.Message.ShouldContain("TestEntity");
        var details = error.GetDetails();
        details["EntityCount"].ShouldBe(75);
        details["EntityType"].ShouldBe("TestEntity");
    }

    [Fact]
    public void BulkUpdateFailed_WithFailedIndex_IncludesIndexInDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkUpdateFailed<TestEntity>(50, exception, failedIndex: 10);

        // Assert
        error.Message.ShouldContain("at index 10");
        var details = error.GetDetails();
        details["FailedIndex"].ShouldBe(10);
    }

    [Fact]
    public void BulkUpdateFailed_NullException_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            RepositoryErrors.BulkUpdateFailed<TestEntity>(50, null!));
    }

    [Fact]
    public void BulkUpdateFailed_WithReason_IncludesReasonInMessage()
    {
        // Act
        var error = RepositoryErrors.BulkUpdateFailed<TestEntity>("TVP operations require SqlConnection");

        // Assert
        error.Message.ShouldContain("TVP operations require SqlConnection");
        var details = error.GetDetails();
        details["Reason"].ShouldBe("TVP operations require SqlConnection");
    }

    #endregion

    #region BulkDeleteFailed Tests

    [Fact]
    public void BulkDeleteFailed_WithException_ReturnsErrorWithCorrectCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Delete error");

        // Act
        var error = RepositoryErrors.BulkDeleteFailed<TestEntity>(30, exception);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.BulkDeleteFailedErrorCode));
    }

    [Fact]
    public void BulkDeleteFailed_WithException_IncludesEntityCountAndType()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkDeleteFailed<TestEntity>(25, exception);

        // Assert
        error.Message.ShouldContain("25");
        error.Message.ShouldContain("TestEntity");
    }

    [Fact]
    public void BulkDeleteFailed_WithFailedIndex_IncludesIndexInDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkDeleteFailed<TestEntity>(30, exception, failedIndex: 5);

        // Assert
        error.Message.ShouldContain("at index 5");
        var details = error.GetDetails();
        details["FailedIndex"].ShouldBe(5);
    }

    [Fact]
    public void BulkDeleteFailed_NullException_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            RepositoryErrors.BulkDeleteFailed<TestEntity>(30, null!));
    }

    [Fact]
    public void BulkDeleteFailed_WithReason_IncludesReasonInMessage()
    {
        // Act
        var error = RepositoryErrors.BulkDeleteFailed<TestEntity>("Foreign key constraint violation");

        // Assert
        error.Message.ShouldContain("Foreign key constraint violation");
        var details = error.GetDetails();
        details["Reason"].ShouldBe("Foreign key constraint violation");
    }

    #endregion

    #region BulkMergeFailed Tests

    [Fact]
    public void BulkMergeFailed_WithException_ReturnsErrorWithCorrectCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Merge error");

        // Act
        var error = RepositoryErrors.BulkMergeFailed<TestEntity>(80, exception);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.BulkMergeFailedErrorCode));
    }

    [Fact]
    public void BulkMergeFailed_WithException_IncludesEntityCountAndType()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkMergeFailed<TestEntity>(120, exception);

        // Assert
        error.Message.ShouldContain("120");
        error.Message.ShouldContain("TestEntity");
    }

    [Fact]
    public void BulkMergeFailed_WithFailedIndex_IncludesIndexInDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkMergeFailed<TestEntity>(80, exception, failedIndex: 15);

        // Assert
        error.Message.ShouldContain("at index 15");
        var details = error.GetDetails();
        details["FailedIndex"].ShouldBe(15);
    }

    [Fact]
    public void BulkMergeFailed_NullException_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            RepositoryErrors.BulkMergeFailed<TestEntity>(80, null!));
    }

    [Fact]
    public void BulkMergeFailed_WithReason_IncludesReasonInMessage()
    {
        // Act
        var error = RepositoryErrors.BulkMergeFailed<TestEntity>("Unique constraint violation on Name column");

        // Assert
        error.Message.ShouldContain("Unique constraint violation on Name column");
        var details = error.GetDetails();
        details["Reason"].ShouldBe("Unique constraint violation on Name column");
    }

    #endregion

    #region BulkReadFailed Tests

    [Fact]
    public void BulkReadFailed_WithException_ReturnsErrorWithCorrectCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Read error");

        // Act
        var error = RepositoryErrors.BulkReadFailed<TestEntity>(200, exception);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.BulkReadFailedErrorCode));
    }

    [Fact]
    public void BulkReadFailed_WithException_IncludesIdCount()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkReadFailed<TestEntity>(500, exception);

        // Assert
        error.Message.ShouldContain("500");
        error.Message.ShouldContain("IDs");
        var details = error.GetDetails();
        details.ShouldContainKey("IdCount");
        details["IdCount"].ShouldBe(500);
    }

    [Fact]
    public void BulkReadFailed_WithException_IncludesEntityType()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkReadFailed<TestEntity>(100, exception);

        // Assert
        error.Message.ShouldContain("TestEntity");
        var details = error.GetDetails();
        details["EntityType"].ShouldBe("TestEntity");
    }

    [Fact]
    public void BulkReadFailed_WithException_IncludesExceptionDetails()
    {
        // Arrange
        var exception = new TimeoutException("Query timeout");

        // Act
        var error = RepositoryErrors.BulkReadFailed<TestEntity>(100, exception);

        // Assert
        error.Message.ShouldContain("Query timeout");
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBe(exception));
        var details = error.GetDetails();
        details["ExceptionType"].ShouldBe(typeof(TimeoutException).FullName);
    }

    [Fact]
    public void BulkReadFailed_NullException_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            RepositoryErrors.BulkReadFailed<TestEntity>(100, null!));
    }

    [Fact]
    public void BulkReadFailed_WithReason_IncludesReasonInMessage()
    {
        // Act
        var error = RepositoryErrors.BulkReadFailed<TestEntity>("Invalid ID format in bulk read request");

        // Assert
        error.Message.ShouldContain("Invalid ID format in bulk read request");
        var details = error.GetDetails();
        details["Reason"].ShouldBe("Invalid ID format in bulk read request");
    }

    #endregion

    #region Generic Entity Type Tests

    [Fact]
    public void BulkInsertFailed_WithDifferentEntityType_UsesCorrectTypeName()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        // Act
        var error = RepositoryErrors.BulkInsertFailed<Order>(100, exception);

        // Assert
        error.Message.ShouldContain("Order");
        var details = error.GetDetails();
        details["EntityType"].ShouldBe("Order");
    }

    private sealed class Order
    {
        public Guid Id { get; set; }
    }

    #endregion
}

using Encina;
using Encina.DomainModeling;
using LanguageExt;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.DomainModeling;

/// <summary>
/// Unit tests for <see cref="UnitOfWorkErrors"/>.
/// </summary>
[Trait("Category", "Unit")]
public class UnitOfWorkErrorsTests
{
    #region TransactionAlreadyActive Tests

    [Fact]
    public void TransactionAlreadyActive_ReturnsErrorWithCorrectCode()
    {
        // Act
        var error = UnitOfWorkErrors.TransactionAlreadyActive();

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.TransactionAlreadyActiveErrorCode));
    }

    [Fact]
    public void TransactionAlreadyActive_ReturnsErrorWithDescriptiveMessage()
    {
        // Act
        var error = UnitOfWorkErrors.TransactionAlreadyActive();

        // Assert
        error.Message.ShouldContain("transaction");
        error.Message.ShouldContain("already active");
    }

    #endregion

    #region NoActiveTransaction Tests

    [Fact]
    public void NoActiveTransaction_ReturnsErrorWithCorrectCode()
    {
        // Act
        var error = UnitOfWorkErrors.NoActiveTransaction();

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.NoActiveTransactionErrorCode));
    }

    [Fact]
    public void NoActiveTransaction_WithOperation_IncludesOperationInMessage()
    {
        // Act
        var error = UnitOfWorkErrors.NoActiveTransaction("Rollback");

        // Assert
        error.Message.ShouldContain("Rollback");
        var details = error.GetDetails();
        details.ShouldContainKey("Operation");
        details["Operation"].ShouldBe("Rollback");
    }

    [Fact]
    public void NoActiveTransaction_DefaultsToCommit()
    {
        // Act
        var error = UnitOfWorkErrors.NoActiveTransaction();

        // Assert
        error.Message.ShouldContain("Commit");
    }

    #endregion

    #region SaveChangesFailed Tests

    [Fact]
    public void SaveChangesFailed_NullException_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            UnitOfWorkErrors.SaveChangesFailed(null!));
    }

    [Fact]
    public void SaveChangesFailed_WithException_ReturnsErrorWithCorrectCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var error = UnitOfWorkErrors.SaveChangesFailed(exception);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.SaveChangesFailedErrorCode));
    }

    [Fact]
    public void SaveChangesFailed_WithException_IncludesExceptionMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Database connection failed");

        // Act
        var error = UnitOfWorkErrors.SaveChangesFailed(exception);

        // Assert
        error.Message.ShouldContain("Database connection failed");
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBe(exception));
    }

    [Fact]
    public void SaveChangesFailed_WithException_IncludesExceptionTypeInDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Test");

        // Act
        var error = UnitOfWorkErrors.SaveChangesFailed(exception);

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("ExceptionType");
        details["ExceptionType"].ShouldBe(typeof(InvalidOperationException).FullName);
    }

    [Fact]
    public void SaveChangesFailed_WithConflictingEntities_IncludesEntitiesInDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Concurrency conflict");
        var entities = new[] { "Order", "OrderItem" };

        // Act
        var error = UnitOfWorkErrors.SaveChangesFailed(exception, entities);

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("ConflictingEntities");
        var conflictingEntities = details["ConflictingEntities"] as List<string>;
        conflictingEntities.ShouldNotBeNull();
        conflictingEntities.ShouldContain("Order");
        conflictingEntities.ShouldContain("OrderItem");
    }

    [Fact]
    public void SaveChangesFailed_WithConflictingEntities_IncludesEntitiesInMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Concurrency");
        var entities = new[] { "Order", "Customer" };

        // Act
        var error = UnitOfWorkErrors.SaveChangesFailed(exception, entities);

        // Assert
        error.Message.ShouldContain("Order");
        error.Message.ShouldContain("Customer");
    }

    [Fact]
    public void SaveChangesFailed_NullConflictingEntities_CreatesEmptyList()
    {
        // Arrange
        var exception = new InvalidOperationException("Test");

        // Act
        var error = UnitOfWorkErrors.SaveChangesFailed(exception, null);

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("ConflictingEntities");
        var conflictingEntities = details["ConflictingEntities"] as List<string>;
        conflictingEntities.ShouldNotBeNull();
        conflictingEntities.ShouldBeEmpty();
    }

    #endregion

    #region CommitFailed Tests

    [Fact]
    public void CommitFailed_NullException_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            UnitOfWorkErrors.CommitFailed(null!));
    }

    [Fact]
    public void CommitFailed_ReturnsErrorWithCorrectCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Commit error");

        // Act
        var error = UnitOfWorkErrors.CommitFailed(exception);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.CommitFailedErrorCode));
    }

    [Fact]
    public void CommitFailed_IncludesExceptionDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Connection lost");

        // Act
        var error = UnitOfWorkErrors.CommitFailed(exception);

        // Assert
        error.Message.ShouldContain("Connection lost");
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBe(exception));
        var details = error.GetDetails();
        details["ExceptionType"].ShouldBe(typeof(InvalidOperationException).FullName);
    }

    #endregion

    #region TransactionStartFailed Tests

    [Fact]
    public void TransactionStartFailed_NullException_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            UnitOfWorkErrors.TransactionStartFailed(null!));
    }

    [Fact]
    public void TransactionStartFailed_ReturnsErrorWithCorrectCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Cannot start transaction");

        // Act
        var error = UnitOfWorkErrors.TransactionStartFailed(exception);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.TransactionStartFailedErrorCode));
    }

    [Fact]
    public void TransactionStartFailed_IncludesExceptionDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Database unavailable");

        // Act
        var error = UnitOfWorkErrors.TransactionStartFailed(exception);

        // Assert
        error.Message.ShouldContain("Database unavailable");
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBe(exception));
        var details = error.GetDetails();
        details["ExceptionType"].ShouldBe(typeof(InvalidOperationException).FullName);
    }

    #endregion

    #region Error Code Properties Tests

    [Fact]
    public void ErrorCodeProperties_ReturnCorrectCodes()
    {
        // Assert
        UnitOfWorkErrors.TransactionAlreadyActiveErrorCode.ShouldBe("UnitOfWork.TransactionAlreadyActive");
        UnitOfWorkErrors.NoActiveTransactionErrorCode.ShouldBe("UnitOfWork.NoActiveTransaction");
        UnitOfWorkErrors.SaveChangesFailedErrorCode.ShouldBe("UnitOfWork.SaveChangesFailed");
        UnitOfWorkErrors.CommitFailedErrorCode.ShouldBe("UnitOfWork.CommitFailed");
        UnitOfWorkErrors.TransactionStartFailedErrorCode.ShouldBe("UnitOfWork.TransactionStartFailed");
    }

    #endregion
}

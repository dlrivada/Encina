using Encina.DomainModeling;

namespace Encina.UnitTests.DomainModeling;

/// <summary>
/// Tests for immutable-related RepositoryErrors factory methods (EntityNotTracked, UpdateFailed).
/// </summary>
public class RepositoryErrorsImmutableTests
{
    private sealed class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #region EntityNotTracked Tests

    [Fact]
    public void EntityNotTracked_Generic_ShouldCreateErrorWithCorrectCode()
    {
        // Act
        var error = RepositoryErrors.EntityNotTracked<TestEntity>();

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.EntityNotTrackedErrorCode));
    }

    [Fact]
    public void EntityNotTracked_Generic_ShouldContainEntityTypeInMessage()
    {
        // Act
        var error = RepositoryErrors.EntityNotTracked<TestEntity>();

        // Assert
        error.Message.ShouldContain("TestEntity");
        error.Message.ShouldContain("not being tracked");
    }

    [Fact]
    public void EntityNotTracked_Generic_ShouldContainEntityTypeInDetails()
    {
        // Act
        var error = RepositoryErrors.EntityNotTracked<TestEntity>();

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("EntityType");
        details["EntityType"].ShouldBe("TestEntity");
    }

    [Fact]
    public void EntityNotTracked_WithId_ShouldCreateErrorWithCorrectCode()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = RepositoryErrors.EntityNotTracked<TestEntity, Guid>(id);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.EntityNotTrackedErrorCode));
    }

    [Fact]
    public void EntityNotTracked_WithId_ShouldContainIdInMessage()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = RepositoryErrors.EntityNotTracked<TestEntity, Guid>(id);

        // Assert
        error.Message.ShouldContain(id.ToString());
        error.Message.ShouldContain("TestEntity");
    }

    [Fact]
    public void EntityNotTracked_WithId_ShouldContainIdInDetails()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = RepositoryErrors.EntityNotTracked<TestEntity, Guid>(id);

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("EntityId");
        details["EntityId"].ShouldBe(id.ToString());
    }

    #endregion

    #region UpdateFailed Tests

    [Fact]
    public void UpdateFailed_WithReason_ShouldCreateErrorWithCorrectCode()
    {
        // Act
        var error = RepositoryErrors.UpdateFailed<TestEntity>("Test reason");

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.UpdateFailedErrorCode));
    }

    [Fact]
    public void UpdateFailed_WithReason_ShouldContainReasonInMessage()
    {
        // Arrange
        const string reason = "Entity state conflict";

        // Act
        var error = RepositoryErrors.UpdateFailed<TestEntity>(reason);

        // Assert
        error.Message.ShouldContain(reason);
        error.Message.ShouldContain("TestEntity");
    }

    [Fact]
    public void UpdateFailed_WithReason_ShouldContainReasonInDetails()
    {
        // Arrange
        const string reason = "Entity state conflict";

        // Act
        var error = RepositoryErrors.UpdateFailed<TestEntity>(reason);

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("Reason");
        details["Reason"].ShouldBe(reason);
    }

    [Fact]
    public void UpdateFailed_WithException_ShouldIncludeException()
    {
        // Arrange
        var exception = new InvalidOperationException("Inner error");

        // Act
        var error = RepositoryErrors.UpdateFailed<TestEntity>("Update failed", exception);

        // Assert
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBe(exception));
    }

    [Fact]
    public void UpdateFailed_WithNullException_ShouldNotThrow()
    {
        // Act - Verify that passing null exception doesn't throw
        var error = RepositoryErrors.UpdateFailed<TestEntity>("Update failed", null);

        // Assert - Error is created successfully with the message
        error.Message.ShouldContain("Update failed");
        // Note: EncinaError.Exception may still be Some (containing internal EncinaException)
        // when created via EncinaErrors.Create, even without a user-provided exception.
        // The important thing is that it doesn't throw when null is passed.
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void UpdateFailed_WithId_ShouldContainIdInMessage()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = RepositoryErrors.UpdateFailed<TestEntity, Guid>(id, "Test reason");

        // Assert
        error.Message.ShouldContain(id.ToString());
        error.Message.ShouldContain("TestEntity");
    }

    [Fact]
    public void UpdateFailed_WithId_ShouldContainIdInDetails()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = RepositoryErrors.UpdateFailed<TestEntity, Guid>(id, "Test reason");

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("EntityId");
        details["EntityId"].ShouldBe(id.ToString());
    }

    [Fact]
    public void UpdateFailed_WithIdAndException_ShouldIncludeException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var exception = new InvalidOperationException("Inner error");

        // Act
        var error = RepositoryErrors.UpdateFailed<TestEntity, Guid>(id, "Update failed", exception);

        // Assert
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBe(exception));
    }

    #endregion

    #region Error Code Constants Tests

    [Fact]
    public void EntityNotTrackedErrorCode_ShouldBeRepositoryEntityNotTracked()
    {
        // Assert
        RepositoryErrors.EntityNotTrackedErrorCode.ShouldBe("Repository.EntityNotTracked");
    }

    [Fact]
    public void UpdateFailedErrorCode_ShouldBeRepositoryUpdateFailed()
    {
        // Assert
        RepositoryErrors.UpdateFailedErrorCode.ShouldBe("Repository.UpdateFailed");
    }

    #endregion
}

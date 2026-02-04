using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;

namespace Encina.UnitTests.DomainModeling.Concurrency;

/// <summary>
/// Tests for <see cref="RepositoryErrors"/> ConcurrencyConflict factory methods.
/// </summary>
public sealed class RepositoryErrorsConcurrencyTests
{
    #region ConcurrencyConflict<TEntity, TId> Tests

    [Fact]
    public void ConcurrencyConflict_WithId_ShouldCreateErrorWithCorrectCode()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = RepositoryErrors.ConcurrencyConflict<TestEntity, Guid>(id);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode));
    }

    [Fact]
    public void ConcurrencyConflict_WithId_ShouldContainIdInMessage()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = RepositoryErrors.ConcurrencyConflict<TestEntity, Guid>(id);

        // Assert
        error.Message.ShouldContain(id.ToString());
        error.Message.ShouldContain("TestEntity");
        error.Message.ShouldContain("modified by another process");
    }

    [Fact]
    public void ConcurrencyConflict_WithId_ShouldContainIdInDetails()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = RepositoryErrors.ConcurrencyConflict<TestEntity, Guid>(id);

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("EntityType");
        details.ShouldContainKey("EntityId");
        details["EntityType"].ShouldBe("TestEntity");
        details["EntityId"].ShouldBe(id.ToString());
    }

    [Fact]
    public void ConcurrencyConflict_WithIdAndException_ShouldIncludeException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var exception = new InvalidOperationException("Concurrency violation");

        // Act
        var error = RepositoryErrors.ConcurrencyConflict<TestEntity, Guid>(id, exception);

        // Assert
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBe(exception));
    }

    #endregion

    #region ConcurrencyConflict<TEntity> Tests (No Id)

    [Fact]
    public void ConcurrencyConflict_WithoutId_ShouldCreateErrorWithCorrectCode()
    {
        // Act
        var error = RepositoryErrors.ConcurrencyConflict<TestEntity>();

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode));
    }

    [Fact]
    public void ConcurrencyConflict_WithoutId_ShouldContainEntityTypeInMessage()
    {
        // Act
        var error = RepositoryErrors.ConcurrencyConflict<TestEntity>();

        // Assert
        error.Message.ShouldContain("TestEntity");
        error.Message.ShouldContain("modified by another process");
    }

    [Fact]
    public void ConcurrencyConflict_WithoutId_ShouldContainEntityTypeInDetails()
    {
        // Act
        var error = RepositoryErrors.ConcurrencyConflict<TestEntity>();

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("EntityType");
        details["EntityType"].ShouldBe("TestEntity");
    }

    [Fact]
    public void ConcurrencyConflict_WithoutIdAndException_ShouldIncludeException()
    {
        // Arrange
        var exception = new InvalidOperationException("Concurrency violation");

        // Act
        var error = RepositoryErrors.ConcurrencyConflict<TestEntity>(exception);

        // Assert
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBe(exception));
    }

    #endregion

    #region ConcurrencyConflict with ConcurrencyConflictInfo Tests

    [Fact]
    public void ConcurrencyConflict_WithConflictInfo_ShouldCreateErrorWithCorrectCode()
    {
        // Arrange
        var conflictInfo = CreateConflictInfo();

        // Act
        var error = RepositoryErrors.ConcurrencyConflict(conflictInfo);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode));
    }

    [Fact]
    public void ConcurrencyConflict_WithConflictInfo_ShouldContainEntityTypeInMessage()
    {
        // Arrange
        var conflictInfo = CreateConflictInfo();

        // Act
        var error = RepositoryErrors.ConcurrencyConflict(conflictInfo);

        // Assert
        error.Message.ShouldContain("TestEntity");
        error.Message.ShouldContain("modified by another process");
    }

    [Fact]
    public void ConcurrencyConflict_WithConflictInfo_ShouldContainAllEntityStatesInDetails()
    {
        // Arrange
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };
        var database = new TestEntity { Id = current.Id, Name = "Database" };
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(current, proposed, database);

        // Act
        var error = RepositoryErrors.ConcurrencyConflict(conflictInfo);

        // Assert
        var details = error.GetDetails();
        details.ShouldContainKey("EntityType");
        details.ShouldContainKey("CurrentEntity");
        details.ShouldContainKey("ProposedEntity");
        details.ShouldContainKey("DatabaseEntity");
        details["EntityType"].ShouldBe("TestEntity");
        details["CurrentEntity"].ShouldBe(current);
        details["ProposedEntity"].ShouldBe(proposed);
        details["DatabaseEntity"].ShouldBe(database);
    }

    [Fact]
    public void ConcurrencyConflict_WithConflictInfo_WhenDeleted_ShouldIndicateDeletionInMessage()
    {
        // Arrange
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(current, proposed, null);

        // Act
        var error = RepositoryErrors.ConcurrencyConflict(conflictInfo);

        // Assert
        error.Message.ShouldContain("deleted");
    }

    [Fact]
    public void ConcurrencyConflict_WithConflictInfo_WhenNotDeleted_ShouldNotMentionDeletion()
    {
        // Arrange
        var conflictInfo = CreateConflictInfo();

        // Act
        var error = RepositoryErrors.ConcurrencyConflict(conflictInfo);

        // Assert
        error.Message.ShouldNotContain("deleted");
    }

    [Fact]
    public void ConcurrencyConflict_WithConflictInfoAndException_ShouldIncludeException()
    {
        // Arrange
        var conflictInfo = CreateConflictInfo();
        var exception = new InvalidOperationException("DB Error");

        // Act
        var error = RepositoryErrors.ConcurrencyConflict(conflictInfo, exception);

        // Assert
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBe(exception));
    }

    [Fact]
    public void ConcurrencyConflict_WithNullConflictInfo_ShouldThrowArgumentNullException()
    {
        // Arrange
        ConcurrencyConflictInfo<TestEntity> conflictInfo = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            RepositoryErrors.ConcurrencyConflict(conflictInfo));
        ex.ParamName.ShouldBe("conflictInfo");
    }

    #endregion

    #region Error Code Constant Tests

    [Fact]
    public void ConcurrencyConflictErrorCode_ShouldBeRepositoryConcurrencyConflict()
    {
        // Assert
        RepositoryErrors.ConcurrencyConflictErrorCode.ShouldBe("Repository.ConcurrencyConflict");
    }

    #endregion

    #region Test Infrastructure

    private static ConcurrencyConflictInfo<TestEntity> CreateConflictInfo()
    {
        var id = Guid.NewGuid();
        return new ConcurrencyConflictInfo<TestEntity>(
            new TestEntity { Id = id, Name = "Current" },
            new TestEntity { Id = id, Name = "Proposed" },
            new TestEntity { Id = id, Name = "Database" });
    }

    private sealed class TestEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    #endregion
}

using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;
using Encina.DomainModeling.Concurrency.Resolvers;
using Shouldly;

namespace Encina.GuardTests.DomainModeling.Concurrency;

/// <summary>
/// Guard clause tests for concurrency resolvers.
/// Tests that null checks and argument validation are enforced.
/// </summary>
[Trait("Category", "Guard")]
public sealed class ConcurrencyResolverGuardTests
{
    #region LastWriteWinsResolver Guard Tests

    [Fact]
    public async Task LastWriteWinsResolver_ResolveAsync_NullCurrent_ShouldNotThrow()
    {
        // Arrange - LastWriteWinsResolver doesn't use 'current', so null should be allowed
        var resolver = new LastWriteWinsResolver<TestEntity>();
        TestEntity current = null!;
        var proposed = new TestEntity { Id = Guid.NewGuid(), Name = "Proposed" };
        var database = new TestEntity { Id = proposed.Id, Name = "Database", Version = 1 };

        // Act - Should not throw because LastWriteWins doesn't use current
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task LastWriteWinsResolver_ResolveAsync_NullProposed_ThrowsValueIsNullException()
    {
        // Arrange
        var resolver = new LastWriteWinsResolver<TestEntity>();
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        TestEntity proposed = null!;
        var database = new TestEntity { Id = current.Id, Name = "Database", Version = 1 };

        // Act & Assert - LanguageExt's Either.Right throws when value is null
        await Should.ThrowAsync<LanguageExt.ValueIsNullException>(async () =>
            await resolver.ResolveAsync(current, proposed, database));
    }

    [Fact]
    public async Task LastWriteWinsResolver_ResolveAsync_NullDatabase_ShouldNotThrowButNotIncrementVersion()
    {
        // Arrange
        var resolver = new LastWriteWinsResolver<TestEntity>();
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed", Version = 2 };
        TestEntity database = null!;

        // Act - Should not throw but version won't be incremented
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r =>
        {
            r.ShouldNotBeNull();
            r.Version.ShouldBe(2); // Version unchanged because database was null
        });
    }

    #endregion

    #region FirstWriteWinsResolver Guard Tests

    [Fact]
    public async Task FirstWriteWinsResolver_ResolveAsync_NullCurrent_ShouldNotThrow()
    {
        // Arrange - FirstWriteWins doesn't use 'current'
        var resolver = new FirstWriteWinsResolver<TestEntity>();
        TestEntity current = null!;
        var proposed = new TestEntity { Id = Guid.NewGuid(), Name = "Proposed" };
        var database = new TestEntity { Id = proposed.Id, Name = "Database" };

        // Act
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.ShouldBe(database));
    }

    [Fact]
    public async Task FirstWriteWinsResolver_ResolveAsync_NullProposed_ShouldNotThrow()
    {
        // Arrange - FirstWriteWins doesn't use 'proposed'
        var resolver = new FirstWriteWinsResolver<TestEntity>();
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        TestEntity proposed = null!;
        var database = new TestEntity { Id = current.Id, Name = "Database" };

        // Act
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.ShouldBe(database));
    }

    [Fact]
    public async Task FirstWriteWinsResolver_ResolveAsync_NullDatabase_ThrowsValueIsNullException()
    {
        // Arrange
        var resolver = new FirstWriteWinsResolver<TestEntity>();
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };
        TestEntity database = null!;

        // Act & Assert - LanguageExt's Either.Right throws when value is null
        await Should.ThrowAsync<LanguageExt.ValueIsNullException>(async () =>
            await resolver.ResolveAsync(current, proposed, database));
    }

    #endregion

    #region MergeResolver Guard Tests

    [Fact]
    public async Task MergeResolver_ResolveAsync_WithNullParameters_CallsMergeAsync()
    {
        // Arrange
        var resolver = new TestMergeResolver();
        TestEntity current = null!;
        TestEntity proposed = null!;
        TestEntity database = null!;

        // Act - Default implementation returns error (not null check exception)
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert - MergeAsync is called, default returns error
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe("Repository.MergeNotImplemented"));
        });
    }

    #endregion

    #region RepositoryErrors.ConcurrencyConflict Guard Tests

    [Fact]
    public void RepositoryErrors_ConcurrencyConflict_WithNullConflictInfo_ThrowsArgumentNullException()
    {
        // Arrange
        ConcurrencyConflictInfo<TestEntity> conflictInfo = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            RepositoryErrors.ConcurrencyConflict(conflictInfo));
        ex.ParamName.ShouldBe("conflictInfo");
    }

    [Fact]
    public void RepositoryErrors_ConcurrencyConflict_WithNullConflictInfoAndException_ThrowsArgumentNullException()
    {
        // Arrange
        ConcurrencyConflictInfo<TestEntity> conflictInfo = null!;
        var exception = new InvalidOperationException("Test");

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            RepositoryErrors.ConcurrencyConflict(conflictInfo, exception));
        ex.ParamName.ShouldBe("conflictInfo");
    }

    #endregion

    #region ConcurrencyConflictInfo Guard Tests

    [Fact]
    public void ConcurrencyConflictInfo_ToDictionary_WithNullSerializerOptions_ShouldNotThrow()
    {
        // Arrange
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(
            new TestEntity { Id = Guid.NewGuid(), Name = "Current" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Proposed" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Database" });

        // Act - Null options means entities stored as objects, not serialized
        var dictionary = conflictInfo.ToDictionary(null);

        // Assert
        dictionary.ShouldNotBeNull();
        dictionary.Count.ShouldBe(4); // EntityType + 3 entities
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestEntity : IVersionedEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Version { get; set; }
        long IVersioned.Version => Version;
    }

    /// <summary>
    /// Test resolver that uses the default MergeAsync implementation.
    /// </summary>
    private sealed class TestMergeResolver : MergeResolver<TestEntity> { }

    #endregion
}

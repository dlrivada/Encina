using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;
using Encina.DomainModeling.Concurrency.Resolvers;

namespace Encina.UnitTests.DomainModeling.Concurrency;

/// <summary>
/// Tests for <see cref="LastWriteWinsResolver{TEntity}"/>.
/// </summary>
public sealed class LastWriteWinsResolverTests
{
    private readonly LastWriteWinsResolver<TestVersionedEntity> _resolver = new();

    #region Basic Resolution Tests

    [Fact]
    public async Task ResolveAsync_ShouldReturnProposedEntity()
    {
        // Arrange
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Modified", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "Other Changes", Version = 3 };

        // Act
        var result = await _resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.Name.ShouldBe("Modified"); // Proposed entity's value wins
        });
    }

    [Fact]
    public async Task ResolveAsync_ShouldIncrementVersionBasedOnDatabase()
    {
        // Arrange
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Modified", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "Other Changes", Version = 5 };

        // Act
        var result = await _resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.Version.ShouldBe(6); // database.Version + 1
        });
    }

    [Fact]
    public async Task ResolveAsync_WithNonVersionedEntity_ShouldReturnProposedUnchanged()
    {
        // Arrange
        var resolver = new LastWriteWinsResolver<TestNonVersionedEntity>();
        var current = new TestNonVersionedEntity { Id = Guid.NewGuid(), Name = "Original" };
        var proposed = new TestNonVersionedEntity { Id = current.Id, Name = "Modified" };
        var database = new TestNonVersionedEntity { Id = current.Id, Name = "Other Changes" };

        // Act
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.Name.ShouldBe("Modified");
        });
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ResolveAsync_WithSameVersions_ShouldStillIncrementDatabaseVersion()
    {
        // Arrange
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Modified", Version = 1 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "Same", Version = 1 };

        // Act
        var result = await _resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.Version.ShouldBe(2); // database.Version + 1
        });
    }

    [Fact]
    public async Task ResolveAsync_NeverReturnsLeft()
    {
        // Arrange - Any combination of entities
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "A", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "B", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "C", Version = 100 };

        // Act
        var result = await _resolver.ResolveAsync(current, proposed, database);

        // Assert - LastWriteWins never fails
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ResolveAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Modified", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "Other", Version = 3 };

        // Act
        var result = await _resolver.ResolveAsync(current, proposed, database, cts.Token);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Multiple Conflict Scenarios

    [Fact]
    public async Task ResolveAsync_WhenDatabaseHasManyUpdates_ShouldUseLatestDatabaseVersion()
    {
        // Arrange - Simulates a scenario where many updates happened since we loaded
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Modified", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "Many Updates", Version = 50 };

        // Act
        var result = await _resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.Version.ShouldBe(51);
            resolved.Name.ShouldBe("Modified"); // Our changes still win
        });
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestVersionedEntity : IVersionedEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Version { get; set; }

        // IVersioned explicit implementation
        long IVersioned.Version => Version;
    }

    private sealed class TestNonVersionedEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    #endregion
}

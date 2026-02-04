using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;
using Encina.DomainModeling.Concurrency.Resolvers;

namespace Encina.UnitTests.DomainModeling.Concurrency;

/// <summary>
/// Tests for <see cref="FirstWriteWinsResolver{TEntity}"/>.
/// </summary>
public sealed class FirstWriteWinsResolverTests
{
    private readonly FirstWriteWinsResolver<TestVersionedEntity> _resolver = new();

    #region Basic Resolution Tests

    [Fact]
    public async Task ResolveAsync_ShouldReturnDatabaseEntity()
    {
        // Arrange
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Modified", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "First Write", Version = 3 };

        // Act
        var result = await _resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.Name.ShouldBe("First Write"); // Database entity wins
        });
    }

    [Fact]
    public async Task ResolveAsync_ShouldPreserveDatabaseVersion()
    {
        // Arrange
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Modified", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "First Write", Version = 5 };

        // Act
        var result = await _resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.Version.ShouldBe(5); // Version unchanged - no actual update
        });
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnExactDatabaseInstance()
    {
        // Arrange
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Modified", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "First Write", Version = 3 };

        // Act
        var result = await _resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.ShouldBeSameAs(database); // Returns the exact same instance
        });
    }

    #endregion

    #region Proposed Changes Are Discarded

    [Fact]
    public async Task ResolveAsync_ShouldDiscardProposedChanges()
    {
        // Arrange - User made many changes that will be discarded
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity
        {
            Id = current.Id,
            Name = "User's Important Changes",
            Version = 2
        };
        var database = new TestVersionedEntity
        {
            Id = current.Id,
            Name = "Someone Else's Changes",
            Version = 3
        };

        // Act
        var result = await _resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.Name.ShouldNotBe("User's Important Changes");
            resolved.Name.ShouldBe("Someone Else's Changes");
        });
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ResolveAsync_WithNonVersionedEntity_ShouldReturnDatabase()
    {
        // Arrange
        var resolver = new FirstWriteWinsResolver<TestNonVersionedEntity>();
        var current = new TestNonVersionedEntity { Id = Guid.NewGuid(), Name = "Original" };
        var proposed = new TestNonVersionedEntity { Id = current.Id, Name = "Modified" };
        var database = new TestNonVersionedEntity { Id = current.Id, Name = "First Write" };

        // Act
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.Name.ShouldBe("First Write");
            resolved.ShouldBeSameAs(database);
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

        // Assert - FirstWriteWins never fails
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

    #region Comparison with LastWriteWins

    [Fact]
    public async Task ResolveAsync_DiffersFromLastWriteWins()
    {
        // Arrange
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Proposed", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "Database", Version = 3 };

        var firstWriteWins = new FirstWriteWinsResolver<TestVersionedEntity>();
        var lastWriteWins = new LastWriteWinsResolver<TestVersionedEntity>();

        // Act
        var firstResult = await firstWriteWins.ResolveAsync(current, proposed, database);
        var lastResult = await lastWriteWins.ResolveAsync(current, proposed, database);

        // Assert - They should return different entities
        firstResult.IfRight(first =>
        {
            lastResult.IfRight(last =>
            {
                first.Name.ShouldBe("Database");
                last.Name.ShouldBe("Proposed");
                first.Name.ShouldNotBe(last.Name);
            });
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

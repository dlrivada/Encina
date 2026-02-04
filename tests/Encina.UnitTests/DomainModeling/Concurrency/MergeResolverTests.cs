using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;
using Encina.DomainModeling.Concurrency.Resolvers;
using LanguageExt;

namespace Encina.UnitTests.DomainModeling.Concurrency;

/// <summary>
/// Tests for <see cref="MergeResolver{TEntity}"/> abstract base class behavior.
/// </summary>
public sealed class MergeResolverTests
{
    #region Default Implementation Tests

    [Fact]
    public async Task ResolveAsync_DefaultImplementation_ReturnsLeftWithNotImplementedError()
    {
        // Arrange
        var resolver = new DefaultMergeResolver();
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Modified", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "Other", Version = 3 };

        // Act
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe("Repository.MergeNotImplemented"));
            error.Message.ShouldContain("TestVersionedEntity");
            error.Message.ShouldContain("DefaultMergeResolver");
        });
    }

    #endregion

    #region Custom Merge Implementation Tests

    [Fact]
    public async Task ResolveAsync_CustomMerger_CanReturnMergedEntity()
    {
        // Arrange
        var resolver = new SuccessfulMergeResolver();
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Proposed", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "Database", Version = 3 };

        // Act
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.Name.ShouldBe("Merged: Proposed + Database");
            resolved.Version.ShouldBe(4); // database.Version + 1
        });
    }

    [Fact]
    public async Task ResolveAsync_CustomMerger_CanReturnLeftOnConflict()
    {
        // Arrange
        var resolver = new ConflictingMergeResolver();
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Changed", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "Also Changed", Version = 3 };

        // Act
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("Cannot merge");
        });
    }

    [Fact]
    public async Task ResolveAsync_PropertyMerger_MergesNonConflictingProperties()
    {
        // Arrange
        var resolver = new PropertyMergeResolver();
        var id = Guid.NewGuid();
        var current = new TestComplexEntity { Id = id, Name = "Original", Description = "Desc", Version = 1 };
        var proposed = new TestComplexEntity { Id = id, Name = "New Name", Description = "Desc", Version = 2 };
        var database = new TestComplexEntity { Id = id, Name = "Original", Description = "New Desc", Version = 3 };

        // Act
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.Name.ShouldBe("New Name"); // Our change
            resolved.Description.ShouldBe("New Desc"); // Their change
            resolved.Version.ShouldBe(4);
        });
    }

    [Fact]
    public async Task ResolveAsync_PropertyMerger_RejectsConflictingPropertyChanges()
    {
        // Arrange
        var resolver = new PropertyMergeResolver();
        var id = Guid.NewGuid();
        var current = new TestComplexEntity { Id = id, Name = "Original", Description = "Desc", Version = 1 };
        var proposed = new TestComplexEntity { Id = id, Name = "Our Name", Description = "Desc", Version = 2 };
        var database = new TestComplexEntity { Id = id, Name = "Their Name", Description = "Desc", Version = 3 };

        // Act
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("Name");
            error.Message.ShouldContain("conflicting");
        });
    }

    #endregion

    #region Async Behavior Tests

    [Fact]
    public async Task ResolveAsync_CustomMerger_CanBeAsync()
    {
        // Arrange
        var resolver = new AsyncMergeResolver();
        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Proposed", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "Database", Version = 3 };

        // Act
        var result = await resolver.ResolveAsync(current, proposed, database);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(resolved =>
        {
            resolved.Name.ShouldBe("Async Merged");
        });
    }

    [Fact]
    public async Task ResolveAsync_RespectsCancellationToken()
    {
        // Arrange
        var resolver = new CancellationAwareMergeResolver();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var current = new TestVersionedEntity { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var proposed = new TestVersionedEntity { Id = current.Id, Name = "Proposed", Version = 2 };
        var database = new TestVersionedEntity { Id = current.Id, Name = "Database", Version = 3 };

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await resolver.ResolveAsync(current, proposed, database, cts.Token));
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestVersionedEntity : IVersionedEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Version { get; set; }
        long IVersioned.Version => Version;
    }

    private sealed class TestComplexEntity : IVersionedEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public int Version { get; set; }
        long IVersioned.Version => Version;
    }

    /// <summary>
    /// A resolver that doesn't override MergeAsync, testing the default behavior.
    /// </summary>
    private sealed class DefaultMergeResolver : MergeResolver<TestVersionedEntity> { }

    /// <summary>
    /// A resolver that always succeeds with a merged entity.
    /// </summary>
    private sealed class SuccessfulMergeResolver : MergeResolver<TestVersionedEntity>
    {
        protected override Task<Either<EncinaError, TestVersionedEntity>> MergeAsync(
            TestVersionedEntity current,
            TestVersionedEntity proposed,
            TestVersionedEntity database,
            CancellationToken cancellationToken)
        {
            var merged = new TestVersionedEntity
            {
                Id = database.Id,
                Name = $"Merged: {proposed.Name} + {database.Name}",
                Version = database.Version + 1
            };
            return Task.FromResult(Either<EncinaError, TestVersionedEntity>.Right(merged));
        }
    }

    /// <summary>
    /// A resolver that always fails due to conflict.
    /// </summary>
    private sealed class ConflictingMergeResolver : MergeResolver<TestVersionedEntity>
    {
        protected override Task<Either<EncinaError, TestVersionedEntity>> MergeAsync(
            TestVersionedEntity current,
            TestVersionedEntity proposed,
            TestVersionedEntity database,
            CancellationToken cancellationToken)
        {
            var error = EncinaErrors.Create(
                "Repository.MergeConflict",
                "Cannot merge: conflicting changes detected");
            return Task.FromResult(Either<EncinaError, TestVersionedEntity>.Left(error));
        }
    }

    /// <summary>
    /// A resolver that merges non-conflicting property changes.
    /// </summary>
    private sealed class PropertyMergeResolver : MergeResolver<TestComplexEntity>
    {
        protected override Task<Either<EncinaError, TestComplexEntity>> MergeAsync(
            TestComplexEntity current,
            TestComplexEntity proposed,
            TestComplexEntity database,
            CancellationToken cancellationToken)
        {
            // Check for conflicting Name changes
            var weChangedName = current.Name != proposed.Name;
            var theyChangedName = current.Name != database.Name;

            if (weChangedName && theyChangedName)
            {
                var error = EncinaErrors.Create(
                    "Repository.MergeConflict",
                    "Property 'Name' has conflicting changes");
                return Task.FromResult(Either<EncinaError, TestComplexEntity>.Left(error));
            }

            // Check for conflicting Description changes
            var weChangedDesc = current.Description != proposed.Description;
            var theyChangedDesc = current.Description != database.Description;

            if (weChangedDesc && theyChangedDesc)
            {
                var error = EncinaErrors.Create(
                    "Repository.MergeConflict",
                    "Property 'Description' has conflicting changes");
                return Task.FromResult(Either<EncinaError, TestComplexEntity>.Left(error));
            }

            // Merge non-conflicting changes
            var merged = new TestComplexEntity
            {
                Id = database.Id,
                Name = weChangedName ? proposed.Name : database.Name,
                Description = weChangedDesc ? proposed.Description : database.Description,
                Version = database.Version + 1
            };

            return Task.FromResult(Either<EncinaError, TestComplexEntity>.Right(merged));
        }
    }

    /// <summary>
    /// A resolver that performs async operations.
    /// </summary>
    private sealed class AsyncMergeResolver : MergeResolver<TestVersionedEntity>
    {
        protected override async Task<Either<EncinaError, TestVersionedEntity>> MergeAsync(
            TestVersionedEntity current,
            TestVersionedEntity proposed,
            TestVersionedEntity database,
            CancellationToken cancellationToken)
        {
            // Simulate async work
            await Task.Delay(1, cancellationToken);

            var merged = new TestVersionedEntity
            {
                Id = database.Id,
                Name = "Async Merged",
                Version = database.Version + 1
            };
            return Either<EncinaError, TestVersionedEntity>.Right(merged);
        }
    }

    /// <summary>
    /// A resolver that checks the cancellation token.
    /// </summary>
    private sealed class CancellationAwareMergeResolver : MergeResolver<TestVersionedEntity>
    {
        protected override Task<Either<EncinaError, TestVersionedEntity>> MergeAsync(
            TestVersionedEntity current,
            TestVersionedEntity proposed,
            TestVersionedEntity database,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var merged = new TestVersionedEntity
            {
                Id = database.Id,
                Name = "Merged",
                Version = database.Version + 1
            };
            return Task.FromResult(Either<EncinaError, TestVersionedEntity>.Right(merged));
        }
    }

    #endregion
}

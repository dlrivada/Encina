using Encina.DomainModeling;
using LanguageExt;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Tests for PagedResult, RepositoryError, RepositoryExtensions, and EntityNotFoundException.
/// </summary>
// Test entity needs to be public for NSubstitute proxy generation
public sealed class TestRepoEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class TestRepoEntitySpec : Specification<TestRepoEntity>
{
    // Store an expression so it can be translated by query providers (e.g. EF Core).
    private readonly System.Linq.Expressions.Expression<Func<TestRepoEntity, bool>> _expression;

    public TestRepoEntitySpec(System.Linq.Expressions.Expression<Func<TestRepoEntity, bool>> expression)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public override System.Linq.Expressions.Expression<Func<TestRepoEntity, bool>> ToExpression()
    {
        return _expression;
    }
}

public sealed class RepositoryTests
{
    #region Test Entities

    // TestRepoEntity is declared at namespace level for NSubstitute compatibility

    #endregion

    #region PagedResult Tests

    [Fact]
    public void PagedResult_TotalPages_CalculatesCorrectly()
    {
        // Arrange & Act
        var result = new PagedResult<int>([1, 2, 3], PageNumber: 1, PageSize: 10, TotalCount: 25);

        // Assert
        result.TotalPages.ShouldBe(3); // ceil(25/10) = 3
    }

    [Fact]
    public void PagedResult_TotalPages_ZeroPageSize_ReturnsZero()
    {
        // Arrange & Act
        var result = new PagedResult<int>([1, 2], PageNumber: 1, PageSize: 0, TotalCount: 10);

        // Assert
        result.TotalPages.ShouldBe(0);
    }

    [Fact]
    public void PagedResult_HasPreviousPage_FirstPage_ReturnsFalse()
    {
        // Arrange & Act
        var result = new PagedResult<int>([1, 2], PageNumber: 1, PageSize: 10, TotalCount: 25);

        // Assert
        result.HasPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public void PagedResult_HasPreviousPage_SecondPage_ReturnsTrue()
    {
        // Arrange & Act
        var result = new PagedResult<int>([1, 2], PageNumber: 2, PageSize: 10, TotalCount: 25);

        // Assert
        result.HasPreviousPage.ShouldBeTrue();
    }

    [Fact]
    public void PagedResult_HasNextPage_LastPage_ReturnsFalse()
    {
        // Arrange & Act
        var result = new PagedResult<int>([1, 2], PageNumber: 3, PageSize: 10, TotalCount: 25);

        // Assert
        result.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public void PagedResult_HasNextPage_FirstPage_ReturnsTrue()
    {
        // Arrange & Act
        var result = new PagedResult<int>([1, 2], PageNumber: 1, PageSize: 10, TotalCount: 25);

        // Assert
        result.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public void PagedResult_IsEmpty_EmptyItems_ReturnsTrue()
    {
        // Arrange & Act
        var result = new PagedResult<int>([], PageNumber: 1, PageSize: 10, TotalCount: 0);

        // Assert
        result.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void PagedResult_IsEmpty_WithItems_ReturnsFalse()
    {
        // Arrange & Act
        var result = new PagedResult<int>([1, 2], PageNumber: 1, PageSize: 10, TotalCount: 2);

        // Assert
        result.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void PagedResult_Empty_ReturnsEmptyResult()
    {
        // Act
        var result = PagedResult<string>.Empty();

        // Assert
        result.Items.Count.ShouldBe(0);
        result.PageNumber.ShouldBe(1);
        result.PageSize.ShouldBe(10);
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public void PagedResult_Empty_WithCustomParameters_ReturnsConfiguredResult()
    {
        // Act
        var result = PagedResult<string>.Empty(pageNumber: 5, pageSize: 20);

        // Assert
        result.PageNumber.ShouldBe(5);
        result.PageSize.ShouldBe(20);
    }

    [Fact]
    public void PagedResult_Map_TransformsItems()
    {
        // Arrange
        var result = new PagedResult<int>([1, 2, 3], PageNumber: 1, PageSize: 10, TotalCount: 3);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.Items.ShouldBe([2, 4, 6]);
        mapped.PageNumber.ShouldBe(1);
        mapped.PageSize.ShouldBe(10);
        mapped.TotalCount.ShouldBe(3);
    }

    [Fact]
    public void PagedResult_Map_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var result = new PagedResult<int>([1, 2], PageNumber: 1, PageSize: 10, TotalCount: 2);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => result.Map<string>(null!));
    }

    #endregion

    #region RepositoryError Tests

    [Fact]
    public void RepositoryError_NotFound_CreatesCorrectError()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = RepositoryError.NotFound<TestRepoEntity, Guid>(id);

        // Assert
        error.ErrorCode.ShouldBe("REPOSITORY_NOT_FOUND");
        error.EntityType.ShouldBe(typeof(TestRepoEntity));
        error.EntityId.ShouldBe(id);
        error.Message.ShouldContain("TestRepoEntity");
        error.Message.ShouldContain(id.ToString());
    }

    [Fact]
    public void RepositoryError_AlreadyExists_CreatesCorrectError()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = RepositoryError.AlreadyExists<TestRepoEntity, Guid>(id);

        // Assert
        error.ErrorCode.ShouldBe("REPOSITORY_ALREADY_EXISTS");
        error.EntityType.ShouldBe(typeof(TestRepoEntity));
        error.EntityId.ShouldBe(id);
    }

    [Fact]
    public void RepositoryError_ConcurrencyConflict_CreatesCorrectError()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = RepositoryError.ConcurrencyConflict<TestRepoEntity, Guid>(id);

        // Assert
        error.ErrorCode.ShouldBe("REPOSITORY_CONCURRENCY_CONFLICT");
        error.EntityType.ShouldBe(typeof(TestRepoEntity));
    }

    [Fact]
    public void RepositoryError_OperationFailed_CreatesCorrectError()
    {
        // Arrange
        var exception = new InvalidOperationException("Database error");

        // Act
        var error = RepositoryError.OperationFailed<TestRepoEntity>("Insert", exception);

        // Assert
        error.ErrorCode.ShouldBe("REPOSITORY_OPERATION_FAILED");
        error.EntityType.ShouldBe(typeof(TestRepoEntity));
        error.InnerException.ShouldBe(exception);
        error.Message.ShouldContain("Insert");
        error.Message.ShouldContain("Database error");
    }

    [Fact]
    public void RepositoryError_InvalidPagination_CreatesCorrectError()
    {
        // Act
        var error = RepositoryError.InvalidPagination<TestRepoEntity>(-1, 0);

        // Assert
        error.ErrorCode.ShouldBe("REPOSITORY_INVALID_PAGINATION");
        error.EntityType.ShouldBe(typeof(TestRepoEntity));
        error.Message.ShouldContain("-1");
        error.Message.ShouldContain("0");
    }

    #endregion

    #region EntityNotFoundException Tests

    [Fact]
    public void EntityNotFoundException_Constructor_SetsProperties()
    {
        // Arrange
        var entityType = typeof(TestRepoEntity);
        var entityId = "test-id-123";

        // Act
        var exception = new EntityNotFoundException(entityType, entityId);

        // Assert
        exception.EntityType.ShouldBe(entityType);
        exception.EntityId.ShouldBe(entityId);
        exception.Message.ShouldContain("TestRepoEntity");
        exception.Message.ShouldContain("test-id-123");
    }

    [Fact]
    public void EntityNotFoundException_ConstructorWithInnerException_SetsProperties()
    {
        // Arrange
        var entityType = typeof(TestRepoEntity);
        var entityId = "test-id-456";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new EntityNotFoundException(entityType, entityId, innerException);

        // Assert
        exception.EntityType.ShouldBe(entityType);
        exception.EntityId.ShouldBe(entityId);
        exception.InnerException.ShouldBe(innerException);
    }

    [Fact]
    public void EntityNotFoundException_NullEntityId_HandlesGracefully()
    {
        // Act
        var exception = new EntityNotFoundException(typeof(TestRepoEntity), null);

        // Assert
        exception.EntityId.ShouldBeNull();
        exception.Message.ShouldContain("TestRepoEntity");
    }

    #endregion

    #region RepositoryExtensions Tests

    [Fact]
    public async Task GetByIdOrErrorAsync_EntityFound_ReturnsRight()
    {
        // Arrange
        var repository = Substitute.For<IReadOnlyRepository<TestRepoEntity, Guid>>();
        var entity = new TestRepoEntity { Id = Guid.NewGuid(), Name = "Test" };
        repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(Some(entity));

        // Act
        var result = await repository.GetByIdOrErrorAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: e => e.ShouldBe(entity),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetByIdOrErrorAsync_EntityNotFound_ReturnsLeft()
    {
        // Arrange
        var repository = Substitute.For<IReadOnlyRepository<TestRepoEntity, Guid>>();
        var id = Guid.NewGuid();
        repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Option<TestRepoEntity>.None);

        // Act
        var result = await repository.GetByIdOrErrorAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.ErrorCode.ShouldBe("REPOSITORY_NOT_FOUND"));
    }

    [Fact]
    public async Task GetByIdOrErrorAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyRepository<TestRepoEntity, Guid>? repository = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository!.GetByIdOrErrorAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdOrThrowAsync_EntityFound_ReturnsEntity()
    {
        // Arrange
        var repository = Substitute.For<IReadOnlyRepository<TestRepoEntity, Guid>>();
        var entity = new TestRepoEntity { Id = Guid.NewGuid(), Name = "Test" };
        repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(Some(entity));

        // Act
        var result = await repository.GetByIdOrThrowAsync(entity.Id);

        // Assert
        result.ShouldBe(entity);
    }

    [Fact]
    public async Task GetByIdOrThrowAsync_EntityNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var repository = Substitute.For<IReadOnlyRepository<TestRepoEntity, Guid>>();
        var id = Guid.NewGuid();
        repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Option<TestRepoEntity>.None);

        // Act & Assert
        var exception = await Should.ThrowAsync<EntityNotFoundException>(async () =>
            await repository.GetByIdOrThrowAsync(id));

        exception.EntityType.ShouldBe(typeof(TestRepoEntity));
    }

    [Fact]
    public async Task GetByIdOrThrowAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyRepository<TestRepoEntity, Guid>? repository = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository!.GetByIdOrThrowAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AddIfNotExistsAsync_EntityDoesNotExist_AddsAndReturnsRight()
    {
        // Arrange
        var repository = Substitute.For<IRepository<TestRepoEntity, Guid>>();
        var entity = new TestRepoEntity { Id = Guid.NewGuid(), Name = "New" };
        repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(Option<TestRepoEntity>.None);

        // Act
        var result = await repository.AddIfNotExistsAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        await repository.Received(1).AddAsync(entity, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddIfNotExistsAsync_EntityExists_ReturnsLeft()
    {
        // Arrange
        var repository = Substitute.For<IRepository<TestRepoEntity, Guid>>();
        var entity = new TestRepoEntity { Id = Guid.NewGuid(), Name = "Existing" };
        repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(Some(entity));

        // Act
        var result = await repository.AddIfNotExistsAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.ErrorCode.ShouldBe("REPOSITORY_ALREADY_EXISTS"));
        await repository.DidNotReceive().AddAsync(Arg.Any<TestRepoEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddIfNotExistsAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IRepository<TestRepoEntity, Guid>? repository = null;
        var entity = new TestRepoEntity { Id = Guid.NewGuid() };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository!.AddIfNotExistsAsync(entity));
    }

    [Fact]
    public async Task AddIfNotExistsAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = Substitute.For<IRepository<TestRepoEntity, Guid>>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AddIfNotExistsAsync(null!));
    }

    [Fact]
    public async Task UpdateIfExistsAsync_EntityExists_UpdatesAndReturnsRight()
    {
        // Arrange
        var repository = Substitute.For<IRepository<TestRepoEntity, Guid>>();
        var entity = new TestRepoEntity { Id = Guid.NewGuid(), Name = "Original" };
        repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(Some(entity));

        // Act
        var result = await repository.UpdateIfExistsAsync(entity.Id, e => e.Name = "Updated");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: e => e.Name.ShouldBe("Updated"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
        repository.Received(1).Update(entity);
    }

    [Fact]
    public async Task UpdateIfExistsAsync_EntityNotFound_ReturnsLeft()
    {
        // Arrange
        var repository = Substitute.For<IRepository<TestRepoEntity, Guid>>();
        var id = Guid.NewGuid();
        repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Option<TestRepoEntity>.None);

        // Act
        var result = await repository.UpdateIfExistsAsync(id, _ => { });

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.ErrorCode.ShouldBe("REPOSITORY_NOT_FOUND"));
    }

    [Fact]
    public async Task UpdateIfExistsAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IRepository<TestRepoEntity, Guid>? repository = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository!.UpdateIfExistsAsync(Guid.NewGuid(), _ => { }));
    }

    [Fact]
    public async Task UpdateIfExistsAsync_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = Substitute.For<IRepository<TestRepoEntity, Guid>>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.UpdateIfExistsAsync(Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task FindOrEmptyAsync_Success_ReturnsEntities()
    {
        // Arrange
        var repository = Substitute.For<IReadOnlyRepository<TestRepoEntity, Guid>>();
        var entities = new List<TestRepoEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Entity1" },
            new() { Id = Guid.NewGuid(), Name = "Entity2" }
        };
        var spec = new TestRepoEntitySpec(_ => true);
        repository.FindAsync(spec, Arg.Any<CancellationToken>())
            .Returns(entities);

        // Act
        var result = await repository.FindOrEmptyAsync(spec);

        // Assert
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task FindOrEmptyAsync_Exception_ReturnsEmptyList()
    {
        // Arrange
        var repository = Substitute.For<IReadOnlyRepository<TestRepoEntity, Guid>>();
        var spec = new TestRepoEntitySpec(_ => true);
        repository.FindAsync(Arg.Any<Specification<TestRepoEntity>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<TestRepoEntity>>(_ => throw new InvalidOperationException("DB error"));

        // Act
        var result = await repository.FindOrEmptyAsync(spec);

        // Assert
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task FindOrEmptyAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyRepository<TestRepoEntity, Guid>? repository = null;
        var spec = new TestRepoEntitySpec(_ => true);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository!.FindOrEmptyAsync(spec));
    }

    [Fact]
    public async Task FindOrEmptyAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = Substitute.For<IReadOnlyRepository<TestRepoEntity, Guid>>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.FindOrEmptyAsync(null!));
    }

    #endregion
}

using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Repository;

/// <summary>
/// Integration tests for <see cref="FunctionalRepositoryEF{TEntity, TId}"/> using real SQL Server.
/// </summary>
[Collection("RepositoryTests")]
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class FunctionalRepositoryEFIntegrationTests : IAsyncLifetime
{
    private readonly RepositoryFixture _fixture;
    private RepositoryTestDbContext _dbContext = null!;
    private FunctionalRepositoryEF<TestEntity, Guid> _repository = null!;

    public FunctionalRepositoryEFIntegrationTests(RepositoryFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearDataAsync();
        _dbContext = _fixture.CreateDbContext();
        _repository = new FunctionalRepositoryEF<TestEntity, Guid>(_dbContext);
    }

    public ValueTask DisposeAsync()
    {
        _dbContext.Dispose();
        return ValueTask.CompletedTask;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsRight()
    {
        // Arrange
        var entity = CreateTestEntity();
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e =>
        {
            e.Id.ShouldBe(entity.Id);
            e.Name.ShouldBe(entity.Name);
        });
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("not found");
            error.Message.ShouldContain(nonExistingId.ToString());
        });
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_EmptyTable_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.ShouldBeEmpty());
    }

    [Fact]
    public async Task ListAsync_WithEntities_ReturnsAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        };
        _dbContext.TestEntities.AddRange(entities);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(3));
    }

    [Fact]
    public async Task ListAsync_WithSpecification_ReturnsFilteredEntities()
    {
        // Arrange
        var activeEntity = CreateTestEntity("Active", isActive: true);
        var inactiveEntity = CreateTestEntity("Inactive", isActive: false);
        _dbContext.TestEntities.AddRange(activeEntity, inactiveEntity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("Active");
        });
    }

    [Fact]
    public async Task ListAsync_WithCombinedSpecification_ReturnsFilteredEntities()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("High Active", isActive: true, amount: 200),
            CreateTestEntity("Low Active", isActive: true, amount: 50),
            CreateTestEntity("High Inactive", isActive: false, amount: 300)
        );
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var spec = new ActiveEntitySpec().And(new MinAmountSpec(100));

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("High Active");
        });
    }

    #endregion

    #region FirstOrDefaultAsync Tests

    [Fact]
    public async Task FirstOrDefaultAsync_MatchingEntity_ReturnsRight()
    {
        // Arrange
        var entity = CreateTestEntity("Test", isActive: true);
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Name.ShouldBe("Test"));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NoMatch_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var entity = CreateTestEntity("Inactive", isActive: false);
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_EmptyTable_ReturnsZero()
    {
        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task CountAsync_WithEntities_ReturnsCorrectCount()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        );
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(3));
    }

    [Fact]
    public async Task CountAsync_WithSpecification_ReturnsFilteredCount()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("Active 1", isActive: true),
            CreateTestEntity("Active 2", isActive: true),
            CreateTestEntity("Inactive", isActive: false)
        );
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.CountAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));
    }

    #endregion

    #region AnyAsync Tests

    [Fact]
    public async Task AnyAsync_EmptyTable_ReturnsFalse()
    {
        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    [Fact]
    public async Task AnyAsync_WithEntities_ReturnsTrue()
    {
        // Arrange
        _dbContext.TestEntities.Add(CreateTestEntity());
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_NoMatch_ReturnsFalse()
    {
        // Arrange
        _dbContext.TestEntities.Add(CreateTestEntity("Inactive", isActive: false));
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_HasMatch_ReturnsTrue()
    {
        // Arrange
        _dbContext.TestEntities.Add(CreateTestEntity("Active", isActive: true));
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidEntity_ReturnsRightAndPersists()
    {
        // Arrange
        var entity = CreateTestEntity("New Entity");

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Name.ShouldBe("New Entity"));

        // Verify persisted
        _dbContext.ChangeTracker.Clear();
        var stored = await _dbContext.TestEntities.FindAsync(entity.Id);
        stored.ShouldNotBeNull();
        stored!.Name.ShouldBe("New Entity");
    }

    [Fact]
    public async Task AddAsync_DuplicateId_ReturnsLeftWithAlreadyExistsError()
    {
        // Arrange
        var existingEntity = CreateTestEntity("Existing");
        _dbContext.TestEntities.Add(existingEntity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var duplicateEntity = new TestEntity
        {
            Id = existingEntity.Id, // Same ID
            Name = "Duplicate",
            Amount = 100m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        var result = await _repository.AddAsync(duplicateEntity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("already exists"));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_ReturnsRightAndUpdates()
    {
        // Arrange
        var entity = CreateTestEntity("Original");
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Modify entity
        var updatedEntity = new TestEntity
        {
            Id = entity.Id,
            Name = "Updated",
            Amount = entity.Amount,
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc
        };

        // Act
        var result = await _repository.UpdateAsync(updatedEntity);

        // Assert
        result.IsRight.ShouldBeTrue();

        _dbContext.ChangeTracker.Clear();
        var stored = await _dbContext.TestEntities.FindAsync(entity.Id);
        stored.ShouldNotBeNull();
        stored!.Name.ShouldBe("Updated");
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsLeft()
    {
        // Arrange
        var entity = CreateTestEntity("Non-existing");

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        // EF Core may return "not found" or "Concurrency conflict" for non-existing entities
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ById_ExistingEntity_ReturnsRightAndRemoves()
    {
        // Arrange
        var entity = CreateTestEntity();
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.DeleteAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _dbContext.TestEntities.FindAsync(entity.Id);
        stored.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ById_NonExistingEntity_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task DeleteAsync_ByEntity_ExistingEntity_ReturnsRightAndRemoves()
    {
        // Arrange
        var entity = CreateTestEntity();
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        _dbContext.ChangeTracker.Clear();
        var stored = await _dbContext.TestEntities.FindAsync(entity.Id);
        stored.ShouldBeNull();
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_ValidEntities_ReturnsRightAndPersists()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        };

        // Act
        var result = await _repository.AddRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(3));

        _dbContext.ChangeTracker.Clear();
        var storedCount = await _dbContext.TestEntities.CountAsync();
        storedCount.ShouldBe(3);
    }

    #endregion

    #region UpdateRangeAsync Tests

    [Fact]
    public async Task UpdateRangeAsync_ExistingEntities_ReturnsRightAndUpdates()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2")
        };
        _dbContext.TestEntities.AddRange(entities);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Modify entities
        foreach (var entity in entities)
        {
            entity.Name = entity.Name + " Updated";
        }

        // Act
        var result = await _repository.UpdateRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();

        _dbContext.ChangeTracker.Clear();
        var stored = await _dbContext.TestEntities.ToListAsync();
        stored.ShouldAllBe(e => e.Name.EndsWith(" Updated"));
    }

    #endregion

    #region DeleteRangeAsync Tests

    [Fact]
    public async Task DeleteRangeAsync_WithSpecification_DeletesMatchingEntities()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("Active 1", isActive: true),
            CreateTestEntity("Active 2", isActive: true),
            CreateTestEntity("Inactive", isActive: false)
        );
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.DeleteRangeAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));

        var remaining = await _dbContext.TestEntities.CountAsync();
        remaining.ShouldBe(1);
    }

    #endregion

    #region String Method Wildcard Tests

    // Each value would also match other names if its LIKE wildcards ('%', '_', SQL Server's '[')
    // or the '!' escape character were not matched literally (#3156).
    private static readonly string[] Names = ["500", "50% off", "100%", "a_b", "axb", "[a]", "hey!"];

    [Theory]
    [InlineData(nameof(string.Contains), "50%", "50% off")]
    [InlineData(nameof(string.StartsWith), "a_", "a_b")]
    [InlineData(nameof(string.EndsWith), "0%", "100%")]
    [InlineData(nameof(string.Contains), "[a]", "[a]")]
    [InlineData(nameof(string.Contains), "!", "hey!")]
    public async Task ListAsync_StringMethodSpecification_MatchesLikeWildcardsLiterally(string method, string value, string expectedName)
    {
        // Arrange
        _dbContext.TestEntities.AddRange(Names.Select(name => CreateTestEntity(name)));
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        var spec = new EntityNameSpec(method, value);

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Select(e => e.Name).ToList().ShouldBe([expectedName]));
    }

    #endregion

    #region Keyset Direction Tests

    private static readonly decimal[] KeysetValues = [100m, 200m, 300m, 400m];

    [Fact]
    public async Task ListAsync_KeysetSortedDescending_ReturnsTheKeysBelowTheLastOne()
    {
        // Arrange - the next page of a descending keyset holds the smaller keys (#3154)
        _dbContext.TestEntities.AddRange(KeysetValues.Select(a => CreateTestEntity($"Entity {a}", amount: a)));
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.ListAsync(new DescendingAmountKeysetSpec(lastAmount: 300m));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Select(e => e.Amount).ShouldBe([200m, 100m]));
    }

    [Fact]
    public async Task ListAsync_QuerySpecificationOrderedAndPaged_ReturnsThatPageInOrder()
    {
        // Arrange - the reference behaviour every provider must match (#3151)
        _dbContext.TestEntities.AddRange(KeysetValues.Select(a => CreateTestEntity($"Entity {a}", amount: a)));
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.ListAsync(new AmountPageSpec(skip: 1, take: 2));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Select(e => e.Amount).ShouldBe([300m, 200m]));
    }

    #endregion

    #region Helper Methods

    private static TestEntity CreateTestEntity(
        string name = "Test Entity",
        bool isActive = true,
        decimal amount = 100m)
    {
        return new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Amount = amount,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    #endregion
}

#region Specifications

/// <summary>
/// Specification for active entities.
/// </summary>
public class ActiveEntitySpec : Specification<TestEntity>
{
    public override Expression<Func<TestEntity, bool>> ToExpression()
        => e => e.IsActive;
}

/// <summary>
/// Specification for entities with minimum amount.
/// </summary>
public class MinAmountSpec : Specification<TestEntity>
{
    private readonly decimal _minAmount;

    public MinAmountSpec(decimal minAmount)
    {
        _minAmount = minAmount;
    }

    public override Expression<Func<TestEntity, bool>> ToExpression()
        => e => e.Amount >= _minAmount;
}

/// <summary>
/// Specification matching a TestEntity whose name contains, starts with or ends with a value.
/// </summary>
public class EntityNameSpec(string method, string value) : Specification<TestEntity>
{
    public override Expression<Func<TestEntity, bool>> ToExpression() => method switch
    {
        nameof(string.Contains) => e => e.Name.Contains(value),
        nameof(string.StartsWith) => e => e.Name.StartsWith(value),
        nameof(string.EndsWith) => e => e.Name.EndsWith(value),
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Expected Contains, StartsWith or EndsWith."),
    };
}

/// <summary>
/// Next page of entities by amount, sorted in descending order, after <c>lastAmount</c>.
/// </summary>
public class DescendingAmountKeysetSpec : QuerySpecification<TestEntity>
{
    public DescendingAmountKeysetSpec(decimal lastAmount)
    {
        ApplyOrderByDescending(e => e.Amount);
        ApplyKeysetPagination(e => e.Amount, lastAmount, 10);
    }
}

/// <summary>
/// One page of entities by amount, sorted in descending order.
/// </summary>
public class AmountPageSpec : QuerySpecification<TestEntity>
{
    public AmountPageSpec(int skip, int take)
    {
        ApplyOrderByDescending(e => e.Amount);
        ApplyPaging(skip, take);
    }
}

#endregion

using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Repository;

/// <summary>
/// Guard clause tests for <see cref="SpecificationEvaluator"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class SpecificationEvaluatorGuardTests
{
    [Fact]
    public void GetQuery_NullInputQuery_ThrowsArgumentNullException()
    {
        // Arrange
        IQueryable<SpecEvaluatorTestEntity> inputQuery = null!;
        var specification = new TestActiveEntitiesSpec();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            SpecificationEvaluator.GetQuery(inputQuery, specification));
        ex.ParamName.ShouldBe("inputQuery");
    }

    [Fact]
    public void GetQuery_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SpecTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new SpecTestDbContext(options);
        var inputQuery = dbContext.TestEntities.AsQueryable();
        Specification<SpecEvaluatorTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            SpecificationEvaluator.GetQuery(inputQuery, specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public void GetQuery_WithProjection_NullInputQuery_ThrowsArgumentNullException()
    {
        // Arrange
        IQueryable<SpecEvaluatorTestEntity> inputQuery = null!;
        var specification = new TestEntitySummarySpec();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            SpecificationEvaluator.GetQuery(inputQuery, specification));
        ex.ParamName.ShouldBe("inputQuery");
    }

    [Fact]
    public void GetQuery_WithProjection_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SpecTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new SpecTestDbContext(options);
        var inputQuery = dbContext.TestEntities.AsQueryable();
        QuerySpecification<SpecEvaluatorTestEntity, SpecEvaluatorTestEntitySummary> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            SpecificationEvaluator.GetQuery(inputQuery, specification));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public void GetQuery_WithProjection_NoSelector_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SpecTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new SpecTestDbContext(options);
        var inputQuery = dbContext.TestEntities.AsQueryable();
        var specification = new TestEntitySummaryNoSelectorSpec();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            SpecificationEvaluator.GetQuery(inputQuery, specification));
        ex.Message.ShouldContain("Selector");
    }

    private sealed class SpecTestDbContext : DbContext
    {
        public SpecTestDbContext(DbContextOptions<SpecTestDbContext> options) : base(options)
        {
        }

        public DbSet<SpecEvaluatorTestEntity> TestEntities => Set<SpecEvaluatorTestEntity>();
    }
}

/// <summary>
/// Test entity for specification evaluator guard tests.
/// </summary>
public sealed class SpecEvaluatorTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Test DTO for specification evaluator guard tests.
/// </summary>
public sealed class SpecEvaluatorTestEntitySummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Simple specification for testing guard clauses.
/// </summary>
public sealed class TestActiveEntitiesSpec : Specification<SpecEvaluatorTestEntity>
{
    public override Expression<Func<SpecEvaluatorTestEntity, bool>> ToExpression()
        => entity => entity.IsActive;
}

/// <summary>
/// Query specification with projection for testing guard clauses.
/// </summary>
public sealed class TestEntitySummarySpec : QuerySpecification<SpecEvaluatorTestEntity, SpecEvaluatorTestEntitySummary>
{
    public TestEntitySummarySpec()
    {
        Selector = entity => new SpecEvaluatorTestEntitySummary
        {
            Id = entity.Id,
            Name = entity.Name
        };
    }

    public override Expression<Func<SpecEvaluatorTestEntity, bool>> ToExpression()
        => entity => entity.IsActive;
}

/// <summary>
/// Query specification without selector for testing guard clauses.
/// </summary>
public sealed class TestEntitySummaryNoSelectorSpec : QuerySpecification<SpecEvaluatorTestEntity, SpecEvaluatorTestEntitySummary>
{
    public override Expression<Func<SpecEvaluatorTestEntity, bool>> ToExpression()
        => entity => entity.IsActive;
}

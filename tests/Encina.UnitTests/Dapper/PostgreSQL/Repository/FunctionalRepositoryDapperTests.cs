using System.Data;
using System.Linq.Expressions;
using Encina.Dapper.PostgreSQL.Repository;
using Encina.DomainModeling;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.PostgreSQL.Repository;

/// <summary>
/// Unit tests for <see cref="FunctionalRepositoryDapper{TEntity, TId}"/>.
/// Tests constructor validation and mapping configuration.
/// Note: Dapper methods use static extensions which cannot be mocked.
/// Full behavior testing is done in integration tests.
/// </summary>
[Trait("Category", "Unit")]
public class FunctionalRepositoryDapperTests
{
    private readonly IEntityMapping<TestEntityDapperPg, Guid> _mapping;

    public FunctionalRepositoryDapperTests()
    {
        _mapping = new EntityMappingBuilder<TestEntityDapperPg, Guid>()
            .ToTable("test_entities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "name")
            .MapProperty(e => e.Amount, "amount")
            .MapProperty(e => e.IsActive, "is_active")
            .MapProperty(e => e.CreatedAtUtc, "created_at_utc")
            .Build();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(null!, _mapping));
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(connection, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var repository = new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(connection, _mapping);

        // Assert
        repository.ShouldNotBeNull();
    }

    #endregion

    #region Null Argument Tests (Guard Clauses)

    [Fact]
    public async Task AddAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AddAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.DeleteAsync((TestEntityDapperPg)null!));
    }

    [Fact]
    public async Task ListAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.ListAsync(null!));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.FirstOrDefaultAsync(null!));
    }

    [Fact]
    public async Task CountAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.CountAsync(null!));
    }

    [Fact]
    public async Task AnyAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AnyAsync(null!));
    }

    [Fact]
    public async Task AddRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AddRangeAsync(null!));
    }

    [Fact]
    public async Task UpdateRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.UpdateRangeAsync(null!));
    }

    [Fact]
    public async Task DeleteRangeAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperPg, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.DeleteRangeAsync(null!));
    }

    #endregion

    #region EntityMappingBuilder Tests

    [Fact]
    public void EntityMappingBuilder_Build_WithoutTableName_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapperPg, Guid>()
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "name");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void EntityMappingBuilder_Build_WithoutId_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapperPg, Guid>()
            .ToTable("test_entities")
            .MapProperty(e => e.Name, "name");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void EntityMappingBuilder_Build_WithoutColumnMappings_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapperPg, Guid>()
            .ToTable("test_entities");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void EntityMappingBuilder_Build_ValidConfiguration_ReturnsMapping()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapperPg, Guid>()
            .ToTable("test_entities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "name")
            .MapProperty(e => e.Amount, "amount");

        // Act
        var mapping = builder.Build();

        // Assert
        mapping.ShouldNotBeNull();
        mapping.TableName.ShouldBe("test_entities");
        mapping.IdColumnName.ShouldBe("Id");
        mapping.ColumnMappings.ShouldContainKey("Id");
        mapping.ColumnMappings.ShouldContainKey("Name");
        mapping.ColumnMappings.ShouldContainKey("Amount");
    }

    [Fact]
    public void EntityMappingBuilder_ExcludeFromInsert_AddsToExcludedProperties()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestEntityDapperPg, Guid>()
            .ToTable("test_entities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "name")
            .MapProperty(e => e.CreatedAtUtc, "created_at_utc")
            .ExcludeFromInsert(e => e.CreatedAtUtc)
            .Build();

        // Assert
        mapping.InsertExcludedProperties.ShouldContain("CreatedAtUtc");
    }

    [Fact]
    public void EntityMappingBuilder_ExcludeFromUpdate_AddsToExcludedProperties()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestEntityDapperPg, Guid>()
            .ToTable("test_entities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "name")
            .MapProperty(e => e.CreatedAtUtc, "created_at_utc")
            .ExcludeFromUpdate(e => e.CreatedAtUtc)
            .Build();

        // Assert
        mapping.UpdateExcludedProperties.ShouldContain("CreatedAtUtc");
        // Id is automatically excluded from update
        mapping.UpdateExcludedProperties.ShouldContain("Id");
    }

    [Fact]
    public void EntityMappingBuilder_GetId_ReturnsEntityId()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestEntityDapperPg, Guid>()
            .ToTable("test_entities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "name")
            .Build();

        var entity = new TestEntityDapperPg { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        var id = mapping.GetId(entity);

        // Assert
        id.ShouldBe(entity.Id);
    }

    #endregion

    #region SpecificationSqlBuilder Tests

    [Fact]
    public void SpecificationSqlBuilder_BuildWhereClause_SimpleEquality_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new ActiveEntityDapperPgSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("WHERE");
        // Boolean member access uses the mapped column name
        whereClause.ShouldContain("\"is_active\"");
    }

    [Fact]
    public void SpecificationSqlBuilder_BuildSelectStatement_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new ActiveEntityDapperPgSpec();

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("test_entities", spec);

        // Assert
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM \"test_entities\"");
        sql.ShouldContain("WHERE");
    }

    [Fact]
    public void SpecificationSqlBuilder_UnsupportedExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new UnsupportedDapperPgSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void SpecificationSqlBuilder_GreaterThanComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new MinAmountDapperPgSpec(100);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("WHERE");
        whereClause.ShouldContain("\"amount\"");
        whereClause.ShouldContain(">=");
    }

    [Fact]
    public void SpecificationSqlBuilder_BuildSelectStatement_WithoutSpecification_GeneratesSqlWithoutWhere()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("test_entities");

        // Assert
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM \"test_entities\"");
        sql.ShouldNotContain("WHERE");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    public void SpecificationSqlBuilder_OrCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new ActiveEntityDapperPgSpec().Or(new MinAmountDapperPgSpec(100));

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("OR");
    }

    [Fact]
    public void SpecificationSqlBuilder_NotSpecification_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new ActiveEntityDapperPgSpec().Not();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("NOT");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringContains_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new NameContainsDapperPgSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("LIKE");
        parameters.Values.First().ShouldBe("%test%");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringStartsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new NameStartsWithDapperPgSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("LIKE");
        parameters.Values.First().ShouldBe("test%");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringEndsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new NameEndsWithDapperPgSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("LIKE");
        parameters.Values.First().ShouldBe("%test");
    }

    [Fact]
    public void SpecificationSqlBuilder_NullEquality_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new NameIsNullDapperPgSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("IS NULL");
    }

    [Fact]
    public void SpecificationSqlBuilder_NullInequality_GeneratesIsNotNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new NameIsNotNullDapperPgSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("IS NOT NULL");
    }

    [Fact]
    public void SpecificationSqlBuilder_LessThanComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new AmountLessThanDapperPgSpec(100);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"amount\"");
        whereClause.ShouldContain("<");
    }

    [Fact]
    public void SpecificationSqlBuilder_NotEqual_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new NameNotEqualDapperPgSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("<>");
    }

    [Fact]
    public void SpecificationSqlBuilder_BooleanConstantTrue_GeneratesTrue()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new TrueConstantDapperPgSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        // PostgreSQL uses native boolean TRUE
        whereClause.ShouldContain("TRUE");
    }

    [Fact]
    public void SpecificationSqlBuilder_BooleanConstantFalse_GeneratesFalse()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new FalseConstantDapperPgSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        // PostgreSQL uses native boolean FALSE
        whereClause.ShouldContain("FALSE");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringEquals_GeneratesEqualityClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new NameStringEqualsDapperPgSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"name\"");
        whereClause.ShouldContain("=");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringEqualsNull_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);
        var spec = new NameStringEqualsNullDapperPgSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("IS NULL");
    }

    [Fact]
    public void SpecificationSqlBuilder_NullColumnMappings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SpecificationSqlBuilder<TestEntityDapperPg>(null!));
    }

    [Fact]
    public void SpecificationSqlBuilder_BuildWhereClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperPg>(_mapping.ColumnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause((Specification<TestEntityDapperPg>)null!));
    }

    #endregion

    #region Helper Methods

    private static IDbConnection CreateMockConnection()
    {
        var connection = Substitute.For<IDbConnection>();
        connection.State.Returns(ConnectionState.Open);
        return connection;
    }

    #endregion
}

#region Test Entity and Specifications

/// <summary>
/// Test entity for PostgreSQL Dapper repository unit tests.
/// </summary>
public class TestEntityDapperPg
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Specification for active entities (PostgreSQL Dapper tests).
/// </summary>
public class ActiveEntityDapperPgSpec : Specification<TestEntityDapperPg>
{
    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => e.IsActive;
}

/// <summary>
/// Specification for minimum amount (PostgreSQL Dapper tests).
/// </summary>
public class MinAmountDapperPgSpec : Specification<TestEntityDapperPg>
{
    private readonly decimal _minAmount;

    public MinAmountDapperPgSpec(decimal minAmount) => _minAmount = minAmount;

    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => e.Amount >= _minAmount;
}

/// <summary>
/// Specification that causes NotSupportedException (for testing unsupported expressions).
/// Uses an unsupported method call expression (List.Contains).
/// </summary>
public class UnsupportedDapperPgSpec : Specification<TestEntityDapperPg>
{
    private readonly List<Guid> _validIds = [Guid.NewGuid()];

    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => _validIds.Contains(e.Id);
}

/// <summary>
/// Specification for name contains string.
/// </summary>
public class NameContainsDapperPgSpec : Specification<TestEntityDapperPg>
{
    private readonly string _value;
    public NameContainsDapperPgSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => e.Name.Contains(_value);
}

/// <summary>
/// Specification for name starts with string.
/// </summary>
public class NameStartsWithDapperPgSpec : Specification<TestEntityDapperPg>
{
    private readonly string _value;
    public NameStartsWithDapperPgSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => e.Name.StartsWith(_value);
}

/// <summary>
/// Specification for name ends with string.
/// </summary>
public class NameEndsWithDapperPgSpec : Specification<TestEntityDapperPg>
{
    private readonly string _value;
    public NameEndsWithDapperPgSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => e.Name.EndsWith(_value);
}

/// <summary>
/// Specification for name is null.
/// </summary>
public class NameIsNullDapperPgSpec : Specification<TestEntityDapperPg>
{
    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => e.Name == null;
}

/// <summary>
/// Specification for name is not null.
/// </summary>
public class NameIsNotNullDapperPgSpec : Specification<TestEntityDapperPg>
{
    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => e.Name != null;
}

/// <summary>
/// Specification for amount less than.
/// </summary>
public class AmountLessThanDapperPgSpec : Specification<TestEntityDapperPg>
{
    private readonly decimal _value;
    public AmountLessThanDapperPgSpec(decimal value) => _value = value;
    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => e.Amount < _value;
}

/// <summary>
/// Specification for name not equal.
/// </summary>
public class NameNotEqualDapperPgSpec : Specification<TestEntityDapperPg>
{
    private readonly string _value;
    public NameNotEqualDapperPgSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => e.Name != _value;
}

/// <summary>
/// Specification that returns constant true.
/// </summary>
public class TrueConstantDapperPgSpec : Specification<TestEntityDapperPg>
{
    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => true;
}

/// <summary>
/// Specification that returns constant false.
/// </summary>
public class FalseConstantDapperPgSpec : Specification<TestEntityDapperPg>
{
    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => false;
}

/// <summary>
/// Specification using string.Equals method.
/// </summary>
public class NameStringEqualsDapperPgSpec : Specification<TestEntityDapperPg>
{
    private readonly string _value;
    public NameStringEqualsDapperPgSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => e.Name.Equals(_value, StringComparison.Ordinal);
}

/// <summary>
/// Specification using string.Equals method with null.
/// </summary>
public class NameStringEqualsNullDapperPgSpec : Specification<TestEntityDapperPg>
{
    public override Expression<Func<TestEntityDapperPg, bool>> ToExpression()
        => e => e.Name.Equals(null, StringComparison.Ordinal);
}

#endregion

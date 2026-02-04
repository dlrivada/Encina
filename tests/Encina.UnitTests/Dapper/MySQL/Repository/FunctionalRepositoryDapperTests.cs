using System.Data;
using System.Linq.Expressions;
using Encina.Dapper.MySQL.Repository;
using Encina.DomainModeling;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.MySQL.Repository;

/// <summary>
/// Unit tests for <see cref="FunctionalRepositoryDapper{TEntity, TId}"/> (MySQL implementation).
/// Tests constructor validation and mapping configuration.
/// Note: Dapper methods use static extensions which cannot be mocked.
/// Full behavior testing is done in integration tests.
/// </summary>
[Trait("Category", "Unit")]
public class FunctionalRepositoryDapperTests
{
    private readonly IEntityMapping<TestEntityDapperMySQL, Guid> _mapping;

    public FunctionalRepositoryDapperTests()
    {
        _mapping = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .MapProperty(e => e.Amount, "Amount")
            .MapProperty(e => e.IsActive, "IsActive")
            .MapProperty(e => e.CreatedAtUtc, "CreatedAtUtc")
            .Build();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(null!, _mapping));
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(connection, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var repository = new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(connection, _mapping);

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
        var repository = new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AddAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.DeleteAsync((TestEntityDapperMySQL)null!));
    }

    [Fact]
    public async Task ListAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.ListAsync(null!));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.FirstOrDefaultAsync(null!));
    }

    [Fact]
    public async Task CountAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.CountAsync(null!));
    }

    [Fact]
    public async Task AnyAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AnyAsync(null!));
    }

    [Fact]
    public async Task AddRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AddRangeAsync(null!));
    }

    [Fact]
    public async Task UpdateRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.UpdateRangeAsync(null!));
    }

    [Fact]
    public async Task DeleteRangeAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapperMySQL, Guid>(connection, _mapping);

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
        var builder = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>()
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void EntityMappingBuilder_Build_WithoutId_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>()
            .ToTable("TestEntities")
            .MapProperty(e => e.Name, "Name");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void EntityMappingBuilder_Build_WithoutColumnMappings_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>()
            .ToTable("TestEntities");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void EntityMappingBuilder_Build_ValidConfiguration_ReturnsMapping()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .MapProperty(e => e.Amount, "Amount");

        // Act
        var mapping = builder.Build();

        // Assert
        mapping.ShouldNotBeNull();
        mapping.TableName.ShouldBe("TestEntities");
        mapping.IdColumnName.ShouldBe("Id");
        mapping.ColumnMappings.ShouldContainKey("Id");
        mapping.ColumnMappings.ShouldContainKey("Name");
        mapping.ColumnMappings.ShouldContainKey("Amount");
    }

    [Fact]
    public void EntityMappingBuilder_ExcludeFromInsert_AddsToExcludedProperties()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .MapProperty(e => e.CreatedAtUtc, "CreatedAtUtc")
            .ExcludeFromInsert(e => e.CreatedAtUtc)
            .Build();

        // Assert
        mapping.InsertExcludedProperties.ShouldContain("CreatedAtUtc");
    }

    [Fact]
    public void EntityMappingBuilder_ExcludeFromUpdate_AddsToExcludedProperties()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .MapProperty(e => e.CreatedAtUtc, "CreatedAtUtc")
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
        var mapping = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .Build();

        var entity = new TestEntityDapperMySQL { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        var id = mapping.GetId(entity);

        // Assert
        id.ShouldBe(entity.Id);
    }

    [Fact]
    public void EntityMappingBuilder_HasId_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.HasId<Guid>(null!));
    }

    [Fact]
    public void EntityMappingBuilder_MapProperty_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.MapProperty<string>(null!));
    }

    [Fact]
    public void EntityMappingBuilder_ExcludeFromInsert_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.ExcludeFromInsert<string>(null!));
    }

    [Fact]
    public void EntityMappingBuilder_ExcludeFromUpdate_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.ExcludeFromUpdate<string>(null!));
    }

    [Fact]
    public void EntityMapping_GetId_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestEntityDapperMySQL, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .Build();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => mapping.GetId(null!));
    }

    #endregion

    #region SpecificationSqlBuilder Tests

    [Fact]
    public void SpecificationSqlBuilder_BuildWhereClause_SimpleEquality_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new ActiveEntityDapperMySQLSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("WHERE");
        whereClause.ShouldContain("`IsActive`");
    }

    [Fact]
    public void SpecificationSqlBuilder_BuildSelectStatement_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new ActiveEntityDapperMySQLSpec();

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("TestEntities", spec);

        // Assert - MySQL uses backticks
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM `TestEntities`");
        sql.ShouldContain("WHERE");
    }

    [Fact]
    public void SpecificationSqlBuilder_UnsupportedExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new UnsupportedDapperMySQLSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void SpecificationSqlBuilder_GreaterThanComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new MinAmountDapperMySQLSpec(100);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("WHERE");
        whereClause.ShouldContain("`Amount`");
        whereClause.ShouldContain(">=");
    }

    [Fact]
    public void SpecificationSqlBuilder_BuildSelectStatement_WithoutSpecification_GeneratesSqlWithoutWhere()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("TestEntities");

        // Assert - MySQL uses backticks
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM `TestEntities`");
        sql.ShouldNotContain("WHERE");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    public void SpecificationSqlBuilder_OrCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new ActiveEntityDapperMySQLSpec().Or(new MinAmountDapperMySQLSpec(100));

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("OR");
    }

    [Fact]
    public void SpecificationSqlBuilder_NotSpecification_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new ActiveEntityDapperMySQLSpec().Not();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("NOT");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringContains_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new NameContainsDapperMySQLSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("LIKE");
        parameters.Values.First().ShouldBe("%test%");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringStartsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new NameStartsWithDapperMySQLSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("LIKE");
        parameters.Values.First().ShouldBe("test%");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringEndsWith_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new NameEndsWithDapperMySQLSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("LIKE");
        parameters.Values.First().ShouldBe("%test");
    }

    [Fact]
    public void SpecificationSqlBuilder_NullEquality_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new NameIsNullDapperMySQLSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("IS NULL");
    }

    [Fact]
    public void SpecificationSqlBuilder_NullInequality_GeneratesIsNotNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new NameIsNotNullDapperMySQLSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("IS NOT NULL");
    }

    [Fact]
    public void SpecificationSqlBuilder_LessThanComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new AmountLessThanDapperMySQLSpec(100);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("`Amount`");
        whereClause.ShouldContain("<");
    }

    [Fact]
    public void SpecificationSqlBuilder_NotEqual_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new NameNotEqualDapperMySQLSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("<>");
    }

    [Fact]
    public void SpecificationSqlBuilder_BooleanConstantTrue_Generates1Equals1()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new TrueConstantDapperMySQLSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=1");
    }

    [Fact]
    public void SpecificationSqlBuilder_BooleanConstantFalse_Generates1Equals0()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new FalseConstantDapperMySQLSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=0");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringEquals_GeneratesEqualityClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new NameStringEqualsDapperMySQLSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - MySQL uses backticks
        whereClause.ShouldContain("`Name`");
        whereClause.ShouldContain("=");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringEqualsNull_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);
        var spec = new NameStringEqualsNullDapperMySQLSpec();

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
            new SpecificationSqlBuilder<TestEntityDapperMySQL>(null!));
    }

    [Fact]
    public void SpecificationSqlBuilder_BuildWhereClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapperMySQL>(_mapping.ColumnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause((Specification<TestEntityDapperMySQL>)null!));
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
/// Test entity for MySQL Dapper repository unit tests.
/// </summary>
public class TestEntityDapperMySQL
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Specification for active entities (MySQL Dapper tests).
/// </summary>
public class ActiveEntityDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => e.IsActive;
}

/// <summary>
/// Specification for minimum amount (MySQL Dapper tests).
/// </summary>
public class MinAmountDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    private readonly decimal _minAmount;

    public MinAmountDapperMySQLSpec(decimal minAmount) => _minAmount = minAmount;

    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => e.Amount >= _minAmount;
}

/// <summary>
/// Specification that causes NotSupportedException (for testing unsupported expressions).
/// Uses an unsupported method call expression (string.Replace).
/// </summary>
public class UnsupportedDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => e.Name.Replace("a", "b") == "test";
}

/// <summary>
/// Specification for name contains string.
/// </summary>
public class NameContainsDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    private readonly string _value;
    public NameContainsDapperMySQLSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => e.Name.Contains(_value);
}

/// <summary>
/// Specification for name starts with string.
/// </summary>
public class NameStartsWithDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    private readonly string _value;
    public NameStartsWithDapperMySQLSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => e.Name.StartsWith(_value);
}

/// <summary>
/// Specification for name ends with string.
/// </summary>
public class NameEndsWithDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    private readonly string _value;
    public NameEndsWithDapperMySQLSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => e.Name.EndsWith(_value);
}

/// <summary>
/// Specification for name is null.
/// </summary>
public class NameIsNullDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => e.Name == null;
}

/// <summary>
/// Specification for name is not null.
/// </summary>
public class NameIsNotNullDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => e.Name != null;
}

/// <summary>
/// Specification for amount less than.
/// </summary>
public class AmountLessThanDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    private readonly decimal _value;
    public AmountLessThanDapperMySQLSpec(decimal value) => _value = value;
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => e.Amount < _value;
}

/// <summary>
/// Specification for name not equal.
/// </summary>
public class NameNotEqualDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    private readonly string _value;
    public NameNotEqualDapperMySQLSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => e.Name != _value;
}

/// <summary>
/// Specification that returns constant true.
/// </summary>
public class TrueConstantDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => true;
}

/// <summary>
/// Specification that returns constant false.
/// </summary>
public class FalseConstantDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => false;
}

/// <summary>
/// Specification using string.Equals method.
/// </summary>
public class NameStringEqualsDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    private readonly string _value;
    public NameStringEqualsDapperMySQLSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => e.Name.Equals(_value, StringComparison.Ordinal);
}

/// <summary>
/// Specification using string.Equals method with null.
/// </summary>
public class NameStringEqualsNullDapperMySQLSpec : Specification<TestEntityDapperMySQL>
{
    public override Expression<Func<TestEntityDapperMySQL, bool>> ToExpression()
        => e => e.Name.Equals(null, StringComparison.Ordinal);
}

#endregion

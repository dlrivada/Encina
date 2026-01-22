using System.Data;
using System.Linq.Expressions;
using Encina.Dapper.Oracle.Repository;
using Encina.DomainModeling;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.Oracle.Repository;

/// <summary>
/// Unit tests for <see cref="FunctionalRepositoryDapper{TEntity, TId}"/> (Oracle implementation).
/// Tests constructor validation and mapping configuration.
/// Note: Dapper methods use static extensions which cannot be mocked.
/// Full behavior testing is done in integration tests.
/// </summary>
[Trait("Category", "Unit")]
public class FunctionalRepositoryDapperTests
{
    private readonly IEntityMapping<TestEntityOracleDapper, Guid> _mapping;

    public FunctionalRepositoryDapperTests()
    {
        _mapping = new EntityMappingBuilder<TestEntityOracleDapper, Guid>()
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
            new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(null!, _mapping));
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(connection, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var repository = new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(connection, _mapping);

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
        var repository = new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AddAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.DeleteAsync((TestEntityOracleDapper)null!));
    }

    [Fact]
    public async Task ListAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.ListAsync(null!));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.FirstOrDefaultAsync(null!));
    }

    [Fact]
    public async Task CountAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.CountAsync(null!));
    }

    [Fact]
    public async Task AnyAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AnyAsync(null!));
    }

    [Fact]
    public async Task AddRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AddRangeAsync(null!));
    }

    [Fact]
    public async Task UpdateRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.UpdateRangeAsync(null!));
    }

    [Fact]
    public async Task DeleteRangeAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityOracleDapper, Guid>(connection, _mapping);

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
        var builder = new EntityMappingBuilder<TestEntityOracleDapper, Guid>()
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void EntityMappingBuilder_Build_WithoutId_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityOracleDapper, Guid>()
            .ToTable("TestEntities")
            .MapProperty(e => e.Name, "Name");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void EntityMappingBuilder_Build_WithoutColumnMappings_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityOracleDapper, Guid>()
            .ToTable("TestEntities");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void EntityMappingBuilder_Build_ValidConfiguration_ReturnsMapping()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityOracleDapper, Guid>()
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
        var mapping = new EntityMappingBuilder<TestEntityOracleDapper, Guid>()
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
        var mapping = new EntityMappingBuilder<TestEntityOracleDapper, Guid>()
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
        var mapping = new EntityMappingBuilder<TestEntityOracleDapper, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .Build();

        var entity = new TestEntityOracleDapper { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        var id = mapping.GetId(entity);

        // Assert
        id.ShouldBe(entity.Id);
    }

    [Fact]
    public void EntityMappingBuilder_HasId_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityOracleDapper, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.HasId<Guid>(null!));
    }

    [Fact]
    public void EntityMappingBuilder_MapProperty_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityOracleDapper, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.MapProperty<string>(null!));
    }

    [Fact]
    public void EntityMappingBuilder_ExcludeFromInsert_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityOracleDapper, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.ExcludeFromInsert<string>(null!));
    }

    [Fact]
    public void EntityMappingBuilder_ExcludeFromUpdate_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityOracleDapper, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.ExcludeFromUpdate<string>(null!));
    }

    [Fact]
    public void EntityMapping_GetId_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestEntityOracleDapper, Guid>()
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
    public void SpecificationSqlBuilder_BuildWhereClause_SimpleEquality_GeneratesOracleSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new ActiveEntityOracleDapperSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - Oracle uses double quotes for identifiers
        whereClause.ShouldContain("WHERE");
        whereClause.ShouldContain("\"IsActive\"");
    }

    [Fact]
    public void SpecificationSqlBuilder_BuildSelectStatement_GeneratesOracleSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new ActiveEntityOracleDapperSpec();

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("TestEntities", spec);

        // Assert - Oracle double quotes around table and columns
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM \"TestEntities\"");
        sql.ShouldContain("WHERE");
    }

    [Fact]
    public void SpecificationSqlBuilder_UnsupportedExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new UnsupportedOracleDapperSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void SpecificationSqlBuilder_GreaterThanComparison_GeneratesOracleSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new MinAmountOracleDapperSpec(100);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("WHERE");
        whereClause.ShouldContain("\"Amount\"");
        whereClause.ShouldContain(">=");
        whereClause.ShouldContain(":p0"); // Oracle colon parameters
    }

    [Fact]
    public void SpecificationSqlBuilder_BuildSelectStatement_WithoutSpecification_GeneratesSqlWithoutWhere()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("TestEntities");

        // Assert
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM \"TestEntities\"");
        sql.ShouldNotContain("WHERE");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    public void SpecificationSqlBuilder_OrCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new ActiveEntityOracleDapperSpec().Or(new MinAmountOracleDapperSpec(100));

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("OR");
    }

    [Fact]
    public void SpecificationSqlBuilder_NotSpecification_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new ActiveEntityOracleDapperSpec().Not();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("NOT");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringContains_GeneratesLikeClauseWithColonParam()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new NameContainsOracleDapperSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("LIKE");
        whereClause.ShouldContain(":p"); // Oracle colon parameter
        parameters.Values.First().ShouldBe("%test%");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringStartsWith_GeneratesLikeClauseWithColonParam()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new NameStartsWithOracleDapperSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("LIKE");
        whereClause.ShouldContain(":p");
        parameters.Values.First().ShouldBe("test%");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringEndsWith_GeneratesLikeClauseWithColonParam()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new NameEndsWithOracleDapperSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("LIKE");
        whereClause.ShouldContain(":p");
        parameters.Values.First().ShouldBe("%test");
    }

    [Fact]
    public void SpecificationSqlBuilder_NullEquality_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new NameIsNullOracleDapperSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("IS NULL");
    }

    [Fact]
    public void SpecificationSqlBuilder_NullInequality_GeneratesIsNotNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new NameIsNotNullOracleDapperSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("IS NOT NULL");
    }

    [Fact]
    public void SpecificationSqlBuilder_LessThanComparison_GeneratesOracleSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new AmountLessThanOracleDapperSpec(100);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Amount\"");
        whereClause.ShouldContain("<");
        whereClause.ShouldContain(":p");
    }

    [Fact]
    public void SpecificationSqlBuilder_NotEqual_GeneratesOracleSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new NameNotEqualOracleDapperSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("<>");
        whereClause.ShouldContain(":p");
    }

    [Fact]
    public void SpecificationSqlBuilder_BooleanConstantTrue_Generates1Equals1()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new TrueConstantOracleDapperSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=1");
    }

    [Fact]
    public void SpecificationSqlBuilder_BooleanConstantFalse_Generates1Equals0()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new FalseConstantOracleDapperSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=0");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringEquals_GeneratesEqualityClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new NameStringEqualsOracleDapperSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("\"Name\"");
        whereClause.ShouldContain("=");
        whereClause.ShouldContain(":p");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringEqualsNull_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);
        var spec = new NameStringEqualsNullOracleDapperSpec();

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
            new SpecificationSqlBuilder<TestEntityOracleDapper>(null!));
    }

    [Fact]
    public void SpecificationSqlBuilder_BuildWhereClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityOracleDapper>(_mapping.ColumnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause((Specification<TestEntityOracleDapper>)null!));
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

#region Test Entity and Specifications for Oracle Dapper

/// <summary>
/// Test entity for Oracle Dapper repository unit tests.
/// </summary>
public class TestEntityOracleDapper
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Specification for active entities (Oracle Dapper tests).
/// </summary>
public class ActiveEntityOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => e.IsActive;
}

/// <summary>
/// Specification for minimum amount (Oracle Dapper tests).
/// </summary>
public class MinAmountOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    private readonly decimal _minAmount;

    public MinAmountOracleDapperSpec(decimal minAmount) => _minAmount = minAmount;

    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => e.Amount >= _minAmount;
}

/// <summary>
/// Specification that causes NotSupportedException (for testing unsupported expressions).
/// Uses an unsupported method call expression (List.Contains).
/// </summary>
public class UnsupportedOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    private readonly List<Guid> _validIds = [Guid.NewGuid()];

    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => _validIds.Contains(e.Id);
}

/// <summary>
/// Specification for name contains string.
/// </summary>
public class NameContainsOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    private readonly string _value;
    public NameContainsOracleDapperSpec(string value) => _value = value;
    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => e.Name.Contains(_value);
}

/// <summary>
/// Specification for name starts with string.
/// </summary>
public class NameStartsWithOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    private readonly string _value;
    public NameStartsWithOracleDapperSpec(string value) => _value = value;
    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => e.Name.StartsWith(_value);
}

/// <summary>
/// Specification for name ends with string.
/// </summary>
public class NameEndsWithOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    private readonly string _value;
    public NameEndsWithOracleDapperSpec(string value) => _value = value;
    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => e.Name.EndsWith(_value);
}

/// <summary>
/// Specification for name is null.
/// </summary>
public class NameIsNullOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => e.Name == null;
}

/// <summary>
/// Specification for name is not null.
/// </summary>
public class NameIsNotNullOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => e.Name != null;
}

/// <summary>
/// Specification for amount less than.
/// </summary>
public class AmountLessThanOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    private readonly decimal _value;
    public AmountLessThanOracleDapperSpec(decimal value) => _value = value;
    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => e.Amount < _value;
}

/// <summary>
/// Specification for name not equal.
/// </summary>
public class NameNotEqualOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    private readonly string _value;
    public NameNotEqualOracleDapperSpec(string value) => _value = value;
    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => e.Name != _value;
}

/// <summary>
/// Specification that returns constant true.
/// </summary>
public class TrueConstantOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => true;
}

/// <summary>
/// Specification that returns constant false.
/// </summary>
public class FalseConstantOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => false;
}

/// <summary>
/// Specification using string.Equals method.
/// </summary>
public class NameStringEqualsOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    private readonly string _value;
    public NameStringEqualsOracleDapperSpec(string value) => _value = value;
    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => e.Name.Equals(_value, StringComparison.Ordinal);
}

/// <summary>
/// Specification using string.Equals method with null.
/// </summary>
public class NameStringEqualsNullOracleDapperSpec : Specification<TestEntityOracleDapper>
{
    public override Expression<Func<TestEntityOracleDapper, bool>> ToExpression()
        => e => e.Name.Equals(null, StringComparison.Ordinal);
}

#endregion

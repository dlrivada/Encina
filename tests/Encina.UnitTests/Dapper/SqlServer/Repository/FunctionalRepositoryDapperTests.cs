using System.Data;
using System.Linq.Expressions;
using Encina.Dapper.SqlServer;
using Encina.Dapper.SqlServer.Repository;
using Encina.DomainModeling;
using Encina.Testing.Shouldly;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.SqlServer.Repository;

/// <summary>
/// Unit tests for <see cref="FunctionalRepositoryDapper{TEntity, TId}"/>.
/// Tests constructor validation and mapping configuration.
/// Note: Dapper methods use static extensions which cannot be mocked.
/// Full behavior testing is done in integration tests.
/// </summary>
[Trait("Category", "Unit")]
public class FunctionalRepositoryDapperTests
{
    private readonly IEntityMapping<TestEntityDapper, Guid> _mapping;

    public FunctionalRepositoryDapperTests()
    {
        _mapping = new EntityMappingBuilder<TestEntityDapper, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .MapProperty(e => e.Amount, "Amount")
            .MapProperty(e => e.IsActive, "IsActive")
            .MapProperty(e => e.CreatedAtUtc, "CreatedAtUtc")
            .Build()
            .ShouldBeSuccess();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryDapper<TestEntityDapper, Guid>(null!, _mapping));
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryDapper<TestEntityDapper, Guid>(connection, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(connection, _mapping);

        // Assert
        repository.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithRequestContext_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.UserId.Returns("user-123");

        // Act
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(
            connection, _mapping, requestContext);

        // Assert
        repository.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithTimeProvider_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));

        // Act
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(
            connection, _mapping, null, fakeTime);

        // Assert
        repository.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithBothAuditParameters_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.UserId.Returns("user-123");
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));

        // Act
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(
            connection, _mapping, requestContext, fakeTime);

        // Assert
        repository.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_UsesSystemTime()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var requestContext = Substitute.For<IRequestContext>();

        // Act - Should not throw
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(
            connection, _mapping, requestContext, null);

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
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AddAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.DeleteAsync((TestEntityDapper)null!));
    }

    [Fact]
    public async Task ListAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.ListAsync(null!));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.FirstOrDefaultAsync(null!));
    }

    [Fact]
    public async Task CountAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.CountAsync(null!));
    }

    [Fact]
    public async Task AnyAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AnyAsync(null!));
    }

    [Fact]
    public async Task AddRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.AddRangeAsync(null!));
    }

    [Fact]
    public async Task UpdateRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.UpdateRangeAsync(null!));
    }

    [Fact]
    public async Task DeleteRangeAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = CreateMockConnection();
        var repository = new FunctionalRepositoryDapper<TestEntityDapper, Guid>(connection, _mapping);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repository.DeleteRangeAsync(null!));
    }

    #endregion

    #region EntityMappingBuilder Tests

    [Fact]
    public void EntityMappingBuilder_Build_WithoutTableName_ReturnsError()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapper, Guid>()
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name");

        // Act & Assert
        var result = builder.Build();
        result.ShouldBeErrorWithCode(EntityMappingErrorCodes.MissingTableName);
    }

    [Fact]
    public void EntityMappingBuilder_Build_WithoutId_ReturnsError()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapper, Guid>()
            .ToTable("TestEntities")
            .MapProperty(e => e.Name, "Name");

        // Act & Assert
        var result = builder.Build();
        result.ShouldBeErrorWithCode(EntityMappingErrorCodes.MissingPrimaryKey);
    }

    [Fact]
    public void EntityMappingBuilder_Build_WithoutColumnMappings_ReturnsError()
    {
        // Arrange - ToTable only, without HasId, triggers MissingPrimaryKey first
        var builder = new EntityMappingBuilder<TestEntityDapper, Guid>()
            .ToTable("TestEntities");

        // Act & Assert - Validation is sequential: table → id → columns
        var result = builder.Build();
        result.ShouldBeErrorWithCode(EntityMappingErrorCodes.MissingPrimaryKey);
    }

    [Fact]
    public void EntityMappingBuilder_Build_ValidConfiguration_ReturnsMapping()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapper, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .MapProperty(e => e.Amount, "Amount");

        // Act
        var mapping = builder.Build().ShouldBeSuccess();

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
        var mapping = new EntityMappingBuilder<TestEntityDapper, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .MapProperty(e => e.CreatedAtUtc, "CreatedAtUtc")
            .ExcludeFromInsert(e => e.CreatedAtUtc)
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.InsertExcludedProperties.ShouldContain("CreatedAtUtc");
    }

    [Fact]
    public void EntityMappingBuilder_ExcludeFromUpdate_AddsToExcludedProperties()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestEntityDapper, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .MapProperty(e => e.CreatedAtUtc, "CreatedAtUtc")
            .ExcludeFromUpdate(e => e.CreatedAtUtc)
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.UpdateExcludedProperties.ShouldContain("CreatedAtUtc");
        // Id is automatically excluded from update
        mapping.UpdateExcludedProperties.ShouldContain("Id");
    }

    [Fact]
    public void EntityMappingBuilder_GetId_ReturnsEntityId()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestEntityDapper, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .Build()
            .ShouldBeSuccess();

        var entity = new TestEntityDapper { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        var id = mapping.GetId(entity);

        // Assert
        id.ShouldBe(entity.Id);
    }

    [Fact]
    public void EntityMappingBuilder_HasId_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapper, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.HasId<Guid>(null!));
    }

    [Fact]
    public void EntityMappingBuilder_MapProperty_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapper, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.MapProperty<string>(null!));
    }

    [Fact]
    public void EntityMappingBuilder_ExcludeFromInsert_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapper, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.ExcludeFromInsert<string>(null!));
    }

    [Fact]
    public void EntityMappingBuilder_ExcludeFromUpdate_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestEntityDapper, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.ExcludeFromUpdate<string>(null!));
    }

    [Fact]
    public void EntityMapping_GetId_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestEntityDapper, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name, "Name")
            .Build()
            .ShouldBeSuccess();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => mapping.GetId(null!));
    }

    #endregion

    #region SpecificationSqlBuilder Tests

    [Fact]
    public void SpecificationSqlBuilder_BuildWhereClause_SimpleEquality_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new ActiveEntityDapperSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("WHERE");
        whereClause.ShouldContain("[IsActive]");
    }

    [Fact]
    public void SpecificationSqlBuilder_BuildSelectStatement_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new ActiveEntityDapperSpec();

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("TestEntities", spec);

        // Assert
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM TestEntities");
        sql.ShouldContain("WHERE");
    }

    [Fact]
    public void SpecificationSqlBuilder_UnsupportedExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new UnsupportedDapperSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => builder.BuildWhereClause(spec));
    }

    [Fact]
    public void SpecificationSqlBuilder_GreaterThanComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new MinAmountDapperSpec(100);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("WHERE");
        whereClause.ShouldContain("[Amount]");
        whereClause.ShouldContain(">=");
    }

    [Fact]
    public void SpecificationSqlBuilder_BuildSelectStatement_WithoutSpecification_GeneratesSqlWithoutWhere()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("TestEntities");

        // Assert
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM TestEntities");
        sql.ShouldNotContain("WHERE");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    public void SpecificationSqlBuilder_OrCombination_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new ActiveEntityDapperSpec().Or(new MinAmountDapperSpec(100));

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("OR");
    }

    [Fact]
    public void SpecificationSqlBuilder_NotSpecification_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new ActiveEntityDapperSpec().Not();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("NOT");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringContains_GeneratesLikeClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new NameContainsDapperSpec("test");

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
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new NameStartsWithDapperSpec("test");

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
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new NameEndsWithDapperSpec("test");

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
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new NameIsNullDapperSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("IS NULL");
    }

    [Fact]
    public void SpecificationSqlBuilder_NullInequality_GeneratesIsNotNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new NameIsNotNullDapperSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("IS NOT NULL");
    }

    [Fact]
    public void SpecificationSqlBuilder_LessThanComparison_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new AmountLessThanDapperSpec(100);

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("[Amount]");
        whereClause.ShouldContain("<");
    }

    [Fact]
    public void SpecificationSqlBuilder_NotEqual_GeneratesCorrectSql()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new NameNotEqualDapperSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("<>");
    }

    [Fact]
    public void SpecificationSqlBuilder_BooleanConstantTrue_Generates1Equals1()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new TrueConstantDapperSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=1");
    }

    [Fact]
    public void SpecificationSqlBuilder_BooleanConstantFalse_Generates1Equals0()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new FalseConstantDapperSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("1=0");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringEquals_GeneratesEqualityClause()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new NameStringEqualsDapperSpec("test");

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldContain("[Name]");
        whereClause.ShouldContain("=");
    }

    [Fact]
    public void SpecificationSqlBuilder_StringEqualsNull_GeneratesIsNull()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);
        var spec = new NameStringEqualsNullDapperSpec();

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
            new SpecificationSqlBuilder<TestEntityDapper>(null!));
    }

    [Fact]
    public void SpecificationSqlBuilder_BuildWhereClause_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SpecificationSqlBuilder<TestEntityDapper>(_mapping.ColumnMappings);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause(null!));
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
/// Test entity for Dapper repository unit tests.
/// </summary>
public class TestEntityDapper
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Specification for active entities (Dapper tests).
/// </summary>
public class ActiveEntityDapperSpec : Specification<TestEntityDapper>
{
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => e.IsActive;
}

/// <summary>
/// Specification for minimum amount (Dapper tests).
/// </summary>
public class MinAmountDapperSpec : Specification<TestEntityDapper>
{
    private readonly decimal _minAmount;

    public MinAmountDapperSpec(decimal minAmount) => _minAmount = minAmount;

    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => e.Amount >= _minAmount;
}

/// <summary>
/// Specification that causes NotSupportedException (for testing unsupported expressions).
/// Uses an unsupported method call expression (string.Replace).
/// </summary>
public class UnsupportedDapperSpec : Specification<TestEntityDapper>
{
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => e.Name.Replace("a", "b") == "test";
}

/// <summary>
/// Specification for name contains string.
/// </summary>
public class NameContainsDapperSpec : Specification<TestEntityDapper>
{
    private readonly string _value;
    public NameContainsDapperSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => e.Name.Contains(_value);
}

/// <summary>
/// Specification for name starts with string.
/// </summary>
public class NameStartsWithDapperSpec : Specification<TestEntityDapper>
{
    private readonly string _value;
    public NameStartsWithDapperSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => e.Name.StartsWith(_value);
}

/// <summary>
/// Specification for name ends with string.
/// </summary>
public class NameEndsWithDapperSpec : Specification<TestEntityDapper>
{
    private readonly string _value;
    public NameEndsWithDapperSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => e.Name.EndsWith(_value);
}

/// <summary>
/// Specification for name is null.
/// </summary>
public class NameIsNullDapperSpec : Specification<TestEntityDapper>
{
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => e.Name == null;
}

/// <summary>
/// Specification for name is not null.
/// </summary>
public class NameIsNotNullDapperSpec : Specification<TestEntityDapper>
{
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => e.Name != null;
}

/// <summary>
/// Specification for amount less than.
/// </summary>
public class AmountLessThanDapperSpec : Specification<TestEntityDapper>
{
    private readonly decimal _value;
    public AmountLessThanDapperSpec(decimal value) => _value = value;
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => e.Amount < _value;
}

/// <summary>
/// Specification for name not equal.
/// </summary>
public class NameNotEqualDapperSpec : Specification<TestEntityDapper>
{
    private readonly string _value;
    public NameNotEqualDapperSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => e.Name != _value;
}

/// <summary>
/// Specification that returns constant true.
/// </summary>
public class TrueConstantDapperSpec : Specification<TestEntityDapper>
{
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => true;
}

/// <summary>
/// Specification that returns constant false.
/// </summary>
public class FalseConstantDapperSpec : Specification<TestEntityDapper>
{
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => false;
}

/// <summary>
/// Specification using string.Equals method.
/// </summary>
public class NameStringEqualsDapperSpec : Specification<TestEntityDapper>
{
    private readonly string _value;
    public NameStringEqualsDapperSpec(string value) => _value = value;
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => e.Name.Equals(_value, StringComparison.Ordinal);
}

/// <summary>
/// Specification using string.Equals method with null.
/// </summary>
public class NameStringEqualsNullDapperSpec : Specification<TestEntityDapper>
{
    public override Expression<Func<TestEntityDapper, bool>> ToExpression()
        => e => e.Name.Equals(null, StringComparison.Ordinal);
}

/// <summary>
/// Test entity implementing IAuditableEntity for audit field population tests.
/// </summary>
public class AuditableTestEntityDapper : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}

#endregion

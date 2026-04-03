using Encina.Dapper.PostgreSQL.SoftDelete;
using Encina.DomainModeling;
using Encina.Messaging.SoftDelete;
using Shouldly;

namespace Encina.GuardTests.Dapper.PostgreSQL.SoftDelete;

/// <summary>
/// Guard clause tests for <see cref="SoftDeleteSpecificationSqlBuilder{TEntity}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Dapper.PostgreSQL")]
public sealed class SoftDeleteSpecificationSqlBuilderGuardTests
{
    // ----- Constructor guards -----

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        ISoftDeleteEntityMapping<SdSpecTestEntity, object> mapping = null!;
        var options = new SoftDeleteOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new SoftDeleteSpecificationSqlBuilder<SdSpecTestEntity>(mapping, options));
        ex.ParamName.ShouldBe("mapping");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildRealMapping();
        SoftDeleteOptions options = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new SoftDeleteSpecificationSqlBuilder<SdSpecTestEntity>(mapping, options));
        ex.ParamName.ShouldBe("options");
    }

    // ----- BuildWhereClause(Specification) guards -----

    [Fact]
    public void BuildWhereClause_Specification_Null_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = CreateBuilder();
        Specification<SdSpecTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause(specification));
        ex.ParamName.ShouldBe("specification");
    }

    // ----- BuildWhereClause(QuerySpecification) guards -----

    [Fact]
    public void BuildWhereClause_QuerySpecification_Null_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = CreateBuilder();
        QuerySpecification<SdSpecTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause(specification));
        ex.ParamName.ShouldBe("specification");
    }

    // ----- BuildSelectStatement(tableName, Specification) guards -----

    [Fact]
    public void BuildSelectStatement_Specification_Null_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = CreateBuilder();
        Specification<SdSpecTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildSelectStatement("TestTable", specification));
        ex.ParamName.ShouldBe("specification");
    }

    // ----- BuildSelectStatement(tableName, QuerySpecification) guards -----

    [Fact]
    public void BuildSelectStatement_QuerySpecification_Null_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = CreateBuilder();
        QuerySpecification<SdSpecTestEntity> specification = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.BuildSelectStatement("TestTable", specification));
        ex.ParamName.ShouldBe("specification");
    }

    // ----- IncludeDeleted guards -----

    [Fact]
    public void IncludeDeleted_ReturnsNewBuilder()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var includedBuilder = builder.IncludeDeleted();

        // Assert
        includedBuilder.ShouldNotBeNull();
        includedBuilder.ShouldNotBeSameAs(builder);
    }

    #region Helper Methods

    private static ISoftDeleteEntityMapping<SdSpecTestEntity, object> BuildRealMapping()
    {
        var builder = new SoftDeleteEntityMappingBuilder<SdSpecTestEntity, object>()
            .ToTable("Tests")
            .HasId(e => (object)e.Id)
            .HasSoftDelete(e => e.IsDeleted)
            .MapProperty(e => e.Name);

        return builder.Build().Match(
            Right: m => m,
            Left: e => throw new InvalidOperationException(e.Message));
    }

    private static SoftDeleteSpecificationSqlBuilder<SdSpecTestEntity> CreateBuilder()
    {
        var mapping = BuildRealMapping();
        var options = new SoftDeleteOptions { AutoFilterSoftDeletedQueries = true };
        return new SoftDeleteSpecificationSqlBuilder<SdSpecTestEntity>(mapping, options);
    }

    #endregion

    #region Test Entities

    private sealed class SdSpecTestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }

    #endregion
}

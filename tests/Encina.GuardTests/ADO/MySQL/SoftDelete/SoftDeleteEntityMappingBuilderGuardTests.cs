using System.Linq.Expressions;
using Encina.ADO.MySQL;
using Encina.ADO.MySQL.SoftDelete;
using Shouldly;

namespace Encina.GuardTests.ADO.MySQL.SoftDelete;

/// <summary>
/// Guard clause tests for <see cref="SoftDeleteEntityMappingBuilder{TEntity, TId}"/>
/// and the internal <c>SoftDeleteEntityMapping</c> class methods.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "ADO.MySQL")]
public sealed class SoftDeleteEntityMappingBuilderGuardTests
{
    // ----- HasId guards -----

    /// <summary>
    /// Verifies that HasId throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void HasId_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>();
        Expression<Func<SoftDeleteTestEntity, Guid>> selector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.HasId(selector));
        ex.ParamName.ShouldBe("propertySelector");
    }

    // ----- HasSoftDelete guards -----

    /// <summary>
    /// Verifies that HasSoftDelete throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void HasSoftDelete_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>();
        Expression<Func<SoftDeleteTestEntity, bool>> selector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.HasSoftDelete(selector));
        ex.ParamName.ShouldBe("propertySelector");
    }

    // ----- HasDeletedAt guards -----

    /// <summary>
    /// Verifies that HasDeletedAt throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void HasDeletedAt_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>();
        Expression<Func<SoftDeleteTestEntity, DateTime?>> selector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.HasDeletedAt(selector));
        ex.ParamName.ShouldBe("propertySelector");
    }

    // ----- HasDeletedBy guards -----

    /// <summary>
    /// Verifies that HasDeletedBy throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void HasDeletedBy_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>();
        Expression<Func<SoftDeleteTestEntity, string?>> selector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.HasDeletedBy(selector));
        ex.ParamName.ShouldBe("propertySelector");
    }

    // ----- MapProperty guards -----

    /// <summary>
    /// Verifies that MapProperty throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void MapProperty_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>();
        Expression<Func<SoftDeleteTestEntity, string>> selector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.MapProperty(selector));
        ex.ParamName.ShouldBe("propertySelector");
    }

    // ----- ExcludeFromInsert guards -----

    /// <summary>
    /// Verifies that ExcludeFromInsert throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void ExcludeFromInsert_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>();
        Expression<Func<SoftDeleteTestEntity, string>> selector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ExcludeFromInsert(selector));
        ex.ParamName.ShouldBe("propertySelector");
    }

    // ----- ExcludeFromUpdate guards -----

    /// <summary>
    /// Verifies that ExcludeFromUpdate throws ArgumentNullException when propertySelector is null.
    /// </summary>
    [Fact]
    public void ExcludeFromUpdate_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>();
        Expression<Func<SoftDeleteTestEntity, string>> selector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.ExcludeFromUpdate(selector));
        ex.ParamName.ShouldBe("propertySelector");
    }

    // ----- Build validation guards -----

    /// <summary>
    /// Verifies that Build returns Left with MissingTableName error when no table name is configured.
    /// </summary>
    [Fact]
    public void Build_MissingTableName_ReturnsLeftError()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>()
            .HasId(e => e.Id)
            .MapProperty(e => e.Name);

        // Act
        var result = builder.Build();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("Table name"));
    }

    /// <summary>
    /// Verifies that Build returns Left with MissingPrimaryKey error when no ID is configured.
    /// </summary>
    [Fact]
    public void Build_MissingPrimaryKey_ReturnsLeftError()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>()
            .ToTable("Tests")
            .MapProperty(e => e.Name);

        // Act
        var result = builder.Build();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("Primary key"));
    }

    /// <summary>
    /// Verifies that Build returns Left when table name is set but no ID and no column mappings.
    /// The MissingPrimaryKey check fires before MissingColumnMappings.
    /// </summary>
    [Fact]
    public void Build_TableOnly_ReturnsLeftError()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>()
            .ToTable("Tests");

        // Act
        var result = builder.Build();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("Primary key"));
    }

    /// <summary>
    /// Verifies that Build succeeds when all required configuration is provided.
    /// </summary>
    [Fact]
    public void Build_ValidConfiguration_ReturnsRightMapping()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>()
            .ToTable("Tests")
            .HasId(e => e.Id)
            .HasSoftDelete(e => e.IsDeleted)
            .HasDeletedAt(e => e.DeletedAtUtc)
            .HasDeletedBy(e => e.DeletedBy)
            .MapProperty(e => e.Name);

        // Act
        var result = builder.Build();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // ----- SoftDeleteEntityMapping instance method guards -----

    /// <summary>
    /// Verifies that GetId throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void GetId_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.GetId(null!));
        ex.ParamName.ShouldBe("entity");
    }

    /// <summary>
    /// Verifies that GetIsDeleted throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void GetIsDeleted_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.GetIsDeleted(null!));
        ex.ParamName.ShouldBe("entity");
    }

    /// <summary>
    /// Verifies that SetIsDeleted throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void SetIsDeleted_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.SetIsDeleted(null!, true));
        ex.ParamName.ShouldBe("entity");
    }

    /// <summary>
    /// Verifies that SetIsDeleted throws InvalidOperationException when soft delete is not configured.
    /// </summary>
    [Fact]
    public void SetIsDeleted_NotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange - build without HasSoftDelete
        var mapping = BuildMappingWithoutSoftDelete();
        var entity = new SoftDeleteTestEntity();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => mapping.SetIsDeleted(entity, true));
    }

    /// <summary>
    /// Verifies that GetDeletedAtUtc throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void GetDeletedAtUtc_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.GetDeletedAtUtc(null!));
        ex.ParamName.ShouldBe("entity");
    }

    /// <summary>
    /// Verifies that SetDeletedAtUtc throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void SetDeletedAtUtc_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.SetDeletedAtUtc(null!, DateTime.UtcNow));
        ex.ParamName.ShouldBe("entity");
    }

    /// <summary>
    /// Verifies that SetDeletedAtUtc throws InvalidOperationException when DeletedAt is not configured.
    /// </summary>
    [Fact]
    public void SetDeletedAtUtc_NotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange - build without HasDeletedAt
        var mapping = BuildMappingWithoutSoftDelete();
        var entity = new SoftDeleteTestEntity();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => mapping.SetDeletedAtUtc(entity, DateTime.UtcNow));
    }

    /// <summary>
    /// Verifies that GetDeletedBy throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void GetDeletedBy_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.GetDeletedBy(null!));
        ex.ParamName.ShouldBe("entity");
    }

    /// <summary>
    /// Verifies that SetDeletedBy throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void SetDeletedBy_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.SetDeletedBy(null!, "admin"));
        ex.ParamName.ShouldBe("entity");
    }

    /// <summary>
    /// Verifies that SetDeletedBy throws InvalidOperationException when DeletedBy is not configured.
    /// </summary>
    [Fact]
    public void SetDeletedBy_NotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange - build without HasDeletedBy
        var mapping = BuildMappingWithoutSoftDelete();
        var entity = new SoftDeleteTestEntity();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => mapping.SetDeletedBy(entity, "admin"));
    }

    #region Helper Methods

    private static ISoftDeleteEntityMapping<SoftDeleteTestEntity, Guid> BuildValidMapping()
    {
        return new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>()
            .ToTable("Tests")
            .HasId(e => e.Id)
            .HasSoftDelete(e => e.IsDeleted)
            .HasDeletedAt(e => e.DeletedAtUtc)
            .HasDeletedBy(e => e.DeletedBy)
            .MapProperty(e => e.Name)
            .Build()
            .Match(Right: m => m, Left: e => throw new InvalidOperationException(e.Message));
    }

    private static ISoftDeleteEntityMapping<SoftDeleteTestEntity, Guid> BuildMappingWithoutSoftDelete()
    {
        return new SoftDeleteEntityMappingBuilder<SoftDeleteTestEntity, Guid>()
            .ToTable("Tests")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build()
            .Match(Right: m => m, Left: e => throw new InvalidOperationException(e.Message));
    }

    #endregion

    #region Test Entities

    private sealed class SoftDeleteTestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
    }

    #endregion
}

using System.Linq.Expressions;
using Encina.Dapper.PostgreSQL.SoftDelete;
using Shouldly;

namespace Encina.GuardTests.Dapper.PostgreSQL.SoftDelete;

/// <summary>
/// Guard clause tests for <see cref="SoftDeleteEntityMappingBuilder{TEntity, TId}"/>
/// and the internal <c>SoftDeleteEntityMapping</c> class methods.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Dapper.PostgreSQL")]
public sealed class SoftDeleteEntityMappingBuilderGuardTests
{
    // ----- HasId guards -----

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

    [Fact]
    public void GetId_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.GetId(null!));
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public void GetIsDeleted_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.GetIsDeleted(null!));
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public void SetIsDeleted_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.SetIsDeleted(null!, true));
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public void SetIsDeleted_NotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange - build without HasSoftDelete
        var mapping = BuildMappingWithoutSoftDelete();
        var entity = new SoftDeleteTestEntity();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => mapping.SetIsDeleted(entity, true));
    }

    [Fact]
    public void GetDeletedAtUtc_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.GetDeletedAtUtc(null!));
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public void SetDeletedAtUtc_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.SetDeletedAtUtc(null!, DateTime.UtcNow));
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public void SetDeletedAtUtc_NotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange - build without HasDeletedAt
        var mapping = BuildMappingWithoutSoftDelete();
        var entity = new SoftDeleteTestEntity();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => mapping.SetDeletedAtUtc(entity, DateTime.UtcNow));
    }

    [Fact]
    public void GetDeletedBy_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.GetDeletedBy(null!));
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public void SetDeletedBy_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = BuildValidMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => mapping.SetDeletedBy(null!, "admin"));
        ex.ParamName.ShouldBe("entity");
    }

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

using Encina.Dapper.MySQL.SoftDelete;
using Shouldly;

namespace Encina.GuardTests.Dapper.MySQL.SoftDelete;

/// <summary>
/// Guard tests for <see cref="SoftDeleteEntityMappingBuilder{TEntity, TId}"/> to verify null parameter handling.
/// </summary>
public class SoftDeleteEntityMappingBuilderGuardTests
{
    [Fact]
    public void HasId_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();

        // Act & Assert
        var act = () => builder.HasId<Guid>(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void HasSoftDelete_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();

        // Act & Assert
        var act = () => builder.HasSoftDelete(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void HasDeletedAt_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();

        // Act & Assert
        var act = () => builder.HasDeletedAt(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void HasDeletedBy_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();

        // Act & Assert
        var act = () => builder.HasDeletedBy(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void MapProperty_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();

        // Act & Assert
        var act = () => builder.MapProperty<string>(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void ExcludeFromInsert_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();

        // Act & Assert
        var act = () => builder.ExcludeFromInsert<string>(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void ExcludeFromUpdate_NullPropertySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();

        // Act & Assert
        var act = () => builder.ExcludeFromUpdate<string>(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("propertySelector");
    }

    [Fact]
    public void Build_WithoutTableName_ReturnsLeft()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>()
            .HasId(e => e.Id);

        // Act
        var result = builder.Build();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Build_WithoutId_ReturnsLeft()
    {
        // Arrange
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>()
            .ToTable("TestEntities");

        // Act
        var result = builder.Build();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    public class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
    }
}

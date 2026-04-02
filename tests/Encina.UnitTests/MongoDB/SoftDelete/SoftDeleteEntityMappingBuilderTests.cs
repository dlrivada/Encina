using Encina.MongoDB.SoftDelete;
using Shouldly;

namespace Encina.UnitTests.MongoDB.SoftDelete;

/// <summary>
/// Unit tests for <see cref="SoftDeleteEntityMappingBuilder{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "MongoDB")]
public sealed class SoftDeleteEntityMappingBuilderTests
{
    #region Build - validation

    [Fact]
    public void Build_WithoutIdProperty_ReturnsLeftError()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        builder.HasSoftDelete(e => e.IsDeleted, "isDeleted");

        var result = builder.Build();

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("ID property must be configured"));
    }

    [Fact]
    public void Build_WithoutSoftDeleteProperty_ReturnsLeftError()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        builder.HasId<Guid>(e => e.Id);

        var result = builder.Build();

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("IsDeleted property must be configured"));
    }

    [Fact]
    public void Build_WithRequiredProperties_ReturnsRightMapping()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        builder.HasId<Guid>(e => e.Id);
        builder.HasSoftDelete(e => e.IsDeleted, "isDeleted");

        var result = builder.Build();

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Build_WithAllProperties_ReturnsRightMapping()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        builder.HasId<Guid>(e => e.Id);
        builder.HasSoftDelete(e => e.IsDeleted, "isDeleted");
        builder.HasDeletedAt(e => e.DeletedAtUtc, "deletedAtUtc");
        builder.HasDeletedBy(e => e.DeletedBy, "deletedBy");

        var result = builder.Build();

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Mapping - field names

    [Fact]
    public void Build_Mapping_HasCorrectFieldNames()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        builder.HasId<Guid>(e => e.Id);
        builder.HasSoftDelete(e => e.IsDeleted, "is_deleted");
        builder.HasDeletedAt(e => e.DeletedAtUtc, "deleted_at");
        builder.HasDeletedBy(e => e.DeletedBy, "deleted_by");

        var result = builder.Build();

        result.Match(
            Right: mapping =>
            {
                mapping.IsDeletedFieldName.ShouldBe("is_deleted");
                mapping.DeletedAtFieldName.ShouldBe("deleted_at");
                mapping.DeletedByFieldName.ShouldBe("deleted_by");
                mapping.IsSoftDeletable.ShouldBeTrue();
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void Build_HasSoftDelete_WithoutFieldName_UsesPropertyName()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        builder.HasId<Guid>(e => e.Id);
        builder.HasSoftDelete(e => e.IsDeleted);

        var result = builder.Build();

        result.Match(
            Right: mapping => mapping.IsDeletedFieldName.ShouldBe("IsDeleted"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void Build_HasDeletedAt_WithoutFieldName_UsesPropertyName()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        builder.HasId<Guid>(e => e.Id);
        builder.HasSoftDelete(e => e.IsDeleted, "isDeleted");
        builder.HasDeletedAt(e => e.DeletedAtUtc);

        var result = builder.Build();

        result.Match(
            Right: mapping => mapping.DeletedAtFieldName.ShouldBe("DeletedAtUtc"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Mapping - getters and setters

    [Fact]
    public void Build_Mapping_GetId_ReturnsCorrectValue()
    {
        var mapping = BuildValidMapping();
        var entity = new TestEntity { Id = Guid.NewGuid() };

        mapping.GetId(entity).ShouldBe(entity.Id);
    }

    [Fact]
    public void Build_Mapping_GetIsDeleted_ReturnsCorrectValue()
    {
        var mapping = BuildValidMapping();
        var entity = new TestEntity { IsDeleted = true };

        mapping.GetIsDeleted(entity).ShouldBeTrue();
    }

    [Fact]
    public void Build_Mapping_SetIsDeleted_UpdatesEntity()
    {
        var mapping = BuildValidMapping();
        var entity = new TestEntity { IsDeleted = false };

        mapping.SetIsDeleted(entity, true);

        entity.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void Build_Mapping_GetDeletedAt_ReturnsCorrectValue()
    {
        var mapping = BuildValidMapping();
        var now = DateTime.UtcNow;
        var entity = new TestEntity { DeletedAtUtc = now };

        mapping.GetDeletedAt(entity).ShouldBe(now);
    }

    [Fact]
    public void Build_Mapping_SetDeletedAt_UpdatesEntity()
    {
        var mapping = BuildValidMapping();
        var entity = new TestEntity();
        var now = DateTime.UtcNow;

        mapping.SetDeletedAt(entity, now);

        entity.DeletedAtUtc.ShouldBe(now);
    }

    [Fact]
    public void Build_Mapping_GetDeletedBy_ReturnsCorrectValue()
    {
        var mapping = BuildValidMapping();
        var entity = new TestEntity { DeletedBy = "admin" };

        mapping.GetDeletedBy(entity).ShouldBe("admin");
    }

    [Fact]
    public void Build_Mapping_SetDeletedBy_UpdatesEntity()
    {
        var mapping = BuildValidMapping();
        var entity = new TestEntity();

        mapping.SetDeletedBy(entity, "admin");

        entity.DeletedBy.ShouldBe("admin");
    }

    [Fact]
    public void Build_Mapping_NullEntity_ThrowsArgumentNullException()
    {
        var mapping = BuildValidMapping();

        Should.Throw<ArgumentNullException>(() => mapping.GetId(null!));
        Should.Throw<ArgumentNullException>(() => mapping.GetIsDeleted(null!));
        Should.Throw<ArgumentNullException>(() => mapping.SetIsDeleted(null!, true));
        Should.Throw<ArgumentNullException>(() => mapping.GetDeletedAt(null!));
        Should.Throw<ArgumentNullException>(() => mapping.SetDeletedAt(null!, DateTime.UtcNow));
        Should.Throw<ArgumentNullException>(() => mapping.GetDeletedBy(null!));
        Should.Throw<ArgumentNullException>(() => mapping.SetDeletedBy(null!, "user"));
    }

    #endregion

    #region Mapping - without optional properties

    [Fact]
    public void Build_WithoutDeletedAt_GetDeletedAt_ReturnsNull()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        builder.HasId<Guid>(e => e.Id);
        builder.HasSoftDelete(e => e.IsDeleted, "isDeleted");

        var result = builder.Build();

        result.Match(
            Right: mapping =>
            {
                var entity = new TestEntity { DeletedAtUtc = DateTime.UtcNow };
                mapping.GetDeletedAt(entity).ShouldBeNull();
                mapping.DeletedAtFieldName.ShouldBeNull();
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void Build_WithoutDeletedBy_GetDeletedBy_ReturnsNull()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        builder.HasId<Guid>(e => e.Id);
        builder.HasSoftDelete(e => e.IsDeleted, "isDeleted");

        var result = builder.Build();

        result.Match(
            Right: mapping =>
            {
                var entity = new TestEntity { DeletedBy = "admin" };
                mapping.GetDeletedBy(entity).ShouldBeNull();
                mapping.DeletedByFieldName.ShouldBeNull();
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Fluent API chaining

    [Fact]
    public void FluentApi_ReturnsSameBuilder()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();

        var result1 = builder.HasId<Guid>(e => e.Id);
        var result2 = builder.HasSoftDelete(e => e.IsDeleted, "isDeleted");
        var result3 = builder.HasDeletedAt(e => e.DeletedAtUtc, "deletedAt");
        var result4 = builder.HasDeletedBy(e => e.DeletedBy, "deletedBy");

        result1.ShouldBeSameAs(builder);
        result2.ShouldBeSameAs(builder);
        result3.ShouldBeSameAs(builder);
        result4.ShouldBeSameAs(builder);
    }

    #endregion

    #region Guard clauses

    [Fact]
    public void HasId_NullSelector_ThrowsArgumentNullException()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();

        Should.Throw<ArgumentNullException>(() =>
            builder.HasId<Guid>(null!));
    }

    [Fact]
    public void HasSoftDelete_NullSelector_ThrowsArgumentNullException()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();

        Should.Throw<ArgumentNullException>(() =>
            builder.HasSoftDelete(null!));
    }

    [Fact]
    public void HasDeletedAt_NullSelector_ThrowsArgumentNullException()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();

        Should.Throw<ArgumentNullException>(() =>
            builder.HasDeletedAt(null!));
    }

    [Fact]
    public void HasDeletedBy_NullSelector_ThrowsArgumentNullException()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();

        Should.Throw<ArgumentNullException>(() =>
            builder.HasDeletedBy(null!));
    }

    #endregion

    #region Helpers

    private static ISoftDeleteEntityMapping<TestEntity, Guid> BuildValidMapping()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        builder.HasId<Guid>(e => e.Id);
        builder.HasSoftDelete(e => e.IsDeleted, "isDeleted");
        builder.HasDeletedAt(e => e.DeletedAtUtc, "deletedAtUtc");
        builder.HasDeletedBy(e => e.DeletedBy, "deletedBy");

        return builder.Build().Match(
            Right: m => m,
            Left: error => throw new InvalidOperationException(error.Message));
    }

    #endregion

    #region Test entities

    public sealed class TestEntity
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}

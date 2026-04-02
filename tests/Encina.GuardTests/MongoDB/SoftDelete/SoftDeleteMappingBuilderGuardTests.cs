using Encina.MongoDB.SoftDelete;

namespace Encina.GuardTests.MongoDB.SoftDelete;

public class SoftDeleteMappingBuilderGuardTests
{
    #region HasId

    [Fact]
    public void HasId_NullSelector_Throws()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        Should.Throw<ArgumentNullException>(() =>
            builder.HasId<Guid>(null!));
    }

    #endregion

    #region HasSoftDelete

    [Fact]
    public void HasSoftDelete_NullSelector_Throws()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        Should.Throw<ArgumentNullException>(() =>
            builder.HasSoftDelete(null!));
    }

    #endregion

    #region HasDeletedAt

    [Fact]
    public void HasDeletedAt_NullSelector_Throws()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        Should.Throw<ArgumentNullException>(() =>
            builder.HasDeletedAt(null!));
    }

    #endregion

    #region HasDeletedBy

    [Fact]
    public void HasDeletedBy_NullSelector_Throws()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>();
        Should.Throw<ArgumentNullException>(() =>
            builder.HasDeletedBy(null!));
    }

    #endregion

    #region Build — validation

    [Fact]
    public void Build_WithoutId_ReturnsLeft()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>()
            .HasSoftDelete(e => e.IsDeleted);

        var result = builder.Build();
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Build_WithoutSoftDelete_ReturnsLeft()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>()
            .HasId<Guid>(e => e.Id);

        var result = builder.Build();
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Build_WithRequiredFields_ReturnsRight()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>()
            .HasId<Guid>(e => e.Id)
            .HasSoftDelete(e => e.IsDeleted);

        var result = builder.Build();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Build_WithAllFields_ReturnsRight()
    {
        var builder = new SoftDeleteEntityMappingBuilder<TestEntity, Guid>()
            .HasId<Guid>(e => e.Id)
            .HasSoftDelete(e => e.IsDeleted, "is_deleted")
            .HasDeletedAt(e => e.DeletedAtUtc, "deleted_at_utc")
            .HasDeletedBy(e => e.DeletedBy, "deleted_by");

        var result = builder.Build();
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region SoftDeleteEntityMapping — method guards

    [Fact]
    public void Mapping_GetId_NullEntity_Throws()
    {
        var mapping = BuildValidMapping();
        Should.Throw<ArgumentNullException>(() => mapping.GetId(null!));
    }

    [Fact]
    public void Mapping_GetIsDeleted_NullEntity_Throws()
    {
        var mapping = BuildValidMapping();
        Should.Throw<ArgumentNullException>(() => mapping.GetIsDeleted(null!));
    }

    [Fact]
    public void Mapping_SetIsDeleted_NullEntity_Throws()
    {
        var mapping = BuildValidMapping();
        Should.Throw<ArgumentNullException>(() => mapping.SetIsDeleted(null!, true));
    }

    [Fact]
    public void Mapping_GetDeletedAt_NullEntity_Throws()
    {
        var mapping = BuildValidMapping();
        Should.Throw<ArgumentNullException>(() => mapping.GetDeletedAt(null!));
    }

    [Fact]
    public void Mapping_SetDeletedAt_NullEntity_Throws()
    {
        var mapping = BuildValidMapping();
        Should.Throw<ArgumentNullException>(() => mapping.SetDeletedAt(null!, DateTime.UtcNow));
    }

    [Fact]
    public void Mapping_GetDeletedBy_NullEntity_Throws()
    {
        var mapping = BuildValidMapping();
        Should.Throw<ArgumentNullException>(() => mapping.GetDeletedBy(null!));
    }

    [Fact]
    public void Mapping_SetDeletedBy_NullEntity_Throws()
    {
        var mapping = BuildValidMapping();
        Should.Throw<ArgumentNullException>(() => mapping.SetDeletedBy(null!, "user1"));
    }

    #endregion

    private static ISoftDeleteEntityMapping<TestEntity, Guid> BuildValidMapping()
    {
        return new SoftDeleteEntityMappingBuilder<TestEntity, Guid>()
            .HasId<Guid>(e => e.Id)
            .HasSoftDelete(e => e.IsDeleted, "is_deleted")
            .HasDeletedAt(e => e.DeletedAtUtc, "deleted_at_utc")
            .HasDeletedBy(e => e.DeletedBy, "deleted_by")
            .Build()
            .Match(Right: m => m, Left: _ => throw new InvalidOperationException("Build failed"));
    }

    public class TestEntity
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
    }
}

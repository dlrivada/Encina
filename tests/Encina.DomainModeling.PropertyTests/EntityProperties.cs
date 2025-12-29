using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for Entity equality and identity invariants.
/// </summary>
public sealed class EntityProperties
{
    private sealed class TestEntity : Entity<Guid>
    {
        public string Name { get; }

        public TestEntity(Guid id, string name) : base(id)
        {
            Name = name;
        }
    }

    private sealed class OtherEntity : Entity<Guid>
    {
        public OtherEntity(Guid id) : base(id) { }
    }

    #region Equality Properties

    [Property(MaxTest = 200)]
    public bool Entity_Equality_IsReflexive(Guid id, NonEmptyString name)
    {
        var entity = new TestEntity(id, name.Get);
        return entity.Equals(entity);
    }

    [Property(MaxTest = 200)]
    public bool Entity_Equality_IsSymmetric(Guid id, NonEmptyString name1, NonEmptyString name2)
    {
        var entity1 = new TestEntity(id, name1.Get);
        var entity2 = new TestEntity(id, name2.Get);

        return entity1.Equals(entity2) == entity2.Equals(entity1);
    }

    [Property(MaxTest = 200)]
    public bool Entity_Equality_IsTransitive(Guid id, NonEmptyString name1, NonEmptyString name2, NonEmptyString name3)
    {
        var entity1 = new TestEntity(id, name1.Get);
        var entity2 = new TestEntity(id, name2.Get);
        var entity3 = new TestEntity(id, name3.Get);

        if (entity1.Equals(entity2) && entity2.Equals(entity3))
        {
            return entity1.Equals(entity3);
        }

        return true;
    }

    [Property(MaxTest = 200)]
    public bool Entity_WithSameId_AreEqual(Guid id, NonEmptyString name1, NonEmptyString name2)
    {
        var entity1 = new TestEntity(id, name1.Get);
        var entity2 = new TestEntity(id, name2.Get);

        return entity1.Equals(entity2) && entity1 == entity2;
    }

    [Property(MaxTest = 200)]
    public bool Entity_WithDifferentIds_AreNotEqual(Guid id1, Guid id2, NonEmptyString name)
    {
        if (id1 == id2) return true;

        var entity1 = new TestEntity(id1, name.Get);
        var entity2 = new TestEntity(id2, name.Get);

        return !entity1.Equals(entity2) && entity1 != entity2;
    }

    [Property(MaxTest = 200)]
    public bool Entity_DifferentTypes_WithSameId_AreNotEqual(Guid id)
    {
        var testEntity = new TestEntity(id, "Test");
        var otherEntity = new OtherEntity(id);

        return !testEntity.Equals(otherEntity);
    }

    #endregion

    #region HashCode Properties

    [Property(MaxTest = 200)]
    public bool Entity_HashCode_IsConsistent(Guid id, NonEmptyString name)
    {
        var entity = new TestEntity(id, name.Get);
        var hash1 = entity.GetHashCode();
        var hash2 = entity.GetHashCode();

        return hash1 == hash2;
    }

    [Property(MaxTest = 200)]
    public bool Entity_EqualEntities_HaveSameHashCode(Guid id, NonEmptyString name1, NonEmptyString name2)
    {
        var entity1 = new TestEntity(id, name1.Get);
        var entity2 = new TestEntity(id, name2.Get);

        if (entity1.Equals(entity2))
        {
            return entity1.GetHashCode() == entity2.GetHashCode();
        }

        return true;
    }

    #endregion

    #region Identity Properties

    [Property(MaxTest = 200)]
    public bool Entity_Id_IsPreserved(Guid id, NonEmptyString name)
    {
        var entity = new TestEntity(id, name.Get);
        return entity.Id == id;
    }

    #endregion
}

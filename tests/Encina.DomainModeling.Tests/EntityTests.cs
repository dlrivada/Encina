using Encina.DomainModeling;

namespace Encina.DomainModeling.Tests;

public class EntityTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestEntity(Guid id) : base(id) { }
    }

    private sealed class AnotherEntity : Entity<Guid>
    {
        public AnotherEntity(Guid id) : base(id) { }
    }

    [Fact]
    public void Entity_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        entity1.ShouldBe(entity2);
        entity1.Equals(entity2).ShouldBeTrue();
        (entity1 == entity2).ShouldBeTrue();
        (entity1 != entity2).ShouldBeFalse();
    }

    [Fact]
    public void Entity_WithDifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act & Assert
        entity1.ShouldNotBe(entity2);
        entity1.Equals(entity2).ShouldBeFalse();
        (entity1 == entity2).ShouldBeFalse();
        (entity1 != entity2).ShouldBeTrue();
    }

    [Fact]
    public void Entity_WithDifferentType_ShouldNotBeEqual_EvenWithSameId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new AnotherEntity(id);

        // Act & Assert
        entity1.Equals(entity2).ShouldBeFalse();
    }

    [Fact]
    public void Entity_ComparedWithNull_ShouldNotBeEqual()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert
        entity.Equals(null).ShouldBeFalse();
        (entity == null).ShouldBeFalse();
        (entity != null).ShouldBeTrue();
    }

    [Fact]
    public void Entity_SameReference_ShouldBeEqual()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert
        entity.Equals(entity).ShouldBeTrue();
#pragma warning disable CS1718 // Comparison made to same variable
        (entity == entity).ShouldBeTrue();
#pragma warning restore CS1718
    }

    [Fact]
    public void Entity_GetHashCode_ShouldBeConsistentForSameId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        entity1.GetHashCode().ShouldBe(entity2.GetHashCode());
    }

    [Fact]
    public void Entity_ToString_ShouldIncludeTypeNameAndId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);

        // Act
        var result = entity.ToString();

        // Assert
        result.ShouldContain("TestEntity");
        result.ShouldContain(id.ToString());
    }

    [Fact]
    public void Entity_Id_ShouldBeAccessible()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);

        // Act & Assert
        entity.Id.ShouldBe(id);
    }

    [Fact]
    public void Entity_DifferentProperties_ShouldStillBeEqual_WhenSameId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id) { Name = "Entity1" };
        var entity2 = new TestEntity(id) { Name = "Entity2" };

        // Act & Assert
        entity1.ShouldBe(entity2);
    }

    [Fact]
    public void NullEntities_ShouldBeEqual()
    {
        // Arrange
        TestEntity? entity1 = null;
        TestEntity? entity2 = null;

        // Act & Assert
        (entity1 == entity2).ShouldBeTrue();
        (entity1 != entity2).ShouldBeFalse();
    }
}

namespace Encina.DomainModeling.GuardTests;

/// <summary>
/// Guard tests for Entity&lt;TId&gt; to verify null parameter handling.
/// </summary>
public class EntityGuardTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }
    }

    /// <summary>
    /// Verifies that constructor properly sets the Id (reference types can't be null due to where TId : notnull constraint).
    /// </summary>
    [Fact]
    public void Constructor_WithValidId_SetsId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestEntity(id);

        // Assert
        entity.Id.ShouldBe(id);
    }

    /// <summary>
    /// Verifies that Equals handles null argument correctly.
    /// </summary>
    [Fact]
    public void Equals_NullEntity_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        TestEntity? nullEntity = null;

        // Act & Assert
        entity.Equals(nullEntity).ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that Equals(object) handles null correctly.
    /// </summary>
    [Fact]
    public void EqualsObject_NullObject_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        object? nullObject = null;

        // Act & Assert
        entity.Equals(nullObject).ShouldBeFalse();
    }

    /// <summary>
    /// Verifies equality operator with null on right side.
    /// </summary>
    [Fact]
    public void EqualityOperator_RightNull_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert
        (entity == null).ShouldBeFalse();
    }

    /// <summary>
    /// Verifies equality operator with null on left side.
    /// </summary>
    [Fact]
    public void EqualityOperator_LeftNull_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert
        (null == entity).ShouldBeFalse();
    }

    /// <summary>
    /// Verifies inequality operator with null on right side.
    /// </summary>
    [Fact]
    public void InequalityOperator_RightNull_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert
        (entity != null).ShouldBeTrue();
    }

    /// <summary>
    /// Verifies both null returns true for equality.
    /// </summary>
    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        // Arrange
        TestEntity? entity1 = null;
        TestEntity? entity2 = null;

        // Act & Assert
        (entity1 == entity2).ShouldBeTrue();
    }
}

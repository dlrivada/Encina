using Encina.Dapper.MySQL.SoftDelete;
using Encina.Messaging.SoftDelete;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.MySQL.SoftDelete;

/// <summary>
/// Guard tests for <see cref="SoftDeleteSpecificationSqlBuilder{TEntity}"/> to verify null parameter handling.
/// </summary>
public class SoftDeleteSpecificationSqlBuilderGuardTests
{
    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new SoftDeleteOptions();

        // Act & Assert
        var act = () => new SoftDeleteSpecificationSqlBuilder<TestEntity>(null!, options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("mapping");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = Substitute.For<ISoftDeleteEntityMapping<TestEntity, object>>();

        // Act & Assert
        var act = () => new SoftDeleteSpecificationSqlBuilder<TestEntity>(mapping, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var mapping = Substitute.For<ISoftDeleteEntityMapping<TestEntity, object>>();
        mapping.ColumnMappings.Returns(new Dictionary<string, string>());
        var options = new SoftDeleteOptions();

        // Act & Assert
        Should.NotThrow(() => new SoftDeleteSpecificationSqlBuilder<TestEntity>(mapping, options));
    }

    public class TestEntity
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
    }
}

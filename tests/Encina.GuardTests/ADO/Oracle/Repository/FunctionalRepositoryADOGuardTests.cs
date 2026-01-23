using System.Data;
using Encina.ADO.Oracle.Repository;

namespace Encina.GuardTests.ADO.Oracle.Repository;

/// <summary>
/// Guard tests for FunctionalRepositoryADO to verify null parameter handling.
/// </summary>
[Trait("Category", "Guard")]
public sealed class FunctionalRepositoryADOGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        var mapping = CreateTestMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryADO<FunctionalRepositoryGuardTestEntity, Guid>(connection, mapping));
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        IEntityMapping<FunctionalRepositoryGuardTestEntity, Guid> mapping = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryADO<FunctionalRepositoryGuardTestEntity, Guid>(connection, mapping));
        ex.ParamName.ShouldBe("mapping");
    }

    private static IEntityMapping<FunctionalRepositoryGuardTestEntity, Guid> CreateTestMapping()
    {
        return new EntityMappingBuilder<FunctionalRepositoryGuardTestEntity, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build();
    }
}

/// <summary>
/// Test entity for functional repository guard tests.
/// </summary>
public sealed class FunctionalRepositoryGuardTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

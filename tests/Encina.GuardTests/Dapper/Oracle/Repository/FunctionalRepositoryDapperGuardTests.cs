using System.Data;
using Encina.Dapper.Oracle.Repository;

namespace Encina.GuardTests.Dapper.Oracle.Repository;

/// <summary>
/// Guard tests for FunctionalRepositoryDapper to verify null parameter handling.
/// </summary>
[Trait("Category", "Guard")]
public sealed class FunctionalRepositoryDapperGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        var mapping = CreateTestMapping();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryDapper<GuardTestEntityDapperOracle, Guid>(connection, mapping));
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        IEntityMapping<GuardTestEntityDapperOracle, Guid> mapping = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryDapper<GuardTestEntityDapperOracle, Guid>(connection, mapping));
        ex.ParamName.ShouldBe("mapping");
    }

    private static IEntityMapping<GuardTestEntityDapperOracle, Guid> CreateTestMapping()
    {
        return new EntityMappingBuilder<GuardTestEntityDapperOracle, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build();
    }
}

/// <summary>
/// Test entity for guard tests.
/// </summary>
public sealed class GuardTestEntityDapperOracle
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

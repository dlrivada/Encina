using System.Data;
using Encina.Dapper.SqlServer.Repository;

namespace Encina.GuardTests.Dapper.SqlServer.Repository;

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
            new FunctionalRepositoryDapper<GuardTestEntityDapperSqlServer, Guid>(connection, mapping));
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        IEntityMapping<GuardTestEntityDapperSqlServer, Guid> mapping = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryDapper<GuardTestEntityDapperSqlServer, Guid>(connection, mapping));
        ex.ParamName.ShouldBe("mapping");
    }

    private static IEntityMapping<GuardTestEntityDapperSqlServer, Guid> CreateTestMapping()
    {
        return new EntityMappingBuilder<GuardTestEntityDapperSqlServer, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build()
            .ShouldBeSuccess();
    }
}

/// <summary>
/// Test entity for guard tests.
/// </summary>
public sealed class GuardTestEntityDapperSqlServer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

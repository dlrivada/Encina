using System.Data;
using Encina.Dapper.Sqlite.Repository;

namespace Encina.GuardTests.Dapper.Sqlite.Repository;

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
            new FunctionalRepositoryDapper<GuardTestEntityDapperSqlite, Guid>(connection, mapping));
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        IEntityMapping<GuardTestEntityDapperSqlite, Guid> mapping = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryDapper<GuardTestEntityDapperSqlite, Guid>(connection, mapping));
        ex.ParamName.ShouldBe("mapping");
    }

    private static IEntityMapping<GuardTestEntityDapperSqlite, Guid> CreateTestMapping()
    {
        return new EntityMappingBuilder<GuardTestEntityDapperSqlite, Guid>()
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
public sealed class GuardTestEntityDapperSqlite
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

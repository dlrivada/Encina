using System.Data;
using Encina.Dapper.PostgreSQL.Repository;

namespace Encina.GuardTests.Dapper.PostgreSQL.Repository;

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
            new FunctionalRepositoryDapper<GuardTestEntityDapperPostgreSQL, Guid>(connection, mapping));
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        IEntityMapping<GuardTestEntityDapperPostgreSQL, Guid> mapping = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryDapper<GuardTestEntityDapperPostgreSQL, Guid>(connection, mapping));
        ex.ParamName.ShouldBe("mapping");
    }

    private static IEntityMapping<GuardTestEntityDapperPostgreSQL, Guid> CreateTestMapping()
    {
        return new EntityMappingBuilder<GuardTestEntityDapperPostgreSQL, Guid>()
            .ToTable("test_entities")
            .HasId(e => e.Id, "id")
            .MapProperty(e => e.Name, "name")
            .Build();
    }
}

/// <summary>
/// Test entity for guard tests.
/// </summary>
public sealed class GuardTestEntityDapperPostgreSQL
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

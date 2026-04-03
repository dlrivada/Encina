using Encina.Dapper.MySQL.Repository;
using Encina.Dapper.MySQL.Sharding;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Execution;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.MySQL.Sharding;

/// <summary>
/// Guard tests for <see cref="FunctionalShardedRepositoryDapper{TEntity, TId}"/> to verify null parameter handling.
/// </summary>
public class FunctionalShardedRepositoryDapperGuardTests
{
    [Fact]
    public void Constructor_NullRouter_ThrowsArgumentNullException()
    {
        // Arrange
        var connectionFactory = Substitute.For<IShardedConnectionFactory>();
        var mapping = Substitute.For<IEntityMapping<TestEntity, Guid>>();
        var queryExecutor = Substitute.For<IShardedQueryExecutor>();
        var logger = Substitute.For<ILogger<FunctionalShardedRepositoryDapper<TestEntity, Guid>>>();

        // Act & Assert
        var act = () => new FunctionalShardedRepositoryDapper<TestEntity, Guid>(
            null!, connectionFactory, mapping, queryExecutor, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("router");
    }

    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var router = Substitute.For<IShardRouter<TestEntity>>();
        var mapping = Substitute.For<IEntityMapping<TestEntity, Guid>>();
        var queryExecutor = Substitute.For<IShardedQueryExecutor>();
        var logger = Substitute.For<ILogger<FunctionalShardedRepositoryDapper<TestEntity, Guid>>>();

        // Act & Assert
        var act = () => new FunctionalShardedRepositoryDapper<TestEntity, Guid>(
            router, null!, mapping, queryExecutor, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connectionFactory");
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var router = Substitute.For<IShardRouter<TestEntity>>();
        var connectionFactory = Substitute.For<IShardedConnectionFactory>();
        var queryExecutor = Substitute.For<IShardedQueryExecutor>();
        var logger = Substitute.For<ILogger<FunctionalShardedRepositoryDapper<TestEntity, Guid>>>();

        // Act & Assert
        var act = () => new FunctionalShardedRepositoryDapper<TestEntity, Guid>(
            router, connectionFactory, null!, queryExecutor, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("mapping");
    }

    [Fact]
    public void Constructor_NullQueryExecutor_ThrowsArgumentNullException()
    {
        // Arrange
        var router = Substitute.For<IShardRouter<TestEntity>>();
        var connectionFactory = Substitute.For<IShardedConnectionFactory>();
        var mapping = Substitute.For<IEntityMapping<TestEntity, Guid>>();
        var logger = Substitute.For<ILogger<FunctionalShardedRepositoryDapper<TestEntity, Guid>>>();

        // Act & Assert
        var act = () => new FunctionalShardedRepositoryDapper<TestEntity, Guid>(
            router, connectionFactory, mapping, null!, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("queryExecutor");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var router = Substitute.For<IShardRouter<TestEntity>>();
        var connectionFactory = Substitute.For<IShardedConnectionFactory>();
        var mapping = Substitute.For<IEntityMapping<TestEntity, Guid>>();
        var queryExecutor = Substitute.For<IShardedQueryExecutor>();

        // Act & Assert
        var act = () => new FunctionalShardedRepositoryDapper<TestEntity, Guid>(
            router, connectionFactory, mapping, queryExecutor, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var router = Substitute.For<IShardRouter<TestEntity>>();
        var connectionFactory = Substitute.For<IShardedConnectionFactory>();
        var mapping = Substitute.For<IEntityMapping<TestEntity, Guid>>();
        var queryExecutor = Substitute.For<IShardedQueryExecutor>();
        var logger = Substitute.For<ILogger<FunctionalShardedRepositoryDapper<TestEntity, Guid>>>();

        // Act & Assert
        Should.NotThrow(() => new FunctionalShardedRepositoryDapper<TestEntity, Guid>(
            router, connectionFactory, mapping, queryExecutor, logger));
    }

    public class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

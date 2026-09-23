using Microsoft.EntityFrameworkCore;
using AdoMySql = Encina.ADO.MySQL.ServiceCollectionExtensions;
using AdoMySqlSharding = Encina.ADO.MySQL.Sharding.ShardingServiceCollectionExtensions;
using AdoPostgreSql = Encina.ADO.PostgreSQL.ServiceCollectionExtensions;
using AdoPostgreSqlSharding = Encina.ADO.PostgreSQL.Sharding.ShardingServiceCollectionExtensions;
using AdoSqlServer = Encina.ADO.SqlServer.ServiceCollectionExtensions;
using AdoSqlServerSharding = Encina.ADO.SqlServer.Sharding.ShardingServiceCollectionExtensions;
using DapperMySql = Encina.Dapper.MySQL.ServiceCollectionExtensions;
using DapperMySqlSharding = Encina.Dapper.MySQL.Sharding.ShardingServiceCollectionExtensions;
using DapperPostgreSql = Encina.Dapper.PostgreSQL.ServiceCollectionExtensions;
using DapperPostgreSqlSharding = Encina.Dapper.PostgreSQL.Sharding.ShardingServiceCollectionExtensions;
using DapperSqlServer = Encina.Dapper.SqlServer.ServiceCollectionExtensions;
using DapperSqlServerSharding = Encina.Dapper.SqlServer.Sharding.ShardingServiceCollectionExtensions;
using EFCore = Encina.EntityFrameworkCore.ServiceCollectionExtensions;
using MongoDb = Encina.MongoDB.ServiceCollectionExtensions;
using MongoDbSharding = Encina.MongoDB.Sharding.ShardingServiceCollectionExtensions;

namespace Encina.UnitTests.Core;

/// <summary>
/// Verifies that <see cref="IRequestContextAccessor"/> resolves when a host wires only a single
/// provider or sharding extension package, without the core mediator's <c>AddEncina()</c>.
/// </summary>
/// <remarks>
/// Before #1163, several consumer sites read <c>IRequestContextAccessor</c> via <c>GetService</c>
/// but only the shared messaging helper (<c>AddMessagingServices</c>) registered it. A host that
/// wired <c>Encina.MongoDB</c>, <c>Encina.EntityFrameworkCore</c> or a sharding extension alone
/// silently got a null request context at every consumer site instead of an ambient one. Each
/// provider now calls <c>services.TryAddSingleton&lt;IRequestContextAccessor, RequestContextAccessor&gt;()</c>
/// itself.
/// </remarks>
public sealed class RequestContextAccessorRegistrationTests
{
    private sealed class TestEntity
    {
        public Guid Id { get; set; }
    }

    #region ADO.NET

    [Theory]
    [MemberData(nameof(AdoCases))]
    public void AddEncinaADO_Alone_RegistersRequestContextAccessor(
        Action<IServiceCollection> register)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<System.Data.IDbConnection>());

        // Act
        register(services);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRequestContextAccessor>().ShouldNotBeNull();
    }

    public static IEnumerable<object[]> AdoCases()
    {
        yield return new object[] { (Action<IServiceCollection>)(services => AdoSqlServer.AddEncinaADO(services, _ => { })) };
        yield return new object[] { (Action<IServiceCollection>)(services => AdoPostgreSql.AddEncinaADO(services, _ => { })) };
        yield return new object[] { (Action<IServiceCollection>)(services => AdoMySql.AddEncinaADO(services, _ => { })) };
    }

    #endregion

    #region Dapper

    [Theory]
    [MemberData(nameof(DapperCases))]
    public void AddEncinaDapper_Alone_RegistersRequestContextAccessor(
        Action<IServiceCollection> register)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<System.Data.IDbConnection>());

        // Act
        register(services);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRequestContextAccessor>().ShouldNotBeNull();
    }

    public static IEnumerable<object[]> DapperCases()
    {
        yield return new object[] { (Action<IServiceCollection>)(services => DapperSqlServer.AddEncinaDapper(services, _ => { })) };
        yield return new object[] { (Action<IServiceCollection>)(services => DapperPostgreSql.AddEncinaDapper(services, _ => { })) };
        yield return new object[] { (Action<IServiceCollection>)(services => DapperMySql.AddEncinaDapper(services, _ => { })) };
    }

    #endregion

    #region EF Core

    [Fact]
    public void AddEncinaEntityFrameworkCore_Alone_RegistersRequestContextAccessor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase("RequestContextAccessorTests"));

        // Act
        EFCore.AddEncinaEntityFrameworkCore<TestDbContext>(services);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRequestContextAccessor>().ShouldNotBeNull();
    }

    #endregion

    #region MongoDB

    [Fact]
    public void AddEncinaMongoDB_Alone_RegistersRequestContextAccessor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        MongoDb.AddEncinaMongoDB(services, options =>
        {
            options.ConnectionString = "mongodb://localhost:27017";
            options.DatabaseName = "encina-test";
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRequestContextAccessor>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaMongoDBSharding_Alone_RegistersRequestContextAccessor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        MongoDbSharding.AddEncinaMongoDBSharding<TestEntity, Guid>(services, options =>
        {
            options.CollectionName = "test-entities";
            options.IdProperty = e => e.Id;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRequestContextAccessor>().ShouldNotBeNull();
    }

    #endregion

    #region Sharding extensions (ADO.NET / Dapper)

    [Theory]
    [MemberData(nameof(AdoShardingCases))]
    public void AddEncinaADOSharding_Alone_RegistersRequestContextAccessor(
        Action<IServiceCollection> register)
    {
        // Arrange: the accessor is registered at extension-call time, independent of the core
        // AddEncinaSharding<TEntity> topology registration, so it is not needed here.
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        register(services);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRequestContextAccessor>().ShouldNotBeNull();
    }

    public static IEnumerable<object[]> AdoShardingCases()
    {
        yield return new object[]
        {
            (Action<IServiceCollection>)(services =>
                AdoSqlServerSharding.AddEncinaADOSharding<TestEntity, Guid>(services, m => m.ToTable("TestEntities").HasId(e => e.Id)))
        };
        yield return new object[]
        {
            (Action<IServiceCollection>)(services =>
                AdoPostgreSqlSharding.AddEncinaADOSharding<TestEntity, Guid>(services, m => m.ToTable("TestEntities").HasId(e => e.Id)))
        };
        yield return new object[]
        {
            (Action<IServiceCollection>)(services =>
                AdoMySqlSharding.AddEncinaADOSharding<TestEntity, Guid>(services, m => m.ToTable("TestEntities").HasId(e => e.Id)))
        };
    }

    [Theory]
    [MemberData(nameof(DapperShardingCases))]
    public void AddEncinaDapperSharding_Alone_RegistersRequestContextAccessor(
        Action<IServiceCollection> register)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        register(services);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRequestContextAccessor>().ShouldNotBeNull();
    }

    public static IEnumerable<object[]> DapperShardingCases()
    {
        yield return new object[]
        {
            (Action<IServiceCollection>)(services =>
                DapperSqlServerSharding.AddEncinaDapperSharding<TestEntity, Guid>(services, m => m.ToTable("TestEntities").HasId(e => e.Id)))
        };
        yield return new object[]
        {
            (Action<IServiceCollection>)(services =>
                DapperPostgreSqlSharding.AddEncinaDapperSharding<TestEntity, Guid>(services, m => m.ToTable("TestEntities").HasId(e => e.Id)))
        };
        yield return new object[]
        {
            (Action<IServiceCollection>)(services =>
                DapperMySqlSharding.AddEncinaDapperSharding<TestEntity, Guid>(services, m => m.ToTable("TestEntities").HasId(e => e.Id)))
        };
    }

    #endregion

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}

using Encina.EntityFrameworkCore.Sharding;
using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Data;
using Encina.Sharding.Execution;
using Encina.Sharding.ReferenceTables;
using Encina.Sharding.ReplicaSelection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardingServiceCollectionExtensions"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ShardingServiceCollectionExtensionsTests
{
    #region AddEncinaEFCoreShardingSqlServer

    [Fact]
    public void AddEncinaEFCoreShardingSqlServer_RegistersShardedDbContextFactory()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardingSqlServer<TestDbContext, TestShardEntity, Guid>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ShardedDbContextFactory<TestDbContext>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaEFCoreShardingSqlServer_RegistersIShardedDbContextFactory()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardingSqlServer<TestDbContext, TestShardEntity, Guid>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IShardedDbContextFactory<TestDbContext>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaEFCoreShardingSqlServer_RegistersIShardedQueryExecutor()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardingSqlServer<TestDbContext, TestShardEntity, Guid>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IShardedQueryExecutor));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaEFCoreShardingSqlServer_RegistersFunctionalShardedRepository()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardingSqlServer<TestDbContext, TestShardEntity, Guid>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IFunctionalShardedRepository<TestShardEntity, Guid>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaEFCoreShardingSqlServer_ReturnsServiceCollection()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        var result = services.AddEncinaEFCoreShardingSqlServer<TestDbContext, TestShardEntity, Guid>();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaEFCoreShardingSqlServer_RegistersTimeProvider()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardingSqlServer<TestDbContext, TestShardEntity, Guid>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(TimeProvider));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    #endregion

    #region AddEncinaEFCoreShardingPostgreSql

    [Fact]
    public void AddEncinaEFCoreShardingPostgreSql_RegistersShardedDbContextFactory()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardingPostgreSql<TestDbContext, TestShardEntity, Guid>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ShardedDbContextFactory<TestDbContext>));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEFCoreShardingPostgreSql_ReturnsServiceCollection()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        var result = services.AddEncinaEFCoreShardingPostgreSql<TestDbContext, TestShardEntity, Guid>();

        // Assert
        result.ShouldBeSameAs(services);
    }

    #endregion

    #region AddEncinaEFCoreShardingMySql

    [Fact]
    public void AddEncinaEFCoreShardingMySql_WithNullConfigureProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaEFCoreShardingMySql<TestDbContext, TestShardEntity, Guid>(null!));
    }

    [Fact]
    public void AddEncinaEFCoreShardingMySql_WithValidDelegate_RegistersFactory()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardingMySql<TestDbContext, TestShardEntity, Guid>(
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ShardedDbContextFactory<TestDbContext>));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEFCoreShardingMySql_ReturnsServiceCollection()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        var result = services.AddEncinaEFCoreShardingMySql<TestDbContext, TestShardEntity, Guid>(
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        // Assert
        result.ShouldBeSameAs(services);
    }

    #endregion

    #region AddEncinaEFCoreShardingSqlite

    [Fact]
    public void AddEncinaEFCoreShardingSqlite_RegistersShardedDbContextFactory()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardingSqlite<TestDbContext, TestShardEntity, Guid>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ShardedDbContextFactory<TestDbContext>));
        descriptor.ShouldNotBeNull();
    }

    #endregion

    #region AddEncinaEFCoreShardedReadWrite (SqlServer, PostgreSql, MySql, Sqlite)

    [Fact]
    public void AddEncinaEFCoreShardedReadWriteSqlServer_RegistersReadWriteFactory()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardedReadWriteSqlServer<TestDbContext>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IShardedReadWriteDbContextFactory<TestDbContext>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaEFCoreShardedReadWriteSqlServer_RegistersShardedReadWriteOptions()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardedReadWriteSqlServer<TestDbContext>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ShardedReadWriteOptions));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaEFCoreShardedReadWriteSqlServer_RegistersReplicaHealthTracker()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardedReadWriteSqlServer<TestDbContext>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IReplicaHealthTracker));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaEFCoreShardedReadWriteSqlServer_WithConfigure_AppliesOptions()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardedReadWriteSqlServer<TestDbContext>(options =>
        {
            options.FallbackToPrimaryWhenNoReplicas = false;
            options.DefaultReplicaStrategy = ReplicaSelectionStrategy.Random;
        });

        // Assert - verify options are registered
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ShardedReadWriteOptions));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEFCoreShardedReadWritePostgreSql_RegistersReadWriteFactory()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardedReadWritePostgreSql<TestDbContext>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IShardedReadWriteDbContextFactory<TestDbContext>));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEFCoreShardedReadWriteMySql_WithNullConfigureProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaEFCoreShardedReadWriteMySql<TestDbContext>(null!));
    }

    [Fact]
    public void AddEncinaEFCoreShardedReadWriteMySql_WithValidDelegate_RegistersFactory()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardedReadWriteMySql<TestDbContext>(
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IShardedReadWriteDbContextFactory<TestDbContext>));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEFCoreShardedReadWriteSqlite_RegistersReadWriteFactory()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardedReadWriteSqlite<TestDbContext>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IShardedReadWriteDbContextFactory<TestDbContext>));
        descriptor.ShouldNotBeNull();
    }

    #endregion

    #region AddEncinaEFCoreReferenceTableStore

    [Fact]
    public void AddEncinaEFCoreReferenceTableStoreSqlServer_RegistersFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaEFCoreReferenceTableStoreSqlServer<TestDbContext>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IReferenceTableStoreFactory));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaEFCoreReferenceTableStorePostgreSql_RegistersFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaEFCoreReferenceTableStorePostgreSql<TestDbContext>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IReferenceTableStoreFactory));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEFCoreReferenceTableStoreMySql_WithNullConfigureProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaEFCoreReferenceTableStoreMySql<TestDbContext>(null!));
    }

    [Fact]
    public void AddEncinaEFCoreReferenceTableStoreMySql_WithValidDelegate_RegistersFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaEFCoreReferenceTableStoreMySql<TestDbContext>(
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IReferenceTableStoreFactory));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEFCoreReferenceTableStoreSqlite_RegistersFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaEFCoreReferenceTableStoreSqlite<TestDbContext>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IReferenceTableStoreFactory));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEFCoreReferenceTableStoreSqlServer_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaEFCoreReferenceTableStoreSqlServer<TestDbContext>();

        // Assert
        result.ShouldBeSameAs(services);
    }

    #endregion

    #region TryAdd Idempotency

    [Fact]
    public void AddEncinaEFCoreShardingSqlServer_CalledTwice_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardingSqlServer<TestDbContext, TestShardEntity, Guid>();
        services.AddEncinaEFCoreShardingSqlServer<TestDbContext, TestShardEntity, Guid>();

        // Assert - TryAdd should prevent duplicates
        var factoryDescriptors = services.Where(d =>
            d.ServiceType == typeof(ShardedDbContextFactory<TestDbContext>)).ToList();
        factoryDescriptors.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaEFCoreShardedReadWriteSqlServer_CalledTwice_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = CreateServicesWithShardingDependencies();

        // Act
        services.AddEncinaEFCoreShardedReadWriteSqlServer<TestDbContext>();
        services.AddEncinaEFCoreShardedReadWriteSqlServer<TestDbContext>();

        // Assert
        var factoryDescriptors = services.Where(d =>
            d.ServiceType == typeof(IShardedReadWriteDbContextFactory<TestDbContext>)).ToList();
        factoryDescriptors.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaEFCoreReferenceTableStoreSqlServer_CalledTwice_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaEFCoreReferenceTableStoreSqlServer<TestDbContext>();
        services.AddEncinaEFCoreReferenceTableStoreSqlServer<TestDbContext>();

        // Assert
        var descriptors = services.Where(d =>
            d.ServiceType == typeof(IReferenceTableStoreFactory)).ToList();
        descriptors.Count.ShouldBe(1);
    }

    #endregion

    #region Helpers

    private static ServiceCollection CreateServicesWithShardingDependencies()
    {
        var services = new ServiceCollection();

        // Register core sharding dependencies that the extension methods expect
        var shards = new[]
        {
            new ShardInfo("shard-0", "Server=shard-0;Database=test;"),
            new ShardInfo("shard-1", "Server=shard-1;Database=test;")
        };
        var topology = new ShardTopology(shards);

        services.AddSingleton(topology);
        services.AddSingleton<IShardRouter>(new StubShardRouter(topology));
        services.AddSingleton<IShardRouter<TestShardEntity>>(new StubShardRouter<TestShardEntity>(topology));
        services.Configure<ScatterGatherOptions>(_ => { });
        services.AddLogging();

        return services;
    }

    private sealed class TestShardEntity
    {
        [ShardKey]
        public string TenantId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Minimal IShardRouter stub for DI registration tests.
    /// </summary>
    private sealed class StubShardRouter(ShardTopology topology) : IShardRouter
    {
        public LanguageExt.Either<EncinaError, string> GetShardId(string shardKey) =>
            LanguageExt.Either<EncinaError, string>.Right("shard-0");

        public IReadOnlyList<string> GetAllShardIds() => topology.AllShardIds;

        public LanguageExt.Either<EncinaError, string> GetShardConnectionString(string shardId) =>
            topology.GetConnectionString(shardId);
    }

    private sealed class StubShardRouter<TEntity>(ShardTopology topology) : IShardRouter<TEntity>
        where TEntity : notnull
    {
        public LanguageExt.Either<EncinaError, string> GetShardId(TEntity entity) =>
            LanguageExt.Either<EncinaError, string>.Right("shard-0");

        public LanguageExt.Either<EncinaError, IReadOnlyList<string>> GetShardIds(TEntity entity) =>
            LanguageExt.Either<EncinaError, IReadOnlyList<string>>.Right(topology.AllShardIds);

        public LanguageExt.Either<EncinaError, string> GetShardId(string shardKey) =>
            LanguageExt.Either<EncinaError, string>.Right("shard-0");

        public IReadOnlyList<string> GetAllShardIds() => topology.AllShardIds;

        public LanguageExt.Either<EncinaError, string> GetShardConnectionString(string shardId) =>
            topology.GetConnectionString(shardId);
    }

    #endregion
}

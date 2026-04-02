using Encina.Compliance.Anonymization;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.MongoDB;
using Encina.MongoDB.ReadWriteSeparation;
using Encina.Security.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.MongoDB;

/// <summary>
/// Extended unit tests for MongoDB <see cref="ServiceCollectionExtensions"/>
/// covering additional DI registration branches.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "MongoDB")]
public sealed class ServiceCollectionExtensionsExtendedTests
{
    #region AddEncinaMongoDB with client overload

    [Fact]
    public void AddEncinaMongoDB_WithClient_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        var client = Substitute.For<IMongoClient>();

        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaMongoDB(client, opts => { }));
    }

    [Fact]
    public void AddEncinaMongoDB_WithClient_NullClient_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaMongoDB(null!, opts => { }));
    }

    [Fact]
    public void AddEncinaMongoDB_WithClient_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var client = Substitute.For<IMongoClient>();

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaMongoDB(client, null!));
    }

    [Fact]
    public void AddEncinaMongoDB_WithClient_ReturnsSameCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var client = Substitute.For<IMongoClient>();

        var result = services.AddEncinaMongoDB(client, opts =>
        {
            opts.DatabaseName = "test";
        });

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaMongoDB_WithClient_RegistersClientAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var client = Substitute.For<IMongoClient>();

        services.AddEncinaMongoDB(client, opts =>
        {
            opts.DatabaseName = "test";
        });

        var sp = services.BuildServiceProvider();
        sp.GetService<IMongoClient>().ShouldBeSameAs(client);
    }

    #endregion

    #region Outbox registration

    [Fact]
    public void AddEncinaMongoDB_UseOutboxTrue_RegistersOutboxStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseOutbox = true;
        });

        services.ShouldContain(sd => sd.ServiceType == typeof(IOutboxStore));
        services.ShouldContain(sd => sd.ServiceType == typeof(IOutboxMessageFactory));
    }

    [Fact]
    public void AddEncinaMongoDB_UseOutboxFalse_DoesNotRegisterOutboxStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseOutbox = false;
        });

        services.ShouldNotContain(sd => sd.ServiceType == typeof(IOutboxStore));
    }

    #endregion

    #region Inbox registration

    [Fact]
    public void AddEncinaMongoDB_UseInboxTrue_RegistersInboxStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseInbox = true;
        });

        services.ShouldContain(sd => sd.ServiceType == typeof(IInboxStore));
        services.ShouldContain(sd => sd.ServiceType == typeof(IInboxMessageFactory));
    }

    #endregion

    #region Saga registration

    [Fact]
    public void AddEncinaMongoDB_UseSagasTrue_RegistersSagaStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseSagas = true;
        });

        services.ShouldContain(sd => sd.ServiceType == typeof(ISagaStore));
        services.ShouldContain(sd => sd.ServiceType == typeof(ISagaStateFactory));
    }

    #endregion

    #region Scheduling registration

    [Fact]
    public void AddEncinaMongoDB_UseSchedulingTrue_RegistersScheduledMessageStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseScheduling = true;
        });

        services.ShouldContain(sd => sd.ServiceType == typeof(IScheduledMessageStore));
        services.ShouldContain(sd => sd.ServiceType == typeof(IScheduledMessageFactory));
    }

    #endregion

    #region Audit registration

    [Fact]
    public void AddEncinaMongoDB_UseAuditLogStoreTrue_RegistersAuditLogStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseAuditLogStore = true;
        });

        services.ShouldContain(sd => sd.ServiceType == typeof(IAuditLogStore));
    }

    [Fact]
    public void AddEncinaMongoDB_UseReadAuditStoreTrue_RegistersReadAuditStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseReadAuditStore = true;
        });

        services.ShouldContain(sd => sd.ServiceType == typeof(IReadAuditStore));
    }

    #endregion

    #region Anonymization registration

    [Fact]
    public void AddEncinaMongoDB_UseAnonymizationTrue_RegistersTokenMappingStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseAnonymization = true;
        });

        services.ShouldContain(sd => sd.ServiceType == typeof(ITokenMappingStore));
    }

    #endregion

    #region Health check registration

    [Fact]
    public void AddEncinaMongoDB_HealthCheckEnabled_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.ProviderHealthCheck.Enabled = true;
        });

        services.ShouldContain(sd => sd.ServiceType == typeof(IEncinaHealthCheck));
    }

    #endregion

    #region Unit of Work registration

    [Fact]
    public void AddEncinaUnitOfWork_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;

        Should.Throw<ArgumentNullException>(() => services!.AddEncinaUnitOfWork());
    }

    [Fact]
    public void AddEncinaUnitOfWork_ReturnsSameCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaUnitOfWork();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaUnitOfWork_RegistersUnitOfWork()
    {
        var services = new ServiceCollection();

        services.AddEncinaUnitOfWork();

        services.ShouldContain(sd => sd.ServiceType == typeof(IUnitOfWork));
    }

    #endregion

    #region Bulk Operations registration

    [Fact]
    public void AddEncinaBulkOperations_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;

        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaBulkOperations<TestEntity, Guid>());
    }

    [Fact]
    public void AddEncinaBulkOperations_ReturnsSameCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaBulkOperations<TestEntity, Guid>();

        result.ShouldBeSameAs(services);
    }

    #endregion

    #region Processing Activity registration

    [Fact]
    public void AddEncinaProcessingActivityMongoDB_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;

        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaProcessingActivityMongoDB("mongodb://localhost"));
    }

    [Fact]
    public void AddEncinaProcessingActivityMongoDB_NullConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentException>(() =>
            services.AddEncinaProcessingActivityMongoDB(null!));
    }

    [Fact]
    public void AddEncinaProcessingActivityMongoDB_EmptyConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentException>(() =>
            services.AddEncinaProcessingActivityMongoDB(""));
    }

    [Fact]
    public void AddEncinaProcessingActivityMongoDB_WhitespaceConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentException>(() =>
            services.AddEncinaProcessingActivityMongoDB("   "));
    }

    [Fact]
    public void AddEncinaProcessingActivityMongoDB_NullDatabaseName_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentException>(() =>
            services.AddEncinaProcessingActivityMongoDB("mongodb://localhost", null!));
    }

    // Note: AddEncinaProcessingActivityMongoDB with valid args cannot be tested
    // without a real MongoDB connection since the constructor creates indexes eagerly.

    #endregion

    #region ReadWrite Separation registration

    [Fact]
    public void AddEncinaMongoDB_UseReadWriteSeparation_RegistersReadWriteServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseReadWriteSeparation = true;
        });

        services.ShouldContain(sd => sd.ServiceType == typeof(IReadWriteMongoCollectionFactory));
        services.ShouldContain(sd =>
            sd.ServiceType.IsGenericType &&
            sd.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));
    }

    #endregion

    #region Module Isolation registration

    [Fact]
    public void AddEncinaMongoDB_UseModuleIsolation_RegistersModuleServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseModuleIsolation = true;
        });

        services.ShouldContain(sd =>
            sd.ServiceType == typeof(global::Encina.MongoDB.Modules.IModuleAwareMongoCollectionFactory));
    }

    #endregion

    #region EncinaMongoDbOptions ToString

    [Fact]
    public void EncinaMongoDbOptions_ToString_ContainsDatabaseName()
    {
        var options = new EncinaMongoDbOptions { DatabaseName = "TestDb" };

        var result = options.ToString();

        result.ShouldContain("TestDb");
    }

    #endregion

    #region ReadWrite separation options

    [Fact]
    public void EncinaMongoDbOptions_ReadWriteSeparation_DefaultValues()
    {
        var options = new EncinaMongoDbOptions();

        options.UseReadWriteSeparation.ShouldBeFalse();
        options.ReadWriteSeparationOptions.ShouldNotBeNull();
    }

    #endregion

    #region Test entities

    public sealed class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}

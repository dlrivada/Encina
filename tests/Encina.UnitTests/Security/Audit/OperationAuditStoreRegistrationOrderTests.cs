using System.Data;
using Encina.ADO.SqlServer;
using Encina.OpenTelemetry;
using Encina.OpenTelemetry.Audit;
using Encina.Security.Audit;
using DbOperationAuditStore = Encina.ADO.SqlServer.Auditing.OperationAuditStoreADO;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Verifies <see cref="OperationAuditStoreRegistration"/>: a database provider replaces the in-memory audit
/// default even when the OpenTelemetry instrumentation wrapped it, in every registration order, and never
/// removes a store the application registered itself (#1633, #1269).
/// </summary>
public sealed class OperationAuditStoreRegistrationOrderTests
{
    private static ServiceProvider Build(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IDbConnection>());
        return services;
    }

    private static IOperationAuditStore Resolve(IServiceCollection services)
    {
        var provider = Build(services);
        var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOperationAuditStore>();
    }

    [Fact]
    public void AuditThenOpenTelemetryThenDatabaseProvider_ResolvesDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act: OpenTelemetry wraps the in-memory default before the provider runs
        services.AddEncinaAudit();
        services.AddEncinaOpenTelemetry();
        services.AddEncinaADO(config => config.UseOperationAuditStore = true);

        // Assert
        Resolve(services).ShouldBeOfType<DbOperationAuditStore>();
    }

    [Fact]
    public void OpenTelemetryThenAuditThenDatabaseProvider_ResolvesDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaOpenTelemetry();
        services.AddEncinaAudit();
        services.AddEncinaADO(config => config.UseOperationAuditStore = true);

        // Assert
        Resolve(services).ShouldBeOfType<DbOperationAuditStore>();
    }

    [Fact]
    public void DatabaseProviderThenAuditThenOpenTelemetry_ResolvesInstrumentedDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act: the provider registered first, so OpenTelemetry wraps the database store
        services.AddEncinaADO(config => config.UseOperationAuditStore = true);
        services.AddEncinaAudit();
        services.AddEncinaOpenTelemetry();

        // Assert
        Resolve(services).ShouldBeOfType<InstrumentedOperationAuditStore>();
    }

    [Fact]
    public void DatabaseProviderThenOpenTelemetryThenAudit_ResolvesInstrumentedDatabaseStore()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaADO(config => config.UseOperationAuditStore = true);
        services.AddEncinaOpenTelemetry();
        services.AddEncinaAudit();

        // Assert: the in-memory default is added only if no store is registered, so the database store stays
        Resolve(services).ShouldBeOfType<InstrumentedOperationAuditStore>();
    }

    [Fact]
    public void AuditThenOpenTelemetryThenDatabaseProvider_WithAutoPurge_BuildsTheRetentionService()
    {
        // Arrange
        var services = NewServices();

        // Act
        services.AddEncinaAudit(options => options.EnableAutoPurge = true);
        services.AddEncinaOpenTelemetry();
        services.AddEncinaADO(config => config.UseOperationAuditStore = true);

        // Assert
        using var provider = Build(services);
        provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<OperationAuditRetentionService>()
            .ShouldHaveSingleItem();
    }

    [Fact]
    public void ApplicationStore_WrappedByOpenTelemetry_IsNeverRemovedByTheProvider()
    {
        // Arrange
        var services = NewServices();
        var customStore = Substitute.For<IOperationAuditStore>();
        services.AddSingleton(customStore);

        // Act
        services.AddEncinaAudit();
        services.AddEncinaOpenTelemetry();
        services.AddEncinaADO(config => config.UseOperationAuditStore = true);

        // Assert: still the application's store (instrumented), not the database store
        Resolve(services).ShouldBeOfType<InstrumentedOperationAuditStore>();
        services.Count(d => d.ServiceType == typeof(IOperationAuditStore)).ShouldBe(1);
    }

    [Fact]
    public void IsInMemoryDefault_PlainInMemoryRegistration_ReturnsTrue()
    {
        var descriptor = ServiceDescriptor.Singleton<IOperationAuditStore, InMemoryOperationAuditStore>();

        OperationAuditStoreRegistration.IsInMemoryDefault(descriptor).ShouldBeTrue();
    }

    [Fact]
    public void IsInMemoryDefault_NestedDecorators_LooksThroughEveryLayer()
    {
        var inMemory = ServiceDescriptor.Singleton<IOperationAuditStore, InMemoryOperationAuditStore>();
        var once = Decorate(inMemory);
        var twice = Decorate(once);

        OperationAuditStoreRegistration.IsInMemoryDefault(twice).ShouldBeTrue();
    }

    [Fact]
    public void IsInMemoryDefault_DecoratedApplicationStore_ReturnsFalse()
    {
        var custom = ServiceDescriptor.Singleton<IOperationAuditStore>(Substitute.For<IOperationAuditStore>());

        OperationAuditStoreRegistration.IsInMemoryDefault(Decorate(custom)).ShouldBeFalse();
    }

    [Fact]
    public void IsInMemoryDefault_PlainFactoryRegistration_ReturnsFalse()
    {
        var descriptor = ServiceDescriptor.Singleton<IOperationAuditStore>(_ => Substitute.For<IOperationAuditStore>());

        OperationAuditStoreRegistration.IsInMemoryDefault(descriptor).ShouldBeFalse();
    }

    [Fact]
    public void IsInMemoryDefault_OtherServiceType_ReturnsFalse()
    {
        var descriptor = ServiceDescriptor.Singleton<IReadAuditStore, InMemoryReadAuditStore>();

        OperationAuditStoreRegistration.IsInMemoryDefault(descriptor).ShouldBeFalse();
    }

    [Fact]
    public void RemoveInMemoryDefault_RemovesOnlyTheInMemoryDefault()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOperationAuditStore, InMemoryOperationAuditStore>();
        services.AddSingleton<IReadAuditStore, InMemoryReadAuditStore>();
        var custom = Substitute.For<IOperationAuditStore>();
        services.AddSingleton(custom);

        OperationAuditStoreRegistration.RemoveInMemoryDefault(services);

        services.Count(d => d.ServiceType == typeof(IOperationAuditStore)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IReadAuditStore)).ShouldBe(1);
        services.Single(d => d.ServiceType == typeof(IOperationAuditStore)).ImplementationInstance.ShouldBeSameAs(custom);
    }

    [Fact]
    public void Guards_RejectNull()
    {
        Should.Throw<ArgumentNullException>(() => OperationAuditStoreRegistration.RemoveInMemoryDefault(null!));
        Should.Throw<ArgumentNullException>(() => OperationAuditStoreRegistration.IsInMemoryDefault(null!));
    }

    private static ServiceDescriptor Decorate(ServiceDescriptor decorated)
    {
        var decorating = new Decorating(decorated);
        return ServiceDescriptor.Describe(typeof(IOperationAuditStore), decorating.Create, decorated.Lifetime);
    }

    private sealed class Decorating(ServiceDescriptor decorated) : IDecoratedServiceFactory
    {
        public ServiceDescriptor Decorated { get; } = decorated;

        public object Create(IServiceProvider sp) => throw new NotSupportedException();
    }
}

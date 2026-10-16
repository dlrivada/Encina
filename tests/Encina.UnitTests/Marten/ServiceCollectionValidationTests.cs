using Encina.DomainModeling;
using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Marten.Snapshots;
using Encina.Marten.Versioning;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Marten;

/// <summary>
/// Registration-completeness tests (#1348): the Encina.Marten registrations build with
/// <c>ValidateOnBuild</c> and <c>ValidateScopes</c>, so a dependency that the registered
/// services need but no registration supplies fails here instead of at first resolution.
/// </summary>
public class ServiceCollectionValidationTests
{
    private static readonly ServiceProviderOptions Validation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    [Fact]
    public void AddEncinaMarten_WithEveryOptionalFeatureEnabled_BuildsWithValidateOnBuildAndValidateScopes()
    {
        var services = CreateServicesWithHostDependencies();
        services.AddEncinaMarten(options =>
        {
            options.ProviderHealthCheck.Enabled = true;
            options.Projections.Enabled = true;
            options.Snapshots.Enabled = true;
            options.EventVersioning.Enabled = true;
            options.Metadata.CorrelationIdEnabled = true;
            options.Metadata.CausationIdEnabled = true;
        });
        services.AddAggregateRepository<ValidationAggregate>();
        services.AddProjection<ValidationProjection, ValidationReadModel>();
        services.AddEventUpcaster<ValidationUpcaster>();

        using var provider = services.BuildServiceProvider(Validation);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IAggregateRepository<ValidationAggregate>>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IReadModelRepository<ValidationReadModel>>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IInlineProjectionDispatcher>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<ISnapshotStore<ValidationSnapshotAggregate>>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IEventMetadataQuery>().ShouldNotBeNull();
        provider.GetRequiredService<IProjectionManager>().ShouldNotBeNull();
    }

    [Fact]
    public void AddSnapshotableAggregate_BuildsWithValidateOnBuildAndValidateScopes()
    {
        var services = CreateServicesWithHostDependencies();
        services.AddEncinaMarten(options => options.Snapshots.Enabled = true);
        services.AddSnapshotableAggregate<ValidationSnapshotAggregate>();

        using var provider = services.BuildServiceProvider(Validation);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IAggregateRepository<ValidationSnapshotAggregate>>()
            .ShouldBeOfType<SnapshotAwareAggregateRepository<ValidationSnapshotAggregate>>();
        scope.ServiceProvider.GetRequiredService<ISnapshotStore<ValidationSnapshotAggregate>>()
            .ShouldBeOfType<MartenSnapshotStore<ValidationSnapshotAggregate>>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddEventUpcaster_BuildsWithValidateOnBuildAndValidateScopes(bool versioningEnabled)
    {
        var services = CreateServicesWithHostDependencies();
        services.AddEncinaMarten(options => options.EventVersioning.Enabled = versioningEnabled);
        services.AddEventUpcaster<ValidationUpcaster>();

        using var provider = services.BuildServiceProvider(Validation);

        provider.GetServices<IEventUpcasterRegistrar>().ShouldNotBeEmpty();
        if (versioningEnabled)
        {
            provider.GetRequiredService<EventUpcasterRegistry>().ShouldNotBeNull();
        }
    }

    [Fact]
    public void AddEventUpcaster_ThroughOptionsAndAssemblyScan_ConfiguresMartenOnce()
    {
        var services = CreateServicesWithHostDependencies();
        services.AddEncinaMarten(options =>
        {
            options.EventVersioning.Enabled = true;
            options.EventVersioning.AddUpcaster<ValidationUpcaster>();
            options.EventVersioning.ScanAssembly(typeof(ValidationUpcaster).Assembly);
        });

        using var provider = services.BuildServiceProvider(Validation);
        ConfigureStoreOptions(provider);

        var registry = provider.GetRequiredService<EventUpcasterRegistry>();
        registry.HasUpcasterFor(nameof(ValidationOldEvent)).ShouldBeTrue();
        registry.GetAllUpcasters().Count(u => u.SourceEventTypeName == nameof(ValidationOldEvent)).ShouldBe(1);
    }

    [Fact]
    public void AddEventUpcaster_SameUpcasterTwice_FailsLoudlyWhenMartenIsConfigured()
    {
        // A second upcaster for the same source event type is a conflict, not a no-op: the
        // registry refuses it (see EventUpcasterRegistry.Register) and the failure surfaces when
        // Marten builds its StoreOptions, never silently.
        var services = CreateServicesWithHostDependencies();
        services.AddEncinaMarten(options => options.EventVersioning.Enabled = true);
        services.AddEventUpcaster<ValidationUpcaster>();
        services.AddEventUpcaster<ValidationUpcaster>();

        using var provider = services.BuildServiceProvider(Validation);

        var act = () => ConfigureStoreOptions(provider);

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(nameof(ValidationOldEvent));
    }

    private static void ConfigureStoreOptions(ServiceProvider provider)
    {
        var storeOptions = new StoreOptions();
        foreach (var configure in provider.GetServices<IConfigureOptions<StoreOptions>>())
        {
            configure.Configure(storeOptions);
        }
    }

    private static ServiceCollection CreateServicesWithHostDependencies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => Substitute.For<IDocumentSession>());
        services.AddSingleton(_ => Substitute.For<IDocumentStore>());
        services.AddScoped(_ => Substitute.For<IRequestContext>());
        services.AddScoped(_ => Substitute.For<IEncina>());
        return services;
    }

    public sealed class ValidationAggregate : AggregateBase
    {
        protected override void Apply(object domainEvent) { }
    }

    public sealed class ValidationSnapshotAggregate : AggregateBase, ISnapshotable<ValidationSnapshotAggregate>
    {
        protected override void Apply(object domainEvent) { }
    }

    public sealed class ValidationReadModel : IReadModel
    {
        public Guid Id { get; set; }
    }

    public sealed record ValidationCreatedEvent(string Name);

    public sealed class ValidationProjection :
        IProjection<ValidationReadModel>,
        IProjectionCreator<ValidationCreatedEvent, ValidationReadModel>
    {
        public string ProjectionName => "Validation";

        public ValidationReadModel Create(ValidationCreatedEvent domainEvent, ProjectionContext context) =>
            new() { Id = context.StreamId };
    }

    public sealed record ValidationOldEvent(string Name);

    public sealed record ValidationNewEvent(string Name, string Email);

    public sealed class ValidationUpcaster : EventUpcasterBase<ValidationOldEvent, ValidationNewEvent>
    {
        protected override ValidationNewEvent Upcast(ValidationOldEvent oldEvent) =>
            new(oldEvent.Name, "unknown@example.com");
    }
}

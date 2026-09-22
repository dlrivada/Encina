using Encina.DomainModeling;
using Encina.Marten;
using Encina.Marten.Projections;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Marten.Projections;

/// <summary>
/// Verifies that <c>AddProjection</c> registers a working projection pipeline on its own and that
/// the <see cref="ProjectionRegistry"/> is populated from the registrars (issue #1095).
/// </summary>
public sealed class ProjectionRegistrationTests
{
    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IDocumentStore>());
        services.AddScoped(_ => Substitute.For<IDocumentSession>());
        return services;
    }

    [Fact]
    public void AddProjection_RegistersRegistryAndInlineDispatcher()
    {
        var services = NewServices();

        services.AddProjection<TestProjection, TestReadModel>();

        services.ShouldContain(d => d.ServiceType == typeof(ProjectionRegistry) && d.Lifetime == ServiceLifetime.Singleton);
        services.ShouldContain(d => d.ServiceType == typeof(IInlineProjectionDispatcher) && d.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(d => d.ServiceType == typeof(IProjectionManager));
    }

    [Fact]
    public void AddProjection_ResolvedRegistry_ContainsTheProjection()
    {
        var services = NewServices();
        services.AddProjection<TestProjection, TestReadModel>();
        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<ProjectionRegistry>();

        registry.GetProjectionForReadModel<TestReadModel>().ShouldNotBeNull();
        registry.GetProjectionsForEvent(typeof(TestCreatedEvent)).Count.ShouldBe(1);
        registry.GetAllProjections().Count.ShouldBe(1);
    }

    [Fact]
    public void AddProjection_Twice_RegistersInfrastructureOnce()
    {
        var services = NewServices();

        services.AddProjection<TestProjection, TestReadModel>();
        services.AddProjection<OtherProjection, OtherReadModel>();

        services.Count(d => d.ServiceType == typeof(ProjectionRegistry)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IInlineProjectionDispatcher)).ShouldBe(1);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ProjectionRegistry>().GetAllProjections().Count.ShouldBe(2);
    }

    [Fact]
    public void AddEncinaMarten_WithProjectionsEnabled_ThenAddProjection_SharesOneRegistry()
    {
        var services = NewServices();

        services.AddEncinaMarten(options => options.Projections.Enabled = true);
        services.AddProjection<TestProjection, TestReadModel>();

        services.Count(d => d.ServiceType == typeof(ProjectionRegistry)).ShouldBe(1);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ProjectionRegistry>().GetProjectionForReadModel<TestReadModel>().ShouldNotBeNull();
    }

    [Fact]
    public void AddProjection_DispatcherResolvesInAScope()
    {
        var services = NewServices();
        services.AddProjection<TestProjection, TestReadModel>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dispatcher = scope.ServiceProvider.GetService<IInlineProjectionDispatcher>();

        dispatcher.ShouldBeOfType<MartenInlineProjectionDispatcher>();
    }

    [Fact]
    public void AddProjection_AggregateRepositoryResolvesWithTheDispatcher()
    {
        var services = NewServices();
        services.AddScoped(_ => Substitute.For<IRequestContext>());
        services.AddEncinaMarten();
        services.AddProjection<TestProjection, TestReadModel>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var repository = scope.ServiceProvider.GetService<IAggregateRepository<TestAggregate>>();

        repository.ShouldBeOfType<MartenAggregateRepository<TestAggregate>>();
    }

    public sealed class TestAggregate : AggregateBase
    {
        protected override void Apply(object domainEvent) { }
    }

    public sealed class TestReadModel : IReadModel
    {
        public Guid Id { get; set; }
    }

    public sealed class OtherReadModel : IReadModel
    {
        public Guid Id { get; set; }
    }

    public sealed record TestCreatedEvent(string Name);

    public sealed class TestProjection : IProjection<TestReadModel>, IProjectionCreator<TestCreatedEvent, TestReadModel>
    {
        public string ProjectionName => "Test";

        public TestReadModel Create(TestCreatedEvent domainEvent, ProjectionContext context) => new() { Id = context.StreamId };
    }

    public sealed class OtherProjection : IProjection<OtherReadModel>, IProjectionCreator<TestCreatedEvent, OtherReadModel>
    {
        public string ProjectionName => "Other";

        public OtherReadModel Create(TestCreatedEvent domainEvent, ProjectionContext context) => new() { Id = context.StreamId };
    }
}

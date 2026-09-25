using Encina.Caching;
using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Abstractions;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Tenancy;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests proving that <see cref="ServiceCollectionExtensions.AddEncinaConsent"/> registers
/// every dependency <see cref="DefaultConsentService"/> resolves, in both a single-tenant and a
/// multi-tenant registration, so the <see cref="ConsentOptions.RequireTenantContext"/>
/// auto-detection (#1315) is proven end to end through the real container rather than only by
/// hand-constructing the service in every other test in this package.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaConsent_SingleTenantApplication_ResolvesIConsentService()
    {
        // Arrange — a single-tenant application never registers Encina.Tenancy (SPEC-002 DEC-009).
        var services = new ServiceCollection();
        services.AddLogging();
        RegisterMartenSubstitutes(services);

        services.AddEncinaConsent(options => options.DefinePurpose(ConsentPurposes.Marketing));

        // Act
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IConsentService>();

        // Assert
        service.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaConsent_MultiTenantApplication_ResolvesIConsentService()
    {
        // Arrange — a multi-tenant application also calls AddEncinaTenancy(), which is what
        // DefaultConsentService uses to auto-detect that a missing ambient tenant must fail
        // closed (#1315).
        var services = new ServiceCollection();
        services.AddLogging();
        RegisterMartenSubstitutes(services);

        services.AddEncinaTenancy();
        services.AddEncinaConsent(options => options.DefinePurpose(ConsentPurposes.Marketing));

        // Act
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IConsentService>();

        // Assert
        service.ShouldNotBeNull();
    }

    /// <summary>
    /// Registers substitutes for the Marten-backed collaborators (<see cref="IAggregateRepository{TAggregate}"/>,
    /// <see cref="IReadModelRepository{TReadModel}"/>) and the caching provider, none of which
    /// <see cref="ServiceCollectionExtensions.AddEncinaConsent"/> registers itself — they come
    /// from <c>AddConsentAggregates()</c> and a caching package respectively — so this test can
    /// validate the container without pulling in a real Marten store or cache backend.
    /// </summary>
    private static void RegisterMartenSubstitutes(IServiceCollection services)
    {
        services.AddSingleton(Substitute.For<IAggregateRepository<ConsentAggregate>>());
        services.AddSingleton(Substitute.For<IReadModelRepository<ConsentReadModel>>());
        services.AddSingleton(Substitute.For<ICacheProvider>());
    }
}

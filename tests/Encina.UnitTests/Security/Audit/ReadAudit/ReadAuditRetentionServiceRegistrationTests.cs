using System.Data;
using Encina.ADO.SqlServer;
using Encina.Dapper.SqlServer;
using Encina.Security.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Security.Audit.ReadAudit;

/// <summary>
/// Verifies that <see cref="ReadAuditRetentionService"/> (a singleton hosted service) builds against the scoped
/// <see cref="IReadAuditStore"/> every database provider registers, with <c>ValidateOnBuild</c> and
/// <c>ValidateScopes</c> (#1870).
/// </summary>
public sealed class ReadAuditRetentionServiceRegistrationTests
{
    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IDbConnection>());
        return services;
    }

    private static ServiceProvider Build(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

    private static void AssertRetentionServiceBuildsAgainstTheScopedStore(IServiceCollection services)
    {
        services.Count(d => d.ServiceType == typeof(IReadAuditStore)).ShouldBe(1);
        services.Single(d => d.ServiceType == typeof(IReadAuditStore)).Lifetime.ShouldBe(ServiceLifetime.Scoped);

        using var provider = Build(services);

        provider.GetServices<IHostedService>()
            .OfType<ReadAuditRetentionService>()
            .ShouldHaveSingleItem();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IReadAuditStore>().ShouldNotBeOfType<InMemoryReadAuditStore>();
    }

    [Fact]
    public void AutoPurge_WithAdoScopedStore_BuildsTheRetentionServiceWithValidation()
    {
        var services = NewServices();

        services.AddEncinaReadAuditing(options => options.EnableAutoPurge = true);
        services.AddEncinaADO(config => config.UseReadAuditStore = true);

        AssertRetentionServiceBuildsAgainstTheScopedStore(services);
    }

    [Fact]
    public void AutoPurge_WithDatabaseProviderRegisteredFirst_BuildsTheRetentionServiceWithValidation()
    {
        var services = NewServices();

        services.AddEncinaADO(config => config.UseReadAuditStore = true);
        services.AddEncinaReadAuditing(options => options.EnableAutoPurge = true);

        AssertRetentionServiceBuildsAgainstTheScopedStore(services);
    }

    [Fact]
    public void AutoPurge_WithDapperScopedStore_BuildsTheRetentionServiceWithValidation()
    {
        var services = NewServices();

        services.AddEncinaReadAuditing(options => options.EnableAutoPurge = true);
        services.AddEncinaDapper(config => config.UseReadAuditStore = true);

        AssertRetentionServiceBuildsAgainstTheScopedStore(services);
    }

    [Fact]
    public void AutoPurge_WithTheInMemoryDefault_BuildsTheRetentionServiceWithValidation()
    {
        var services = NewServices();

        services.AddEncinaReadAuditing(options => options.EnableAutoPurge = true);

        using var provider = Build(services);
        provider.GetServices<IHostedService>()
            .OfType<ReadAuditRetentionService>()
            .ShouldHaveSingleItem();
    }
}

using Encina.EntityFrameworkCore;
using Encina.Messaging;
using Encina.Messaging.ContentRouter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.RoutingSlip;
using Encina.Messaging.ScatterGather;
using Encina.Messaging.SoftDelete;
using Encina.Recoverability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore;

/// <summary>
/// Tests that <see cref="ServiceCollectionExtensions.AddEncinaEntityFrameworkCore{TDbContext}(IServiceCollection, Action{MessagingConfiguration})"/>
/// honours <c>UseRecoverability</c>, <c>UseRoutingSlips</c>, <c>UseContentRouter</c> and
/// <c>UseScatterGather</c> exactly as the ADO.NET and Dapper path does (#2711).
/// </summary>
public sealed class ServiceCollectionExtensionsRecoverabilityRoutersTests
{
    private static ServiceCollection Register(Action<MessagingConfiguration> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase("RecoverabilityRoutersDb"));
        services.AddEncinaEntityFrameworkCore<TestDbContext>(configure);
        return services;
    }

    private static ServiceProvider BuildValidated(IServiceCollection services)
        => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    [Fact]
    public void AddEncinaEntityFrameworkCore_UseRecoverability_RegistersAndResolvesRecoverabilityServices()
    {
        var services = Register(config => config.UseRecoverability = true);

        using var provider = BuildValidated(services);
        using var scope = provider.CreateScope();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(RecoverabilityPipelineBehavior<,>));
        provider.GetRequiredService<RecoverabilityOptions>().ShouldNotBeNull();
        provider.GetRequiredService<IErrorClassifier>().ShouldNotBeNull();
        scope.ServiceProvider.GetServices<IPipelineBehavior<TestRequest, string>>()
            .ShouldContain(b => b is RecoverabilityPipelineBehavior<TestRequest, string>);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_UseRecoverabilityFalse_RegistersNoRecoverability()
    {
        var services = Register(_ => { });

        services.ShouldNotContain(d => d.ImplementationType == typeof(RecoverabilityPipelineBehavior<,>));
        services.ShouldNotContain(d => d.ServiceType == typeof(RecoverabilityOptions));
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_UseRoutingSlips_ResolvesRoutingSlipRunner()
    {
        var services = Register(config => config.UseRoutingSlips = true);

        using var provider = BuildValidated(services);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IRoutingSlipRunner>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_UseContentRouter_ResolvesContentRouter()
    {
        var services = Register(config => config.UseContentRouter = true);

        using var provider = BuildValidated(services);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IContentRouter>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_UseScatterGather_ResolvesScatterGatherRunner()
    {
        var services = Register(config => config.UseScatterGather = true);

        using var provider = BuildValidated(services);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IScatterGatherRunner>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_RouterFlagsOff_RegistersNoRouterServices()
    {
        var services = Register(_ => { });

        services.ShouldNotContain(d => d.ServiceType == typeof(IRoutingSlipRunner));
        services.ShouldNotContain(d => d.ServiceType == typeof(IContentRouter));
        services.ShouldNotContain(d => d.ServiceType == typeof(IScatterGatherRunner));
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_AllFlagsOn_BuildsAndResolvesEverything()
    {
        var services = Register(config =>
        {
            config.UseRecoverability = true;
            config.UseRoutingSlips = true;
            config.UseContentRouter = true;
            config.UseScatterGather = true;
            config.UseTransactions = true;
            config.UseOutbox = true;
            config.UseInbox = true;
        });

        using var provider = BuildValidated(services);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IRoutingSlipRunner>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IContentRouter>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IScatterGatherRunner>().ShouldNotBeNull();
        scope.ServiceProvider.GetServices<IPipelineBehavior<TestRequest, string>>()
            .ShouldContain(b => b is RecoverabilityPipelineBehavior<TestRequest, string>);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_Behaviors_RecoverabilityOutermostThenTransactionThenOutboxAndInbox()
    {
        var services = Register(config =>
        {
            config.UseRecoverability = true;
            config.UseTransactions = true;
            config.UseOutbox = true;
            config.UseInbox = true;
        });

        var behaviors = services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .ToList();

        var recoverability = behaviors.IndexOf(typeof(RecoverabilityPipelineBehavior<,>));
        var transaction = behaviors.IndexOf(typeof(global::Encina.EntityFrameworkCore.TransactionPipelineBehavior<,>));
        var outbox = behaviors.IndexOf(typeof(TransactionalOutboxPipelineBehavior<,>));
        var inbox = behaviors.IndexOf(typeof(InboxPipelineBehavior<,>));

        recoverability.ShouldBe(0);
        transaction.ShouldBeGreaterThan(recoverability);
        outbox.ShouldBeGreaterThan(transaction);
        inbox.ShouldBeGreaterThan(transaction);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_UseSoftDelete_DoesNotRegisterSoftDeletePipelineBehavior()
    {
        // EF soft delete is SoftDeleteInterceptor plus the named query filter Encina.SoftDelete (#2711, D6).
        var services = Register(config => config.UseSoftDelete = true);

        services.ShouldNotContain(d => d.ImplementationType == typeof(SoftDeleteQueryFilterBehavior<,>));
        services.ShouldNotContain(d => d.ServiceType == typeof(ISoftDeleteFilterContext));
    }

    [Fact]
    public async Task AddEncinaEntityFrameworkCore_RecoverabilityWithDefaults_HostStartSucceeds()
    {
        var services = Register(config => config.UseRecoverability = true);
        await using var provider = services.BuildServiceProvider();

        var check = provider.GetServices<IHostedService>().OfType<DelayedRetryStartupCheck>().Single();

        await Should.NotThrowAsync(() => check.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AddEncinaEntityFrameworkCore_DelayedRetriesEnabledWithoutStore_HostStartFails()
    {
        var services = Register(config =>
        {
            config.UseRecoverability = true;
            config.RecoverabilityOptions.EnableDelayedRetries = true;
        });
        await using var provider = services.BuildServiceProvider();

        var check = provider.GetServices<IHostedService>().OfType<DelayedRetryStartupCheck>().Single();

        await Should.ThrowAsync<InvalidOperationException>(() => check.StartAsync(CancellationToken.None));
    }

    public sealed record TestRequest : IRequest<string>;
}

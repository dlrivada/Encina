using Encina.Security;
using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// Registration completeness of the decision audit (#751 Phase 4, AGENTS.md section 3): the provider
/// builds with <c>ValidateOnBuild</c> and <c>ValidateScopes</c> in both registration orders of
/// <c>AddEncinaSecurity</c> and <c>AddEncinaABAC</c>, with the audit off (with and without a store) and
/// on (with a scoped store); an enabled audit without a store fails the host start; a closed behavior
/// resolves from a scope; and the pipeline order is the registration order of the two behaviors.
/// </summary>
public sealed class ABACDecisionAuditServiceGraphTests
{
    private static readonly ServiceProviderOptions Validated = new() { ValidateOnBuild = true, ValidateScopes = true };

    private sealed record GuardedRequest : IRequest<string>;

    private static HostApplicationBuilder CreateBuilder(FakeLogCollector? logs = null)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        if (logs is not null)
        {
            builder.Logging.AddProvider(new FakeLoggerProvider(logs));
        }

        builder.ConfigureContainer(new DefaultServiceProviderFactory(Validated));
        return builder;
    }

    private static async Task StartAndStopAsync(IHost host)
    {
        await host.StartAsync();
        await host.StopAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BothRegistrationOrders_AuditEnabledWithAScopedStore_BuildAndStart(bool securityFirst)
    {
        var builder = CreateBuilder();
        builder.Services.AddScoped<IOperationAuditStore, InMemoryOperationAuditStore>();
        Register(builder.Services, securityFirst, options => options.AuditDecisions());
        using var host = builder.Build();

        await StartAndStopAsync(host);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IABACDecisionAuditReader>().ShouldNotBeNull();
        host.Services.GetRequiredService<IABACDecisionRecorder>().ShouldBeOfType<AuditStoreABACDecisionRecorder>();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task BothRegistrationOrders_AuditDisabled_BuildAndStartWithAndWithoutAStore(bool securityFirst, bool withStore)
    {
        var logs = new FakeLogCollector();
        var builder = CreateBuilder(logs);
        if (withStore)
        {
            builder.Services.AddScoped<IOperationAuditStore, InMemoryOperationAuditStore>();
        }

        Register(builder.Services, securityFirst, configure: null);
        using var host = builder.Build();

        await StartAndStopAsync(host);

        logs.GetSnapshot().Where(r => r.Id.Id is 9084 or 9086 or 9087).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BothRegistrationOrders_AuditEnabledWithoutAStore_FailsTheHostStartWithCritical9087(bool securityFirst)
    {
        var logs = new FakeLogCollector();
        var builder = CreateBuilder(logs);
        Register(builder.Services, securityFirst, options => options.AuditDecisions());
        using var host = builder.Build();

        await Should.ThrowAsync<InvalidOperationException>(() => host.StartAsync());

        logs.GetSnapshot().Single(r => r.Id.Id == 9087).Level.ShouldBe(LogLevel.Critical);
    }

    [Fact]
    public async Task AuditEnabledLaterThroughConfigure_IsReadFromTheFinalOptions()
    {
        var builder = CreateBuilder();
        builder.Services.AddEncinaABAC();
        builder.Services.Configure<ABACOptions>(options => options.AuditDecisions());
        using var host = builder.Build();

        await Should.ThrowAsync<InvalidOperationException>(() => host.StartAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BothRegistrationOrders_ClosedBehaviorsResolveFromAScopeInRegistrationOrder(bool securityFirst)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IOperationAuditStore, InMemoryOperationAuditStore>();
        Register(services, securityFirst, options => options.AuditDecisions());

        using var provider = services.BuildServiceProvider(Validated);
        using var scope = provider.CreateScope();
        var behaviors = scope.ServiceProvider.GetServices<IPipelineBehavior<GuardedRequest, string>>().ToList();

        var order = behaviors.Select(b => b.GetType().GetGenericTypeDefinition()).ToList();
        var security = order.IndexOf(typeof(SecurityPipelineBehavior<,>));
        var abac = order.IndexOf(typeof(ABACPipelineBehavior<,>));
        security.ShouldBeGreaterThanOrEqualTo(0);
        abac.ShouldBeGreaterThanOrEqualTo(0);
        (security < abac).ShouldBe(securityFirst);
    }

    [Fact]
    public void AddEncinaABAC_RegistersTheStartupCheckAndTheValidatorOnce()
    {
        var services = new ServiceCollection();
        services.AddEncinaABAC();
        services.AddEncinaABAC();

        services.Count(d => d.ImplementationType == typeof(ABACDecisionAuditStartupCheck)).ShouldBe(1);
        services.Count(d => d.ImplementationType == typeof(ABACOptionsValidator)).ShouldBe(1);
    }

    [Fact]
    public async Task AddEncinaABAC_AuditEnabledWithAnInvalidTimeout_FailsTheHostStart()
    {
        var builder = CreateBuilder();
        builder.Services.AddScoped<IOperationAuditStore, InMemoryOperationAuditStore>();
        builder.Services.AddEncinaABAC(options => options.AuditDecisions(a => a.WriteTimeout = TimeSpan.Zero));
        using var host = builder.Build();

        await Should.ThrowAsync<Exception>(() => host.StartAsync());
    }

    private static void Register(IServiceCollection services, bool securityFirst, Action<ABACOptions>? configure)
    {
        if (securityFirst)
        {
            services.AddEncinaSecurity();
            services.AddEncinaABAC(configure);
        }
        else
        {
            services.AddEncinaABAC(configure);
            services.AddEncinaSecurity();
        }
    }
}

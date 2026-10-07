#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Registration completeness of <c>AddEncinaABAC</c> on the request identity model (#1705 Phase 4):
/// the provider builds with <c>ValidateOnBuild</c> and <c>ValidateScopes</c> with no substituted
/// identity service, resolves the pipeline behavior with every dependency it takes, and a host with
/// seeding starts (so <c>ValidateOnStart</c> runs) and seeds under the built-in service identity.
/// </summary>
public sealed class ABACRegistrationTests
{
    private static readonly ServiceProviderOptions Validated = new() { ValidateOnBuild = true, ValidateScopes = true };

    [RequirePolicy("policy-a")]
    [RequireCondition("user.department == \"HR\"")]
    private sealed record GuardedRequest : IRequest<string>;

    private sealed class PassThroughBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(
            TRequest request,
            IRequestContext context,
            RequestHandlerCallback<TResponse> nextStep,
            CancellationToken cancellationToken) => nextStep();
    }

    private static Policy SeedPolicy(string id = "seed") => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    [Fact]
    public void AddEncinaABAC_WithEveryStartupFeature_RegistersEachHostedServiceAndHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaABAC(options =>
        {
            options.AddHealthCheck = true;
            options.ValidateExpressionsAtStartup = true;
            options.ExpressionScanAssemblies.Add(typeof(ABACRegistrationTests).Assembly);
            options.SeedPolicies.Add(SeedPolicy());
        });

        var hosted = services.Where(d => d.ServiceType == typeof(IHostedService))
            .Select(d => d.ImplementationType?.Name).ToList();

        hosted.ShouldContain("ABACPolicySeedingHostedService");
        hosted.ShouldContain("EELExpressionPrecompilationService");
        hosted.ShouldContain("ABACEnforcementModeStartupCheck");
        services.ShouldContain(d => d.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>));
    }

    [Fact]
    public void AddEncinaABAC_Alone_BuildsValidatedProviderAndResolvesThePipelineBehaviorFromAScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaABAC();

        using var provider = services.BuildServiceProvider(Validated);
        using var scope = provider.CreateScope();

        var behaviors = scope.ServiceProvider.GetServices<IPipelineBehavior<GuardedRequest, string>>().ToList();

        behaviors.OfType<ABACPipelineBehavior<GuardedRequest, string>>().Count().ShouldBe(1);
        scope.ServiceProvider.GetRequiredService<IRequestContextAccessor>().ShouldBeOfType<RequestContextAccessor>();
        scope.ServiceProvider.GetRequiredService<IRequestContextScopeFactory>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaABAC_CalledTwice_RegistersTheBehaviorAndTheStartupCheckOnce()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaABAC(options => options.SeedPolicies.Add(SeedPolicy()));
        services.AddEncinaABAC(options => options.SeedPolicies.Add(SeedPolicy()));

        services.Count(d => d.ServiceType == typeof(IPipelineBehavior<,>)
            && d.ImplementationType == typeof(ABACPipelineBehavior<,>)).ShouldBe(1);
        services.Count(d => d.ImplementationType == typeof(ABACEnforcementModeStartupCheck)).ShouldBe(1);
        services.Count(d => d.ImplementationType == typeof(ABACPolicySeedingHostedService)).ShouldBe(1);

        using var provider = services.BuildServiceProvider(Validated);
        provider.GetRequiredService<IServiceIdentityCatalog>()
            .TryGet(ABACPolicySeedingHostedService.ServiceIdentityName, out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddEncinaWithAConfiguredBehaviorAndAddEncinaABAC_InEitherOrder_KeepTheAbacBehavior(bool encinaFirst)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (encinaFirst)
        {
            services.AddEncina(cfg => cfg.AddPipelineBehavior(typeof(PassThroughBehavior<,>)));
            services.AddEncinaABAC();
        }
        else
        {
            services.AddEncinaABAC();
            services.AddEncina(cfg => cfg.AddPipelineBehavior(typeof(PassThroughBehavior<,>)));
        }

        using var provider = services.BuildServiceProvider(Validated);
        using var scope = provider.CreateScope();
        var behaviors = scope.ServiceProvider.GetServices<IPipelineBehavior<GuardedRequest, string>>().ToList();

        behaviors.OfType<ABACPipelineBehavior<GuardedRequest, string>>().Count().ShouldBe(1);
        behaviors.OfType<PassThroughBehavior<GuardedRequest, string>>().Count().ShouldBe(1);
    }

    [Fact]
    public async Task AddEncinaABAC_WithSeedPolicies_HostStartsValidatedAndSeedsUnderTheBuiltInServiceIdentity()
    {
        RequestIdentity? seedingIdentity = null;
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddLogging();
        builder.Services.AddEncinaABAC(options => options.SeedPolicies.Add(SeedPolicy("host-seed")));
        builder.Services.AddSingleton<IPolicyAdministrationPoint>(sp =>
        {
            var accessor = sp.GetRequiredService<IRequestContextAccessor>();
            var pap = Substitute.For<IPolicyAdministrationPoint>();
            pap.AddPolicyAsync(Arg.Any<Policy>(), null, Arg.Any<CancellationToken>()).Returns(_ =>
            {
                seedingIdentity = accessor.RequestContext?.Identity;
                return new ValueTask<Either<EncinaError, LanguageExt.Unit>>(Right<EncinaError, LanguageExt.Unit>(unit));
            });
            return pap;
        });
        builder.ConfigureContainer(new DefaultServiceProviderFactory(Validated));
        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        seedingIdentity.ShouldNotBeNull();
        seedingIdentity.Kind.ShouldBe(IdentityKind.Service);
        seedingIdentity.UserId.ShouldBe("service:encina.abac.policy-seeding");
    }

    [Fact]
    public void ApplicationDeclaringTheSeedingIdentityName_IsRejected()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentException>(() =>
            services.AddEncinaServiceIdentity(ABACPolicySeedingHostedService.ServiceIdentityName));
    }

    [Fact]
    public async Task ApplicationCodeOpeningTheSeedingIdentity_GetsLeft()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaABAC(options => options.SeedPolicies.Add(SeedPolicy()));
        using var provider = services.BuildServiceProvider(Validated);
        var scopes = provider.GetRequiredService<IRequestContextScopeFactory>();
        var ran = false;

        var outcome = await scopes.RunAsServiceAsync(
            ABACPolicySeedingHostedService.ServiceIdentityName,
            (_, _) =>
            {
                ran = true;
                return Task.FromResult(Right<EncinaError, int>(1));
            });

        outcome.IsLeft.ShouldBeTrue();
        outcome.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(RequestIdentityErrorCodes.ReservedServiceIdentity));
        ran.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaABAC_WithoutTheRequestContextAccessor_FailsValidateOnBuild()
    {
        // The PAP is registered through a factory, which ValidateOnBuild cannot inspect; the identity
        // services AddEncinaABAC registers (the scope factory) take the accessor and fail the build.
        // The PAP's own requirement is proven by the resolution test below.
        var services = PersistentPapServices();
        services.RemoveAll<IRequestContextAccessor>();

        var ex = Should.Throw<AggregateException>(() => services.BuildServiceProvider(Validated));

        ex.ToString().ShouldContain(nameof(IRequestContextAccessor));
    }

    [Fact]
    public void PersistentPap_WithoutTheRequestContextAccessor_CannotBeResolved()
    {
        var services = PersistentPapServices();
        services.RemoveAll<IRequestContextAccessor>();
        using var provider = services.BuildServiceProvider();

        var ex = Should.Throw<InvalidOperationException>(() => provider.GetRequiredService<IPolicyAdministrationPoint>());

        ex.Message.ShouldContain(nameof(IRequestContextAccessor));
    }

    [Fact]
    public void PersistentPap_WithTheRegisteredAccessor_ResolvesUnderValidation()
    {
        using var provider = PersistentPapServices().BuildServiceProvider(Validated);

        provider.GetRequiredService<IPolicyAdministrationPoint>()
            .ShouldBeOfType<global::Encina.Security.ABAC.Administration.PersistentPolicyAdministrationPoint>();
    }

    private static ServiceCollection PersistentPapServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => Substitute.For<IPolicyStore>());
        services.AddEncinaABAC(options => options.UsePersistentPAP = true);
        return services;
    }

    // ── Warning 9085: enforcement disabled (decision N6) ────────────

    [Theory]
    [InlineData(ABACEnforcementMode.Disabled, 1)]
    [InlineData(ABACEnforcementMode.Warn, 0)]
    [InlineData(ABACEnforcementMode.Block, 0)]
    public async Task HostStart_LogsEnforcementDisabledOnceInDisabledModeOnly(ABACEnforcementMode mode, int expected)
    {
        var collector = new FakeLogCollector();
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Logging.AddProvider(new FakeLoggerProvider(collector));
        builder.Services.AddEncinaABAC(options => options.EnforcementMode = mode);
        builder.Services.AddEncinaABAC(options => options.EnforcementMode = mode);
        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        var warnings = collector.GetSnapshot().Where(r => r.Id.Id == 9085).ToList();
        warnings.Count.ShouldBe(expected);
        warnings.ShouldAllBe(r => r.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task HostStart_ModeChangedByALaterConfigure_IsReadFromTheFinalOptions()
    {
        var collector = new FakeLogCollector();
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Logging.AddProvider(new FakeLoggerProvider(collector));
        builder.Services.AddEncinaABAC();
        builder.Services.Configure<ABACOptions>(options => options.EnforcementMode = ABACEnforcementMode.Disabled);
        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        collector.GetSnapshot().Count(r => r.Id.Id == 9085).ShouldBe(1);
    }
}

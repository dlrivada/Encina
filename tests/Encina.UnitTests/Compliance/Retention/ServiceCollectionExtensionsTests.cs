using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Health;
using Encina.Compliance.Retention.Services;

using Shouldly;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions.AddEncinaRetention"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaRetention_DefaultConfiguration_RegistersRequiredServices()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention();

        services.ShouldContain(sd => sd.ServiceType == typeof(IRetentionPolicyService));
        services.ShouldContain(sd => sd.ServiceType == typeof(IRetentionRecordService));
        services.ShouldContain(sd => sd.ServiceType == typeof(ILegalHoldService));
    }

    [Fact]
    public void AddEncinaRetention_DefaultConfiguration_RegistersValidator()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention();

        services.ShouldContain(sd =>
            sd.ServiceType == typeof(IValidateOptions<RetentionOptions>));
    }

    [Fact]
    public void AddEncinaRetention_WithHealthCheck_RegistersHealthCheckService()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention(options =>
        {
            options.AddHealthCheck = true;
        });

        // Health checks are registered via IHealthChecksBuilder, which registers
        // the HealthCheckService infrastructure, not individual IHealthCheck entries
        services.ShouldContain(sd =>
            sd.ServiceType == typeof(HealthCheckService));
    }

    [Fact]
    public void AddEncinaRetention_WithoutHealthCheck_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention(options =>
        {
            options.AddHealthCheck = false;
        });

        services.ShouldNotContain(sd =>
            sd.ImplementationType == typeof(RetentionHealthCheck));
    }

    [Fact]
    public void AddEncinaRetention_AutomaticEnforcementEnabled_RegistersHostedService()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention(options =>
        {
            options.EnableAutomaticEnforcement = true;
        });

        services.ShouldContain(sd =>
            sd.ImplementationType == typeof(RetentionEnforcementService));
    }

    [Fact]
    public void AddEncinaRetention_AutomaticEnforcementDisabled_DoesNotRegisterHostedService()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention(options =>
        {
            options.EnableAutomaticEnforcement = false;
        });

        services.ShouldNotContain(sd =>
            sd.ImplementationType == typeof(RetentionEnforcementService));
    }

    [Fact]
    public void AddEncinaRetention_WithConfigureAction_AppliesConfiguration()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention(options =>
        {
            options.EnforcementMode = RetentionEnforcementMode.Block;
            options.AlertBeforeExpirationDays = 7;
        });

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<RetentionOptions>>().Value;

        opts.EnforcementMode.ShouldBe(RetentionEnforcementMode.Block);
        opts.AlertBeforeExpirationDays.ShouldBe(7);
    }

    [Fact]
    public void AddEncinaRetention_NullConfigure_RegistersDefaultOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention(configure: null);

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<RetentionOptions>>().Value;

        opts.EnforcementMode.ShouldBe(RetentionEnforcementMode.Warn);
    }

    [Fact]
    public void AddEncinaRetention_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaRetention();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaRetention_WithFluentPolicies_RegistersFluentPolicyDescriptor()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention(options =>
        {
            options.AutoRegisterFromAttributes = false;
            options.AddPolicy("user-data", policy =>
            {
                policy.RetainForDays(365);
                policy.WithAutoDelete();
            });
        });

        services.ShouldContain(sd =>
            sd.ServiceType == typeof(RetentionFluentPolicyDescriptor));
    }

    [Fact]
    public void AddEncinaRetention_AutoRegisterEnabled_RegistersAutoRegistrationDescriptor()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention(options =>
        {
            options.AutoRegisterFromAttributes = true;
        });

        services.ShouldContain(sd =>
            sd.ServiceType == typeof(RetentionAutoRegistrationDescriptor));
    }

    [Fact]
    public void AddEncinaRetention_RegistersTimeProvider()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention();

        services.ShouldContain(sd =>
            sd.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    public void AddEncinaRetention_TryAdd_DoesNotOverrideCustomRegistrations()
    {
        var services = new ServiceCollection();
        var customService = NSubstitute.Substitute.For<IRetentionPolicyService>();
        services.AddSingleton(customService);

        services.AddEncinaRetention();

        // The custom registration should remain (TryAdd does not override)
        services.Where(sd => sd.ServiceType == typeof(IRetentionPolicyService))
            .Count().ShouldBe(1);
    }

    [Fact]
    public void AddEncinaRetention_RegistersScopedServices()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention();

        var policyDescriptor = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IRetentionPolicyService) &&
            sd.ImplementationType == typeof(DefaultRetentionPolicyService));

        policyDescriptor.ShouldNotBeNull();
        policyDescriptor!.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaRetention_RegistersPipelineBehavior()
    {
        var services = new ServiceCollection();

        services.AddEncinaRetention();

        services.ShouldContain(sd =>
            sd.ServiceType == typeof(IPipelineBehavior<,>) &&
            sd.ImplementationType == typeof(RetentionValidationPipelineBehavior<,>));
    }
}

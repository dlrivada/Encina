using Encina.AspNetCore;
using Encina.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    // ── AddEncinaAspNetCore (parameterless overload) ───────────────────────

    [Fact]
    public void AddEncinaAspNetCore_RegistersRequestContextAccessor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAspNetCore();
        using var provider = services.BuildServiceProvider();

        var accessor = provider.GetService<IRequestContextAccessor>();
        accessor.ShouldNotBeNull();
        accessor.ShouldBeOfType<RequestContextAccessor>();
    }

    [Fact]
    public void AddEncinaAspNetCore_RegistersHttpContextAccessor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAspNetCore();
        using var provider = services.BuildServiceProvider();

        var http = provider.GetService<IHttpContextAccessor>();
        http.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAspNetCore_RegistersOptions_WithDefaultValues()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAspNetCore();
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<EncinaAspNetCoreOptions>>().Value;
        options.CorrelationIdHeader.ShouldBe("X-Correlation-ID");
        options.TenantIdHeader.ShouldBe("X-Tenant-ID");
        options.IdempotencyKeyHeader.ShouldBe("X-Idempotency-Key");
    }

    [Fact]
    public void AddEncinaAspNetCore_ReturnsSameServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaAspNetCore();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaAspNetCore_RequestContextAccessor_RegisteredAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAspNetCore();

        var descriptor = services.Single(s => s.ServiceType == typeof(IRequestContextAccessor));

        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    // ── AddEncinaAspNetCore (configure overload) ───────────────────────────

    [Fact]
    public void AddEncinaAspNetCore_WithConfigureOptions_AppliesCustomValues()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAspNetCore(o =>
        {
            o.CorrelationIdHeader = "X-Custom-Correlation";
            o.TenantIdHeader = "X-Custom-Tenant";
            o.IdempotencyKeyHeader = "X-Custom-Idempotency";
            o.UserIdClaimType = "sub";
            o.TenantIdClaimType = "tid";
        });
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<EncinaAspNetCoreOptions>>().Value;
        options.CorrelationIdHeader.ShouldBe("X-Custom-Correlation");
        options.TenantIdHeader.ShouldBe("X-Custom-Tenant");
        options.IdempotencyKeyHeader.ShouldBe("X-Custom-Idempotency");
        options.UserIdClaimType.ShouldBe("sub");
        options.TenantIdClaimType.ShouldBe("tid");
    }

    [Fact]
    public void AddEncinaAspNetCore_CalledTwice_DoesNotRegisterDuplicateAccessor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAspNetCore();
        services.AddEncinaAspNetCore();

        var descriptors = services.Where(s => s.ServiceType == typeof(IRequestContextAccessor)).ToList();
        descriptors.Count.ShouldBe(1);
    }

    // ── AddAuthorization (EncinaConfiguration extension) ───────────────────

    [Fact]
    public void AddAuthorization_AddsAuthorizationPipelineBehavior()
    {
        var configuration = new EncinaConfiguration();

        var result = configuration.AddAuthorization();

        result.ShouldBeSameAs(configuration);
        configuration.PipelineBehaviorTypes.ShouldContain(typeof(AuthorizationPipelineBehavior<,>));
    }

    [Fact]
    public void AddAuthorization_CalledTwice_DoesNotDuplicateBehavior()
    {
        // EncinaConfiguration.AddPipelineBehavior deduplicates registrations.
        var configuration = new EncinaConfiguration();

        configuration.AddAuthorization();
        configuration.AddAuthorization();

        configuration.PipelineBehaviorTypes
            .Count(t => t == typeof(AuthorizationPipelineBehavior<,>))
            .ShouldBe(1);
    }

    // ── AddEncinaAuthorization registers RequireAuthenticated policy ───────

    [Fact]
    public void AddEncinaAuthorization_RegistersRequireAuthenticatedPolicy()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAuthorization();
        using var provider = services.BuildServiceProvider();

        var authOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var policy = authOptions.GetPolicy(AuthorizationConfiguration.RequireAuthenticatedPolicyName);
        policy.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAuthorization_RegistersHttpContextAccessor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAuthorization();
        using var provider = services.BuildServiceProvider();

        var http = provider.GetService<IHttpContextAccessor>();
        http.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAuthorization_ReturnsSameServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var result = services.AddEncinaAuthorization();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaAuthorization_WithoutCallbacks_UsesDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAuthorization();
        using var provider = services.BuildServiceProvider();

        var config = provider.GetRequiredService<IOptions<AuthorizationConfiguration>>().Value;
        config.AutoApplyPolicies.ShouldBeFalse();
        config.DefaultCommandPolicy.ShouldBe(AuthorizationConfiguration.RequireAuthenticatedPolicyName);
        config.DefaultQueryPolicy.ShouldBe(AuthorizationConfiguration.RequireAuthenticatedPolicyName);
    }

    [Fact]
    public void AddEncinaAuthorization_PoliciesCallback_RegistersPolicy()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAuthorization(
            configurePolicies: policies =>
            {
                policies.AddPolicy("CustomTest", p => p.RequireAuthenticatedUser());
            });

        using var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var policy = authOptions.GetPolicy("CustomTest");
        policy.ShouldNotBeNull();
    }
}

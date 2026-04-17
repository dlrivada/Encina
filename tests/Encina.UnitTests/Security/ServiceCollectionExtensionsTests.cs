using Encina.Security;
using Encina.Security.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.UnitTests.Security;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaSecurity_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSecurity();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ISecurityContextAccessor>().ShouldNotBeNull();
        provider.GetService<IPermissionEvaluator>().ShouldNotBeNull();
        provider.GetService<IResourceOwnershipEvaluator>().ShouldNotBeNull();
        provider.GetService<IOptions<SecurityOptions>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaSecurity_ShouldRegisterDefaultImplementations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSecurity();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IPermissionEvaluator>()
            .ShouldBeOfType<DefaultPermissionEvaluator>();
        provider.GetRequiredService<IResourceOwnershipEvaluator>()
            .ShouldBeOfType<DefaultResourceOwnershipEvaluator>();
    }

    [Fact]
    public void AddEncinaSecurity_ShouldApplyCustomConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSecurity(options =>
        {
            options.RequireAuthenticatedByDefault = true;
            options.UserIdClaimType = "custom_uid";
            options.ThrowOnMissingSecurityContext = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SecurityOptions>>().Value;

        // Assert
        options.RequireAuthenticatedByDefault.ShouldBeTrue();
        options.UserIdClaimType.ShouldBe("custom_uid");
        options.ThrowOnMissingSecurityContext.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaSecurity_WithoutConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSecurity();
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SecurityOptions>>().Value;

        // Assert
        options.RequireAuthenticatedByDefault.ShouldBeFalse();
        options.ThrowOnMissingSecurityContext.ShouldBeFalse();
        options.UserIdClaimType.ShouldBe("sub");
        options.RoleClaimType.ShouldBe("role");
        options.PermissionClaimType.ShouldBe("permission");
        options.TenantIdClaimType.ShouldBe("tenant_id");
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaSecurity_ShouldAllowCustomPermissionEvaluatorOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IPermissionEvaluator, CustomPermissionEvaluator>();

        // Act
        services.AddEncinaSecurity();
        var provider = services.BuildServiceProvider();

        // Assert - Custom evaluator should be used (TryAdd doesn't override)
        provider.GetRequiredService<IPermissionEvaluator>()
            .ShouldBeOfType<CustomPermissionEvaluator>();
    }

    [Fact]
    public void AddEncinaSecurity_ShouldAllowCustomOwnershipEvaluatorOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IResourceOwnershipEvaluator, CustomOwnershipEvaluator>();

        // Act
        services.AddEncinaSecurity();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IResourceOwnershipEvaluator>()
            .ShouldBeOfType<CustomOwnershipEvaluator>();
    }

    [Fact]
    public void AddEncinaSecurity_ShouldRegisterContextAccessorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSecurity();

        // Assert
        var descriptor = services.First(d => d.ServiceType == typeof(ISecurityContextAccessor));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaSecurity_ShouldRegisterPermissionEvaluatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSecurity();

        // Assert
        var descriptor = services.First(d => d.ServiceType == typeof(IPermissionEvaluator));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaSecurity_ShouldRegisterOwnershipEvaluatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSecurity();

        // Assert
        var descriptor = services.First(d => d.ServiceType == typeof(IResourceOwnershipEvaluator));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaSecurity_ShouldRegisterPipelineBehaviorAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSecurity();

        // Assert
        var descriptor = services.First(d => d.ServiceType == typeof(IPipelineBehavior<,>));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEncinaSecurity_ShouldReturnServicesForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaSecurity();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaSecurity_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaSecurity();

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaSecurity_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var act = () =>
        {
            services.AddEncinaSecurity();
            services.AddEncinaSecurity();
        };

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaSecurity_WithHealthCheckEnabled_ShouldRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSecurity(options => options.AddHealthCheck = true);

        // Assert — verify via the IOptions pattern that health checks are configured
        var provider = services.BuildServiceProvider();
        var healthCheckOptions = provider.GetService<IOptions<HealthCheckServiceOptions>>();
        healthCheckOptions.ShouldNotBeNull();
        healthCheckOptions!.Value.Registrations
            .ShouldContain(r => r.Name == SecurityHealthCheck.DefaultName);
    }

    [Fact]
    public void AddEncinaSecurity_WithHealthCheckDisabled_ShouldNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSecurity(options => options.AddHealthCheck = false);

        // Assert
        var provider = services.BuildServiceProvider();
        var healthCheckOptions = provider.GetService<IOptions<HealthCheckServiceOptions>>();

        // When AddHealthCheck is false, health checks service is not added at all
        if (healthCheckOptions is not null)
        {
            healthCheckOptions.Value.Registrations
                .ShouldNotContain(r => r.Name == SecurityHealthCheck.DefaultName);
        }
    }

    [Fact]
    public void AddEncinaSecurity_Default_ShouldNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSecurity();

        // Assert — AddHealthCheck defaults to false
        var provider = services.BuildServiceProvider();
        var healthCheckOptions = provider.GetService<IOptions<HealthCheckServiceOptions>>();

        if (healthCheckOptions is not null)
        {
            healthCheckOptions.Value.Registrations
                .ShouldNotContain(r => r.Name == SecurityHealthCheck.DefaultName);
        }
    }

    #region Custom Test Implementations

    private sealed class CustomPermissionEvaluator : IPermissionEvaluator
    {
        public ValueTask<bool> HasPermissionAsync(ISecurityContext context, string permission, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public ValueTask<bool> HasAnyPermissionAsync(ISecurityContext context, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public ValueTask<bool> HasAllPermissionsAsync(ISecurityContext context, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);
    }

    private sealed class CustomOwnershipEvaluator : IResourceOwnershipEvaluator
    {
        public ValueTask<bool> IsOwnerAsync<TResource>(ISecurityContext context, TResource resource, string propertyName, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);
    }

    #endregion
}

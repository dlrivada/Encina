using Encina.Security;
using Encina.Security.Health;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

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
        provider.GetService<ISecurityContextAccessor>().Should().NotBeNull();
        provider.GetService<IPermissionEvaluator>().Should().NotBeNull();
        provider.GetService<IResourceOwnershipEvaluator>().Should().NotBeNull();
        provider.GetService<IOptions<SecurityOptions>>().Should().NotBeNull();
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
            .Should().BeOfType<DefaultPermissionEvaluator>();
        provider.GetRequiredService<IResourceOwnershipEvaluator>()
            .Should().BeOfType<DefaultResourceOwnershipEvaluator>();
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
        options.RequireAuthenticatedByDefault.Should().BeTrue();
        options.UserIdClaimType.Should().Be("custom_uid");
        options.ThrowOnMissingSecurityContext.Should().BeTrue();
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
        options.RequireAuthenticatedByDefault.Should().BeFalse();
        options.ThrowOnMissingSecurityContext.Should().BeFalse();
        options.UserIdClaimType.Should().Be("sub");
        options.RoleClaimType.Should().Be("role");
        options.PermissionClaimType.Should().Be("permission");
        options.TenantIdClaimType.Should().Be("tenant_id");
        options.AddHealthCheck.Should().BeFalse();
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
            .Should().BeOfType<CustomPermissionEvaluator>();
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
            .Should().BeOfType<CustomOwnershipEvaluator>();
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
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
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
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
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
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
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
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEncinaSecurity_ShouldReturnServicesForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaSecurity();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEncinaSecurity_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaSecurity();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
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
        act.Should().NotThrow();
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
        healthCheckOptions.Should().NotBeNull();
        healthCheckOptions!.Value.Registrations
            .Should().Contain(r => r.Name == SecurityHealthCheck.DefaultName);
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
                .Should().NotContain(r => r.Name == SecurityHealthCheck.DefaultName);
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
                .Should().NotContain(r => r.Name == SecurityHealthCheck.DefaultName);
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

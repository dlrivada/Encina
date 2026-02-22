using Encina.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.AspNetCore.Authorization;

public class AuthorizationConfigurationTests
{
    // ── Default values ──────────────────────────────────────────────

    [Fact]
    public void DefaultCommandPolicy_IsRequireAuthenticated()
    {
        var config = new AuthorizationConfiguration();
        config.DefaultCommandPolicy.ShouldBe(AuthorizationConfiguration.RequireAuthenticatedPolicyName);
    }

    [Fact]
    public void DefaultQueryPolicy_IsRequireAuthenticated()
    {
        var config = new AuthorizationConfiguration();
        config.DefaultQueryPolicy.ShouldBe(AuthorizationConfiguration.RequireAuthenticatedPolicyName);
    }

    [Fact]
    public void AutoApplyPolicies_DefaultIsFalse()
    {
        var config = new AuthorizationConfiguration();
        config.AutoApplyPolicies.ShouldBeFalse();
    }

    [Fact]
    public void RequireAuthenticationByDefault_DefaultIsTrue()
    {
        var config = new AuthorizationConfiguration();
        config.RequireAuthenticationByDefault.ShouldBeTrue();
    }

    [Fact]
    public void RequireAuthenticatedPolicyName_IsExpectedValue()
    {
        AuthorizationConfiguration.RequireAuthenticatedPolicyName.ShouldBe("RequireAuthenticated");
    }

    // ── Setters ─────────────────────────────────────────────────────

    [Fact]
    public void DefaultCommandPolicy_CanBeChanged()
    {
        var config = new AuthorizationConfiguration
        {
            DefaultCommandPolicy = "AdminOnly"
        };
        config.DefaultCommandPolicy.ShouldBe("AdminOnly");
    }

    [Fact]
    public void DefaultQueryPolicy_CanBeChanged()
    {
        var config = new AuthorizationConfiguration
        {
            DefaultQueryPolicy = "ReadOnly"
        };
        config.DefaultQueryPolicy.ShouldBe("ReadOnly");
    }

    [Fact]
    public void AutoApplyPolicies_CanBeEnabled()
    {
        var config = new AuthorizationConfiguration
        {
            AutoApplyPolicies = true
        };
        config.AutoApplyPolicies.ShouldBeTrue();
    }

    // ── Policy helper extensions ────────────────────────────────────

    [Fact]
    public void AddRolePolicy_RegistersPolicyWithRoles()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuthorization(options =>
        {
            options.AddRolePolicy("AdminPolicy", "Admin", "SuperAdmin");
        });

        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>().Value;

        // Assert
        var policy = authOptions.GetPolicy("AdminPolicy");
        policy.ShouldNotBeNull();
        policy.Requirements.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddRolePolicy_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            AuthorizationConfigurationExtensions.AddRolePolicy(null!, "Test", "Admin"));
    }

    [Fact]
    public void AddRolePolicy_NullPolicyName_ThrowsArgumentException()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddRolePolicy(null!, "Admin"));
    }

    [Fact]
    public void AddRolePolicy_EmptyRoles_ThrowsArgumentException()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddRolePolicy("Test"));
    }

    [Fact]
    public void AddClaimPolicy_RegistersPolicyWithClaim()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuthorization(options =>
        {
            options.AddClaimPolicy("DeptPolicy", "department", "sales", "marketing");
        });

        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>().Value;

        // Assert
        var policy = authOptions.GetPolicy("DeptPolicy");
        policy.ShouldNotBeNull();
        policy.Requirements.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddClaimPolicy_NoAllowedValues_RegistersExistenceClaim()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuthorization(options =>
        {
            options.AddClaimPolicy("HasEmail", "email");
        });

        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>().Value;

        // Assert
        var policy = authOptions.GetPolicy("HasEmail");
        policy.ShouldNotBeNull();
    }

    [Fact]
    public void AddClaimPolicy_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            AuthorizationConfigurationExtensions.AddClaimPolicy(null!, "Test", "type"));
    }

    [Fact]
    public void AddClaimPolicy_NullClaimType_ThrowsArgumentException()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddClaimPolicy("Test", null!));
    }

    [Fact]
    public void AddAuthenticatedPolicy_RegistersAuthenticationRequirement()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuthorization(options =>
        {
            options.AddAuthenticatedPolicy("MustLogin");
        });

        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>().Value;

        // Assert
        var policy = authOptions.GetPolicy("MustLogin");
        policy.ShouldNotBeNull();
        policy.Requirements.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddAuthenticatedPolicy_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            AuthorizationConfigurationExtensions.AddAuthenticatedPolicy(null!, "Test"));
    }

    [Fact]
    public void AddAuthenticatedPolicy_EmptyPolicyName_ThrowsArgumentException()
    {
        var options = new AuthorizationOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddAuthenticatedPolicy(""));
    }

    // ── AddEncinaAuthorization registration ─────────────────────────

    [Fact]
    public void AddEncinaAuthorization_RegistersAuthorizationConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAuthorization(auth =>
        {
            auth.AutoApplyPolicies = true;
            auth.DefaultCommandPolicy = "AdminOnly";
        });

        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationConfiguration>>().Value;

        // Assert
        options.AutoApplyPolicies.ShouldBeTrue();
        options.DefaultCommandPolicy.ShouldBe("AdminOnly");
    }

    [Fact]
    public void AddEncinaAuthorization_RegistersResourceAuthorizer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAuthorization();

        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        var authorizer = scope.ServiceProvider.GetService<IResourceAuthorizer>();

        // Assert
        authorizer.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAuthorization_WithNullCallbacks_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAuthorization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationConfiguration>>().Value;

        // Assert
        options.DefaultCommandPolicy.ShouldBe("RequireAuthenticated");
        options.DefaultQueryPolicy.ShouldBe("RequireAuthenticated");
        options.AutoApplyPolicies.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaAuthorization_ConfiguresPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAuthorization(
            configurePolicies: policies =>
            {
                policies.AddRolePolicy("TestAdmin", "Admin");
            });

        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>().Value;

        // Assert
        var policy = authOptions.GetPolicy("TestAdmin");
        policy.ShouldNotBeNull();
    }

    // Helper: Import ServiceCollectionExtensions
    private static class ServiceCollectionHelper
    {
        public static IServiceCollection AddEncinaAuthorization(
            IServiceCollection services,
            Action<AuthorizationConfiguration>? configureAuthorization = null,
            Action<AuthorizationOptions>? configurePolicies = null)
        {
            return global::Encina.AspNetCore.ServiceCollectionExtensions.AddEncinaAuthorization(
                services, configureAuthorization, configurePolicies);
        }
    }
}

// Extension to make AddEncinaAuthorization discoverable in tests
file static class TestServiceCollectionExtensions
{
    public static IServiceCollection AddEncinaAuthorization(
        this IServiceCollection services,
        Action<AuthorizationConfiguration>? configureAuthorization = null,
        Action<AuthorizationOptions>? configurePolicies = null)
    {
        return global::Encina.AspNetCore.ServiceCollectionExtensions.AddEncinaAuthorization(
            services, configureAuthorization, configurePolicies);
    }
}

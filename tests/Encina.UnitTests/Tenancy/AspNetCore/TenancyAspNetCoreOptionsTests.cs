namespace Encina.UnitTests.Tenancy.AspNetCore;

/// <summary>
/// Unit tests for TenancyAspNetCoreOptions.
/// </summary>
public class TenancyAspNetCoreOptionsTests
{
    [Fact]
    public void TenancyAspNetCoreOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new TenancyAspNetCoreOptions();

        // Assert
        options.Return400WhenTenantRequired.ShouldBeTrue();
    }

    [Fact]
    public void HeaderResolverOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new HeaderResolverOptions();

        // Assert
        options.Enabled.ShouldBeTrue();
        options.HeaderName.ShouldBe("X-Tenant-ID");
        options.Priority.ShouldBe(100);
    }

    [Fact]
    public void ClaimResolverOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new ClaimResolverOptions();

        // Assert
        options.Enabled.ShouldBeTrue();
        options.ClaimType.ShouldBe("tenant_id");
        options.Priority.ShouldBe(110);
    }

    [Fact]
    public void RouteResolverOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new RouteResolverOptions();

        // Assert
        options.Enabled.ShouldBeFalse();
        options.ParameterName.ShouldBe("tenantId");
        options.Priority.ShouldBe(120);
    }

    [Fact]
    public void SubdomainResolverOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new SubdomainResolverOptions();

        // Assert
        options.Enabled.ShouldBeFalse();
        options.BaseDomain.ShouldBeNull();
        options.Priority.ShouldBe(130);
        options.ExcludedSubdomains.ShouldContain("www");
        options.ExcludedSubdomains.ShouldContain("api");
        options.ExcludedSubdomains.ShouldContain("admin");
    }

    [Fact]
    public void TenancyAspNetCoreOptions_CanSetReturn400WhenTenantRequired()
    {
        // Arrange
        var options = new TenancyAspNetCoreOptions();

        // Act
        options.Return400WhenTenantRequired = false;

        // Assert
        options.Return400WhenTenantRequired.ShouldBeFalse();
    }

    [Fact]
    public void HeaderResolverOptions_CanSetCustomHeaderName()
    {
        // Arrange
        var options = new TenancyAspNetCoreOptions();

        // Act
        options.HeaderResolver.HeaderName = "X-Organization-ID";
        options.HeaderResolver.Priority = 50;
        options.HeaderResolver.Enabled = false;

        // Assert
        options.HeaderResolver.HeaderName.ShouldBe("X-Organization-ID");
        options.HeaderResolver.Priority.ShouldBe(50);
        options.HeaderResolver.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void SubdomainResolverOptions_CanAddExcludedSubdomains()
    {
        // Arrange
        var options = new SubdomainResolverOptions();

        // Act
        options.ExcludedSubdomains.Add("staging");
        options.ExcludedSubdomains.Add("dev");

        // Assert
        options.ExcludedSubdomains.ShouldContain("staging");
        options.ExcludedSubdomains.ShouldContain("dev");
    }
}

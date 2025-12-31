using System.Security.Claims;
using Encina.Messaging.Health;
using Shouldly;
using Xunit;

namespace Encina.AzureFunctions.Tests;

public class EncinaAzureFunctionsOptionsTests
{
    [Fact]
    public void DefaultOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new EncinaAzureFunctionsOptions();

        // Assert
        options.EnableRequestContextEnrichment.ShouldBeTrue();
        options.CorrelationIdHeader.ShouldBe("X-Correlation-ID");
        options.TenantIdHeader.ShouldBe("X-Tenant-ID");
        options.UserIdClaimType.ShouldBe(ClaimTypes.NameIdentifier);
        options.TenantIdClaimType.ShouldBe("tenant_id");
        options.IncludeExceptionDetailsInResponse.ShouldBeFalse();
    }

    [Fact]
    public void ProviderHealthCheck_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new EncinaAzureFunctionsOptions();

        // Assert
        options.ProviderHealthCheck.ShouldNotBeNull();
        options.ProviderHealthCheck.Enabled.ShouldBeTrue();
        options.ProviderHealthCheck.Name.ShouldBe("encina-azure-functions");
        options.ProviderHealthCheck.Tags.ShouldContain("encina");
        options.ProviderHealthCheck.Tags.ShouldContain("azure-functions");
        options.ProviderHealthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public void EnableRequestContextEnrichment_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();

        // Act
        options.EnableRequestContextEnrichment = false;

        // Assert
        options.EnableRequestContextEnrichment.ShouldBeFalse();
    }

    [Fact]
    public void CorrelationIdHeader_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();

        // Act
        options.CorrelationIdHeader = "X-Request-ID";

        // Assert
        options.CorrelationIdHeader.ShouldBe("X-Request-ID");
    }

    [Fact]
    public void TenantIdHeader_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();

        // Act
        options.TenantIdHeader = "X-Organization-ID";

        // Assert
        options.TenantIdHeader.ShouldBe("X-Organization-ID");
    }

    [Fact]
    public void UserIdClaimType_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();

        // Act
        options.UserIdClaimType = "sub";

        // Assert
        options.UserIdClaimType.ShouldBe("sub");
    }

    [Fact]
    public void TenantIdClaimType_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();

        // Act
        options.TenantIdClaimType = "tid";

        // Assert
        options.TenantIdClaimType.ShouldBe("tid");
    }

    [Fact]
    public void IncludeExceptionDetailsInResponse_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();

        // Act
        options.IncludeExceptionDetailsInResponse = true;

        // Assert
        options.IncludeExceptionDetailsInResponse.ShouldBeTrue();
    }

    [Fact]
    public void ProviderHealthCheck_ShouldBeReplaceable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();
        var customHealthCheck = new ProviderHealthCheckOptions
        {
            Enabled = false,
            Name = "custom-check",
            Tags = ["custom"]
        };

        // Act
        options.ProviderHealthCheck = customHealthCheck;

        // Assert
        options.ProviderHealthCheck.ShouldBeSameAs(customHealthCheck);
        options.ProviderHealthCheck.Enabled.ShouldBeFalse();
        options.ProviderHealthCheck.Name.ShouldBe("custom-check");
    }
}

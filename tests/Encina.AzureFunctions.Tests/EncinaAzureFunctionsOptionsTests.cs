using System.Security.Claims;
using Encina.Messaging.Health;
using FluentAssertions;
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
        options.EnableRequestContextEnrichment.Should().BeTrue();
        options.CorrelationIdHeader.Should().Be("X-Correlation-ID");
        options.TenantIdHeader.Should().Be("X-Tenant-ID");
        options.UserIdClaimType.Should().Be(ClaimTypes.NameIdentifier);
        options.TenantIdClaimType.Should().Be("tenant_id");
        options.IncludeExceptionDetailsInResponse.Should().BeFalse();
    }

    [Fact]
    public void ProviderHealthCheck_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new EncinaAzureFunctionsOptions();

        // Assert
        options.ProviderHealthCheck.Should().NotBeNull();
        options.ProviderHealthCheck.Enabled.Should().BeTrue();
        options.ProviderHealthCheck.Name.Should().Be("encina-azure-functions");
        options.ProviderHealthCheck.Tags.Should().Contain("encina");
        options.ProviderHealthCheck.Tags.Should().Contain("azure-functions");
        options.ProviderHealthCheck.Tags.Should().Contain("ready");
    }

    [Fact]
    public void EnableRequestContextEnrichment_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();

        // Act
        options.EnableRequestContextEnrichment = false;

        // Assert
        options.EnableRequestContextEnrichment.Should().BeFalse();
    }

    [Fact]
    public void CorrelationIdHeader_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();

        // Act
        options.CorrelationIdHeader = "X-Request-ID";

        // Assert
        options.CorrelationIdHeader.Should().Be("X-Request-ID");
    }

    [Fact]
    public void TenantIdHeader_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();

        // Act
        options.TenantIdHeader = "X-Organization-ID";

        // Assert
        options.TenantIdHeader.Should().Be("X-Organization-ID");
    }

    [Fact]
    public void UserIdClaimType_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();

        // Act
        options.UserIdClaimType = "sub";

        // Assert
        options.UserIdClaimType.Should().Be("sub");
    }

    [Fact]
    public void TenantIdClaimType_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();

        // Act
        options.TenantIdClaimType = "tid";

        // Assert
        options.TenantIdClaimType.Should().Be("tid");
    }

    [Fact]
    public void IncludeExceptionDetailsInResponse_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaAzureFunctionsOptions();

        // Act
        options.IncludeExceptionDetailsInResponse = true;

        // Assert
        options.IncludeExceptionDetailsInResponse.Should().BeTrue();
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
        options.ProviderHealthCheck.Should().BeSameAs(customHealthCheck);
        options.ProviderHealthCheck.Enabled.Should().BeFalse();
        options.ProviderHealthCheck.Name.Should().Be("custom-check");
    }
}

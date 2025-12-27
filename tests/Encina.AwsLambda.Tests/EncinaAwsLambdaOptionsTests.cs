using FluentAssertions;
using Xunit;

namespace Encina.AwsLambda.Tests;

public class EncinaAwsLambdaOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new EncinaAwsLambdaOptions();

        // Assert
        options.EnableRequestContextEnrichment.Should().BeTrue();
        options.CorrelationIdHeader.Should().Be("X-Correlation-ID");
        options.TenantIdHeader.Should().Be("X-Tenant-ID");
        options.UserIdClaimType.Should().Be("sub");
        options.TenantIdClaimType.Should().Be("tenant_id");
        options.IncludeExceptionDetailsInResponse.Should().BeFalse();
        options.UseApiGatewayV2Format.Should().BeFalse();
        options.EnableSqsBatchItemFailures.Should().BeTrue();
        options.ProviderHealthCheck.Should().NotBeNull();
    }

    [Fact]
    public void EnableRequestContextEnrichment_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.EnableRequestContextEnrichment = false;

        // Assert
        options.EnableRequestContextEnrichment.Should().BeFalse();
    }

    [Fact]
    public void CorrelationIdHeader_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.CorrelationIdHeader = "X-Request-ID";

        // Assert
        options.CorrelationIdHeader.Should().Be("X-Request-ID");
    }

    [Fact]
    public void TenantIdHeader_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.TenantIdHeader = "X-Organization-ID";

        // Assert
        options.TenantIdHeader.Should().Be("X-Organization-ID");
    }

    [Fact]
    public void UserIdClaimType_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.UserIdClaimType = "user_id";

        // Assert
        options.UserIdClaimType.Should().Be("user_id");
    }

    [Fact]
    public void TenantIdClaimType_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.TenantIdClaimType = "org_id";

        // Assert
        options.TenantIdClaimType.Should().Be("org_id");
    }

    [Fact]
    public void IncludeExceptionDetailsInResponse_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.IncludeExceptionDetailsInResponse = true;

        // Assert
        options.IncludeExceptionDetailsInResponse.Should().BeTrue();
    }

    [Fact]
    public void UseApiGatewayV2Format_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.UseApiGatewayV2Format = true;

        // Assert
        options.UseApiGatewayV2Format.Should().BeTrue();
    }

    [Fact]
    public void EnableSqsBatchItemFailures_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.EnableSqsBatchItemFailures = false;

        // Assert
        options.EnableSqsBatchItemFailures.Should().BeFalse();
    }
}

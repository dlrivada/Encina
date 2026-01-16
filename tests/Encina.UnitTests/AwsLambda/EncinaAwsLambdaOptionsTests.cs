using Encina.AwsLambda;

namespace Encina.UnitTests.AwsLambda;

public class EncinaAwsLambdaOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new EncinaAwsLambdaOptions();

        // Assert
        options.EnableRequestContextEnrichment.ShouldBeTrue();
        options.CorrelationIdHeader.ShouldBe("X-Correlation-ID");
        options.TenantIdHeader.ShouldBe("X-Tenant-ID");
        options.UserIdClaimType.ShouldBe("sub");
        options.TenantIdClaimType.ShouldBe("tenant_id");
        options.IncludeExceptionDetailsInResponse.ShouldBeFalse();
        options.UseApiGatewayV2Format.ShouldBeFalse();
        options.EnableSqsBatchItemFailures.ShouldBeTrue();
        options.ProviderHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void EnableRequestContextEnrichment_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.EnableRequestContextEnrichment = false;

        // Assert
        options.EnableRequestContextEnrichment.ShouldBeFalse();
    }

    [Fact]
    public void CorrelationIdHeader_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.CorrelationIdHeader = "X-Request-ID";

        // Assert
        options.CorrelationIdHeader.ShouldBe("X-Request-ID");
    }

    [Fact]
    public void TenantIdHeader_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.TenantIdHeader = "X-Organization-ID";

        // Assert
        options.TenantIdHeader.ShouldBe("X-Organization-ID");
    }

    [Fact]
    public void UserIdClaimType_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.UserIdClaimType = "user_id";

        // Assert
        options.UserIdClaimType.ShouldBe("user_id");
    }

    [Fact]
    public void TenantIdClaimType_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.TenantIdClaimType = "org_id";

        // Assert
        options.TenantIdClaimType.ShouldBe("org_id");
    }

    [Fact]
    public void IncludeExceptionDetailsInResponse_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.IncludeExceptionDetailsInResponse = true;

        // Assert
        options.IncludeExceptionDetailsInResponse.ShouldBeTrue();
    }

    [Fact]
    public void UseApiGatewayV2Format_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.UseApiGatewayV2Format = true;

        // Assert
        options.UseApiGatewayV2Format.ShouldBeTrue();
    }

    [Fact]
    public void EnableSqsBatchItemFailures_CanBeSet()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions();

        // Act
        options.EnableSqsBatchItemFailures = false;

        // Assert
        options.EnableSqsBatchItemFailures.ShouldBeFalse();
    }
}

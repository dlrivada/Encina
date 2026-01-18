using System.Security.Claims;
using Encina.AspNetCore;

namespace Encina.UnitTests.AspNetCore;

public sealed class EncinaAspNetCoreOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new EncinaAspNetCoreOptions();

        options.CorrelationIdHeader.ShouldBe("X-Correlation-ID");
        options.TenantIdHeader.ShouldBe("X-Tenant-ID");
        options.IdempotencyKeyHeader.ShouldBe("X-Idempotency-Key");
        options.UserIdClaimType.ShouldBe(ClaimTypes.NameIdentifier);
        options.TenantIdClaimType.ShouldBe("tenant_id");
        options.IncludeRequestPathInProblemDetails.ShouldBeFalse();
        options.IncludeExceptionDetails.ShouldBeFalse();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new EncinaAspNetCoreOptions
        {
            CorrelationIdHeader = "X-Custom-Correlation",
            TenantIdHeader = "X-Custom-Tenant",
            IdempotencyKeyHeader = "X-Custom-Idempotency",
            UserIdClaimType = "sub",
            TenantIdClaimType = "tid",
            IncludeRequestPathInProblemDetails = true,
            IncludeExceptionDetails = true
        };

        options.CorrelationIdHeader.ShouldBe("X-Custom-Correlation");
        options.TenantIdHeader.ShouldBe("X-Custom-Tenant");
        options.IdempotencyKeyHeader.ShouldBe("X-Custom-Idempotency");
        options.UserIdClaimType.ShouldBe("sub");
        options.TenantIdClaimType.ShouldBe("tid");
        options.IncludeRequestPathInProblemDetails.ShouldBeTrue();
        options.IncludeExceptionDetails.ShouldBeTrue();
    }
}

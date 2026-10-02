using Encina.Compliance.Anonymization.Health;
using Encina.Compliance.Consent.Health;
using Encina.Compliance.CrossBorderTransfer.Health;
using Encina.Compliance.DataResidency.Health;
using Encina.Compliance.PrivacyByDesign.Health;
using Encina.Compliance.Retention.Health;
using Encina.Security.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Messaging.Health;

/// <summary>
/// Health checks that implement Microsoft's <c>IHealthCheck</c> directly get no catch-all from
/// <c>EncinaHealthCheck</c>: if a dependency throws, <c>DefaultHealthCheckService</c> would copy the
/// exception message and object onto the report entry. Each of them must catch the exception itself and
/// report only its type (#1301).
/// </summary>
public sealed class MicrosoftHealthCheckCatchAllTests
{
    public static TheoryData<string, Func<IServiceProvider, IHealthCheck>> Checks => new()
    {
        { "Anonymization", sp => new AnonymizationHealthCheck(sp, NullLogger<AnonymizationHealthCheck>.Instance) },
        { "Consent", sp => new ConsentHealthCheck(sp, NullLogger<ConsentHealthCheck>.Instance) },
        { "CrossBorderTransfer", sp => new CrossBorderTransferHealthCheck(sp, NullLogger<CrossBorderTransferHealthCheck>.Instance) },
        { "DataResidency", sp => new DataResidencyHealthCheck(sp, NullLogger<DataResidencyHealthCheck>.Instance) },
        { "PrivacyByDesign", sp => new PrivacyByDesignHealthCheck(sp, NullLogger<PrivacyByDesignHealthCheck>.Instance) },
        { "Retention", sp => new RetentionHealthCheck(sp, NullLogger<RetentionHealthCheck>.Instance) },
        { "Security", sp => new SecurityHealthCheck(sp) }
    };

    [Theory]
    [MemberData(nameof(Checks))]
    public async Task CheckHealthAsync_WhenResolutionThrows_ReportsOnlyTheExceptionType(
        string name,
        Func<IServiceProvider, IHealthCheck> create)
    {
        _ = name;
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IServiceScopeFactory))
            .Returns(_ => throw new InvalidOperationException("secret-host-4711"));

        var result = await create(provider).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain(nameof(InvalidOperationException));
        result.Description!.ShouldNotContain("secret-host-4711");
        result.Exception.ShouldBeNull();
    }
}

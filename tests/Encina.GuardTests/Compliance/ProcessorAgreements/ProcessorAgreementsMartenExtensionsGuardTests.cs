using Encina.Compliance.ProcessorAgreements;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="ProcessorAgreementsMartenExtensions"/> to verify null parameter handling.
/// </summary>
public sealed class ProcessorAgreementsMartenExtensionsGuardTests
{
    [Fact]
    public void AddProcessorAgreementAggregates_NullServices_ThrowsArgumentNullException()
    {
        Microsoft.Extensions.DependencyInjection.IServiceCollection services = null!;

        var act = () => services.AddProcessorAgreementAggregates();

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }
}

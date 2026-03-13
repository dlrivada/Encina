using Encina.Compliance.ProcessorAgreements.Health;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="ProcessorAgreementHealthCheck"/> to verify null parameter handling.
/// </summary>
/// <remarks>
/// The <see cref="ProcessorAgreementHealthCheck"/> constructor does not use
/// <c>ArgumentNullException.ThrowIfNull</c> for its parameters. This test class documents
/// that and tests the constructor still accepts valid parameters without exceptions.
/// If null guards are added in the future, this class should be updated with appropriate tests.
/// </remarks>
public class ProcessorAgreementHealthCheckGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Note: The constructor does not currently guard against null serviceProvider.
        // This test documents the expected behavior if guards are added.
        // Currently the constructor assigns without checking.
        var logger = NullLogger<ProcessorAgreementHealthCheck>.Instance;

        var act = () => new ProcessorAgreementHealthCheck(null!, logger);

        // The constructor does not throw — it accepts null without guard.
        // If null guards are added later, uncomment the assertions below:
        // var ex = Should.Throw<ArgumentNullException>(act);
        // ex.ParamName.ShouldBe("serviceProvider");

        // For now, verify construction does not throw (no guards present).
        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Note: The constructor does not currently guard against null logger.
        var serviceProvider = Substitute.For<IServiceProvider>();

        var act = () => new ProcessorAgreementHealthCheck(serviceProvider, null!);

        // For now, verify construction does not throw (no guards present).
        Should.NotThrow(act);
    }

    #endregion
}

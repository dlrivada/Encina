using Encina.Security.Audit;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard tests for <see cref="OperationAuditMaskingStartupValidator"/>.
/// </summary>
public class OperationAuditMaskingStartupValidatorGuardTests
{
    private static readonly IServiceProvider Services = Substitute.For<IServiceProvider>();
    private static readonly IOptions<OperationAuditOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new OperationAuditOptions());
    private static readonly NullLogger<OperationAuditMaskingStartupValidator> Logger =
        NullLogger<OperationAuditMaskingStartupValidator>.Instance;

    [Fact]
    public void Constructor_NullServices_ThrowsArgumentNullException()
    {
        var act = () => new OperationAuditMaskingStartupValidator(null!, Options, Logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new OperationAuditMaskingStartupValidator(Services, null!, Logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new OperationAuditMaskingStartupValidator(Services, Options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        var act = () => new OperationAuditMaskingStartupValidator(Services, Options, Logger);

        Should.NotThrow(act);
    }
}

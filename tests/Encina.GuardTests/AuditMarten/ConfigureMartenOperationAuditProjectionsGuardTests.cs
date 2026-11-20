using Encina.Audit.Marten;
using Encina.Audit.Marten.Projections;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.AuditMarten;

/// <summary>
/// Guard tests for <see cref="ConfigureMartenOperationAuditProjections"/>.
/// </summary>
public class ConfigureMartenOperationAuditProjectionsGuardTests
{
    private static readonly IOptions<MartenOperationAuditOptions> AuditOptions =
        Microsoft.Extensions.Options.Options.Create(new MartenOperationAuditOptions());

    [Fact]
    public void Constructor_NullAuditOptions_ThrowsArgumentNullException()
    {
        var act = () => new ConfigureMartenOperationAuditProjections(null!, NullLoggerFactory.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("auditOptions");
    }

    [Fact]
    public void Constructor_NullLoggerFactory_ThrowsArgumentNullException()
    {
        var act = () => new ConfigureMartenOperationAuditProjections(AuditOptions, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("loggerFactory");
    }

    [Fact]
    public void Configure_NullOptions_ThrowsArgumentNullException()
    {
        var sut = new ConfigureMartenOperationAuditProjections(AuditOptions, NullLoggerFactory.Instance);

        var act = () => sut.Configure(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Configure_ValidStoreOptions_DoesNotThrow()
    {
        var sut = new ConfigureMartenOperationAuditProjections(AuditOptions, NullLoggerFactory.Instance);

        var act = () => sut.Configure(new StoreOptions());

        Should.NotThrow(act);
    }
}

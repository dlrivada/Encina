using Encina.Audit.Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Marten.Audit;

/// <summary>
/// Guard tests for MartenOperationAuditRetentionService covering constructor null checks.
/// </summary>
public class MartenOperationAuditRetentionServiceGuardTests
{
    private static readonly IServiceProvider ServiceProvider = Substitute.For<IServiceProvider>();
    private static readonly IOptions<MartenOperationAuditOptions> Options = Microsoft.Extensions.Options.Options.Create(new MartenOperationAuditOptions());
    private static readonly TimeProvider TimeProviderInstance = TimeProvider.System;
    private static readonly NullLogger<MartenOperationAuditRetentionService> Logger = NullLogger<MartenOperationAuditRetentionService>.Instance;

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new MartenOperationAuditRetentionService(null!, Options, TimeProviderInstance, Logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new MartenOperationAuditRetentionService(ServiceProvider, null!, TimeProviderInstance, Logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new MartenOperationAuditRetentionService(ServiceProvider, Options, null!, Logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new MartenOperationAuditRetentionService(ServiceProvider, Options, TimeProviderInstance, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        using var sut = new MartenOperationAuditRetentionService(ServiceProvider, Options, TimeProviderInstance, Logger);

        sut.ShouldNotBeNull();
    }
}

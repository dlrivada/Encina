using Encina.Audit.Marten;
using Encina.Security.Audit;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for Audit.Marten <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaAuditMarten_NullServices_ShouldThrow()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddEncinaAuditMarten());
    }

    [Fact]
    public void AddEncinaAuditMarten_ReturnsSameCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddEncinaAuditMarten();
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaAuditMarten_RegistersAuditStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAuditMarten();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuditStore));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAuditMarten_WithConfigure_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAuditMarten(opts =>
        {
            opts.EnableAutoPurge = true;
            opts.PurgeIntervalHours = 12;
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetService<IOptions<MartenAuditOptions>>();
        options.ShouldNotBeNull();
        options.Value.EnableAutoPurge.ShouldBeTrue();
        options.Value.PurgeIntervalHours.ShouldBe(12);
    }

    [Fact]
    public void AddEncinaAuditMarten_RegistersEncryptor()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAuditMarten();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(AuditEventEncryptor));
        descriptor.ShouldNotBeNull();
    }
}

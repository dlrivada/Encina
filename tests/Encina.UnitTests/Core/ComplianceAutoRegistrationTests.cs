using Encina.Compliance.DPIA;
using Encina.Compliance.GDPR;

namespace Encina.UnitTests.Core;

/// <summary>
/// Covers the attribute auto-registration and the optional hosted services of the Compliance
/// <c>AddEncina*</c> methods after their branches moved into private helpers (#1635).
/// </summary>
public sealed class ComplianceAutoRegistrationTests
{
    [Fact]
    public void AddEncinaDPIA_AutoRegisterWithScanAssemblies_RegistersDescriptorAndHostedService()
    {
        var services = new ServiceCollection();
        var assembly = typeof(ComplianceAutoRegistrationTests).Assembly;

        services.AddEncinaDPIA(o =>
        {
            o.AutoRegisterFromAttributes = true;
            o.AssembliesToScan.Add(assembly);
        });

        var descriptor = services.Single(d => d.ServiceType == typeof(DPIAAutoRegistrationDescriptor));
        var registered = (DPIAAutoRegistrationDescriptor)descriptor.ImplementationInstance!;
        registered.Assemblies.ShouldContain(assembly);
        HostedServiceTypes(services).ShouldContain(typeof(DPIAAutoRegistrationHostedService));
    }

    [Fact]
    public void AddEncinaDPIA_AutoRegisterWithoutAssemblies_ScansTheEntryOrCallingAssembly()
    {
        var services = new ServiceCollection();

        services.AddEncinaDPIA(o => o.AutoRegisterFromAttributes = true);

        var descriptor = services.Single(d => d.ServiceType == typeof(DPIAAutoRegistrationDescriptor));
        var registered = (DPIAAutoRegistrationDescriptor)descriptor.ImplementationInstance!;
        registered.Assemblies.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaDPIA_AutoRegisterDisabled_RegistersNoDescriptor()
    {
        var services = new ServiceCollection();

        services.AddEncinaDPIA(o => o.AutoRegisterFromAttributes = false);

        services.Any(d => d.ServiceType == typeof(DPIAAutoRegistrationDescriptor)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddEncinaDPIA_ExpirationMonitoring_RegistersReminderServiceOnlyWhenEnabled(bool enabled)
    {
        var services = new ServiceCollection();

        services.AddEncinaDPIA(o => o.EnableExpirationMonitoring = enabled);

        HostedServiceTypes(services).Contains(typeof(DPIAReviewReminderService)).ShouldBe(enabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddEncinaGDPR_AutoRegister_RegistersHostedServiceOnlyWhenEnabled(bool enabled)
    {
        var services = new ServiceCollection();

        services.AddEncinaGDPR(o => o.AutoRegisterFromAttributes = enabled);

        HostedServiceTypes(services).Contains(typeof(GDPRAutoRegistrationHostedService)).ShouldBe(enabled);
    }

    private static List<Type?> HostedServiceTypes(IServiceCollection services) =>
        services
            .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
            .Select(d => d.ImplementationType)
            .ToList();
}

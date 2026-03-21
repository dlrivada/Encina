using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Providers;

namespace Encina.UnitTests.Compliance.Attestation;

[Trait("Category", "Unit")]
[Trait("Feature", "Attestation")]
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaAttestation_UseInMemory_RegistersInMemoryProvider()
    {
        var services = new ServiceCollection();

        services.AddEncinaAttestation(options => options.UseInMemory());

        var provider = services.BuildServiceProvider();
        var attestationProvider = provider.GetRequiredService<IAuditAttestationProvider>();

        attestationProvider.ShouldBeOfType<InMemoryAttestationProvider>();
        attestationProvider.ProviderName.ShouldBe("InMemory");
    }

    [Fact]
    public void AddEncinaAttestation_UseHashChain_RegistersHashChainProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAttestation(options => options.UseHashChain());

        var provider = services.BuildServiceProvider();
        var attestationProvider = provider.GetRequiredService<IAuditAttestationProvider>();

        attestationProvider.ShouldBeOfType<HashChainAttestationProvider>();
        attestationProvider.ProviderName.ShouldBe("HashChain");
    }

    [Fact]
    public void AddEncinaAttestation_UseHttp_RegistersHttpProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAttestation(options => options.UseHttp(http =>
        {
            http.AttestEndpointUrl = new Uri("https://example.com/attest");
        }));

        var provider = services.BuildServiceProvider();
        var attestationProvider = provider.GetRequiredService<IAuditAttestationProvider>();

        attestationProvider.ShouldBeOfType<HttpAttestationProvider>();
        attestationProvider.ProviderName.ShouldBe("Http");
    }

    [Fact]
    public void AddEncinaAttestation_RegistersTimeProvider()
    {
        var services = new ServiceCollection();

        services.AddEncinaAttestation(options => options.UseInMemory());

        var provider = services.BuildServiceProvider();
        var timeProvider = provider.GetRequiredService<TimeProvider>();

        timeProvider.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAttestation_WithHealthCheck_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAttestation(options =>
        {
            options.UseInMemory();
            options.AddHealthCheck = true;
        });

        var provider = services.BuildServiceProvider();

        // Health check registration adds IHealthCheckRegistration entries
        // We verify the service collection contains health check services
        services.Any(s => s.ServiceType.Name.Contains("HealthCheck")).ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaAttestation_NoProviderConfigured_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddEncinaAttestation(options => { }));
    }
}

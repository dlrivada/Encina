using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Behaviors;
using Encina.Compliance.Attestation.Providers;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.Attestation;

[Trait("Category", "Unit")]
[Trait("Feature", "Attestation")]
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaAttestation_UseInMemory_RegistersInMemoryProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

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

        // Health check registration uses HealthCheckServiceOptions, not direct IHealthCheck
        var sp = services.BuildServiceProvider();
        var healthCheckOptions = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        healthCheckOptions.Value.Registrations.ShouldContain(r => r.Name == "encina-attestation");
    }

    [Fact]
    public void AddEncinaAttestation_NoProviderConfigured_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddEncinaAttestation(options => { }));
    }

    [Fact]
    public void AddEncinaAttestation_RegistersAttestationPipelineBehavior()
    {
        var services = new ServiceCollection();

        services.AddEncinaAttestation(options => options.UseInMemory());

        // Pipeline behaviors are registered as open generics
        services.Any(s =>
            s.ServiceType == typeof(IPipelineBehavior<,>) &&
            s.ImplementationType == typeof(AttestationPipelineBehavior<,>)
        ).ShouldBeTrue();
    }

    #region SEC-1: SSRF validation

    [Theory]
    [InlineData("http://example.com/attest")]        // plain HTTP
    [InlineData("https://localhost/attest")]          // localhost
    [InlineData("https://127.0.0.1/attest")]          // loopback IPv4
    [InlineData("https://[::1]/attest")]              // loopback IPv6
    [InlineData("https://169.254.1.1/attest")]        // link-local IPv4
    public void AddEncinaAttestation_UseHttp_InvalidUrl_OptionsValidationFails(string url)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAttestation(options => options.UseHttp(http =>
        {
            http.AttestEndpointUrl = new Uri(url);
        }));

        var provider = services.BuildServiceProvider();

        // IValidateOptions<T> is triggered on first access of IOptions<T>.Value
        Should.Throw<OptionsValidationException>(() =>
        {
            var opts = provider.GetRequiredService<IOptions<HttpAttestationOptions>>();
            _ = opts.Value;
        });
    }

    [Theory]
    [InlineData("http://example.com/attest")]        // plain HTTP allowed
    [InlineData("https://localhost/attest")]          // localhost allowed
    [InlineData("https://127.0.0.1/attest")]          // loopback allowed
    public void AddEncinaAttestation_UseHttp_AllowInsecureHttp_BypassesValidation(string url)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAttestation(options => options.UseHttp(http =>
        {
            http.AttestEndpointUrl = new Uri(url);
            http.AllowInsecureHttp = true;
        }));

        var provider = services.BuildServiceProvider();

        // Should NOT throw — AllowInsecureHttp bypasses all URL restrictions
        var opts = provider.GetRequiredService<IOptions<HttpAttestationOptions>>();
        opts.Value.ShouldNotBeNull();
    }

    #endregion
}

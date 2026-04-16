using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Behaviors;
using Encina.Compliance.Attestation.Model;
using Encina.Compliance.Attestation.Providers;

using Shouldly;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Encina.GuardTests.Compliance.Attestation;

[Trait("Category", "Guard")]
[Trait("Feature", "Attestation")]
public sealed class AttestationGuardTests
{
    #region InMemoryAttestationProvider

    [Fact]
    public void InMemoryProvider_NullTimeProvider_Throws()
    {
        var act = () => new InMemoryAttestationProvider(
            null!,
            NullLogger<InMemoryAttestationProvider>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void InMemoryProvider_NullLogger_Throws()
    {
        var act = () => new InMemoryAttestationProvider(
            new FakeTimeProvider(),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task InMemoryProvider_AttestAsync_NullRecord_Throws()
    {
        var sut = new InMemoryAttestationProvider(
            new FakeTimeProvider(),
            NullLogger<InMemoryAttestationProvider>.Instance);

        var act = () => sut.AttestAsync(null!).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("record");
    }

    [Fact]
    public async Task InMemoryProvider_VerifyAsync_NullReceipt_Throws()
    {
        var sut = new InMemoryAttestationProvider(
            new FakeTimeProvider(),
            NullLogger<InMemoryAttestationProvider>.Instance);

        var act = () => sut.VerifyAsync(null!).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("receipt");
    }

    #endregion

    #region HashChainAttestationProvider

    [Fact]
    public void HashChainProvider_NullTimeProvider_Throws()
    {
        var act = () => new HashChainAttestationProvider(
            null!,
            NullLogger<HashChainAttestationProvider>.Instance,
            Options.Create(new HashChainOptions()));

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void HashChainProvider_NullLogger_Throws()
    {
        var act = () => new HashChainAttestationProvider(
            new FakeTimeProvider(),
            null!,
            Options.Create(new HashChainOptions()));

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void HashChainProvider_NullOptions_Throws()
    {
        var act = () => new HashChainAttestationProvider(
            new FakeTimeProvider(),
            NullLogger<HashChainAttestationProvider>.Instance,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public async Task HashChainProvider_AttestAsync_NullRecord_Throws()
    {
        var sut = new HashChainAttestationProvider(
            new FakeTimeProvider(),
            NullLogger<HashChainAttestationProvider>.Instance,
            Options.Create(new HashChainOptions()));

        var act = () => sut.AttestAsync(null!).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("record");
    }

    [Fact]
    public async Task HashChainProvider_VerifyAsync_NullReceipt_Throws()
    {
        var sut = new HashChainAttestationProvider(
            new FakeTimeProvider(),
            NullLogger<HashChainAttestationProvider>.Instance,
            Options.Create(new HashChainOptions()));

        var act = () => sut.VerifyAsync(null!).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("receipt");
    }

    #endregion

    #region HttpAttestationProvider

    [Fact]
    public void HttpProvider_NullHttpClient_Throws()
    {
        var act = () => new HttpAttestationProvider(
            null!,
            new FakeTimeProvider(),
            NullLogger<HttpAttestationProvider>.Instance,
            Options.Create(new HttpAttestationOptions { AttestEndpointUrl = new Uri("https://example.com/attest") }));

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public void HttpProvider_NullTimeProvider_Throws()
    {
        var act = () => new HttpAttestationProvider(
            new HttpClient(),
            null!,
            NullLogger<HttpAttestationProvider>.Instance,
            Options.Create(new HttpAttestationOptions { AttestEndpointUrl = new Uri("https://example.com/attest") }));

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void HttpProvider_NullLogger_Throws()
    {
        var act = () => new HttpAttestationProvider(
            new HttpClient(),
            new FakeTimeProvider(),
            null!,
            Options.Create(new HttpAttestationOptions { AttestEndpointUrl = new Uri("https://example.com/attest") }));

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void HttpProvider_NullOptions_Throws()
    {
        var act = () => new HttpAttestationProvider(
            new HttpClient(),
            new FakeTimeProvider(),
            NullLogger<HttpAttestationProvider>.Instance,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public async Task HttpProvider_AttestAsync_NullRecord_Throws()
    {
        var sut = new HttpAttestationProvider(
            new HttpClient(),
            new FakeTimeProvider(),
            NullLogger<HttpAttestationProvider>.Instance,
            Options.Create(new HttpAttestationOptions { AttestEndpointUrl = new Uri("https://example.com/attest") }));

        var act = () => sut.AttestAsync(null!).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("record");
    }

    [Fact]
    public async Task HttpProvider_VerifyAsync_NullReceipt_Throws()
    {
        var sut = new HttpAttestationProvider(
            new HttpClient(),
            new FakeTimeProvider(),
            NullLogger<HttpAttestationProvider>.Instance,
            Options.Create(new HttpAttestationOptions { AttestEndpointUrl = new Uri("https://example.com/attest") }));

        var act = () => sut.VerifyAsync(null!).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("receipt");
    }

    #endregion

    #region ServiceCollectionExtensions

    [Fact]
    public void AddEncinaAttestation_NullServices_Throws()
    {
        var act = () => global::Encina.Compliance.Attestation.ServiceCollectionExtensions.AddEncinaAttestation(null!, _ => { });

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaAttestation_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        var act = () => services.AddEncinaAttestation(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configure");
    }

    #endregion

    #region AttestationPipelineBehavior

    [Fact]
    public void AttestationPipelineBehavior_NullProvider_Throws()
    {
        var act = () => new AttestationPipelineBehavior<FakeRequest, FakeResponse>(
            null!,
            new FakeTimeProvider(),
            NullLogger<AttestationPipelineBehavior<FakeRequest, FakeResponse>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("provider");
    }

    [Fact]
    public void AttestationPipelineBehavior_NullTimeProvider_Throws()
    {
        var act = () => new AttestationPipelineBehavior<FakeRequest, FakeResponse>(
            new InMemoryAttestationProvider(
                new FakeTimeProvider(),
                NullLogger<InMemoryAttestationProvider>.Instance),
            null!,
            NullLogger<AttestationPipelineBehavior<FakeRequest, FakeResponse>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void AttestationPipelineBehavior_NullLogger_Throws()
    {
        var act = () => new AttestationPipelineBehavior<FakeRequest, FakeResponse>(
            new InMemoryAttestationProvider(
                new FakeTimeProvider(),
                NullLogger<InMemoryAttestationProvider>.Instance),
            new FakeTimeProvider(),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    // Minimal stubs for AttestationPipelineBehavior guard tests
    private sealed record FakeRequest : IRequest<FakeResponse>;
    private sealed record FakeResponse;

    #endregion

    #region AttestationOptionsExtensions

    [Fact]
    public void UseInMemory_NullOptions_Throws()
    {
        var act = () => AttestationOptionsExtensions.UseInMemory(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void UseHashChain_NullOptions_Throws()
    {
        var act = () => AttestationOptionsExtensions.UseHashChain(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void UseHttp_NullOptions_Throws()
    {
        var act = () => AttestationOptionsExtensions.UseHttp(null!, _ => { });

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void UseHttp_NullConfigure_Throws()
    {
        var act = () => new AttestationOptions().UseHttp(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configure");
    }

    #endregion
}

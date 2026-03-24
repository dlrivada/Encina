using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Behaviors;
using Encina.Compliance.Attestation.Model;
using Encina.Compliance.Attestation.Providers;

using FluentAssertions;

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

        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void InMemoryProvider_NullLogger_Throws()
    {
        var act = () => new InMemoryAttestationProvider(
            new FakeTimeProvider(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task InMemoryProvider_AttestAsync_NullRecord_Throws()
    {
        var sut = new InMemoryAttestationProvider(
            new FakeTimeProvider(),
            NullLogger<InMemoryAttestationProvider>.Instance);

        var act = () => sut.AttestAsync(null!).AsTask();

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("record");
    }

    [Fact]
    public async Task InMemoryProvider_VerifyAsync_NullReceipt_Throws()
    {
        var sut = new InMemoryAttestationProvider(
            new FakeTimeProvider(),
            NullLogger<InMemoryAttestationProvider>.Instance);

        var act = () => sut.VerifyAsync(null!).AsTask();

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("receipt");
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

        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void HashChainProvider_NullLogger_Throws()
    {
        var act = () => new HashChainAttestationProvider(
            new FakeTimeProvider(),
            null!,
            Options.Create(new HashChainOptions()));

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void HashChainProvider_NullOptions_Throws()
    {
        var act = () => new HashChainAttestationProvider(
            new FakeTimeProvider(),
            NullLogger<HashChainAttestationProvider>.Instance,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public async Task HashChainProvider_AttestAsync_NullRecord_Throws()
    {
        var sut = new HashChainAttestationProvider(
            new FakeTimeProvider(),
            NullLogger<HashChainAttestationProvider>.Instance,
            Options.Create(new HashChainOptions()));

        var act = () => sut.AttestAsync(null!).AsTask();

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("record");
    }

    [Fact]
    public async Task HashChainProvider_VerifyAsync_NullReceipt_Throws()
    {
        var sut = new HashChainAttestationProvider(
            new FakeTimeProvider(),
            NullLogger<HashChainAttestationProvider>.Instance,
            Options.Create(new HashChainOptions()));

        var act = () => sut.VerifyAsync(null!).AsTask();

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("receipt");
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

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void HttpProvider_NullTimeProvider_Throws()
    {
        var act = () => new HttpAttestationProvider(
            new HttpClient(),
            null!,
            NullLogger<HttpAttestationProvider>.Instance,
            Options.Create(new HttpAttestationOptions { AttestEndpointUrl = new Uri("https://example.com/attest") }));

        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void HttpProvider_NullLogger_Throws()
    {
        var act = () => new HttpAttestationProvider(
            new HttpClient(),
            new FakeTimeProvider(),
            null!,
            Options.Create(new HttpAttestationOptions { AttestEndpointUrl = new Uri("https://example.com/attest") }));

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void HttpProvider_NullOptions_Throws()
    {
        var act = () => new HttpAttestationProvider(
            new HttpClient(),
            new FakeTimeProvider(),
            NullLogger<HttpAttestationProvider>.Instance,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
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

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("record");
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

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("receipt");
    }

    #endregion

    #region ServiceCollectionExtensions

    [Fact]
    public void AddEncinaAttestation_NullServices_Throws()
    {
        var act = () => global::Encina.Compliance.Attestation.ServiceCollectionExtensions.AddEncinaAttestation(null!, _ => { });

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddEncinaAttestation_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        var act = () => services.AddEncinaAttestation(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
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

        act.Should().Throw<ArgumentNullException>().WithParameterName("provider");
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

        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
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

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
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

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void UseHashChain_NullOptions_Throws()
    {
        var act = () => AttestationOptionsExtensions.UseHashChain(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void UseHttp_NullOptions_Throws()
    {
        var act = () => AttestationOptionsExtensions.UseHttp(null!, _ => { });

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void UseHttp_NullConfigure_Throws()
    {
        var act = () => new AttestationOptions().UseHttp(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    #endregion
}

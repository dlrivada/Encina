using Encina.Security.AntiTampering;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.HMAC;
using Encina.Security.AntiTampering.Http;
using Encina.Security.AntiTampering.Pipeline;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.GuardTests.Security.AntiTampering;

/// <summary>
/// Guard clause tests for Encina.Security.AntiTampering types.
/// Verifies that null/invalid arguments are properly rejected.
/// </summary>
public class AntiTamperingGuardTests
{
    #region HMACSigner Guard Tests

    [Fact]
    public void HMACSigner_Constructor_NullKeyProvider_ThrowsArgumentNullException()
    {
        var act = () => new HMACSigner(null!, Options.Create(new AntiTamperingOptions()));

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("keyProvider");
    }

    [Fact]
    public void HMACSigner_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var keyProvider = Substitute.For<IKeyProvider>();
        var act = () => new HMACSigner(keyProvider, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public async Task HMACSigner_SignAsync_NullContext_ThrowsArgumentNullException()
    {
        var keyProvider = Substitute.For<IKeyProvider>();
        var sut = new HMACSigner(keyProvider, Options.Create(new AntiTamperingOptions()));

        var act = async () => await sut.SignAsync(ReadOnlyMemory<byte>.Empty, null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task HMACSigner_VerifyAsync_NullContext_ThrowsArgumentNullException()
    {
        var keyProvider = Substitute.For<IKeyProvider>();
        var sut = new HMACSigner(keyProvider, Options.Create(new AntiTamperingOptions()));

        var act = async () => await sut.VerifyAsync(ReadOnlyMemory<byte>.Empty, "sig", null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HMACSigner_VerifyAsync_NullOrWhitespaceSignature_ThrowsArgumentException(string? signature)
    {
        var keyProvider = Substitute.For<IKeyProvider>();
        var sut = new HMACSigner(keyProvider, Options.Create(new AntiTamperingOptions()));
        var context = new SigningContext
        {
            KeyId = "k", HttpMethod = "GET", RequestPath = "/",
            Timestamp = DateTimeOffset.UtcNow, Nonce = "n"
        };

        var act = async () => await sut.VerifyAsync(ReadOnlyMemory<byte>.Empty, signature!, context);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region InMemoryKeyProvider Guard Tests

    [Fact]
    public void InMemoryKeyProvider_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryKeyProvider(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InMemoryKeyProvider_GetKeyAsync_NullOrWhitespaceKeyId_ThrowsArgumentException(string? keyId)
    {
        var sut = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        var act = async () => await sut.GetKeyAsync(keyId!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void InMemoryKeyProvider_AddKey_NullKey_ThrowsArgumentNullException()
    {
        var sut = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        var act = () => sut.AddKey("key-id", null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InMemoryKeyProvider_AddKey_NullOrWhitespaceKeyId_ThrowsArgumentException(string? keyId)
    {
        var sut = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        var act = () => sut.AddKey(keyId!, new byte[32]);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region RequestSigningClient Guard Tests

    [Fact]
    public void RequestSigningClient_Constructor_NullRequestSigner_ThrowsArgumentNullException()
    {
        var act = () => new RequestSigningClient(
            null!, Options.Create(new AntiTamperingOptions()), TimeProvider.System);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("requestSigner");
    }

    [Fact]
    public void RequestSigningClient_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var requestSigner = Substitute.For<IRequestSigner>();
        var act = () => new RequestSigningClient(requestSigner, null!, TimeProvider.System);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void RequestSigningClient_Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var requestSigner = Substitute.For<IRequestSigner>();
        var act = () => new RequestSigningClient(
            requestSigner, Options.Create(new AntiTamperingOptions()), null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("timeProvider");
    }

    [Fact]
    public async Task RequestSigningClient_SignRequestAsync_NullRequest_ThrowsArgumentNullException()
    {
        var requestSigner = Substitute.For<IRequestSigner>();
        var sut = new RequestSigningClient(
            requestSigner, Options.Create(new AntiTamperingOptions()), TimeProvider.System);

        var act = async () => await sut.SignRequestAsync(null!, "key-id");

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RequestSigningClient_SignRequestAsync_NullOrWhitespaceKeyId_ThrowsArgumentException(string? keyId)
    {
        var requestSigner = Substitute.For<IRequestSigner>();
        var sut = new RequestSigningClient(
            requestSigner, Options.Create(new AntiTamperingOptions()), TimeProvider.System);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

        var act = async () => await sut.SignRequestAsync(request, keyId!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region HMACValidationPipelineBehavior Guard Tests

    [Fact]
    public void PipelineBehavior_Constructor_NullRequestSigner_ThrowsArgumentNullException()
    {
        var act = () => new HMACValidationPipelineBehavior<TestCommand, Unit>(
            null!,
            Substitute.For<INonceStore>(),
            Substitute.For<IHttpContextAccessor>(),
            Options.Create(new AntiTamperingOptions()),
            TimeProvider.System,
            Substitute.For<ILogger<HMACValidationPipelineBehavior<TestCommand, Unit>>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("requestSigner");
    }

    [Fact]
    public void PipelineBehavior_Constructor_NullNonceStore_ThrowsArgumentNullException()
    {
        var act = () => new HMACValidationPipelineBehavior<TestCommand, Unit>(
            Substitute.For<IRequestSigner>(),
            null!,
            Substitute.For<IHttpContextAccessor>(),
            Options.Create(new AntiTamperingOptions()),
            TimeProvider.System,
            Substitute.For<ILogger<HMACValidationPipelineBehavior<TestCommand, Unit>>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("nonceStore");
    }

    [Fact]
    public void PipelineBehavior_Constructor_NullHttpContextAccessor_ThrowsArgumentNullException()
    {
        var act = () => new HMACValidationPipelineBehavior<TestCommand, Unit>(
            Substitute.For<IRequestSigner>(),
            Substitute.For<INonceStore>(),
            null!,
            Options.Create(new AntiTamperingOptions()),
            TimeProvider.System,
            Substitute.For<ILogger<HMACValidationPipelineBehavior<TestCommand, Unit>>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpContextAccessor");
    }

    [Fact]
    public void PipelineBehavior_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new HMACValidationPipelineBehavior<TestCommand, Unit>(
            Substitute.For<IRequestSigner>(),
            Substitute.For<INonceStore>(),
            Substitute.For<IHttpContextAccessor>(),
            null!,
            TimeProvider.System,
            Substitute.For<ILogger<HMACValidationPipelineBehavior<TestCommand, Unit>>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void PipelineBehavior_Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new HMACValidationPipelineBehavior<TestCommand, Unit>(
            Substitute.For<IRequestSigner>(),
            Substitute.For<INonceStore>(),
            Substitute.For<IHttpContextAccessor>(),
            Options.Create(new AntiTamperingOptions()),
            null!,
            Substitute.For<ILogger<HMACValidationPipelineBehavior<TestCommand, Unit>>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("timeProvider");
    }

    [Fact]
    public void PipelineBehavior_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new HMACValidationPipelineBehavior<TestCommand, Unit>(
            Substitute.For<IRequestSigner>(),
            Substitute.For<INonceStore>(),
            Substitute.For<IHttpContextAccessor>(),
            Options.Create(new AntiTamperingOptions()),
            TimeProvider.System,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region AntiTamperingOptions Guard Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AntiTamperingOptions_AddKey_NullOrWhitespaceKeyId_ThrowsArgumentException(string? keyId)
    {
        var options = new AntiTamperingOptions();

        var act = () => options.AddKey(keyId!, "secret");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AntiTamperingOptions_AddKey_NullOrWhitespaceSecret_ThrowsArgumentException(string? secret)
    {
        var options = new AntiTamperingOptions();

        var act = () => options.AddKey("key-id", secret!);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region ServiceCollectionExtensions Guard Tests

    [Fact]
    public void AddEncinaAntiTampering_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        var act = () => services.AddEncinaAntiTampering();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddDistributedNonceStore_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        var act = () => services.AddDistributedNonceStore();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    #endregion

    #region Test Types

    [RequireSignature]
    public sealed record TestCommand : ICommand;

    #endregion
}

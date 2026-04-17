using Encina.Security.AntiTampering;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.HMAC;
using Encina.Security.AntiTampering.Http;
using Encina.Security.AntiTampering.Pipeline;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("keyProvider");
    }

    [Fact]
    public void HMACSigner_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var keyProvider = Substitute.For<IKeyProvider>();
        var act = () => new HMACSigner(keyProvider, null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public async Task HMACSigner_SignAsync_NullContext_ThrowsArgumentNullException()
    {
        var keyProvider = Substitute.For<IKeyProvider>();
        var sut = new HMACSigner(keyProvider, Options.Create(new AntiTamperingOptions()));

        var act = async () => await sut.SignAsync(ReadOnlyMemory<byte>.Empty, null!);

        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task HMACSigner_VerifyAsync_NullContext_ThrowsArgumentNullException()
    {
        var keyProvider = Substitute.For<IKeyProvider>();
        var sut = new HMACSigner(keyProvider, Options.Create(new AntiTamperingOptions()));

        var act = async () => await sut.VerifyAsync(ReadOnlyMemory<byte>.Empty, "sig", null!);

        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("context");
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
            KeyId = "k",
            HttpMethod = "GET",
            RequestPath = "/",
            Timestamp = DateTimeOffset.UtcNow,
            Nonce = "n"
        };

        var act = async () => await sut.VerifyAsync(ReadOnlyMemory<byte>.Empty, signature!, context);

        await Should.ThrowAsync<ArgumentException>(act);
    }

    #endregion

    #region InMemoryKeyProvider Guard Tests

    [Fact]
    public void InMemoryKeyProvider_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryKeyProvider(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InMemoryKeyProvider_GetKeyAsync_NullOrWhitespaceKeyId_ThrowsArgumentException(string? keyId)
    {
        var sut = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        var act = async () => await sut.GetKeyAsync(keyId!);

        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public void InMemoryKeyProvider_AddKey_NullKey_ThrowsArgumentNullException()
    {
        var sut = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        var act = () => sut.AddKey("key-id", null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("key");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InMemoryKeyProvider_AddKey_NullOrWhitespaceKeyId_ThrowsArgumentException(string? keyId)
    {
        var sut = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        var act = () => sut.AddKey(keyId!, new byte[32]);

        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region RequestSigningClient Guard Tests

    [Fact]
    public void RequestSigningClient_Constructor_NullRequestSigner_ThrowsArgumentNullException()
    {
        var act = () => new RequestSigningClient(
            null!, Options.Create(new AntiTamperingOptions()), TimeProvider.System);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("requestSigner");
    }

    [Fact]
    public void RequestSigningClient_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var requestSigner = Substitute.For<IRequestSigner>();
        var act = () => new RequestSigningClient(requestSigner, null!, TimeProvider.System);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void RequestSigningClient_Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var requestSigner = Substitute.For<IRequestSigner>();
        var act = () => new RequestSigningClient(
            requestSigner, Options.Create(new AntiTamperingOptions()), null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public async Task RequestSigningClient_SignRequestAsync_NullRequest_ThrowsArgumentNullException()
    {
        var requestSigner = Substitute.For<IRequestSigner>();
        var sut = new RequestSigningClient(
            requestSigner, Options.Create(new AntiTamperingOptions()), TimeProvider.System);

        var act = async () => await sut.SignRequestAsync(null!, "key-id");

        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("request");
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

        await Should.ThrowAsync<ArgumentException>(act);
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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("requestSigner");
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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("nonceStore");
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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("httpContextAccessor");
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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("timeProvider");
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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
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

        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AntiTamperingOptions_AddKey_NullOrWhitespaceSecret_ThrowsArgumentException(string? secret)
    {
        var options = new AntiTamperingOptions();

        var act = () => options.AddKey("key-id", secret!);

        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region ServiceCollectionExtensions Guard Tests

    [Fact]
    public void AddEncinaAntiTampering_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        var act = () => services.AddEncinaAntiTampering();

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddDistributedNonceStore_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        var act = () => services.AddDistributedNonceStore();

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    #endregion

    #region Test Types

    [RequireSignature]
    public sealed record TestCommand : ICommand;

    #endregion
}

#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using System.Net.Http.Json;
using Encina.Security.AntiTampering;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.HMAC;
using Encina.Security.AntiTampering.Http;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.AntiTampering;

/// <summary>
/// Unit tests for <see cref="RequestSigningClient"/>.
/// Verifies header attachment, nonce generation, and error propagation.
/// </summary>
public sealed class RequestSigningClientTests
{
    private readonly IRequestSigner _requestSigner;
    private readonly AntiTamperingOptions _options;
    private readonly FakeTimeProvider _timeProvider;
    private readonly RequestSigningClient _sut;

    public RequestSigningClientTests()
    {
        _requestSigner = Substitute.For<IRequestSigner>();
        _options = new AntiTamperingOptions();
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _sut = new RequestSigningClient(_requestSigner, Options.Create(_options), _timeProvider);
    }

    #region Header Attachment

    [Fact]
    public async Task SignRequestAsync_ValidRequest_AddsAllHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/orders")
        {
            Content = JsonContent.Create(new { Amount = 42.00m })
        };

        _requestSigner.SignAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<SigningContext>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(Right("base64-signature")));

        // Act
        var result = await _sut.SignRequestAsync(request, "api-key-v1");

        // Assert
        result.IsRight.Should().BeTrue();
        var signedRequest = (HttpRequestMessage)result;

        signedRequest.Headers.GetValues(_options.SignatureHeader).Should().ContainSingle("base64-signature");
        signedRequest.Headers.GetValues(_options.TimestampHeader).Should().ContainSingle();
        signedRequest.Headers.GetValues(_options.NonceHeader).Should().ContainSingle();
        signedRequest.Headers.GetValues(_options.KeyIdHeader).Should().ContainSingle("api-key-v1");
    }

    [Fact]
    public async Task SignRequestAsync_NoContent_AddsHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/health");

        _requestSigner.SignAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<SigningContext>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(Right("sig-no-body")));

        // Act
        var result = await _sut.SignRequestAsync(request, "test-key");

        // Assert
        result.IsRight.Should().BeTrue();
        var signedRequest = (HttpRequestMessage)result;
        signedRequest.Headers.GetValues(_options.SignatureHeader).Should().ContainSingle("sig-no-body");
    }

    [Fact]
    public async Task SignRequestAsync_GeneratesUniqueNonce()
    {
        // Arrange
        _requestSigner.SignAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<SigningContext>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(Right("sig")));

        var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/1");
        var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/2");

        // Act
        var result1 = await _sut.SignRequestAsync(request1, "key");
        var result2 = await _sut.SignRequestAsync(request2, "key");

        // Assert
        var nonce1 = ((HttpRequestMessage)result1).Headers.GetValues(_options.NonceHeader).First();
        var nonce2 = ((HttpRequestMessage)result2).Headers.GetValues(_options.NonceHeader).First();
        nonce1.Should().NotBe(nonce2);
    }

    [Fact]
    public async Task SignRequestAsync_UsesCurrentTimestamp()
    {
        // Arrange
        var expectedTime = _timeProvider.GetUtcNow();

        _requestSigner.SignAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<SigningContext>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(Right("sig")));

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/");

        // Act
        var result = await _sut.SignRequestAsync(request, "key");

        // Assert
        var timestamp = ((HttpRequestMessage)result).Headers.GetValues(_options.TimestampHeader).First();
        DateTimeOffset.TryParse(timestamp, out var parsed).Should().BeTrue();
        parsed.Should().Be(expectedTime);
    }

    #endregion

    #region Error Propagation

    [Fact]
    public async Task SignRequestAsync_SignerFails_PropagatesError()
    {
        // Arrange
        var error = AntiTamperingErrors.KeyNotFound("missing-key");

        _requestSigner.SignAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<SigningContext>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(Left(error)));

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/");

        // Act
        var result = await _sut.SignRequestAsync(request, "missing-key");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion
}

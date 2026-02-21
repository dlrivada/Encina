#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using System.Text;
using Encina.Security.AntiTampering;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.HMAC;
using Encina.Security.AntiTampering.Pipeline;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.AntiTampering;

/// <summary>
/// Unit tests for <see cref="HMACValidationPipelineBehavior{TRequest,TResponse}"/>.
/// Verifies attribute detection, header validation, timestamp, nonce, and signature checks.
/// </summary>
public sealed class HMACValidationPipelineBehaviorTests
{
    private readonly IRequestSigner _requestSigner;
    private readonly INonceStore _nonceStore;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AntiTamperingOptions _options;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<HMACValidationPipelineBehavior<TestSignedCommand, Unit>> _logger;
    private readonly IRequestContext _context;

    public HMACValidationPipelineBehaviorTests()
    {
        _requestSigner = Substitute.For<IRequestSigner>();
        _nonceStore = Substitute.For<INonceStore>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _options = new AntiTamperingOptions();
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _logger = Substitute.For<ILogger<HMACValidationPipelineBehavior<TestSignedCommand, Unit>>>();
        _context = RequestContext.CreateForTest(userId: "user-1");
    }

    private HMACValidationPipelineBehavior<TestSignedCommand, Unit> CreateSut() =>
        new(_requestSigner, _nonceStore, _httpContextAccessor,
            Options.Create(_options), _timeProvider, _logger);

    private HMACValidationPipelineBehavior<TestPlainCommand, Unit> CreatePlainSut() =>
        new(
            _requestSigner,
            _nonceStore,
            _httpContextAccessor,
            Options.Create(_options),
            _timeProvider,
            Substitute.For<ILogger<HMACValidationPipelineBehavior<TestPlainCommand, Unit>>>());

    private static RequestHandlerCallback<Unit> SuccessNextStep() =>
        () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default));

    private DefaultHttpContext SetupHttpContext(
        string signature = "valid-sig",
        string? timestamp = null,
        string nonce = "nonce-123",
        string keyId = "test-key",
        string method = "POST",
        string path = "/api/test",
        string body = "{}")
    {
        timestamp ??= _timeProvider.GetUtcNow().ToString("O");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        httpContext.Request.Headers[_options.SignatureHeader] = signature;
        httpContext.Request.Headers[_options.TimestampHeader] = timestamp;
        httpContext.Request.Headers[_options.NonceHeader] = nonce;
        httpContext.Request.Headers[_options.KeyIdHeader] = keyId;

        var bodyBytes = Encoding.UTF8.GetBytes(body);
        httpContext.Request.Body = new MemoryStream(bodyBytes);
        httpContext.Request.ContentLength = bodyBytes.Length;

        _httpContextAccessor.HttpContext.Returns(httpContext);

        return httpContext;
    }

    #region Bypass (No Attribute / No HttpContext)

    [Fact]
    public async Task Handle_NoAttribute_PassesThrough()
    {
        // Arrange
        var sut = CreatePlainSut();
        var request = new TestPlainCommand();
        var nextCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default));
        };

        // Act
        var result = await sut.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoHttpContext_PassesThrough()
    {
        // Arrange
        var sut = CreateSut();
        var request = new TestSignedCommand();
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        var result = await sut.Handle(request, _context, SuccessNextStep(), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Missing Headers

    [Fact]
    public async Task Handle_MissingSignatureHeader_ReturnsError()
    {
        // Arrange
        var sut = CreateSut();
        SetupHttpContext(signature: "");

        // Act
        var result = await sut.Handle(new TestSignedCommand(), _context, SuccessNextStep(), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().IfNone("").Should().Be(AntiTamperingErrors.SignatureMissingCode);
    }

    [Fact]
    public async Task Handle_MissingKeyIdHeader_ReturnsError()
    {
        // Arrange
        var sut = CreateSut();
        SetupHttpContext(keyId: "");

        // Act
        var result = await sut.Handle(new TestSignedCommand(), _context, SuccessNextStep(), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().IfNone("").Should().Be(AntiTamperingErrors.SignatureMissingCode);
    }

    #endregion

    #region Timestamp Validation

    [Fact]
    public async Task Handle_ExpiredTimestamp_ReturnsError()
    {
        // Arrange
        var sut = CreateSut();
        var oldTimestamp = _timeProvider.GetUtcNow().AddMinutes(-(_options.TimestampToleranceMinutes + 1));
        SetupHttpContext(timestamp: oldTimestamp.ToString("O"));

        // Act
        var result = await sut.Handle(new TestSignedCommand(), _context, SuccessNextStep(), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().IfNone("").Should().Be(AntiTamperingErrors.TimestampExpiredCode);
    }

    [Fact]
    public async Task Handle_InvalidTimestampFormat_ReturnsError()
    {
        // Arrange
        var sut = CreateSut();
        SetupHttpContext(timestamp: "not-a-date");

        // Act
        var result = await sut.Handle(new TestSignedCommand(), _context, SuccessNextStep(), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().IfNone("").Should().Be(AntiTamperingErrors.TimestampExpiredCode);
    }

    #endregion

    #region Nonce Validation

    [Fact]
    public async Task Handle_ReusedNonce_ReturnsError()
    {
        // Arrange
        var sut = CreateSut();
        SetupHttpContext();

        _nonceStore.TryAddAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(false));

        // Act
        var result = await sut.Handle(new TestSignedCommand(), _context, SuccessNextStep(), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().IfNone("").Should().Be(AntiTamperingErrors.NonceReusedCode);
    }

    [Fact]
    public async Task Handle_MissingNonce_WhenRequired_ReturnsError()
    {
        // Arrange
        var sut = CreateSut();
        _options.RequireNonce = true;
        SetupHttpContext(nonce: "");

        // Act
        var result = await sut.Handle(new TestSignedCommand(), _context, SuccessNextStep(), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().IfNone("").Should().Be(AntiTamperingErrors.NonceMissingCode);
    }

    #endregion

    #region Signature Validation

    [Fact]
    public async Task Handle_ValidSignature_CallsNextStep()
    {
        // Arrange
        var sut = CreateSut();
        SetupHttpContext();
        var nextCalled = false;

        _nonceStore.TryAddAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(true));

        _requestSigner.VerifyAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<string>(),
                Arg.Any<SigningContext>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(Right(true)));

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default));
        };

        // Act
        var result = await sut.Handle(new TestSignedCommand(), _context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidSignature_ReturnsError()
    {
        // Arrange
        var sut = CreateSut();
        SetupHttpContext();

        _nonceStore.TryAddAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(true));

        _requestSigner.VerifyAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<string>(),
                Arg.Any<SigningContext>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(Right(false)));

        // Act
        var result = await sut.Handle(new TestSignedCommand(), _context, SuccessNextStep(), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().IfNone("").Should().Be(AntiTamperingErrors.SignatureInvalidCode);
    }

    #endregion

    #region Test Types

    [RequireSignature]
    public sealed record TestSignedCommand : ICommand;

    public sealed record TestPlainCommand : ICommand;

    #endregion
}

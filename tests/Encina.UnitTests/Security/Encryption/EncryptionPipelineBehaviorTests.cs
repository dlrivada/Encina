#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using System.Security.Cryptography;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Encryption;

public sealed class EncryptionPipelineBehaviorTests : IDisposable
{
    private readonly IEncryptionOrchestrator _orchestrator;
    private readonly ILogger<EncryptionPipelineBehavior<TestCommand, Unit>> _logger;
    private readonly EncryptionOptions _options;
    private readonly EncryptionPipelineBehavior<TestCommand, Unit> _sut;
    private readonly IRequestContext _context;

    public EncryptionPipelineBehaviorTests()
    {
        _orchestrator = Substitute.For<IEncryptionOrchestrator>();
        _logger = Substitute.For<ILogger<EncryptionPipelineBehavior<TestCommand, Unit>>>();
        _options = new EncryptionOptions();
        var optionsWrapper = Options.Create(_options);
        _sut = new EncryptionPipelineBehavior<TestCommand, Unit>(_orchestrator, optionsWrapper, _logger);
        _context = RequestContext.CreateForTest(userId: "user-1");

        EncryptedPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        EncryptedPropertyCache.ClearCache();
    }

    #region No Attributes (Passthrough)

    [Fact]
    public async Task Handle_NoEncryptedProperties_PassesThrough()
    {
        var request = new TestPlainCommand { Name = "John" };
        var behavior = CreateBehavior<TestPlainCommand, Unit>();

        var result = await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        await _orchestrator.DidNotReceive().EncryptAsync(
            Arg.Any<TestPlainCommand>(),
            Arg.Any<IRequestContext>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Pre-handler Encryption

    [Fact]
    public async Task Handle_WithEncryptedProperties_CallsEncrypt()
    {
        var request = new TestCommand { Email = "user@test.com" };

        _orchestrator.EncryptAsync(Arg.Any<TestCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestCommand>>(Right(request)));

        var result = await _sut.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        await _orchestrator.Received(1).EncryptAsync(
            Arg.Any<TestCommand>(),
            Arg.Any<IRequestContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EncryptionFails_ReturnsError()
    {
        var request = new TestCommand { Email = "user@test.com" };
        var error = EncinaError.New("Encryption failed");

        _orchestrator.EncryptAsync(Arg.Any<TestCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestCommand>>(Left(error)));

        var result = await _sut.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region Pre-handler Decryption (DecryptOnReceive)

    [Fact]
    public async Task Handle_WithDecryptOnReceive_CallsDecrypt()
    {
        var request = new TestDecryptOnReceiveCommand { EncryptedData = "ENC:v1:..." };
        var behavior = CreateBehavior<TestDecryptOnReceiveCommand, Unit>();

        _orchestrator.DecryptAsync(Arg.Any<TestDecryptOnReceiveCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestDecryptOnReceiveCommand>>(Right(request)));
        _orchestrator.EncryptAsync(Arg.Any<TestDecryptOnReceiveCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestDecryptOnReceiveCommand>>(Right(request)));

        var result = await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        await _orchestrator.Received(1).DecryptAsync(
            Arg.Any<TestDecryptOnReceiveCommand>(),
            Arg.Any<IRequestContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DecryptionFails_ReturnsError()
    {
        var request = new TestDecryptOnReceiveCommand { EncryptedData = "bad-data" };
        var behavior = CreateBehavior<TestDecryptOnReceiveCommand, Unit>();
        var error = EncinaError.New("Decryption failed");

        _orchestrator.DecryptAsync(Arg.Any<TestDecryptOnReceiveCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestDecryptOnReceiveCommand>>(Left(error)));

        var result = await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region Handler delegates to nextStep

    [Fact]
    public async Task Handle_InvokesNextStep()
    {
        var request = new TestPlainCommand { Name = "John" };
        var behavior = CreateBehavior<TestPlainCommand, Unit>();
        var nextStepCalled = false;

        var result = await behavior.Handle(
            request,
            _context,
            () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default));
            },
            CancellationToken.None);

        nextStepCalled.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private EncryptionPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        var logger = Substitute.For<ILogger<EncryptionPipelineBehavior<TRequest, TResponse>>>();
        return new EncryptionPipelineBehavior<TRequest, TResponse>(
            _orchestrator,
            Options.Create(_options),
            logger);
    }

    #endregion

    #region Test Request Types

    public sealed class TestCommand : ICommand<Unit>
    {
        [Encrypt(Purpose = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class TestPlainCommand : ICommand<Unit>
    {
        public string Name { get; set; } = string.Empty;
    }

    [DecryptOnReceive]
    public sealed class TestDecryptOnReceiveCommand : ICommand<Unit>
    {
        [Encrypt(Purpose = "Payload")]
        public string EncryptedData { get; set; } = string.Empty;
    }

    #endregion
}

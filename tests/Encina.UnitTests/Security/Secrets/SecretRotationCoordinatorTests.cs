#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Rotation;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretRotationCoordinatorTests
{
    private readonly ISecretRotationHandler _handler;
    private readonly ISecretRotator _rotator;
    private readonly ISecretReader _reader;
    private readonly ILogger<SecretRotationCoordinator> _logger;

    public SecretRotationCoordinatorTests()
    {
        _handler = Substitute.For<ISecretRotationHandler>();
        _rotator = Substitute.For<ISecretRotator>();
        _reader = Substitute.For<ISecretReader>();
        _logger = Substitute.For<ILogger<SecretRotationCoordinator>>();
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_NullHandlers_ThrowsArgumentNullException()
    {
        var act = () => new SecretRotationCoordinator(null!, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("handlers");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SecretRotationCoordinator([], logger: null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullRotator_DoesNotThrow()
    {
        var act = () => new SecretRotationCoordinator([_handler], _logger, rotator: null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NullReader_DoesNotThrow()
    {
        var act = () => new SecretRotationCoordinator([_handler], _logger, reader: null);

        act.Should().NotThrow();
    }

    #endregion

    #region RotateWithCallbacksAsync - No Handler

    [Fact]
    public async Task RotateWithCallbacksAsync_NoHandler_ReturnsRotationFailed()
    {
        var coordinator = new SecretRotationCoordinator([], _logger);

        var result = await coordinator.RotateWithCallbacksAsync("db-password");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.RotationFailedCode));
    }

    #endregion

    #region RotateWithCallbacksAsync - Full Workflow

    [Fact]
    public async Task RotateWithCallbacksAsync_FullWorkflow_Success()
    {
        SetupHandler("db-password", "new-password");
        SetupRotator("db-password");
        SetupReader("db-password", "old-password");

        var coordinator = new SecretRotationCoordinator([_handler], _logger, _rotator, _reader);

        var result = await coordinator.RotateWithCallbacksAsync("db-password");

        result.IsRight.Should().BeTrue();

        // Verify the full workflow was executed
        await _reader.Received(1).GetSecretAsync("db-password", Arg.Any<CancellationToken>());
        await _handler.Received(1).GenerateNewSecretAsync("db-password", Arg.Any<CancellationToken>());
        await _rotator.Received(1).RotateSecretAsync("db-password", Arg.Any<CancellationToken>());
        await _handler.Received(1).OnRotationAsync("db-password", "old-password", "new-password", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateWithCallbacksAsync_NoReader_SkipsCurrentValueRetrieval()
    {
        SetupHandler("key", "new-value");
        SetupRotator("key");

        var coordinator = new SecretRotationCoordinator([_handler], _logger, _rotator, reader: null);

        var result = await coordinator.RotateWithCallbacksAsync("key");

        result.IsRight.Should().BeTrue();
        // OnRotation should be called with empty string for oldValue
        await _handler.Received(1).OnRotationAsync("key", string.Empty, "new-value", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateWithCallbacksAsync_NoRotator_SkipsVaultRotation()
    {
        SetupHandler("key", "new-value");
        SetupReader("key", "old-value");

        var coordinator = new SecretRotationCoordinator([_handler], _logger, rotator: null, reader: _reader);

        var result = await coordinator.RotateWithCallbacksAsync("key");

        result.IsRight.Should().BeTrue();
        await _rotator.DidNotReceive().RotateSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region RotateWithCallbacksAsync - Step Failures

    [Fact]
    public async Task RotateWithCallbacksAsync_GenerateFails_ReturnsError()
    {
        _handler.GenerateNewSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.RotationFailed("key", "generation failed")));

        var coordinator = new SecretRotationCoordinator([_handler], _logger, _rotator);

        var result = await coordinator.RotateWithCallbacksAsync("key");

        result.IsLeft.Should().BeTrue();
        // Should NOT proceed to rotate or notify
        await _rotator.DidNotReceive().RotateSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _handler.DidNotReceive().OnRotationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateWithCallbacksAsync_VaultRotationFails_ReturnsError()
    {
        SetupHandler("key", "new-value");
        _rotator.RotateSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(
                SecretsErrors.RotationFailed("key", "vault unavailable")));

        var coordinator = new SecretRotationCoordinator([_handler], _logger, _rotator);

        var result = await coordinator.RotateWithCallbacksAsync("key");

        result.IsLeft.Should().BeTrue();
        // Should NOT proceed to notify
        await _handler.DidNotReceive().OnRotationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateWithCallbacksAsync_NotificationFails_ReturnsError()
    {
        _handler.GenerateNewSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("new-value"));
        _handler.OnRotationAsync("key", Arg.Any<string>(), "new-value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(
                SecretsErrors.RotationFailed("key", "notification failed")));

        var coordinator = new SecretRotationCoordinator([_handler], _logger);

        var result = await coordinator.RotateWithCallbacksAsync("key");

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region RotateWithCallbacksAsync - Reader Failure

    [Fact]
    public async Task RotateWithCallbacksAsync_ReaderFails_ContinuesWithEmptyOldValue()
    {
        SetupHandler("key", "new-value");
        SetupRotator("key");

        _reader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound("key")));

        var coordinator = new SecretRotationCoordinator([_handler], _logger, _rotator, _reader);

        var result = await coordinator.RotateWithCallbacksAsync("key");

        result.IsRight.Should().BeTrue();
        // oldValue should be empty string when reader fails
        await _handler.Received(1).OnRotationAsync("key", string.Empty, "new-value", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Input Validation

    [Fact]
    public async Task RotateWithCallbacksAsync_NullSecretName_ThrowsArgumentException()
    {
        var coordinator = new SecretRotationCoordinator([_handler], _logger);

        var act = () => coordinator.RotateWithCallbacksAsync(null!).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RotateWithCallbacksAsync_EmptySecretName_ThrowsArgumentException()
    {
        var coordinator = new SecretRotationCoordinator([_handler], _logger);

        var act = () => coordinator.RotateWithCallbacksAsync("").AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Helpers

    private void SetupHandler(string secretName, string newValue)
    {
        _handler.GenerateNewSecretAsync(secretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(newValue));
        _handler.OnRotationAsync(secretName, Arg.Any<string>(), newValue, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));
    }

    private void SetupRotator(string secretName)
    {
        _rotator.RotateSecretAsync(secretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));
    }

    private void SetupReader(string secretName, string currentValue)
    {
        _reader.GetSecretAsync(secretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(currentValue));
    }

    #endregion
}

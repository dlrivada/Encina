using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Rotation;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Security.Secrets.Rotation;

/// <summary>
/// Guard tests for <see cref="SecretRotationCoordinator"/> including constructor and method-level guards.
/// </summary>
public sealed class SecretRotationCoordinatorGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullHandlers_ThrowsArgumentNullException()
    {
        var act = () => new SecretRotationCoordinator(
            null!, NullLogger<SecretRotationCoordinator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("handlers");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SecretRotationCoordinator(
            Enumerable.Empty<ISecretRotationHandler>(), null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Method Guards

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RotateWithCallbacksAsync_NullOrWhitespaceSecretName_ThrowsArgumentException(string? secretName)
    {
        var sut = new SecretRotationCoordinator(
            Enumerable.Empty<ISecretRotationHandler>(),
            NullLogger<SecretRotationCoordinator>.Instance);

        var act = () => sut.RotateWithCallbacksAsync(secretName!).AsTask();
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task RotateWithCallbacksAsync_NoHandlers_ReturnsLeft()
    {
        var sut = new SecretRotationCoordinator(
            Enumerable.Empty<ISecretRotationHandler>(),
            NullLogger<SecretRotationCoordinator>.Instance);

        var result = await sut.RotateWithCallbacksAsync("test-secret");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion
}

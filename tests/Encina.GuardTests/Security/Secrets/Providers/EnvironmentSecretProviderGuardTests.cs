using Encina.Security.Secrets;
using Encina.Security.Secrets.Providers;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Security.Secrets.Providers;

/// <summary>
/// Guard tests for <see cref="EnvironmentSecretProvider"/> including constructor and method-level guards.
/// </summary>
public sealed class EnvironmentSecretProviderGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new EnvironmentSecretProvider(null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Method Guards

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsync_NullOrWhitespaceSecretName_ThrowsArgumentException(string? secretName)
    {
        var sut = new EnvironmentSecretProvider(NullLogger<EnvironmentSecretProvider>.Instance);

        var act = () => sut.GetSecretAsync(secretName!).AsTask();
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsyncT_NullOrWhitespaceSecretName_ThrowsArgumentException(string? secretName)
    {
        var sut = new EnvironmentSecretProvider(NullLogger<EnvironmentSecretProvider>.Instance);

        var act = () => sut.GetSecretAsync<object>(secretName!).AsTask();
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetSecretAsync_NonexistentEnvVar_ReturnsLeft()
    {
        var sut = new EnvironmentSecretProvider(NullLogger<EnvironmentSecretProvider>.Instance);

        var result = await sut.GetSecretAsync("ENCINA_TEST_NONEXISTENT_" + Guid.NewGuid().ToString("N"));

        result.IsLeft.ShouldBeTrue();
    }

    #endregion
}

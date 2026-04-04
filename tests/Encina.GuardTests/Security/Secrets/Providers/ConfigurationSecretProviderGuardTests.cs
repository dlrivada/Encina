using Encina.Security.Secrets;
using Encina.Security.Secrets.Providers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Security.Secrets.Providers;

/// <summary>
/// Guard tests for <see cref="ConfigurationSecretProvider"/> including constructor and method-level guards.
/// </summary>
public sealed class ConfigurationSecretProviderGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        var act = () => new ConfigurationSecretProvider(
            null!, NullLogger<ConfigurationSecretProvider>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configuration");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ConfigurationSecretProvider(
            Substitute.For<IConfiguration>(), null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceSectionPath_ThrowsArgumentException(string? sectionPath)
    {
        var act = () => new ConfigurationSecretProvider(
            Substitute.For<IConfiguration>(),
            NullLogger<ConfigurationSecretProvider>.Instance,
            sectionPath!);
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Method Guards

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsync_NullOrWhitespaceSecretName_ThrowsArgumentException(string? secretName)
    {
        var sut = new ConfigurationSecretProvider(
            Substitute.For<IConfiguration>(),
            NullLogger<ConfigurationSecretProvider>.Instance);

        var act = () => sut.GetSecretAsync(secretName!).AsTask();
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsyncT_NullOrWhitespaceSecretName_ThrowsArgumentException(string? secretName)
    {
        var sut = new ConfigurationSecretProvider(
            Substitute.For<IConfiguration>(),
            NullLogger<ConfigurationSecretProvider>.Instance);

        var act = () => sut.GetSecretAsync<object>(secretName!).AsTask();
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetSecretAsync_MissingKey_ReturnsLeft()
    {
        var config = new ConfigurationBuilder().Build();
        var sut = new ConfigurationSecretProvider(
            config, NullLogger<ConfigurationSecretProvider>.Instance);

        var result = await sut.GetSecretAsync("nonexistent-secret");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion
}

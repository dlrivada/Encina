using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using FluentAssertions;

namespace Encina.GuardTests.Security.Secrets.Caching;

/// <summary>
/// Guard clause tests for <see cref="CachingSecretReaderDecorator"/>.
/// Verifies that null arguments are properly rejected in the constructor and methods.
/// </summary>
public sealed class CachingSecretReaderDecoratorGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretReaderDecorator(
            null!,
            Substitute.For<ICacheProvider>(),
            new SecretCachingOptions(),
            new SecretsOptions(),
            Substitute.For<ILogger<CachingSecretReaderDecorator>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretReaderDecorator(
            Substitute.For<ISecretReader>(),
            null!,
            new SecretCachingOptions(),
            new SecretsOptions(),
            Substitute.For<ILogger<CachingSecretReaderDecorator>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void Constructor_NullCachingOptions_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretReaderDecorator(
            Substitute.For<ISecretReader>(),
            Substitute.For<ICacheProvider>(),
            null!,
            new SecretsOptions(),
            Substitute.For<ILogger<CachingSecretReaderDecorator>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cachingOptions");
    }

    [Fact]
    public void Constructor_NullSecretsOptions_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretReaderDecorator(
            Substitute.For<ISecretReader>(),
            Substitute.For<ICacheProvider>(),
            new SecretCachingOptions(),
            null!,
            Substitute.For<ILogger<CachingSecretReaderDecorator>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("secretsOptions");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretReaderDecorator(
            Substitute.For<ISecretReader>(),
            Substitute.For<ICacheProvider>(),
            new SecretCachingOptions(),
            new SecretsOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetSecretAsync Guards

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsync_NullOrWhiteSpaceSecretName_ThrowsArgumentException(string? secretName)
    {
        var sut = CreateSut();

        var act = async () => await sut.GetSecretAsync(secretName!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(secretName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsyncTyped_NullOrWhiteSpaceSecretName_ThrowsArgumentException(string? secretName)
    {
        var sut = CreateSut();

        var act = async () => await sut.GetSecretAsync<FakeTypedSecret>(secretName!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(secretName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidateAsync_NullOrWhiteSpaceSecretName_ThrowsArgumentException(string? secretName)
    {
        var sut = CreateSut();

        var act = async () => await sut.InvalidateAsync(secretName!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(secretName));
    }

    #endregion

    #region Helpers

    private static CachingSecretReaderDecorator CreateSut() =>
        new(
            Substitute.For<ISecretReader>(),
            Substitute.For<ICacheProvider>(),
            new SecretCachingOptions(),
            new SecretsOptions(),
            Substitute.For<ILogger<CachingSecretReaderDecorator>>());

    private sealed class FakeTypedSecret
    {
        public string Value { get; set; } = string.Empty;
    }

    #endregion
}

using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using FluentAssertions;

namespace Encina.GuardTests.Security.Secrets.Caching;

/// <summary>
/// Guard clause tests for <see cref="CachingSecretWriterDecorator"/>.
/// Verifies that null arguments are properly rejected in the constructor and methods.
/// </summary>
public sealed class CachingSecretWriterDecoratorGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretWriterDecorator(
            null!,
            Substitute.For<ICacheProvider>(),
            Substitute.For<IPubSubProvider>(),
            new SecretCachingOptions(),
            Substitute.For<ILogger<CachingSecretWriterDecorator>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretWriterDecorator(
            Substitute.For<ISecretWriter>(),
            null!,
            Substitute.For<IPubSubProvider>(),
            new SecretCachingOptions(),
            Substitute.For<ILogger<CachingSecretWriterDecorator>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretWriterDecorator(
            Substitute.For<ISecretWriter>(),
            Substitute.For<ICacheProvider>(),
            Substitute.For<IPubSubProvider>(),
            null!,
            Substitute.For<ILogger<CachingSecretWriterDecorator>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretWriterDecorator(
            Substitute.For<ISecretWriter>(),
            Substitute.For<ICacheProvider>(),
            Substitute.For<IPubSubProvider>(),
            new SecretCachingOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullPubSub_DoesNotThrow()
    {
        // PubSub is explicitly nullable — no guard expected
        var act = () => new CachingSecretWriterDecorator(
            Substitute.For<ISecretWriter>(),
            Substitute.For<ICacheProvider>(),
            null,
            new SecretCachingOptions(),
            Substitute.For<ILogger<CachingSecretWriterDecorator>>());

        act.Should().NotThrow();
    }

    #endregion

    #region SetSecretAsync Guards

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetSecretAsync_NullOrWhiteSpaceSecretName_ThrowsArgumentException(string? secretName)
    {
        var sut = CreateSut();

        var act = async () => await sut.SetSecretAsync(secretName!, "value");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(secretName));
    }

    [Fact]
    public async Task SetSecretAsync_NullValue_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.SetSecretAsync("key", null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("value");
    }

    #endregion

    #region Helpers

    private static CachingSecretWriterDecorator CreateSut() =>
        new(
            Substitute.For<ISecretWriter>(),
            Substitute.For<ICacheProvider>(),
            null,
            new SecretCachingOptions(),
            Substitute.For<ILogger<CachingSecretWriterDecorator>>());

    #endregion
}

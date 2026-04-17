using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using Shouldly;

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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("inner");
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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("cache");
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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
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

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
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

        Should.NotThrow(act);
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

        (await Should.ThrowAsync<ArgumentException>(act))
            .ParamName.ShouldBe(nameof(secretName));
    }

    [Fact]
    public async Task SetSecretAsync_NullValue_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.SetSecretAsync("key", null!);

        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("value");
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

using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Providers;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Security.Secrets.Providers;

/// <summary>
/// Guard tests for <see cref="FailoverSecretReader"/> including constructor and method-level guards.
/// </summary>
public sealed class FailoverSecretReaderGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullProviders_ThrowsArgumentNullException()
    {
        var act = () => new FailoverSecretReader(
            null!, NullLogger<FailoverSecretReader>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("providers");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new FailoverSecretReader(
            [Substitute.For<ISecretReader>()], null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_EmptyProviders_ThrowsArgumentException()
    {
        var act = () => new FailoverSecretReader(
            Enumerable.Empty<ISecretReader>(),
            NullLogger<FailoverSecretReader>.Instance);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("providers");
    }

    #endregion

    #region Method Guards

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsync_NullOrWhitespaceSecretName_ThrowsArgumentException(string? secretName)
    {
        var provider = Substitute.For<ISecretReader>();
        var sut = new FailoverSecretReader(
            [provider], NullLogger<FailoverSecretReader>.Instance);

        var act = () => sut.GetSecretAsync(secretName!).AsTask();
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsyncT_NullOrWhitespaceSecretName_ThrowsArgumentException(string? secretName)
    {
        var provider = Substitute.For<ISecretReader>();
        var sut = new FailoverSecretReader(
            [provider], NullLogger<FailoverSecretReader>.Instance);

        var act = () => sut.GetSecretAsync<object>(secretName!).AsTask();
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetSecretAsync_AllProvidersFail_ReturnsLeft()
    {
        var provider = Substitute.For<ISecretReader>();
        provider.GetSecretAsync("test", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, string>.Left(
                EncinaErrors.Create("test.error", "provider failed")));

        var sut = new FailoverSecretReader(
            [provider], NullLogger<FailoverSecretReader>.Instance);

        var result = await sut.GetSecretAsync("test");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ProviderCount_SingleProvider_ReturnsOne()
    {
        var provider = Substitute.For<ISecretReader>();
        var sut = new FailoverSecretReader(
            [provider], NullLogger<FailoverSecretReader>.Instance);

        sut.ProviderCount.ShouldBe(1);
    }

    #endregion

    #region Extension Method Guards

    [Fact]
    public void WithFailover_NullPrimary_ThrowsArgumentNullException()
    {
        ISecretReader primary = null!;
        var act = () => primary.WithFailover(
            NullLogger<FailoverSecretReader>.Instance,
            Substitute.For<ISecretReader>());
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("primary");
    }

    [Fact]
    public void WithFailover_NullLogger_ThrowsArgumentNullException()
    {
        var primary = Substitute.For<ISecretReader>();
        var act = () => primary.WithFailover(null!, Substitute.For<ISecretReader>());
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void WithFailover_NullFallbacks_ThrowsArgumentNullException()
    {
        var primary = Substitute.For<ISecretReader>();
        var act = () => primary.WithFailover(
            NullLogger<FailoverSecretReader>.Instance, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("fallbacks");
    }

    #endregion
}

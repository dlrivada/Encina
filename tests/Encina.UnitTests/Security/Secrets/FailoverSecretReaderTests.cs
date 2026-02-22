#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Providers;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets;

public sealed class FailoverSecretReaderTests
{
    private readonly ILogger<FailoverSecretReader> _logger;

    public FailoverSecretReaderTests()
    {
        _logger = Substitute.For<ILogger<FailoverSecretReader>>();
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_NullProviders_ThrowsArgumentNullException()
    {
        var act = () => new FailoverSecretReader(null!, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("providers");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var providers = new[] { Substitute.For<ISecretReader>() };

        var act = () => new FailoverSecretReader(providers, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_EmptyProviders_ThrowsArgumentException()
    {
        var act = () => new FailoverSecretReader([], _logger);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("providers");
    }

    #endregion

    #region GetSecretAsync - Single Provider

    [Fact]
    public async Task GetSecretAsync_SingleProvider_Success_ReturnsValue()
    {
        var provider = Substitute.For<ISecretReader>();
        provider.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("secret-value"));

        var reader = new FailoverSecretReader([provider], _logger);

        var result = await reader.GetSecretAsync("key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("secret-value"));
    }

    [Fact]
    public async Task GetSecretAsync_SingleProvider_Failure_ReturnsFailoverExhausted()
    {
        var provider = Substitute.For<ISecretReader>();
        provider.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound("key")));

        var reader = new FailoverSecretReader([provider], _logger);

        var result = await reader.GetSecretAsync("key");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.FailoverExhaustedCode));
    }

    #endregion

    #region GetSecretAsync - Failover Chain

    [Fact]
    public async Task GetSecretAsync_FirstProviderSucceeds_DoesNotTrySecond()
    {
        var primary = Substitute.For<ISecretReader>();
        var secondary = Substitute.For<ISecretReader>();

        primary.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("primary-value"));

        var reader = new FailoverSecretReader([primary, secondary], _logger);

        var result = await reader.GetSecretAsync("key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("primary-value"));
        await secondary.DidNotReceive().GetSecretAsync("key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_FirstFails_SecondSucceeds_ReturnsSecondValue()
    {
        var primary = Substitute.For<ISecretReader>();
        var secondary = Substitute.For<ISecretReader>();

        primary.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.ProviderUnavailable("primary")));

        secondary.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("fallback-value"));

        var reader = new FailoverSecretReader([primary, secondary], _logger);

        var result = await reader.GetSecretAsync("key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("fallback-value"));
    }

    [Fact]
    public async Task GetSecretAsync_AllProvidersFail_ReturnsFailoverExhausted()
    {
        var primary = Substitute.For<ISecretReader>();
        var secondary = Substitute.For<ISecretReader>();
        var tertiary = Substitute.For<ISecretReader>();

        primary.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.ProviderUnavailable("primary")));

        secondary.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.ProviderUnavailable("secondary")));

        tertiary.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound("key")));

        var reader = new FailoverSecretReader([primary, secondary, tertiary], _logger);

        var result = await reader.GetSecretAsync("key");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.FailoverExhaustedCode));
    }

    #endregion

    #region GetSecretAsync<T> - Typed

    [Fact]
    public async Task GetSecretAsync_Typed_FirstProviderSucceeds_ReturnsValue()
    {
        var primary = Substitute.For<ISecretReader>();
        var expected = new TestConfig { Host = "localhost" };

        primary.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(expected));

        var reader = new FailoverSecretReader([primary], _logger);

        var result = await reader.GetSecretAsync<TestConfig>("config");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Host.Should().Be("localhost"));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_FirstFails_SecondSucceeds()
    {
        var primary = Substitute.For<ISecretReader>();
        var secondary = Substitute.For<ISecretReader>();
        var expected = new TestConfig { Host = "fallback-host" };

        primary.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(
                SecretsErrors.ProviderUnavailable("primary")));

        secondary.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(expected));

        var reader = new FailoverSecretReader([primary, secondary], _logger);

        var result = await reader.GetSecretAsync<TestConfig>("config");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Host.Should().Be("fallback-host"));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_AllFail_ReturnsFailoverExhausted()
    {
        var primary = Substitute.For<ISecretReader>();

        primary.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(
                SecretsErrors.NotFound("config")));

        var reader = new FailoverSecretReader([primary], _logger);

        var result = await reader.GetSecretAsync<TestConfig>("config");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.FailoverExhaustedCode));
    }

    #endregion

    #region WithFailover Extension

    [Fact]
    public async Task WithFailover_CreatesChainWithPrimaryFirst()
    {
        var primary = Substitute.For<ISecretReader>();
        var fallback = Substitute.For<ISecretReader>();

        primary.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("primary-value"));

        var reader = primary.WithFailover(_logger, fallback);

        var result = await reader.GetSecretAsync("key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("primary-value"));
        await fallback.DidNotReceive().GetSecretAsync("key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void WithFailover_NullPrimary_ThrowsArgumentNullException()
    {
        ISecretReader primary = null!;

        var act = () => primary.WithFailover(_logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("primary");
    }

    [Fact]
    public void WithFailover_NullLogger_ThrowsArgumentNullException()
    {
        var primary = Substitute.For<ISecretReader>();

        var act = () => primary.WithFailover(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Input Validation

    [Fact]
    public async Task GetSecretAsync_NullSecretName_ThrowsArgumentException()
    {
        var provider = Substitute.For<ISecretReader>();
        var reader = new FailoverSecretReader([provider], _logger);

        var act = () => reader.GetSecretAsync(null!).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSecretAsync_EmptySecretName_ThrowsArgumentException()
    {
        var provider = Substitute.For<ISecretReader>();
        var reader = new FailoverSecretReader([provider], _logger);

        var act = () => reader.GetSecretAsync("").AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Helpers

    private sealed class TestConfig
    {
        public string Host { get; set; } = "";
    }

    #endregion
}

#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Injection;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretInjectionOrchestratorTests : IDisposable
{
    private readonly ISecretReader _secretReader;
    private readonly ILogger<SecretInjectionOrchestrator> _logger;
    private readonly SecretInjectionOrchestrator _orchestrator;

    public SecretInjectionOrchestratorTests()
    {
        _secretReader = Substitute.For<ISecretReader>();
        _logger = NullLogger<SecretInjectionOrchestrator>.Instance;
        _orchestrator = new SecretInjectionOrchestrator(_secretReader, _logger);
    }

    public void Dispose()
    {
        SecretPropertyCache.ClearCache();
    }

    #region Test Fixtures

    private sealed class PlainRequest
    {
        public string Name { get; set; } = "";
    }

    private sealed class SingleSecretRequest
    {
        [InjectSecret("api-key")]
        public string ApiKey { get; set; } = "";
    }

    private sealed class MultiSecretRequest
    {
        [InjectSecret("api-key")]
        public string ApiKey { get; set; } = "";

        [InjectSecret("db-password")]
        public string DbPassword { get; set; } = "";
    }

    private sealed class OptionalSecretRequest
    {
        [InjectSecret("optional-key", FailOnError = false)]
        public string OptionalKey { get; set; } = "";
    }

    private sealed class VersionedSecretRequest
    {
        [InjectSecret("secret-key", Version = "v2")]
        public string Secret { get; set; } = "";
    }

    private sealed class MixedFailOnErrorRequest
    {
        [InjectSecret("required-key")]
        public string RequiredKey { get; set; } = "";

        [InjectSecret("optional-key", FailOnError = false)]
        public string OptionalKey { get; set; } = "";
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_NullSecretReader_ThrowsArgumentNullException()
    {
        var act = () => new SecretInjectionOrchestrator(null!, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("secretReader");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SecretInjectionOrchestrator(_secretReader, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region InjectAsync - No Injectable Properties

    [Fact]
    public async Task InjectAsync_NoInjectableProperties_ReturnsZero()
    {
        var request = new PlainRequest { Name = "test" };

        var result = await _orchestrator.InjectAsync(request, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        result.Match(Right: v => v, Left: _ => -1).Should().Be(0);
        await _secretReader.DidNotReceive().GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region InjectAsync - Single Property

    [Fact]
    public async Task InjectAsync_SingleProperty_CallsSecretReader_SetsValue()
    {
        var request = new SingleSecretRequest();
        _secretReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("sk-12345"));

        var result = await _orchestrator.InjectAsync(request, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        result.Match(Right: v => v, Left: _ => -1).Should().Be(1);
        request.ApiKey.Should().Be("sk-12345");
    }

    #endregion

    #region InjectAsync - Multiple Properties

    [Fact]
    public async Task InjectAsync_MultipleProperties_InjectsAll_ReturnsCount()
    {
        var request = new MultiSecretRequest();
        _secretReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("key-value"));
        _secretReader.GetSecretAsync("db-password", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("db-pass"));

        var result = await _orchestrator.InjectAsync(request, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        result.Match(Right: v => v, Left: _ => -1).Should().Be(2);
        request.ApiKey.Should().Be("key-value");
        request.DbPassword.Should().Be("db-pass");
    }

    #endregion

    #region InjectAsync - FailOnError

    [Fact]
    public async Task InjectAsync_SecretNotFound_FailOnErrorTrue_ReturnsError()
    {
        var request = new SingleSecretRequest();
        _secretReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound("api-key")));

        var result = await _orchestrator.InjectAsync(request, CancellationToken.None);

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.InjectionFailedCode));
    }

    [Fact]
    public async Task InjectAsync_SecretNotFound_FailOnErrorFalse_ContinuesAndReturnsZero()
    {
        var request = new OptionalSecretRequest();
        _secretReader.GetSecretAsync("optional-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound("optional-key")));

        var result = await _orchestrator.InjectAsync(request, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        result.Match(Right: v => v, Left: _ => -1).Should().Be(0);
        request.OptionalKey.Should().Be("");
    }

    #endregion

    #region InjectAsync - Versioned Secret

    [Fact]
    public async Task InjectAsync_VersionSpecified_PassesVersionedSecretName()
    {
        var request = new VersionedSecretRequest();
        _secretReader.GetSecretAsync("secret-key/v2", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("versioned-value"));

        var result = await _orchestrator.InjectAsync(request, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        request.Secret.Should().Be("versioned-value");
        await _secretReader.Received(1).GetSecretAsync("secret-key/v2", Arg.Any<CancellationToken>());
    }

    #endregion

    #region InjectAsync - Mixed FailOnError

    [Fact]
    public async Task InjectAsync_MixedFailOnError_RequiredFails_ReturnsError()
    {
        var request = new MixedFailOnErrorRequest();
        _secretReader.GetSecretAsync("required-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound("required-key")));
        // optional-key should never be reached because required fails first
        _secretReader.GetSecretAsync("optional-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("optional-value"));

        var result = await _orchestrator.InjectAsync(request, CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    #endregion
}

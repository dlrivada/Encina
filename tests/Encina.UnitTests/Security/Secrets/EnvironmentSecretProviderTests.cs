using Encina.Security.Secrets;
using Encina.Security.Secrets.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets;

public sealed class EnvironmentSecretProviderTests : IDisposable
{
    private const string TestEnvVarName = "ENCINA_TEST_SECRET_E2E";
    private const string TestJsonEnvVarName = "ENCINA_TEST_SECRET_JSON_E2E";
    private readonly EnvironmentSecretProvider _provider;

    public EnvironmentSecretProviderTests()
    {
        var logger = Substitute.For<ILogger<EnvironmentSecretProvider>>();
        _provider = new EnvironmentSecretProvider(logger);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(TestEnvVarName, null);
        Environment.SetEnvironmentVariable(TestJsonEnvVarName, null);
    }

    #region GetSecretAsync (string)

    [Fact]
    public async Task GetSecretAsync_ExistingEnvVar_ReturnsRight()
    {
        Environment.SetEnvironmentVariable(TestEnvVarName, "my-secret-value");

        var result = await _provider.GetSecretAsync(TestEnvVarName);

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("my-secret-value"));
    }

    [Fact]
    public async Task GetSecretAsync_NonExistentEnvVar_ReturnsLeftNotFound()
    {
        var result = await _provider.GetSecretAsync("NON_EXISTENT_VAR_12345");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_NullSecretName_ThrowsArgumentException()
    {
        var act = () => _provider.GetSecretAsync(null!).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSecretAsync_EmptySecretName_ThrowsArgumentException()
    {
        var act = () => _provider.GetSecretAsync("").AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSecretAsync_WhitespaceSecretName_ThrowsArgumentException()
    {
        var act = () => _provider.GetSecretAsync("   ").AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSecretAsync_CancelledToken_StillReturnsResult()
    {
        // EnvironmentSecretProvider is synchronous, so a cancelled token
        // does not affect the result. This documents the expected behavior.
        Environment.SetEnvironmentVariable(TestEnvVarName, "my-value");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _provider.GetSecretAsync(TestEnvVarName, cts.Token);

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("my-value"));
    }

    [Fact]
    public async Task GetSecretAsync_CancelledToken_MissingVar_StillReturnsNotFound()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _provider.GetSecretAsync("NON_EXISTENT_CANCEL_TEST", cts.Token);

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    #endregion

    #region GetSecretAsync<T> (typed)

    [Fact]
    public async Task GetSecretAsync_Typed_ValidJson_ReturnsDeserializedObject()
    {
        Environment.SetEnvironmentVariable(TestJsonEnvVarName, """{"Host":"localhost","Port":5432}""");

        var result = await _provider.GetSecretAsync<TestConnectionConfig>(TestJsonEnvVarName);

        result.IsRight.Should().BeTrue();
        result.IfRight(v =>
        {
            v.Host.Should().Be("localhost");
            v.Port.Should().Be(5432);
        });
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NonExistentEnvVar_ReturnsLeftNotFound()
    {
        var result = await _provider.GetSecretAsync<TestConnectionConfig>("NON_EXISTENT_VAR_67890");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_InvalidJson_ReturnsLeftDeserializationFailed()
    {
        Environment.SetEnvironmentVariable(TestJsonEnvVarName, "not-valid-json");

        var result = await _provider.GetSecretAsync<TestConnectionConfig>(TestJsonEnvVarName);

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.DeserializationFailedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NullSecretName_ThrowsArgumentException()
    {
        var act = () => _provider.GetSecretAsync<TestConnectionConfig>(null!).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Test Types

    private sealed class TestConnectionConfig
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
    }

    #endregion
}

using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Encina.Security.Secrets;
using Encina.Security.Secrets.AwsSecretsManager;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.Security.Secrets.AwsSecretsManager;

public sealed class AwsSecretsManagerProviderTests
{
    private readonly IAmazonSecretsManager _client = Substitute.For<IAmazonSecretsManager>();
    private readonly ILogger<AwsSecretsManagerProvider> _logger = Substitute.For<ILogger<AwsSecretsManagerProvider>>();
    private readonly AwsSecretsManagerProvider _provider;

    public AwsSecretsManagerProviderTests()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _provider = new AwsSecretsManagerProvider(_client, _logger);
    }

    #region GetSecretAsync (string)

    [Fact]
    public async Task GetSecretAsync_ExistingSecret_ReturnsRightWithValue()
    {
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetSecretValueResponse { SecretString = "secret-value" }));

        var result = await _provider.GetSecretAsync("api-key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("secret-value"));
    }

    [Fact]
    public async Task GetSecretAsync_NotFound_ReturnsLeftNotFound()
    {
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException("Secret not found"));

        var result = await _provider.GetSecretAsync("missing");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_AccessDenied_ReturnsLeftAccessDenied()
    {
        var ex = new AmazonSecretsManagerException("Forbidden")
        {
            ErrorCode = "AccessDeniedException"
        };
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Throws(ex);

        var result = await _provider.GetSecretAsync("restricted");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task GetSecretAsync_ServiceError_ReturnsLeftProviderUnavailable()
    {
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Throws(new AmazonSecretsManagerException("Service unavailable"));

        var result = await _provider.GetSecretAsync("key");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.ProviderUnavailableCode));
    }

    [Fact]
    public async Task GetSecretAsync_Throttled_ReturnsLeftProviderUnavailable()
    {
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Throws(new AmazonSecretsManagerException("Rate exceeded"));

        var result = await _provider.GetSecretAsync("key");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.ProviderUnavailableCode));
    }

    [Fact]
    public void GetSecretAsync_NullSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.GetSecretAsync(null!);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GetSecretAsync_EmptySecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.GetSecretAsync("");

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GetSecretAsync_WhitespaceSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.GetSecretAsync("   ");

        act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetSecretAsync<T> (typed)

    [Fact]
    public async Task GetSecretAsync_Typed_ValidJson_ReturnsDeserializedObject()
    {
        var json = JsonSerializer.Serialize(new TestConfig { Host = "localhost", Port = 5432 });
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetSecretValueResponse { SecretString = json }));

        var result = await _provider.GetSecretAsync<TestConfig>("db-config");

        result.IsRight.Should().BeTrue();
        result.IfRight(v =>
        {
            v.Host.Should().Be("localhost");
            v.Port.Should().Be(5432);
        });
    }

    [Fact]
    public async Task GetSecretAsync_Typed_InvalidJson_ReturnsLeftDeserializationFailed()
    {
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetSecretValueResponse { SecretString = "not-valid-json" }));

        var result = await _provider.GetSecretAsync<TestConfig>("bad-json");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.DeserializationFailedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NullDeserialization_ReturnsLeftDeserializationFailed()
    {
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetSecretValueResponse { SecretString = "null" }));

        var result = await _provider.GetSecretAsync<TestConfig>("null-json");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.DeserializationFailedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NotFound_ReturnsLeftNotFound()
    {
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException("Not found"));

        var result = await _provider.GetSecretAsync<TestConfig>("missing-config");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_AccessDenied_ReturnsLeftAccessDenied()
    {
        var ex = new AmazonSecretsManagerException("Forbidden")
        {
            ErrorCode = "AccessDeniedException"
        };
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Throws(ex);

        var result = await _provider.GetSecretAsync<TestConfig>("restricted");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public void GetSecretAsync_Typed_NullSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.GetSecretAsync<TestConfig>(null!);

        act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region SetSecretAsync

    [Fact]
    public async Task SetSecretAsync_PutSucceeds_ReturnsRightUnit()
    {
        _client.PutSecretValueAsync(Arg.Any<PutSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PutSecretValueResponse()));

        var result = await _provider.SetSecretAsync("my-secret", "my-value");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task SetSecretAsync_PutNotFound_FallsBackToCreate_ReturnsRightUnit()
    {
        _client.PutSecretValueAsync(Arg.Any<PutSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException("Secret not found"));
        _client.CreateSecretAsync(Arg.Any<CreateSecretRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CreateSecretResponse()));

        var result = await _provider.SetSecretAsync("new-secret", "new-value");

        result.IsRight.Should().BeTrue();
        await _client.Received(1).CreateSecretAsync(
            Arg.Is<CreateSecretRequest>(r => r.Name == "new-secret" && r.SecretString == "new-value"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetSecretAsync_AccessDenied_ReturnsLeftAccessDenied()
    {
        var ex = new AmazonSecretsManagerException("Forbidden")
        {
            ErrorCode = "AccessDeniedException"
        };
        _client.PutSecretValueAsync(Arg.Any<PutSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Throws(ex);

        var result = await _provider.SetSecretAsync("read-only", "value");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task SetSecretAsync_ServiceError_ReturnsLeftProviderUnavailable()
    {
        _client.PutSecretValueAsync(Arg.Any<PutSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Throws(new AmazonSecretsManagerException("Internal server error"));

        var result = await _provider.SetSecretAsync("key", "value");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.ProviderUnavailableCode));
    }

    [Fact]
    public void SetSecretAsync_NullSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.SetSecretAsync(null!, "value");

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void SetSecretAsync_NullValue_ThrowsArgumentNullException()
    {
        var act = async () => await _provider.SetSecretAsync("key", null!);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region RotateSecretAsync

    [Fact]
    public async Task RotateSecretAsync_ValidSecret_ReturnsRightUnit()
    {
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetSecretValueResponse { SecretString = "current-value" }));
        _client.PutSecretValueAsync(Arg.Any<PutSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PutSecretValueResponse()));

        var result = await _provider.RotateSecretAsync("rotatable");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RotateSecretAsync_NotFound_ReturnsLeftRotationFailed()
    {
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException("Not found"));

        var result = await _provider.RotateSecretAsync("missing");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public async Task RotateSecretAsync_AccessDenied_ReturnsLeftRotationFailed()
    {
        var ex = new AmazonSecretsManagerException("Forbidden")
        {
            ErrorCode = "AccessDeniedException"
        };
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Throws(ex);

        var result = await _provider.RotateSecretAsync("restricted");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public async Task RotateSecretAsync_PutFails_ReturnsLeftRotationFailed()
    {
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetSecretValueResponse { SecretString = "current" }));
        _client.PutSecretValueAsync(Arg.Any<PutSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Throws(new AmazonSecretsManagerException("Write failed"));

        var result = await _provider.RotateSecretAsync("key");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public void RotateSecretAsync_NullSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.RotateSecretAsync(null!);

        act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Helpers

    private sealed class TestConfig
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
    }

    #endregion
}

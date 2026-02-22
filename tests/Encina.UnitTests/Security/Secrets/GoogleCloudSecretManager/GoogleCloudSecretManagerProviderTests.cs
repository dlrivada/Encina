using System.Text.Json;
using Encina.Security.Secrets;
using Encina.Security.Secrets.GoogleCloudSecretManager;
using FluentAssertions;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.SecretManager.V1;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.Security.Secrets.GoogleCloudSecretManager;

public sealed class GoogleCloudSecretManagerProviderTests
{
    private const string ProjectId = "test-project";

    private readonly SecretManagerServiceClient _client = Substitute.For<SecretManagerServiceClient>();
    private readonly ILogger<GoogleCloudSecretManagerProvider> _logger =
        Substitute.For<ILogger<GoogleCloudSecretManagerProvider>>();
    private readonly GoogleCloudSecretManagerProvider _provider;

    public GoogleCloudSecretManagerProviderTests()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var options = new GoogleCloudSecretManagerOptions { ProjectId = ProjectId };
        _provider = new GoogleCloudSecretManagerProvider(_client, options, _logger);
    }

    private static AccessSecretVersionResponse CreateResponse(string value)
    {
        return new AccessSecretVersionResponse
        {
            Payload = new SecretPayload
            {
                Data = ByteString.CopyFromUtf8(value)
            }
        };
    }

    private static RpcException CreateRpcException(StatusCode statusCode, string message = "error")
    {
        return new RpcException(new Status(statusCode, message));
    }

    #region GetSecretAsync (string)

    [Fact]
    public async Task GetSecretAsync_ExistingSecret_ReturnsRightWithValue()
    {
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResponse("secret-value")));

        var result = await _provider.GetSecretAsync("api-key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("secret-value"));
    }

    [Fact]
    public async Task GetSecretAsync_NotFound_ReturnsLeftNotFound()
    {
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Throws(CreateRpcException(StatusCode.NotFound));

        var result = await _provider.GetSecretAsync("missing");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_PermissionDenied_ReturnsLeftAccessDenied()
    {
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Throws(CreateRpcException(StatusCode.PermissionDenied));

        var result = await _provider.GetSecretAsync("restricted");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task GetSecretAsync_ServiceError_ReturnsLeftProviderUnavailable()
    {
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Throws(CreateRpcException(StatusCode.Unavailable));

        var result = await _provider.GetSecretAsync("key");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.ProviderUnavailableCode));
    }

    [Fact]
    public async Task GetSecretAsync_InternalError_ReturnsLeftProviderUnavailable()
    {
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Throws(CreateRpcException(StatusCode.Internal));

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
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResponse(json)));

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
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResponse("not-valid-json")));

        var result = await _provider.GetSecretAsync<TestConfig>("bad-json");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.DeserializationFailedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NullDeserialization_ReturnsLeftDeserializationFailed()
    {
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResponse("null")));

        var result = await _provider.GetSecretAsync<TestConfig>("null-json");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.DeserializationFailedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NotFound_ReturnsLeftNotFound()
    {
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Throws(CreateRpcException(StatusCode.NotFound));

        var result = await _provider.GetSecretAsync<TestConfig>("missing-config");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_PermissionDenied_ReturnsLeftAccessDenied()
    {
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Throws(CreateRpcException(StatusCode.PermissionDenied));

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
    public async Task SetSecretAsync_AddSucceeds_ReturnsRightUnit()
    {
        _client.AddSecretVersionAsync(
                Arg.Any<SecretName>(), Arg.Any<SecretPayload>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SecretVersion()));

        var result = await _provider.SetSecretAsync("my-secret", "my-value");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task SetSecretAsync_NotFound_FallsBackToCreate_ReturnsRightUnit()
    {
        _client.AddSecretVersionAsync(
                Arg.Any<SecretName>(), Arg.Any<SecretPayload>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => throw CreateRpcException(StatusCode.NotFound),
                _ => Task.FromResult(new SecretVersion()));

        _client.CreateSecretAsync(
                Arg.Any<ProjectName>(), Arg.Any<string>(), Arg.Any<Secret>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new Secret()));

        var result = await _provider.SetSecretAsync("new-secret", "new-value");

        result.IsRight.Should().BeTrue();
        await _client.Received(1).CreateSecretAsync(
            Arg.Any<ProjectName>(),
            Arg.Is<string>(s => s == "new-secret"),
            Arg.Any<Secret>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetSecretAsync_PermissionDenied_ReturnsLeftAccessDenied()
    {
        _client.AddSecretVersionAsync(
                Arg.Any<SecretName>(), Arg.Any<SecretPayload>(), Arg.Any<CancellationToken>())
            .Throws(CreateRpcException(StatusCode.PermissionDenied));

        var result = await _provider.SetSecretAsync("read-only", "value");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task SetSecretAsync_ServiceError_ReturnsLeftProviderUnavailable()
    {
        _client.AddSecretVersionAsync(
                Arg.Any<SecretName>(), Arg.Any<SecretPayload>(), Arg.Any<CancellationToken>())
            .Throws(CreateRpcException(StatusCode.Internal));

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
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResponse("current-value")));
        _client.AddSecretVersionAsync(
                Arg.Any<SecretName>(), Arg.Any<SecretPayload>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SecretVersion()));

        var result = await _provider.RotateSecretAsync("rotatable");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RotateSecretAsync_NotFound_ReturnsLeftRotationFailed()
    {
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Throws(CreateRpcException(StatusCode.NotFound));

        var result = await _provider.RotateSecretAsync("missing");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public async Task RotateSecretAsync_PermissionDenied_ReturnsLeftRotationFailed()
    {
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Throws(CreateRpcException(StatusCode.PermissionDenied));

        var result = await _provider.RotateSecretAsync("restricted");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public async Task RotateSecretAsync_AddFails_ReturnsLeftRotationFailed()
    {
        _client.AccessSecretVersionAsync(
                Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResponse("current")));
        _client.AddSecretVersionAsync(
                Arg.Any<SecretName>(), Arg.Any<SecretPayload>(), Arg.Any<CancellationToken>())
            .Throws(CreateRpcException(StatusCode.Internal));

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

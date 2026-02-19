using Encina.Secrets;
using Encina.Secrets.GoogleSecretManager;
using Encina.TestInfrastructure.Extensions;
using Google.Api.Gax;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.SecretManager.V1;
using Google.Protobuf;
using Grpc.Core;

namespace Encina.UnitTests.Secrets.Providers;

/// <summary>
/// Unit tests for <see cref="GoogleSecretManagerProvider"/>.
/// Verifies that gRPC exceptions are correctly mapped to <see cref="EncinaError"/> values
/// following the Railway Oriented Programming pattern.
/// </summary>
public sealed class GoogleSecretManagerProviderTests
{
    private const string ProjectId = "my-gcp-project";
    private const string SecretName = "my-gcp-secret";
    private const string SecretValue = "gcp-secret-value";
    private const string SecretVersionId = "5";

    private readonly SecretManagerServiceClient _client;
    private readonly ILogger<GoogleSecretManagerProvider> _logger;
    private readonly GoogleSecretManagerProvider _sut;

    public GoogleSecretManagerProviderTests()
    {
        _client = Substitute.For<SecretManagerServiceClient>();
        _logger = Substitute.For<ILogger<GoogleSecretManagerProvider>>();

        var options = Options.Create(new GoogleSecretManagerOptions { ProjectId = ProjectId });
        _sut = new GoogleSecretManagerProvider(_client, options, _logger);
    }

    // ---------------------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        var options = Options.Create(new GoogleSecretManagerOptions { ProjectId = ProjectId });
        var act = () => new GoogleSecretManagerProvider(null!, options, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("client");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new GoogleSecretManagerProvider(_client, null!, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var options = Options.Create(new GoogleSecretManagerOptions { ProjectId = ProjectId });
        var act = () => new GoogleSecretManagerProvider(_client, options, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("logger");
    }

    // ---------------------------------------------------------------------------
    // GetSecretAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetSecretAsync_WhenClientSucceeds_ReturnsRightWithSecret()
    {
        // Arrange
        var secretVersionName = new SecretVersionName(ProjectId, SecretName, "latest");
        var fullResourceName = $"projects/{ProjectId}/secrets/{SecretName}/versions/{SecretVersionId}";

        var response = new AccessSecretVersionResponse
        {
            Name = fullResourceName,
            Payload = new SecretPayload { Data = ByteString.CopyFromUtf8(SecretValue) }
        };

        _client.AccessSecretVersionAsync(
                Arg.Is<SecretVersionName>(n => n.ProjectId == ProjectId && n.SecretId == SecretName && n.SecretVersionId == "latest"),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        var secret = result.ShouldBeSuccess();
        secret.Name.ShouldBe(SecretName);
        secret.Value.ShouldBe(SecretValue);
    }

    [Fact]
    public async Task GetSecretAsync_WhenNotFound_ReturnsLeftWithNotFoundCode()
    {
        // Arrange
        _client.AccessSecretVersionAsync(Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Returns<Task<AccessSecretVersionResponse>>(_ =>
                throw new RpcException(new Status(StatusCode.NotFound, "Secret not found.")));

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.NotFoundCode);
    }

    [Fact]
    public async Task GetSecretAsync_WhenPermissionDenied_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        _client.AccessSecretVersionAsync(Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Returns<Task<AccessSecretVersionResponse>>(_ =>
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied.")));

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task GetSecretAsync_WhenOtherRpcError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        _client.AccessSecretVersionAsync(Arg.Any<SecretVersionName>(), Arg.Any<CancellationToken>())
            .Returns<Task<AccessSecretVersionResponse>>(_ =>
                throw new RpcException(new Status(StatusCode.Unavailable, "Service unavailable.")));

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // GetSecretVersionAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetSecretVersionAsync_WhenClientSucceeds_ReturnsRightWithSecret()
    {
        // Arrange
        var fullResourceName = $"projects/{ProjectId}/secrets/{SecretName}/versions/{SecretVersionId}";
        var response = new AccessSecretVersionResponse
        {
            Name = fullResourceName,
            Payload = new SecretPayload { Data = ByteString.CopyFromUtf8(SecretValue) }
        };

        _client.AccessSecretVersionAsync(
                Arg.Is<SecretVersionName>(n =>
                    n.ProjectId == ProjectId &&
                    n.SecretId == SecretName &&
                    n.SecretVersionId == SecretVersionId),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, SecretVersionId);

        // Assert
        var secret = result.ShouldBeSuccess();
        secret.Name.ShouldBe(SecretName);
        secret.Value.ShouldBe(SecretValue);
        secret.Version.ShouldBe(SecretVersionId);
    }

    [Fact]
    public async Task GetSecretVersionAsync_WhenNotFound_ReturnsLeftWithVersionNotFoundCode()
    {
        // Arrange
        _client.AccessSecretVersionAsync(
                Arg.Is<SecretVersionName>(n => n.SecretVersionId == SecretVersionId),
                Arg.Any<CancellationToken>())
            .Returns<Task<AccessSecretVersionResponse>>(_ =>
                throw new RpcException(new Status(StatusCode.NotFound, "Version not found.")));

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, SecretVersionId);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.VersionNotFoundCode);
    }

    [Fact]
    public async Task GetSecretVersionAsync_WhenPermissionDenied_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        _client.AccessSecretVersionAsync(
                Arg.Is<SecretVersionName>(n => n.SecretVersionId == SecretVersionId),
                Arg.Any<CancellationToken>())
            .Returns<Task<AccessSecretVersionResponse>>(_ =>
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied.")));

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, SecretVersionId);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task GetSecretVersionAsync_WhenOtherRpcError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        _client.AccessSecretVersionAsync(
                Arg.Is<SecretVersionName>(n => n.SecretVersionId == SecretVersionId),
                Arg.Any<CancellationToken>())
            .Returns<Task<AccessSecretVersionResponse>>(_ =>
                throw new RpcException(new Status(StatusCode.Internal, "Internal error.")));

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, SecretVersionId);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // SetSecretAsync – secret exists path (GetSecret succeeds → AddSecretVersion)
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task SetSecretAsync_WhenSecretAlreadyExists_AddsVersionAndReturnsRightWithMetadata()
    {
        // Arrange – GetSecret succeeds (secret exists), then AddSecretVersion is called
        var secretName = new SecretName(ProjectId, SecretName);
        var existingSecret = new Google.Cloud.SecretManager.V1.Secret
        {
            SecretName = secretName
        };

        _client.GetSecretAsync(
                Arg.Is<SecretName>(n => n.ProjectId == ProjectId && n.SecretId == SecretName),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(existingSecret));

        var versionResourceName = $"projects/{ProjectId}/secrets/{SecretName}/versions/{SecretVersionId}";
        var addedVersion = new SecretVersion { Name = versionResourceName };

        _client.AddSecretVersionAsync(
                Arg.Is<AddSecretVersionRequest>(r =>
                    r.ParentAsSecretName.SecretId == SecretName &&
                    r.Payload.Data == ByteString.CopyFromUtf8(SecretValue)),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(addedVersion));

        // Act
        var result = await _sut.SetSecretAsync(SecretName, SecretValue);

        // Assert
        var metadata = result.ShouldBeSuccess();
        metadata.Name.ShouldBe(SecretName);
        metadata.Version.ShouldBe(SecretVersionId);
    }

    [Fact]
    public async Task SetSecretAsync_WhenSecretDoesNotExist_CreatesSecretThenAddsVersionAndReturnsRightWithMetadata()
    {
        // Arrange – GetSecret throws NotFound → CreateSecret → AddSecretVersion
        _client.GetSecretAsync(
                Arg.Is<SecretName>(n => n.ProjectId == ProjectId && n.SecretId == SecretName),
                Arg.Any<CancellationToken>())
            .Returns<Task<Google.Cloud.SecretManager.V1.Secret>>(_ =>
                throw new RpcException(new Status(StatusCode.NotFound, "Secret not found.")));

        var secretName = new SecretName(ProjectId, SecretName);
        var createdSecret = new Google.Cloud.SecretManager.V1.Secret
        {
            SecretName = secretName
        };

        _client.CreateSecretAsync(Arg.Any<CreateSecretRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(createdSecret));

        var versionResourceName = $"projects/{ProjectId}/secrets/{SecretName}/versions/{SecretVersionId}";
        var addedVersion = new SecretVersion { Name = versionResourceName };

        _client.AddSecretVersionAsync(
                Arg.Any<AddSecretVersionRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(addedVersion));

        // Act
        var result = await _sut.SetSecretAsync(SecretName, SecretValue);

        // Assert
        var metadata = result.ShouldBeSuccess();
        metadata.Name.ShouldBe(SecretName);

        await _client.Received(1).CreateSecretAsync(Arg.Any<CreateSecretRequest>(), Arg.Any<CancellationToken>());
        await _client.Received(1).AddSecretVersionAsync(Arg.Any<AddSecretVersionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetSecretAsync_WhenPermissionDenied_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        _client.GetSecretAsync(Arg.Any<SecretName>(), Arg.Any<CancellationToken>())
            .Returns<Task<Google.Cloud.SecretManager.V1.Secret>>(_ =>
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied.")));

        // Act
        var result = await _sut.SetSecretAsync(SecretName, SecretValue);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task SetSecretAsync_WhenOtherRpcError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        _client.GetSecretAsync(Arg.Any<SecretName>(), Arg.Any<CancellationToken>())
            .Returns<Task<Google.Cloud.SecretManager.V1.Secret>>(_ =>
                throw new RpcException(new Status(StatusCode.Internal, "Internal error.")));

        // Act
        var result = await _sut.SetSecretAsync(SecretName, SecretValue);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // DeleteSecretAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task DeleteSecretAsync_WhenClientSucceeds_ReturnsRightWithUnit()
    {
        // Arrange
        _client.DeleteSecretAsync(
                Arg.Is<SecretName>(n => n.ProjectId == ProjectId && n.SecretId == SecretName),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task DeleteSecretAsync_WhenNotFound_ReturnsLeftWithNotFoundCode()
    {
        // Arrange
        _client.DeleteSecretAsync(Arg.Any<SecretName>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new RpcException(new Status(StatusCode.NotFound, "Secret not found.")));

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.NotFoundCode);
    }

    [Fact]
    public async Task DeleteSecretAsync_WhenPermissionDenied_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        _client.DeleteSecretAsync(Arg.Any<SecretName>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied.")));

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task DeleteSecretAsync_WhenOtherRpcError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        _client.DeleteSecretAsync(Arg.Any<SecretName>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new RpcException(new Status(StatusCode.Unavailable, "Service unavailable.")));

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // ListSecretsAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ListSecretsAsync_WhenClientSucceeds_ReturnsRightWithNames()
    {
        // Arrange – configure the pageable to return two secrets
        var secretAlpha = new Google.Cloud.SecretManager.V1.Secret
        {
            SecretName = new SecretName(ProjectId, "secret-alpha")
        };
        var secretBeta = new Google.Cloud.SecretManager.V1.Secret
        {
            SecretName = new SecretName(ProjectId, "secret-beta")
        };

        var fakePageable = new FakeSecretPageable(secretAlpha, secretBeta);

        _client.ListSecretsAsync(Arg.Any<ListSecretsRequest>())
            .Returns(fakePageable);

        // Act
        var result = await _sut.ListSecretsAsync();

        // Assert
        var names = result.ShouldBeSuccess().ToList();
        names.Count.ShouldBe(2);
        names.ShouldContain("secret-alpha");
        names.ShouldContain("secret-beta");
    }

    [Fact]
    public async Task ListSecretsAsync_WhenRpcError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        var failingPageable = new ThrowingSecretPageable(
            new RpcException(new Status(StatusCode.Internal, "Internal error.")));

        _client.ListSecretsAsync(Arg.Any<ListSecretsRequest>())
            .Returns(failingPageable);

        // Act
        var result = await _sut.ListSecretsAsync();

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // ExistsAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExistsAsync_WhenFound_ReturnsTrue()
    {
        // Arrange
        var secretResourceName = new SecretName(ProjectId, SecretName);
        var foundSecret = new Google.Cloud.SecretManager.V1.Secret
        {
            SecretName = secretResourceName
        };

        _client.GetSecretAsync(
                Arg.Is<SecretName>(n => n.ProjectId == ProjectId && n.SecretId == SecretName),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(foundSecret));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeSuccess(true);
    }

    [Fact]
    public async Task ExistsAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange – NotFound maps to Right(false), not Left
        _client.GetSecretAsync(Arg.Any<SecretName>(), Arg.Any<CancellationToken>())
            .Returns<Task<Google.Cloud.SecretManager.V1.Secret>>(_ =>
                throw new RpcException(new Status(StatusCode.NotFound, "Not found.")));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert – must be Right(false), not Left
        result.ShouldBeSuccess(false);
    }

    [Fact]
    public async Task ExistsAsync_WhenPermissionDenied_ReturnsLeft()
    {
        // Arrange
        _client.GetSecretAsync(Arg.Any<SecretName>(), Arg.Any<CancellationToken>())
            .Returns<Task<Google.Cloud.SecretManager.V1.Secret>>(_ =>
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied.")));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    [Fact]
    public async Task ExistsAsync_WhenOtherRpcError_ReturnsLeft()
    {
        // Arrange
        _client.GetSecretAsync(Arg.Any<SecretName>(), Arg.Any<CancellationToken>())
            .Returns<Task<Google.Cloud.SecretManager.V1.Secret>>(_ =>
                throw new RpcException(new Status(StatusCode.Unavailable, "Service unavailable.")));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // Helpers: fake and throwing AsyncPageable implementations
    // ---------------------------------------------------------------------------

    /// <summary>
    /// A <see cref="PagedAsyncEnumerable{TResponse,TResource}"/> that yields a fixed set of secrets.
    /// Used to simulate successful Google Secret Manager paginated list responses.
    /// The provider iterates via <c>await foreach</c>, which calls <c>GetAsyncEnumerator</c>.
    /// </summary>
    private sealed class FakeSecretPageable(params Google.Cloud.SecretManager.V1.Secret[] items)
        : PagedAsyncEnumerable<ListSecretsResponse, Google.Cloud.SecretManager.V1.Secret>
    {
        public override IAsyncEnumerator<Google.Cloud.SecretManager.V1.Secret> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            return YieldItems().GetAsyncEnumerator(cancellationToken);
        }

        private async IAsyncEnumerable<Google.Cloud.SecretManager.V1.Secret> YieldItems()
        {
            await Task.Yield();
            foreach (var item in items)
            {
                yield return item;
            }
        }

        public override IAsyncEnumerable<ListSecretsResponse> AsRawResponses()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// A <see cref="PagedAsyncEnumerable{TResponse,TResource}"/> that throws a specified exception when enumerated.
    /// Used to simulate Google Secret Manager failures during <c>await foreach</c> enumeration.
    /// </summary>
    private sealed class ThrowingSecretPageable(Exception exception)
        : PagedAsyncEnumerable<ListSecretsResponse, Google.Cloud.SecretManager.V1.Secret>
    {
        public override IAsyncEnumerator<Google.Cloud.SecretManager.V1.Secret> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            return ThrowAsync().GetAsyncEnumerator(cancellationToken);
        }

        private async IAsyncEnumerable<Google.Cloud.SecretManager.V1.Secret> ThrowAsync()
        {
            await Task.Yield();
            throw exception;

#pragma warning disable CS0162 // Unreachable code needed for IAsyncEnumerable type inference
            yield break;
#pragma warning restore CS0162
        }

        public override IAsyncEnumerable<ListSecretsResponse> AsRawResponses()
        {
            throw new NotImplementedException();
        }
    }
}

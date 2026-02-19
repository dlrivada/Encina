using Azure;
using Azure.Security.KeyVault.Secrets;
using Encina.Secrets;
using Encina.Secrets.AzureKeyVault;
using Encina.TestInfrastructure.Extensions;

namespace Encina.UnitTests.Secrets.Providers;

/// <summary>
/// Unit tests for <see cref="KeyVaultSecretProvider"/>.
/// Verifies that Azure SDK exceptions are correctly mapped to <see cref="EncinaError"/> values
/// following the Railway Oriented Programming pattern.
/// </summary>
public sealed class KeyVaultSecretProviderTests
{
    private const string SecretName = "my-secret";
    private const string SecretValue = "super-secret-value";
    private const string SecretVersion = "abc123version";

    private readonly SecretClient _client;
    private readonly ILogger<KeyVaultSecretProvider> _logger;
    private readonly KeyVaultSecretProvider _sut;

    public KeyVaultSecretProviderTests()
    {
        _client = Substitute.For<SecretClient>();
        _logger = Substitute.For<ILogger<KeyVaultSecretProvider>>();
        _sut = new KeyVaultSecretProvider(_client, _logger);
    }

    // ---------------------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        var act = () => new KeyVaultSecretProvider(null!, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("client");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new KeyVaultSecretProvider(_client, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("logger");
    }

    // ---------------------------------------------------------------------------
    // GetSecretAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetSecretAsync_WhenClientSucceeds_ReturnsRightWithSecret()
    {
        // Arrange
        var kvSecret = new KeyVaultSecret(SecretName, SecretValue);
        var mockResponse = Response.FromValue(kvSecret, Substitute.For<Response>());

        _client.GetSecretAsync(SecretName, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockResponse));

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
        _client.GetSecretAsync(SecretName, null, Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultSecret>>>(_ => throw new RequestFailedException(404, "Secret not found."));

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.NotFoundCode);
    }

    [Fact]
    public async Task GetSecretAsync_WhenForbidden_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        _client.GetSecretAsync(SecretName, null, Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultSecret>>>(_ => throw new RequestFailedException(403, "Forbidden."));

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task GetSecretAsync_WhenUnauthorized_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        _client.GetSecretAsync(SecretName, null, Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultSecret>>>(_ => throw new RequestFailedException(401, "Unauthorized."));

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task GetSecretAsync_WhenOtherError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        _client.GetSecretAsync(SecretName, null, Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultSecret>>>(_ => throw new RequestFailedException(500, "Internal Server Error."));

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
        var kvSecret = new KeyVaultSecret(SecretName, SecretValue);
        var mockResponse = Response.FromValue(kvSecret, Substitute.For<Response>());

        _client.GetSecretAsync(SecretName, SecretVersion, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockResponse));

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, SecretVersion);

        // Assert
        var secret = result.ShouldBeSuccess();
        secret.Name.ShouldBe(SecretName);
        secret.Value.ShouldBe(SecretValue);
    }

    [Fact]
    public async Task GetSecretVersionAsync_WhenNotFound_ReturnsLeftWithVersionNotFoundCode()
    {
        // Arrange
        _client.GetSecretAsync(SecretName, SecretVersion, Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultSecret>>>(_ => throw new RequestFailedException(404, "Version not found."));

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, SecretVersion);

        // Assert – 404 on a versioned get maps to VersionNotFound, not NotFound
        result.ShouldBeErrorWithCode(SecretsErrorCodes.VersionNotFoundCode);
    }

    [Fact]
    public async Task GetSecretVersionAsync_WhenForbidden_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        _client.GetSecretAsync(SecretName, SecretVersion, Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultSecret>>>(_ => throw new RequestFailedException(403, "Forbidden."));

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, SecretVersion);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task GetSecretVersionAsync_WhenOtherError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        _client.GetSecretAsync(SecretName, SecretVersion, Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultSecret>>>(_ => throw new RequestFailedException(503, "Service Unavailable."));

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, SecretVersion);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // SetSecretAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task SetSecretAsync_WhenClientSucceeds_ReturnsRightWithMetadata()
    {
        // Arrange
        var kvSecret = new KeyVaultSecret(SecretName, SecretValue);
        var mockResponse = Response.FromValue(kvSecret, Substitute.For<Response>());

        _client.SetSecretAsync(Arg.Is<KeyVaultSecret>(s => s.Name == SecretName && s.Value == SecretValue), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockResponse));

        // Act
        var result = await _sut.SetSecretAsync(SecretName, SecretValue);

        // Assert
        var metadata = result.ShouldBeSuccess();
        metadata.Name.ShouldBe(SecretName);
    }

    [Fact]
    public async Task SetSecretAsync_WhenForbidden_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        _client.SetSecretAsync(Arg.Any<KeyVaultSecret>(), Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultSecret>>>(_ => throw new RequestFailedException(403, "Forbidden."));

        // Act
        var result = await _sut.SetSecretAsync(SecretName, SecretValue);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task SetSecretAsync_WhenOtherError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        _client.SetSecretAsync(Arg.Any<KeyVaultSecret>(), Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultSecret>>>(_ => throw new RequestFailedException(500, "Internal Server Error."));

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
        var operation = Substitute.For<DeleteSecretOperation>();
        _client.StartDeleteSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(operation));

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task DeleteSecretAsync_WhenNotFound_ReturnsLeftWithNotFoundCode()
    {
        // Arrange
        _client.StartDeleteSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns<Task<DeleteSecretOperation>>(_ => throw new RequestFailedException(404, "Secret not found."));

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.NotFoundCode);
    }

    [Fact]
    public async Task DeleteSecretAsync_WhenForbidden_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        _client.StartDeleteSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns<Task<DeleteSecretOperation>>(_ => throw new RequestFailedException(403, "Forbidden."));

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    // ---------------------------------------------------------------------------
    // ListSecretsAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ListSecretsAsync_WhenClientSucceeds_ReturnsRightWithNames()
    {
        // Arrange – GetPropertiesOfSecretsAsync returns an AsyncPageable
        // The success case is validated via an empty pageable (no pages → empty enumerable).
        var emptyPageable = AsyncPageable<SecretProperties>.FromPages(
            Array.Empty<Page<SecretProperties>>());

        _client.GetPropertiesOfSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(emptyPageable);

        // Act
        var result = await _sut.ListSecretsAsync();

        // Assert
        var names = result.ShouldBeSuccess();
        names.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListSecretsAsync_WhenForbidden_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange – configure the pageable to throw on enumeration
        var failingPageable = new ThrowingAsyncPageable<SecretProperties>(
            new RequestFailedException(403, "Forbidden."));

        _client.GetPropertiesOfSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(failingPageable);

        // Act
        var result = await _sut.ListSecretsAsync();

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task ListSecretsAsync_WhenOtherError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        var failingPageable = new ThrowingAsyncPageable<SecretProperties>(
            new RequestFailedException(500, "Internal Server Error."));

        _client.GetPropertiesOfSecretsAsync(Arg.Any<CancellationToken>())
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
        var kvSecret = new KeyVaultSecret(SecretName, SecretValue);
        var mockResponse = Response.FromValue(kvSecret, Substitute.For<Response>());

        _client.GetSecretAsync(SecretName, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockResponse));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeSuccess(true);
    }

    [Fact]
    public async Task ExistsAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange – 404 on ExistsAsync returns Right(false), not Left
        _client.GetSecretAsync(SecretName, null, Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultSecret>>>(_ => throw new RequestFailedException(404, "Not found."));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert – must be Right(false), not Left
        result.ShouldBeSuccess(false);
    }

    [Fact]
    public async Task ExistsAsync_WhenForbidden_ReturnsLeft()
    {
        // Arrange
        _client.GetSecretAsync(SecretName, null, Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultSecret>>>(_ => throw new RequestFailedException(403, "Forbidden."));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task ExistsAsync_WhenOtherError_ReturnsLeft()
    {
        // Arrange
        _client.GetSecretAsync(SecretName, null, Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultSecret>>>(_ => throw new RequestFailedException(503, "Service Unavailable."));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // Helper: AsyncPageable that throws on enumeration
    // ---------------------------------------------------------------------------

    /// <summary>
    /// An <see cref="AsyncPageable{T}"/> that throws a specified exception when enumerated.
    /// Used to simulate Azure SDK failures during <c>await foreach</c> enumeration.
    /// </summary>
    private sealed class ThrowingAsyncPageable<T>(Exception exception) : AsyncPageable<T>
        where T : notnull
    {
        public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            await Task.Yield();
            throw exception;

#pragma warning disable CS0162 // Unreachable code needed for IAsyncEnumerable type inference
            yield break;
#pragma warning restore CS0162
        }
    }
}

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Encina.Secrets;
using Encina.Secrets.AWSSecretsManager;
using Encina.TestInfrastructure.Extensions;

namespace Encina.UnitTests.Secrets.Providers;

/// <summary>
/// Unit tests for <see cref="AWSSecretsManagerProvider"/>.
/// Verifies that AWS SDK exceptions are correctly mapped to <see cref="EncinaError"/> values
/// following the Railway Oriented Programming pattern.
/// </summary>
public sealed class AWSSecretsManagerProviderTests
{
    private const string SecretName = "my-aws-secret";
    private const string SecretValue = "aws-secret-value";
    private const string SecretVersion = "version-abc-123";

    private readonly IAmazonSecretsManager _client;
    private readonly ILogger<AWSSecretsManagerProvider> _logger;
    private readonly AWSSecretsManagerProvider _sut;

    public AWSSecretsManagerProviderTests()
    {
        _client = Substitute.For<IAmazonSecretsManager>();
        _logger = Substitute.For<ILogger<AWSSecretsManagerProvider>>();
        _sut = new AWSSecretsManagerProvider(_client, _logger);
    }

    // ---------------------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        var act = () => new AWSSecretsManagerProvider(null!, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("client");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new AWSSecretsManagerProvider(_client, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("logger");
    }

    // ---------------------------------------------------------------------------
    // GetSecretAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetSecretAsync_WhenClientSucceeds_ReturnsRightWithSecret()
    {
        // Arrange
        var response = new GetSecretValueResponse
        {
            Name = SecretName,
            SecretString = SecretValue,
            VersionId = SecretVersion
        };

        _client.GetSecretValueAsync(
                Arg.Is<GetSecretValueRequest>(r => r.SecretId == SecretName),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        var secret = result.ShouldBeSuccess();
        secret.Name.ShouldBe(SecretName);
        secret.Value.ShouldBe(SecretValue);
        secret.Version.ShouldBe(SecretVersion);
    }

    [Fact]
    public async Task GetSecretAsync_WhenNotFound_ReturnsLeftWithNotFoundCode()
    {
        // Arrange
        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<GetSecretValueResponse>>(_ => throw new ResourceNotFoundException("Secret not found."));

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.NotFoundCode);
    }

    [Fact]
    public async Task GetSecretAsync_WhenAccessDenied_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        var ex = new AmazonSecretsManagerException("Access denied.")
        {
            ErrorCode = "AccessDeniedException"
        };

        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<GetSecretValueResponse>>(_ => throw ex);

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task GetSecretAsync_WhenOtherAwsException_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        var ex = new AmazonSecretsManagerException("Internal server error.")
        {
            ErrorCode = "InternalServiceError"
        };

        _client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<GetSecretValueResponse>>(_ => throw ex);

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
        var response = new GetSecretValueResponse
        {
            Name = SecretName,
            SecretString = SecretValue,
            VersionId = SecretVersion
        };

        _client.GetSecretValueAsync(
                Arg.Is<GetSecretValueRequest>(r => r.SecretId == SecretName && r.VersionId == SecretVersion),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

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
        // Arrange – ResourceNotFoundException on a versioned get maps to VersionNotFound
        _client.GetSecretValueAsync(
                Arg.Is<GetSecretValueRequest>(r => r.VersionId == SecretVersion),
                Arg.Any<CancellationToken>())
            .Returns<Task<GetSecretValueResponse>>(_ => throw new ResourceNotFoundException("Version not found."));

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, SecretVersion);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.VersionNotFoundCode);
    }

    [Fact]
    public async Task GetSecretVersionAsync_WhenAccessDenied_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        var ex = new AmazonSecretsManagerException("Access denied.")
        {
            ErrorCode = "AccessDeniedException"
        };

        _client.GetSecretValueAsync(
                Arg.Is<GetSecretValueRequest>(r => r.VersionId == SecretVersion),
                Arg.Any<CancellationToken>())
            .Returns<Task<GetSecretValueResponse>>(_ => throw ex);

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, SecretVersion);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task GetSecretVersionAsync_WhenOtherError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        var ex = new AmazonSecretsManagerException("Throttling exception.")
        {
            ErrorCode = "ThrottlingException"
        };

        _client.GetSecretValueAsync(
                Arg.Is<GetSecretValueRequest>(r => r.VersionId == SecretVersion),
                Arg.Any<CancellationToken>())
            .Returns<Task<GetSecretValueResponse>>(_ => throw ex);

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, SecretVersion);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // SetSecretAsync – update existing (PutSecretValue path)
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task SetSecretAsync_WhenSecretExists_CallsPutAndReturnsRightWithMetadata()
    {
        // Arrange
        var putResponse = new PutSecretValueResponse
        {
            Name = SecretName,
            VersionId = SecretVersion
        };

        _client.PutSecretValueAsync(
                Arg.Is<PutSecretValueRequest>(r => r.SecretId == SecretName && r.SecretString == SecretValue),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(putResponse));

        // Act
        var result = await _sut.SetSecretAsync(SecretName, SecretValue);

        // Assert
        var metadata = result.ShouldBeSuccess();
        metadata.Name.ShouldBe(SecretName);
        metadata.Version.ShouldBe(SecretVersion);
    }

    [Fact]
    public async Task SetSecretAsync_WhenSecretNotFound_FallsBackToCreateAndReturnsRightWithMetadata()
    {
        // Arrange – PutSecretValue throws ResourceNotFoundException → CreateSecret is called
        _client.PutSecretValueAsync(Arg.Any<PutSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<PutSecretValueResponse>>(_ => throw new ResourceNotFoundException("Secret does not exist."));

        var createResponse = new CreateSecretResponse
        {
            Name = SecretName,
            VersionId = "new-version-id"
        };

        _client.CreateSecretAsync(
                Arg.Is<CreateSecretRequest>(r => r.Name == SecretName && r.SecretString == SecretValue),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(createResponse));

        // Act
        var result = await _sut.SetSecretAsync(SecretName, SecretValue);

        // Assert
        var metadata = result.ShouldBeSuccess();
        metadata.Name.ShouldBe(SecretName);
        metadata.Version.ShouldBe("new-version-id");
    }

    [Fact]
    public async Task SetSecretAsync_WhenAccessDenied_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        var ex = new AmazonSecretsManagerException("Access denied.")
        {
            ErrorCode = "AccessDeniedException"
        };

        _client.PutSecretValueAsync(Arg.Any<PutSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<PutSecretValueResponse>>(_ => throw ex);

        // Act
        var result = await _sut.SetSecretAsync(SecretName, SecretValue);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task SetSecretAsync_WhenOtherError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        var ex = new AmazonSecretsManagerException("Service unavailable.")
        {
            ErrorCode = "ServiceUnavailable"
        };

        _client.PutSecretValueAsync(Arg.Any<PutSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<PutSecretValueResponse>>(_ => throw ex);

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
                Arg.Is<DeleteSecretRequest>(r => r.SecretId == SecretName),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeleteSecretResponse()));

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task DeleteSecretAsync_WhenNotFound_ReturnsLeftWithNotFoundCode()
    {
        // Arrange
        _client.DeleteSecretAsync(Arg.Any<DeleteSecretRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<DeleteSecretResponse>>(_ => throw new ResourceNotFoundException("Secret not found."));

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.NotFoundCode);
    }

    [Fact]
    public async Task DeleteSecretAsync_WhenAccessDenied_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        var ex = new AmazonSecretsManagerException("Access denied.")
        {
            ErrorCode = "AccessDeniedException"
        };

        _client.DeleteSecretAsync(Arg.Any<DeleteSecretRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<DeleteSecretResponse>>(_ => throw ex);

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task DeleteSecretAsync_WhenOtherError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        var ex = new AmazonSecretsManagerException("Internal error.")
        {
            ErrorCode = "InternalServiceError"
        };

        _client.DeleteSecretAsync(Arg.Any<DeleteSecretRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<DeleteSecretResponse>>(_ => throw ex);

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
        // Arrange – single page with two secrets, no pagination
        var response = new ListSecretsResponse
        {
            SecretList =
            [
                new SecretListEntry { Name = "secret-alpha" },
                new SecretListEntry { Name = "secret-beta" }
            ],
            NextToken = null
        };

        _client.ListSecretsAsync(
                Arg.Is<ListSecretsRequest>(r => r.NextToken == null),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        // Act
        var result = await _sut.ListSecretsAsync();

        // Assert
        var names = result.ShouldBeSuccess().ToList();
        names.Count.ShouldBe(2);
        names.ShouldContain("secret-alpha");
        names.ShouldContain("secret-beta");
    }

    [Fact]
    public async Task ListSecretsAsync_WhenMultiplePages_ReturnsAllNames()
    {
        // Arrange – two pages
        var firstPage = new ListSecretsResponse
        {
            SecretList = [new SecretListEntry { Name = "secret-page1" }],
            NextToken = "token-for-page2"
        };

        var secondPage = new ListSecretsResponse
        {
            SecretList = [new SecretListEntry { Name = "secret-page2" }],
            NextToken = null
        };

        _client.ListSecretsAsync(
                Arg.Is<ListSecretsRequest>(r => r.NextToken == null),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(firstPage));

        _client.ListSecretsAsync(
                Arg.Is<ListSecretsRequest>(r => r.NextToken == "token-for-page2"),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(secondPage));

        // Act
        var result = await _sut.ListSecretsAsync();

        // Assert
        var names = result.ShouldBeSuccess().ToList();
        names.Count.ShouldBe(2);
        names.ShouldContain("secret-page1");
        names.ShouldContain("secret-page2");
    }

    [Fact]
    public async Task ListSecretsAsync_WhenAwsException_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        var ex = new AmazonSecretsManagerException("Internal error.")
        {
            ErrorCode = "InternalServiceError"
        };

        _client.ListSecretsAsync(Arg.Any<ListSecretsRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<ListSecretsResponse>>(_ => throw ex);

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
        _client.DescribeSecretAsync(
                Arg.Is<DescribeSecretRequest>(r => r.SecretId == SecretName),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DescribeSecretResponse { Name = SecretName }));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeSuccess(true);
    }

    [Fact]
    public async Task ExistsAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange – ResourceNotFoundException maps to Right(false), not Left
        _client.DescribeSecretAsync(Arg.Any<DescribeSecretRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<DescribeSecretResponse>>(_ => throw new ResourceNotFoundException("Secret not found."));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert – must be Right(false), not Left
        result.ShouldBeSuccess(false);
    }

    [Fact]
    public async Task ExistsAsync_WhenOtherError_ReturnsLeft()
    {
        // Arrange
        var ex = new AmazonSecretsManagerException("Internal error.")
        {
            ErrorCode = "InternalServiceError"
        };

        _client.DescribeSecretAsync(Arg.Any<DescribeSecretRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<DescribeSecretResponse>>(_ => throw ex);

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }
}

using Amazon.KeyManagementService;
using Encina.Messaging.Encryption.AwsKms;

namespace Encina.GuardTests.Messaging.Encryption.AwsKms;

/// <summary>
/// Guard clause tests for <see cref="AwsKmsKeyProvider"/>.
/// Verifies that null/empty/whitespace parameters are properly guarded.
/// </summary>
public sealed class AwsKmsKeyProviderGuardTests
{
    private readonly IAmazonKeyManagementService _kmsClient = Substitute.For<IAmazonKeyManagementService>();
    private readonly IOptions<AwsKmsOptions> _options = Options.Create(new AwsKmsOptions { KeyId = "test-key" });
    private readonly ILogger<AwsKmsKeyProvider> _logger = NullLogger<AwsKmsKeyProvider>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when kmsClient is null.
    /// </summary>
    [Fact]
    public void Constructor_NullKmsClient_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new AwsKmsKeyProvider(null!, _options, _logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("kmsClient");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new AwsKmsKeyProvider(_kmsClient, null!, _logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new AwsKmsKeyProvider(_kmsClient, _options, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region GetKeyAsync Guards

    /// <summary>
    /// Verifies that GetKeyAsync throws ArgumentNullException when keyId is null.
    /// </summary>
    [Fact]
    public async Task GetKeyAsync_NullKeyId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new AwsKmsKeyProvider(_kmsClient, _options, _logger);

        // Act
        var act = async () => await provider.GetKeyAsync(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    /// <summary>
    /// Verifies that GetKeyAsync throws ArgumentException when keyId is empty.
    /// </summary>
    [Fact]
    public async Task GetKeyAsync_EmptyKeyId_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new AwsKmsKeyProvider(_kmsClient, _options, _logger);

        // Act
        var act = async () => await provider.GetKeyAsync("");

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that GetKeyAsync throws ArgumentException when keyId is whitespace.
    /// </summary>
    [Fact]
    public async Task GetKeyAsync_WhitespaceKeyId_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new AwsKmsKeyProvider(_kmsClient, _options, _logger);

        // Act
        var act = async () => await provider.GetKeyAsync("   ");

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    #endregion
}

using Amazon.SecretsManager;
using Azure.Security.KeyVault.Secrets;
using Encina.Secrets;
using Encina.Secrets.AWSSecretsManager;
using Encina.Secrets.AzureKeyVault;
using Encina.Secrets.GoogleSecretManager;
using Encina.Secrets.HashiCorpVault;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Caching.Memory;
using VaultSharp;

namespace Encina.GuardTests.Secrets;

/// <summary>
/// Guard clause tests for all public Secrets provider classes.
/// Verifies that null parameters throw <see cref="ArgumentNullException"/>
/// both in constructors and on method-level null inputs.
/// </summary>
public class SecretProviderGuardTests : IDisposable
{
    private readonly MemoryCache _memoryCache;
    private bool _disposed;

    public SecretProviderGuardTests()
    {
        _memoryCache = new MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _memoryCache.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    // -------------------------------------------------------------------------
    // CachedSecretProvider constructor guards
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the constructor throws when <c>inner</c> is null.
    /// </summary>
    [Fact]
    public void CachedSecretProvider_Constructor_NullInner_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new SecretCacheOptions());
        var logger = NullLogger<CachedSecretProvider>.Instance;

        // Act & Assert
        var act = () => new CachedSecretProvider(null!, _memoryCache, options, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("inner");
    }

    /// <summary>
    /// Verifies that the constructor throws when <c>cache</c> is null.
    /// </summary>
    [Fact]
    public void CachedSecretProvider_Constructor_NullCache_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var options = Options.Create(new SecretCacheOptions());
        var logger = NullLogger<CachedSecretProvider>.Instance;

        // Act & Assert
        var act = () => new CachedSecretProvider(inner, null!, options, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cache");
    }

    /// <summary>
    /// Verifies that the constructor throws when <c>options</c> is null.
    /// </summary>
    [Fact]
    public void CachedSecretProvider_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var logger = NullLogger<CachedSecretProvider>.Instance;

        // Act & Assert
        var act = () => new CachedSecretProvider(inner, _memoryCache, null!, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws when <c>logger</c> is null.
    /// </summary>
    [Fact]
    public void CachedSecretProvider_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var options = Options.Create(new SecretCacheOptions());

        // Act & Assert
        var act = () => new CachedSecretProvider(inner, _memoryCache, options, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    // -------------------------------------------------------------------------
    // KeyVaultSecretProvider constructor guards
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the constructor throws when <c>client</c> is null.
    /// </summary>
    [Fact]
    public void KeyVaultSecretProvider_Constructor_NullClient_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<KeyVaultSecretProvider>.Instance;

        // Act & Assert
        // SecretClient has a protected no-arg constructor used for mocking/subclassing;
        // passing null! for the concrete type exercises the guard.
        var act = () => new KeyVaultSecretProvider(null!, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("client");
    }

    /// <summary>
    /// Verifies that the constructor throws when <c>logger</c> is null.
    /// </summary>
    [Fact]
    public void KeyVaultSecretProvider_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        // SecretClient supports NSubstitute mocking (non-sealed, virtual members).
        var client = Substitute.For<SecretClient>();

        // Act & Assert
        var act = () => new KeyVaultSecretProvider(client, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    // -------------------------------------------------------------------------
    // KeyVaultSecretProvider method-level guards
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="KeyVaultSecretProvider.GetSecretAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task KeyVaultSecretProvider_GetSecretAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateKeyVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.GetSecretAsync(null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="KeyVaultSecretProvider.GetSecretVersionAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task KeyVaultSecretProvider_GetSecretVersionAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateKeyVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.GetSecretVersionAsync(null!, "v1").AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="KeyVaultSecretProvider.GetSecretVersionAsync"/> throws when <c>version</c> is null.
    /// </summary>
    [Fact]
    public async Task KeyVaultSecretProvider_GetSecretVersionAsync_NullVersion_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateKeyVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.GetSecretVersionAsync("my-secret", null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("version");
    }

    /// <summary>
    /// Verifies that <see cref="KeyVaultSecretProvider.SetSecretAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task KeyVaultSecretProvider_SetSecretAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateKeyVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.SetSecretAsync(null!, "value").AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="KeyVaultSecretProvider.SetSecretAsync"/> throws when <c>value</c> is null.
    /// </summary>
    [Fact]
    public async Task KeyVaultSecretProvider_SetSecretAsync_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateKeyVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.SetSecretAsync("my-secret", null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("value");
    }

    /// <summary>
    /// Verifies that <see cref="KeyVaultSecretProvider.DeleteSecretAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task KeyVaultSecretProvider_DeleteSecretAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateKeyVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.DeleteSecretAsync(null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="KeyVaultSecretProvider.ExistsAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task KeyVaultSecretProvider_ExistsAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateKeyVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.ExistsAsync(null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    // -------------------------------------------------------------------------
    // AWSSecretsManagerProvider constructor guards
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the constructor throws when <c>client</c> is null.
    /// </summary>
    [Fact]
    public void AWSSecretsManagerProvider_Constructor_NullClient_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<AWSSecretsManagerProvider>.Instance;

        // Act & Assert
        var act = () => new AWSSecretsManagerProvider(null!, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("client");
    }

    /// <summary>
    /// Verifies that the constructor throws when <c>logger</c> is null.
    /// </summary>
    [Fact]
    public void AWSSecretsManagerProvider_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var client = Substitute.For<IAmazonSecretsManager>();

        // Act & Assert
        var act = () => new AWSSecretsManagerProvider(client, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    // -------------------------------------------------------------------------
    // AWSSecretsManagerProvider method-level guards
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="AWSSecretsManagerProvider.GetSecretAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task AWSSecretsManagerProvider_GetSecretAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateAWSProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.GetSecretAsync(null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="AWSSecretsManagerProvider.GetSecretVersionAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task AWSSecretsManagerProvider_GetSecretVersionAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateAWSProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.GetSecretVersionAsync(null!, "v1").AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="AWSSecretsManagerProvider.GetSecretVersionAsync"/> throws when <c>version</c> is null.
    /// </summary>
    [Fact]
    public async Task AWSSecretsManagerProvider_GetSecretVersionAsync_NullVersion_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateAWSProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.GetSecretVersionAsync("my-secret", null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("version");
    }

    /// <summary>
    /// Verifies that <see cref="AWSSecretsManagerProvider.SetSecretAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task AWSSecretsManagerProvider_SetSecretAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateAWSProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.SetSecretAsync(null!, "value").AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="AWSSecretsManagerProvider.SetSecretAsync"/> throws when <c>value</c> is null.
    /// </summary>
    [Fact]
    public async Task AWSSecretsManagerProvider_SetSecretAsync_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateAWSProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.SetSecretAsync("my-secret", null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("value");
    }

    /// <summary>
    /// Verifies that <see cref="AWSSecretsManagerProvider.DeleteSecretAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task AWSSecretsManagerProvider_DeleteSecretAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateAWSProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.DeleteSecretAsync(null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="AWSSecretsManagerProvider.ExistsAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task AWSSecretsManagerProvider_ExistsAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateAWSProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.ExistsAsync(null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    // -------------------------------------------------------------------------
    // HashiCorpVaultProvider constructor guards
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the constructor throws when <c>client</c> is null.
    /// </summary>
    [Fact]
    public void HashiCorpVaultProvider_Constructor_NullClient_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new HashiCorpVaultOptions());
        var logger = NullLogger<HashiCorpVaultProvider>.Instance;

        // Act & Assert
        var act = () => new HashiCorpVaultProvider(null!, options, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("client");
    }

    /// <summary>
    /// Verifies that the constructor throws when <c>options</c> is null.
    /// </summary>
    [Fact]
    public void HashiCorpVaultProvider_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var client = Substitute.For<IVaultClient>();
        var logger = NullLogger<HashiCorpVaultProvider>.Instance;

        // Act & Assert
        var act = () => new HashiCorpVaultProvider(client, null!, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws when <c>logger</c> is null.
    /// </summary>
    [Fact]
    public void HashiCorpVaultProvider_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var client = Substitute.For<IVaultClient>();
        var options = Options.Create(new HashiCorpVaultOptions());

        // Act & Assert
        var act = () => new HashiCorpVaultProvider(client, options, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    // -------------------------------------------------------------------------
    // HashiCorpVaultProvider method-level guards
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="HashiCorpVaultProvider.GetSecretAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task HashiCorpVaultProvider_GetSecretAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateHashiCorpVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.GetSecretAsync(null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="HashiCorpVaultProvider.GetSecretVersionAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task HashiCorpVaultProvider_GetSecretVersionAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateHashiCorpVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.GetSecretVersionAsync(null!, "1").AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="HashiCorpVaultProvider.GetSecretVersionAsync"/> throws when <c>version</c> is null.
    /// </summary>
    [Fact]
    public async Task HashiCorpVaultProvider_GetSecretVersionAsync_NullVersion_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateHashiCorpVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.GetSecretVersionAsync("my-secret", null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("version");
    }

    /// <summary>
    /// Verifies that <see cref="HashiCorpVaultProvider.SetSecretAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task HashiCorpVaultProvider_SetSecretAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateHashiCorpVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.SetSecretAsync(null!, "value").AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="HashiCorpVaultProvider.SetSecretAsync"/> throws when <c>value</c> is null.
    /// </summary>
    [Fact]
    public async Task HashiCorpVaultProvider_SetSecretAsync_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateHashiCorpVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.SetSecretAsync("my-secret", null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("value");
    }

    /// <summary>
    /// Verifies that <see cref="HashiCorpVaultProvider.DeleteSecretAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task HashiCorpVaultProvider_DeleteSecretAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateHashiCorpVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.DeleteSecretAsync(null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="HashiCorpVaultProvider.ExistsAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task HashiCorpVaultProvider_ExistsAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateHashiCorpVaultProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.ExistsAsync(null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    // -------------------------------------------------------------------------
    // GoogleSecretManagerProvider constructor guards
    // -------------------------------------------------------------------------
    //
    // Note: SecretManagerServiceClient is an abstract class in the Google SDK,
    // so NSubstitute can create a proxy for it via Substitute.For<SecretManagerServiceClient>().

    /// <summary>
    /// Verifies that the constructor throws when <c>client</c> is null.
    /// </summary>
    [Fact]
    public void GoogleSecretManagerProvider_Constructor_NullClient_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new GoogleSecretManagerOptions());
        var logger = NullLogger<GoogleSecretManagerProvider>.Instance;

        // Act & Assert
        var act = () => new GoogleSecretManagerProvider(null!, options, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("client");
    }

    /// <summary>
    /// Verifies that the constructor throws when <c>options</c> is null.
    /// </summary>
    [Fact]
    public void GoogleSecretManagerProvider_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        // SecretManagerServiceClient is abstract - NSubstitute can mock it.
        var client = Substitute.For<SecretManagerServiceClient>();
        var logger = NullLogger<GoogleSecretManagerProvider>.Instance;

        // Act & Assert
        var act = () => new GoogleSecretManagerProvider(client, null!, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws when <c>logger</c> is null.
    /// </summary>
    [Fact]
    public void GoogleSecretManagerProvider_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var client = Substitute.For<SecretManagerServiceClient>();
        var options = Options.Create(new GoogleSecretManagerOptions());

        // Act & Assert
        var act = () => new GoogleSecretManagerProvider(client, options, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    // -------------------------------------------------------------------------
    // GoogleSecretManagerProvider method-level guards
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="GoogleSecretManagerProvider.GetSecretAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task GoogleSecretManagerProvider_GetSecretAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateGoogleProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.GetSecretAsync(null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="GoogleSecretManagerProvider.GetSecretVersionAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task GoogleSecretManagerProvider_GetSecretVersionAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateGoogleProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.GetSecretVersionAsync(null!, "1").AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="GoogleSecretManagerProvider.GetSecretVersionAsync"/> throws when <c>version</c> is null.
    /// </summary>
    [Fact]
    public async Task GoogleSecretManagerProvider_GetSecretVersionAsync_NullVersion_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateGoogleProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.GetSecretVersionAsync("my-secret", null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("version");
    }

    /// <summary>
    /// Verifies that <see cref="GoogleSecretManagerProvider.SetSecretAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task GoogleSecretManagerProvider_SetSecretAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateGoogleProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.SetSecretAsync(null!, "value").AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="GoogleSecretManagerProvider.SetSecretAsync"/> throws when <c>value</c> is null.
    /// </summary>
    [Fact]
    public async Task GoogleSecretManagerProvider_SetSecretAsync_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateGoogleProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.SetSecretAsync("my-secret", null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("value");
    }

    /// <summary>
    /// Verifies that <see cref="GoogleSecretManagerProvider.DeleteSecretAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task GoogleSecretManagerProvider_DeleteSecretAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateGoogleProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.DeleteSecretAsync(null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that <see cref="GoogleSecretManagerProvider.ExistsAsync"/> throws when <c>name</c> is null.
    /// </summary>
    [Fact]
    public async Task GoogleSecretManagerProvider_ExistsAsync_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateGoogleProvider();

        // Act & Assert
#pragma warning disable CA2012
        var act = () => provider.ExistsAsync(null!).AsTask();
#pragma warning restore CA2012
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("name");
    }

    // -------------------------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------------------------

    private static KeyVaultSecretProvider CreateKeyVaultProvider()
    {
        var client = Substitute.For<SecretClient>();
        var logger = NullLogger<KeyVaultSecretProvider>.Instance;
        return new KeyVaultSecretProvider(client, logger);
    }

    private static AWSSecretsManagerProvider CreateAWSProvider()
    {
        var client = Substitute.For<IAmazonSecretsManager>();
        var logger = NullLogger<AWSSecretsManagerProvider>.Instance;
        return new AWSSecretsManagerProvider(client, logger);
    }

    private static HashiCorpVaultProvider CreateHashiCorpVaultProvider()
    {
        var client = Substitute.For<IVaultClient>();
        var options = Options.Create(new HashiCorpVaultOptions());
        var logger = NullLogger<HashiCorpVaultProvider>.Instance;
        return new HashiCorpVaultProvider(client, options, logger);
    }

    private static GoogleSecretManagerProvider CreateGoogleProvider()
    {
        var client = Substitute.For<SecretManagerServiceClient>();
        var options = Options.Create(new GoogleSecretManagerOptions { ProjectId = "test-project" });
        var logger = NullLogger<GoogleSecretManagerProvider>.Instance;
        return new GoogleSecretManagerProvider(client, options, logger);
    }
}

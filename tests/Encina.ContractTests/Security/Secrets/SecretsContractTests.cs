using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Providers;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Security.Secrets;

/// <summary>
/// Contract tests for <see cref="ISecretReader"/> to verify consistent behavior
/// across implementations (ConfigurationSecretProvider, EnvironmentSecretProvider).
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Secrets")]
public sealed class SecretsContractTests
{
    #region ConfigurationSecretProvider Contract

    [Fact]
    public async Task ConfigurationSecretProvider_ExistingKey_ReturnsRight()
    {
        // Arrange
        var provider = CreateConfigurationProvider(new Dictionary<string, string?>
        {
            ["Secrets:api-key"] = "my-secret-value"
        });

        // Act
        var result = await provider.GetSecretAsync("api-key");

        // Assert
        result.IsRight.ShouldBeTrue(
            "ConfigurationSecretProvider must return Right for existing keys");

        var value = result.Match(
            Right: v => v,
            Left: _ => string.Empty);

        value.ShouldBe("my-secret-value");
    }

    [Fact]
    public async Task ConfigurationSecretProvider_MissingKey_ReturnsLeftNotFound()
    {
        // Arrange
        var provider = CreateConfigurationProvider([]);

        // Act
        var result = await provider.GetSecretAsync("non-existent-key");

        // Assert
        result.IsLeft.ShouldBeTrue(
            "ConfigurationSecretProvider must return Left for missing keys");

        result.Match(
            Right: _ => { },
            Left: error =>
            {
                var code = error.GetCode().IfNone(string.Empty);
                code.ShouldBe(SecretsErrors.NotFoundCode,
                    "Missing key error must use NotFoundCode");
            });
    }

    [Fact]
    public async Task ConfigurationSecretProvider_MultipleKeys_ReturnsCorrectValues()
    {
        // Arrange
        var provider = CreateConfigurationProvider(new Dictionary<string, string?>
        {
            ["Secrets:key-1"] = "value-1",
            ["Secrets:key-2"] = "value-2",
            ["Secrets:key-3"] = "value-3"
        });

        // Act & Assert
        var result1 = await provider.GetSecretAsync("key-1");
        result1.IsRight.ShouldBeTrue();
        result1.Match(Right: v => v, Left: _ => string.Empty).ShouldBe("value-1");

        var result2 = await provider.GetSecretAsync("key-2");
        result2.IsRight.ShouldBeTrue();
        result2.Match(Right: v => v, Left: _ => string.Empty).ShouldBe("value-2");

        var result3 = await provider.GetSecretAsync("key-3");
        result3.IsRight.ShouldBeTrue();
        result3.Match(Right: v => v, Left: _ => string.Empty).ShouldBe("value-3");
    }

    #endregion

    #region EnvironmentSecretProvider Contract

    [Fact]
    public async Task EnvironmentSecretProvider_ExistingVariable_ReturnsRight()
    {
        // Arrange
        var envVarName = $"ENCINA_TEST_SECRET_{Guid.NewGuid():N}";
        const string expectedValue = "env-secret-value";

        try
        {
            Environment.SetEnvironmentVariable(envVarName, expectedValue);

            var provider = new EnvironmentSecretProvider(
                NullLogger<EnvironmentSecretProvider>.Instance);

            // Act
            var result = await provider.GetSecretAsync(envVarName);

            // Assert
            result.IsRight.ShouldBeTrue(
                "EnvironmentSecretProvider must return Right for existing environment variables");

            var value = result.Match(
                Right: v => v,
                Left: _ => string.Empty);

            value.ShouldBe(expectedValue);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }

    [Fact]
    public async Task EnvironmentSecretProvider_MissingVariable_ReturnsLeftNotFound()
    {
        // Arrange
        var provider = new EnvironmentSecretProvider(
            NullLogger<EnvironmentSecretProvider>.Instance);

        var nonExistentVar = $"ENCINA_NONEXISTENT_{Guid.NewGuid():N}";

        // Act
        var result = await provider.GetSecretAsync(nonExistentVar);

        // Assert
        result.IsLeft.ShouldBeTrue(
            "EnvironmentSecretProvider must return Left for missing environment variables");

        result.Match(
            Right: _ => { },
            Left: error =>
            {
                var code = error.GetCode().IfNone(string.Empty);
                code.ShouldBe(SecretsErrors.NotFoundCode,
                    "Missing environment variable error must use NotFoundCode");
            });
    }

    #endregion

    #region Cross-Provider Consistency

    [Fact]
    public async Task AllProviders_MissingKey_ReturnSameErrorCode()
    {
        // Arrange
        var configProvider = CreateConfigurationProvider([]);
        var envProvider = new EnvironmentSecretProvider(
            NullLogger<EnvironmentSecretProvider>.Instance);

        var missingKey = $"ENCINA_MISSING_{Guid.NewGuid():N}";

        // Act
        var configResult = await configProvider.GetSecretAsync(missingKey);
        var envResult = await envProvider.GetSecretAsync(missingKey);

        // Assert - both must return the same error code
        configResult.IsLeft.ShouldBeTrue();
        envResult.IsLeft.ShouldBeTrue();

        var configCode = configResult.Match(
            Right: _ => string.Empty,
            Left: e => e.GetCode().IfNone(string.Empty));

        var envCode = envResult.Match(
            Right: _ => string.Empty,
            Left: e => e.GetCode().IfNone(string.Empty));

        configCode.ShouldBe(envCode,
            "All ISecretReader implementations must use the same error code for missing keys");
        configCode.ShouldBe(SecretsErrors.NotFoundCode);
    }

    #endregion

    #region Helpers

    private static ConfigurationSecretProvider CreateConfigurationProvider(
        IEnumerable<KeyValuePair<string, string?>> configData)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        return new ConfigurationSecretProvider(
            configuration,
            NullLogger<ConfigurationSecretProvider>.Instance);
    }

    #endregion
}

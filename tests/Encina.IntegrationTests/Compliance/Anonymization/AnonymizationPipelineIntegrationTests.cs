using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.InMemory;
using Encina.Compliance.Anonymization.Model;
using Encina.Compliance.Anonymization.Techniques;

using Shouldly;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Compliance.Anonymization;

/// <summary>
/// Integration tests for the full Encina.Compliance.Anonymization pipeline.
/// Tests DI registration, full lifecycle flows (tokenize/detokenize, pseudonymize/depseudonymize,
/// anonymize), options configuration, and concurrent access safety.
/// No Docker containers needed — all operations use in-memory stores.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AnonymizationPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaAnonymization_RegistersIAnonymizer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAnonymization();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IAnonymizer>().ShouldNotBeNull();
        provider.GetService<IAnonymizer>().ShouldBeOfType<DefaultAnonymizer>();
    }

    [Fact]
    public void AddEncinaAnonymization_RegistersIPseudonymizer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAnonymization();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IPseudonymizer>().ShouldNotBeNull();
        provider.GetService<IPseudonymizer>().ShouldBeOfType<DefaultPseudonymizer>();
    }

    [Fact]
    public void AddEncinaAnonymization_RegistersITokenizer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAnonymization();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ITokenizer>().ShouldNotBeNull();
        provider.GetService<ITokenizer>().ShouldBeOfType<DefaultTokenizer>();
    }

    [Fact]
    public void AddEncinaAnonymization_RegistersIRiskAssessor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAnonymization();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRiskAssessor>().ShouldNotBeNull();
        provider.GetService<IRiskAssessor>().ShouldBeOfType<DefaultRiskAssessor>();
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void AddEncinaAnonymization_DefaultOptions_EnforcementModeIsBlock()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAnonymization();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<AnonymizationOptions>>().Value;
        options.EnforcementMode.ShouldBe(AnonymizationEnforcementMode.Block);
        options.TrackAuditTrail.ShouldBeTrue();
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaAnonymization_CustomOptions_AreRespected()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAnonymization(options =>
        {
            options.EnforcementMode = AnonymizationEnforcementMode.Warn;
            options.TrackAuditTrail = false;
            options.AddHealthCheck = true;
            options.AutoRegisterFromAttributes = false;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<AnonymizationOptions>>().Value;
        options.EnforcementMode.ShouldBe(AnonymizationEnforcementMode.Warn);
        options.TrackAuditTrail.ShouldBeFalse();
        options.AutoRegisterFromAttributes.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaAnonymization_WithConfigure_CallsCallback()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var callbackInvoked = false;

        // Act
        services.AddEncinaAnonymization(options =>
        {
            callbackInvoked = true;
            options.EnforcementMode = AnonymizationEnforcementMode.Disabled;
        });
        var provider = services.BuildServiceProvider();

        // Force options resolution to trigger the configure callback
        var options = provider.GetRequiredService<IOptions<AnonymizationOptions>>().Value;

        // Assert
        callbackInvoked.ShouldBeTrue();
        options.EnforcementMode.ShouldBe(AnonymizationEnforcementMode.Disabled);
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public async Task Tokenize_Detokenize_Roundtrip()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAnonymization();
        var provider = services.BuildServiceProvider();

        var tokenizer = provider.GetRequiredService<ITokenizer>();
        var originalValue = "4111-1111-1111-1111";
        var options = new TokenizationOptions
        {
            Format = TokenFormat.Uuid
        };

        // Act: Tokenize
        var tokenResult = await tokenizer.TokenizeAsync(originalValue, options);
        tokenResult.IsRight.ShouldBeTrue("tokenization should succeed");

        var token = (string)tokenResult;
        token.ShouldNotBeNullOrWhiteSpace();
        token.ShouldNotBe(originalValue, "token must differ from original value");

        // Act: Detokenize
        var detokenizeResult = await tokenizer.DetokenizeAsync(token);
        detokenizeResult.IsRight.ShouldBeTrue("detokenization should succeed");

        var recovered = (string)detokenizeResult;

        // Assert: Roundtrip produces the original value
        recovered.ShouldBe(originalValue);
    }

    [Fact]
    public async Task Pseudonymize_Depseudonymize_Value_Roundtrip()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAnonymization();
        var provider = services.BuildServiceProvider();

        var pseudonymizer = provider.GetRequiredService<IPseudonymizer>();
        var keyProvider = provider.GetRequiredService<IKeyProvider>();

        // Get the active key ID generated at startup by InMemoryKeyProvider
        var activeKeyResult = await keyProvider.GetActiveKeyIdAsync();
        activeKeyResult.IsRight.ShouldBeTrue("active key should exist");
        var keyId = (string)activeKeyResult;

        var originalValue = "john.doe@example.com";

        // Act: Pseudonymize with AES-256-GCM (reversible)
        var pseudonymResult = await pseudonymizer.PseudonymizeValueAsync(
            originalValue,
            keyId,
            PseudonymizationAlgorithm.Aes256Gcm);
        pseudonymResult.IsRight.ShouldBeTrue("pseudonymization should succeed");

        var pseudonym = (string)pseudonymResult;
        pseudonym.ShouldNotBeNullOrWhiteSpace();
        pseudonym.ShouldNotBe(originalValue, "pseudonym must differ from original value");

        // Act: Depseudonymize
        var depseudonymizeResult = await pseudonymizer.DepseudonymizeValueAsync(pseudonym, keyId);
        depseudonymizeResult.IsRight.ShouldBeTrue("depseudonymization should succeed");

        var recovered = (string)depseudonymizeResult;

        // Assert: Roundtrip produces the original value
        recovered.ShouldBe(originalValue);
    }

    [Fact]
    public async Task Anonymize_WithDataMasking_ReturnsAnonymizedData()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAnonymization();
        var provider = services.BuildServiceProvider();

        var anonymizer = provider.GetRequiredService<IAnonymizer>();

        var profile = AnonymizationProfile.Create(
            name: "test-masking",
            fieldRules:
            [
                new FieldAnonymizationRule
                {
                    FieldName = "Name",
                    Technique = AnonymizationTechnique.DataMasking,
                    Parameters = new Dictionary<string, object>
                    {
                        ["PreserveStart"] = 1,
                        ["PreserveEnd"] = 0
                    }
                },
                new FieldAnonymizationRule
                {
                    FieldName = "Email",
                    Technique = AnonymizationTechnique.DataMasking,
                    Parameters = new Dictionary<string, object>
                    {
                        ["PreserveDomain"] = true,
                        ["PreserveStart"] = 1
                    }
                }
            ]);

        var data = new TestPersonRecord
        {
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30
        };

        // Act
        var result = await anonymizer.AnonymizeAsync(data, profile);
        var errorMsg = result.Match(Right: _ => "no error", Left: e => e.Message);
        result.IsRight.ShouldBeTrue($"anonymization should succeed, but got error: {errorMsg}");

        var anonymized = (TestPersonRecord)result;

        // Assert: Masked fields differ from originals
        anonymized.Name.ShouldNotBe("John Doe", "Name should be masked");
        anonymized.Name.ShouldStartWith("J", customMessage: "first character should be preserved");
        anonymized.Name.ShouldContain("*", customMessage: "masked portion should contain asterisks");

        anonymized.Email.ShouldNotBe("john@example.com", "Email should be masked");
        anonymized.Email!.ShouldContain("@example.com", customMessage: "domain should be preserved");

        // Assert: Non-targeted field is unchanged
        anonymized.Age.ShouldBe(30, "Age has no rule and should be preserved");

        // Assert: Original data is not mutated
        data.Name.ShouldBe("John Doe");
        data.Email.ShouldBe("john@example.com");
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public async Task ConcurrentTokenization_NoDataCorruption()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAnonymization();
        var provider = services.BuildServiceProvider();

        var tokenizer = provider.GetRequiredService<ITokenizer>();
        var tokenCount = 50;
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        // Act: Tokenize different values concurrently
        var tasks = Enumerable.Range(0, tokenCount).Select(async i =>
        {
            var value = $"sensitive-value-{i}";
            var tokenResult = await tokenizer.TokenizeAsync(value, options);
            return (Value: value, TokenResult: tokenResult);
        });

        var results = await Task.WhenAll(tasks);

        // Assert: All tokenizations succeeded
        results.ShouldAllBe(r => r.TokenResult.IsRight);

        // Assert: All tokens are unique (no collisions)
        var tokens = results.Select(r => (string)r.TokenResult).ToList();
        tokens.ShouldBeUnique("each value should produce a unique token");

        // Assert: All tokens can be detokenized back
        foreach (var result in results)
        {
            var token = (string)result.TokenResult;
            var detokenizeResult = await tokenizer.DetokenizeAsync(token);
            detokenizeResult.IsRight.ShouldBeTrue();
            ((string)detokenizeResult).ShouldBe(result.Value);
        }
    }

    [Fact]
    public async Task ConcurrentPseudonymization_NoDataCorruption()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAnonymization();
        var provider = services.BuildServiceProvider();

        var pseudonymizer = provider.GetRequiredService<IPseudonymizer>();
        var keyProvider = provider.GetRequiredService<IKeyProvider>();

        var activeKeyResult = await keyProvider.GetActiveKeyIdAsync();
        activeKeyResult.IsRight.ShouldBeTrue();
        var keyId = (string)activeKeyResult;

        var valueCount = 50;

        // Act: Pseudonymize different values concurrently with AES-256-GCM
        var tasks = Enumerable.Range(0, valueCount).Select(async i =>
        {
            var value = $"user-{i}@example.com";
            var pseudonymResult = await pseudonymizer.PseudonymizeValueAsync(
                value,
                keyId,
                PseudonymizationAlgorithm.Aes256Gcm);
            return (Value: value, PseudonymResult: pseudonymResult);
        });

        var results = await Task.WhenAll(tasks);

        // Assert: All pseudonymizations succeeded
        results.ShouldAllBe(r => r.PseudonymResult.IsRight);

        // Assert: All pseudonyms can be depseudonymized back to original values
        foreach (var result in results)
        {
            var pseudonym = (string)result.PseudonymResult;
            var depseudonymizeResult = await pseudonymizer.DepseudonymizeValueAsync(pseudonym, keyId);
            depseudonymizeResult.IsRight.ShouldBeTrue();
            ((string)depseudonymizeResult).ShouldBe(result.Value);
        }
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Simple record used as a test data object for anonymization operations.
    /// </summary>
    private sealed class TestPersonRecord
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Age { get; set; }
    }

    #endregion
}

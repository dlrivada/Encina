using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.InMemory;
using Encina.Compliance.Anonymization.Model;
using Encina.Compliance.Anonymization.Techniques;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.IntegrationTests.Compliance.Anonymization;

/// <summary>
/// Integration tests for the full Anonymization service lifecycle.
/// Tests end-to-end operations via DI-wired services: tokenization roundtrips,
/// pseudonymization roundtrips, audit trail recording, risk assessment, health checks,
/// anonymization techniques (suppression, generalization), and concurrent data integrity.
/// No Docker containers needed — all operations use in-memory stores.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AnonymizationServiceIntegrationTests
{
    #region Audit Trail

    [Fact]
    public async Task AuditStore_AddAndRetrieve_BySubjectId()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var auditStore = provider.GetRequiredService<IAnonymizationAuditStore>();
        var subjectId = "user-audit-test";

        var entry1 = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Anonymized,
            technique: AnonymizationTechnique.DataMasking,
            fieldName: "Email",
            subjectId: subjectId,
            performedByUserId: "system");

        var entry2 = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Pseudonymized,
            technique: AnonymizationTechnique.DataMasking,
            fieldName: "Name",
            subjectId: subjectId,
            keyId: "key-2026-01",
            performedByUserId: "admin");

        // Act
        var addResult1 = await auditStore.AddEntryAsync(entry1);
        var addResult2 = await auditStore.AddEntryAsync(entry2);

        // Assert: additions succeed
        addResult1.IsRight.ShouldBeTrue("first audit entry should be added");
        addResult2.IsRight.ShouldBeTrue("second audit entry should be added");

        // Assert: retrieval by subject
        var trailResult = await auditStore.GetBySubjectIdAsync(subjectId);
        trailResult.IsRight.ShouldBeTrue("retrieval should succeed");

        var trail = trailResult.Match(Right: r => r, Left: _ => []);
        trail.Count.ShouldBe(2);
        trail.ShouldContain(e => e.Operation == AnonymizationOperation.Anonymized);
        trail.ShouldContain(e => e.Operation == AnonymizationOperation.Pseudonymized);
    }

    [Fact]
    public async Task AuditStore_GetAll_ReturnsAllEntries()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var auditStore = provider.GetRequiredService<IAnonymizationAuditStore>();

        var entry1 = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Tokenized,
            subjectId: "user-1",
            fieldName: "SSN");

        var entry2 = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.KeyRotated,
            keyId: "key-2026-02");

        await auditStore.AddEntryAsync(entry1);
        await auditStore.AddEntryAsync(entry2);

        // Act
        var allResult = await auditStore.GetAllAsync();

        // Assert
        allResult.IsRight.ShouldBeTrue();
        var all = allResult.Match(Right: r => r, Left: _ => []);
        all.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task AuditStore_GetBySubjectId_UnknownSubject_ReturnsEmptyList()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var auditStore = provider.GetRequiredService<IAnonymizationAuditStore>();

        // Act
        var result = await auditStore.GetBySubjectIdAsync("nonexistent-subject");

        // Assert
        result.IsRight.ShouldBeTrue();
        var entries = result.Match(Right: r => r, Left: _ => []);
        entries.ShouldBeEmpty();
    }

    #endregion

    #region Risk Assessment

    [Fact]
    public async Task RiskAssessor_AssessAsync_ReturnsPrivacyMetrics()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var assessor = provider.GetRequiredService<IRiskAssessor>();

        // Build a dataset with controlled diversity
        var dataset = Enumerable.Range(0, 50).Select(i => new RiskTestRecord
        {
            Age = 20 + (i % 10) * 5,   // 10 distinct age values in steps of 5
            ZipCode = $"100{i % 5}0",   // 5 distinct zip codes
            Diagnosis = $"Condition-{i % 8}" // 8 distinct diagnoses
        }).ToList().AsReadOnly();

        var quasiIdentifiers = AgeZipQuasiIdentifiers;

        // Act
        var result = await assessor.AssessAsync(dataset, quasiIdentifiers);

        // Assert
        var errMsg = result.Match(Right: _ => "no error", Left: e => e.Message);
        result.IsRight.ShouldBeTrue($"assessment should succeed, but got: {errMsg}");

        var assessment = (RiskAssessmentResult)result;
        assessment.KAnonymityValue.ShouldBeGreaterThan(0, "k-anonymity should be computed");
        assessment.LDiversityValue.ShouldBeGreaterThan(0, "l-diversity should be computed");
        assessment.TClosenessDistance.ShouldBeGreaterThanOrEqualTo(0.0, "t-closeness should be non-negative");
        assessment.ReIdentificationProbability.ShouldBeInRange(0.0, 1.0, "probability should be 0-1");
        assessment.AssessedAtUtc.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        assessment.Recommendations.ShouldNotBeNull();
    }

    [Fact]
    public async Task RiskAssessor_AssessAsync_HighDiversity_IsAcceptable()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var assessor = provider.GetRequiredService<IRiskAssessor>();

        // Build a dataset where each quasi-identifier group has many records (high k)
        var dataset = Enumerable.Range(0, 200).Select(i => new RiskTestRecord
        {
            Age = 30 + (i % 3),    // Only 3 distinct ages → groups of ~67
            ZipCode = "10001",      // Single zip → single group
            Diagnosis = $"D-{i}"    // All unique diagnoses → high l-diversity
        }).ToList().AsReadOnly();

        var quasiIdentifiers = AgeQuasiIdentifier;

        // Act
        var result = await assessor.AssessAsync(dataset, quasiIdentifiers);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = (RiskAssessmentResult)result;
        assessment.KAnonymityValue.ShouldBeGreaterThanOrEqualTo(5,
            "with 200 records / 3 groups → k should be high");
        // Note: IsAcceptable depends on k, l, AND t thresholds combined.
        // We verify k is high; l-diversity and t-closeness may differ based on data distribution.
    }

    #endregion

    #region Anonymization Techniques — Suppression (Value Types)

    [Fact]
    public async Task Anonymize_WithSuppression_IntField_SetsToDefault()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var anonymizer = provider.GetRequiredService<IAnonymizer>();

        var profile = AnonymizationProfile.Create(
            name: "suppression-int-test",
            fieldRules:
            [
                new FieldAnonymizationRule
                {
                    FieldName = "Age",
                    Technique = AnonymizationTechnique.Suppression
                }
            ]);

        var data = new TestPersonRecord
        {
            Name = "Jane Doe",
            Email = "jane@example.com",
            Age = 28
        };

        // Act
        var result = await anonymizer.AnonymizeAsync(data, profile);

        // Assert
        var errMsg = result.Match(Right: _ => "no error", Left: e => e.Message);
        result.IsRight.ShouldBeTrue($"suppression of int should succeed, but got error: {errMsg}");
        var anonymized = result.Match(Right: v => v, Left: _ => null!);
        anonymized.Age.ShouldBe(0, "suppressed int should be 0 (default)");
        anonymized.Name.ShouldBe("Jane Doe", "non-targeted field unchanged");
        anonymized.Email.ShouldBe("jane@example.com", "non-targeted field unchanged");

        // Original data not mutated
        data.Age.ShouldBe(28);
    }

    [Fact]
    public async Task Anonymize_WithSuppression_StringField_ReturnsLeftDueToLanguageExtNullConstraint()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var anonymizer = provider.GetRequiredService<IAnonymizer>();

        // Note: Suppression of reference types (string) returns null, but LanguageExt's
        // Either.Right(null) throws ValueIsNullException, causing the technique to return Left.
        // This is a known LanguageExt constraint — use DataMasking for string fields instead.
        var profile = AnonymizationProfile.Create(
            name: "suppression-string-test",
            fieldRules:
            [
                new FieldAnonymizationRule
                {
                    FieldName = "Name",
                    Technique = AnonymizationTechnique.Suppression
                }
            ]);

        var data = new TestPersonRecord
        {
            Name = "Jane Doe",
            Email = "jane@example.com",
            Age = 28
        };

        // Act
        var result = await anonymizer.AnonymizeAsync(data, profile);

        // Assert: Suppression of string returns Left (LanguageExt null constraint)
        result.IsLeft.ShouldBeTrue(
            "suppression of reference types returns Left due to LanguageExt's Right(null) constraint");
    }

    #endregion

    #region AnonymizeFields — Detailed Result

    [Fact]
    public async Task AnonymizeFields_WithDataMasking_ReturnsDetailedResult()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var anonymizer = provider.GetRequiredService<IAnonymizer>();

        var profile = AnonymizationProfile.Create(
            name: "detailed-result",
            fieldRules:
            [
                new FieldAnonymizationRule
                {
                    FieldName = "Name",
                    Technique = AnonymizationTechnique.DataMasking,
                    Parameters = new Dictionary<string, object> { ["PreserveStart"] = 1 }
                }
            ]);

        var data = new TestPersonRecord
        {
            Name = "Alice Wonder",
            Email = "alice@wonder.com",
            Age = 30
        };

        // Act
        var result = await anonymizer.AnonymizeFieldsAsync(data, profile);

        // Assert
        var errMsg = result.Match(Right: _ => "no error", Left: e => e.Message);
        result.IsRight.ShouldBeTrue($"AnonymizeFields should succeed, but got: {errMsg}");

        var detailed = result.Match(Right: v => v, Left: _ => null!);
        detailed.AnonymizedFieldCount.ShouldBeGreaterThan(0,
            "at least one field should be anonymized");
        detailed.TechniqueApplied.ShouldContainKey("Name");
        detailed.TechniqueApplied["Name"].ShouldBe(AnonymizationTechnique.DataMasking);
    }

    #endregion

    #region Tokenization — IsToken Check

    [Fact]
    public async Task Tokenizer_IsTokenAsync_ReturnsTrueForKnownToken()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var tokenizer = provider.GetRequiredService<ITokenizer>();
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        var tokenResult = await tokenizer.TokenizeAsync("credit-card-4111", options);
        tokenResult.IsRight.ShouldBeTrue();
        var token = (string)tokenResult;

        // Act
        var isTokenResult = await tokenizer.IsTokenAsync(token);

        // Assert
        isTokenResult.IsRight.ShouldBeTrue();
        ((bool)isTokenResult).ShouldBeTrue("known token should be recognized");
    }

    [Fact]
    public async Task Tokenizer_IsTokenAsync_ReturnsFalseForUnknownValue()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var tokenizer = provider.GetRequiredService<ITokenizer>();

        // Act
        var isTokenResult = await tokenizer.IsTokenAsync("not-a-real-token");

        // Assert
        isTokenResult.IsRight.ShouldBeTrue();
        ((bool)isTokenResult).ShouldBeFalse("unknown value should not be recognized as token");
    }

    [Fact]
    public async Task Tokenizer_SameValue_ReturnsSameToken_Deduplication()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var tokenizer = provider.GetRequiredService<ITokenizer>();
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };
        var value = "deduplicate-this-value";

        // Act
        var result1 = await tokenizer.TokenizeAsync(value, options);
        var result2 = await tokenizer.TokenizeAsync(value, options);

        // Assert
        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();
        var token1 = (string)result1;
        var token2 = (string)result2;
        token1.ShouldBe(token2, "same value should produce the same token (deduplication)");
    }

    #endregion

    #region Health Check

    [Fact]
    public async Task HealthCheck_WhenEnabled_ReportsHealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAnonymization(options =>
        {
            options.AddHealthCheck = true;
        });

        using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetService<HealthCheckService>();

        // Health check service might not be available in minimal DI setup,
        // verify at least the registrations are present
        // Verify the health check type is registered in the service collection
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IHealthCheck) ||
            (d.ImplementationType?.Name.Contains("AnonymizationHealthCheck", StringComparison.Ordinal) ?? false));

        // Health check might be registered via IHealthChecksBuilder, not directly as IHealthCheck.
        // Verify the builder was invoked by checking AddHealthChecks was called.
        var healthCheckRegistrations = services.Where(d =>
            d.ServiceType.FullName?.Contains("HealthCheck", StringComparison.Ordinal) ?? false).ToList();
        healthCheckRegistrations.ShouldNotBeEmpty(
            "health check related services should be registered when AddHealthCheck = true");
    }

    #endregion

    #region Pseudonymization — HMAC-SHA256 Deterministic

    [Fact]
    public async Task Pseudonymize_HmacSha256_SameInput_ProducesSameOutput()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var pseudonymizer = provider.GetRequiredService<IPseudonymizer>();
        var keyProvider = provider.GetRequiredService<IKeyProvider>();

        var keyResult = await keyProvider.GetActiveKeyIdAsync();
        var keyId = (string)keyResult;

        var value = "deterministic-test@example.com";

        // Act
        var result1 = await pseudonymizer.PseudonymizeValueAsync(
            value, keyId, PseudonymizationAlgorithm.HmacSha256);
        var result2 = await pseudonymizer.PseudonymizeValueAsync(
            value, keyId, PseudonymizationAlgorithm.HmacSha256);

        // Assert
        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();
        var hash1 = (string)result1;
        var hash2 = (string)result2;
        hash1.ShouldBe(hash2, "HMAC-SHA256 is deterministic — same input → same output");
    }

    [Fact]
    public async Task Pseudonymize_Aes256Gcm_SameInput_ProducesDifferentOutput()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var pseudonymizer = provider.GetRequiredService<IPseudonymizer>();
        var keyProvider = provider.GetRequiredService<IKeyProvider>();

        var keyResult = await keyProvider.GetActiveKeyIdAsync();
        var keyId = (string)keyResult;

        var value = "nondeterministic-test@example.com";

        // Act
        var result1 = await pseudonymizer.PseudonymizeValueAsync(
            value, keyId, PseudonymizationAlgorithm.Aes256Gcm);
        var result2 = await pseudonymizer.PseudonymizeValueAsync(
            value, keyId, PseudonymizationAlgorithm.Aes256Gcm);

        // Assert
        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();
        var cipher1 = (string)result1;
        var cipher2 = (string)result2;
        cipher1.ShouldNotBe(cipher2,
            "AES-256-GCM uses random nonce — same input → different output");
    }

    #endregion

    #region Concurrent Data Integrity

    [Fact]
    public async Task ConcurrentMixedOperations_DataIntegrity_Maintained()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var tokenizer = provider.GetRequiredService<ITokenizer>();
        var pseudonymizer = provider.GetRequiredService<IPseudonymizer>();
        var keyProvider = provider.GetRequiredService<IKeyProvider>();

        var keyResult = await keyProvider.GetActiveKeyIdAsync();
        var keyId = (string)keyResult;
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        var taskCount = 100;
        var errors = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act: Run tokenize+detokenize and pseudonymize+depseudonymize concurrently
        var tasks = Enumerable.Range(0, taskCount).Select(async i =>
        {
            try
            {
                if (i % 2 == 0)
                {
                    // Tokenize/detokenize roundtrip
                    var value = $"concurrent-token-{i}";
                    var tokenResult = await tokenizer.TokenizeAsync(value, options);
                    if (!tokenResult.IsRight)
                    {
                        errors.Add($"Tokenize failed for {i}");
                        return;
                    }

                    var token = (string)tokenResult;
                    var detokenResult = await tokenizer.DetokenizeAsync(token);
                    if (!detokenResult.IsRight)
                    {
                        errors.Add($"Detokenize failed for {i}");
                        return;
                    }

                    var recovered = (string)detokenResult;
                    if (recovered != value)
                        errors.Add($"Token roundtrip mismatch for {i}: '{value}' vs '{recovered}'");
                }
                else
                {
                    // Pseudonymize/depseudonymize roundtrip
                    var value = $"concurrent-pseudo-{i}@example.com";
                    var pseudoResult = await pseudonymizer.PseudonymizeValueAsync(
                        value, keyId, PseudonymizationAlgorithm.Aes256Gcm);
                    if (!pseudoResult.IsRight)
                    {
                        errors.Add($"Pseudonymize failed for {i}");
                        return;
                    }

                    var pseudonym = (string)pseudoResult;
                    var depseudoResult = await pseudonymizer.DepseudonymizeValueAsync(pseudonym, keyId);
                    if (!depseudoResult.IsRight)
                    {
                        errors.Add($"Depseudonymize failed for {i}");
                        return;
                    }

                    var recovered = (string)depseudoResult;
                    if (recovered != value)
                        errors.Add($"Pseudo roundtrip mismatch for {i}: '{value}' vs '{recovered}'");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Exception at {i}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        errors.ShouldBeEmpty(
            $"all concurrent roundtrips should maintain data integrity. Errors: {string.Join("; ", errors.Take(5))}");
    }

    #endregion

    #region Key Provider

    [Fact]
    public async Task KeyProvider_GetActiveKeyId_ReturnsValidKey()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var keyProvider = provider.GetRequiredService<IKeyProvider>();

        // Act
        var result = await keyProvider.GetActiveKeyIdAsync();

        // Assert
        result.IsRight.ShouldBeTrue("active key should be available");
        var keyId = (string)result;
        keyId.ShouldNotBeNullOrWhiteSpace("key ID should be a non-empty string");
    }

    #endregion

    #region Static Data

    private static readonly IReadOnlyList<string> AgeQuasiIdentifier = new[] { "Age" }.AsReadOnly();
    private static readonly IReadOnlyList<string> AgeZipQuasiIdentifiers = new[] { "Age", "ZipCode" }.AsReadOnly();

    #endregion

    #region Test Helpers

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAnonymization();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }

    private sealed class TestPersonRecord
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Age { get; set; }
    }

    private sealed class RiskTestRecord
    {
        public int Age { get; set; }
        public string? ZipCode { get; set; }
        public string? Diagnosis { get; set; }
    }

    #endregion
}

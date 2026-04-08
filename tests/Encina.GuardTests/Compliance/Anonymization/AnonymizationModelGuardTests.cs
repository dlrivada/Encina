using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for anonymization model types, exercising Create() factories and record constructors
/// to cover executable lines.
/// </summary>
public class AnonymizationModelGuardTests
{
    #region AnonymizationAuditEntry

    [Fact]
    public void AnonymizationAuditEntry_Create_ReturnsValidEntry()
    {
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Anonymized,
            technique: AnonymizationTechnique.Suppression,
            fieldName: "Email",
            subjectId: "subject-1",
            keyId: "key-1",
            performedByUserId: "admin");

        entry.ShouldNotBeNull();
        entry.Id.ShouldNotBeNullOrWhiteSpace();
        entry.Operation.ShouldBe(AnonymizationOperation.Anonymized);
        entry.Technique.ShouldBe(AnonymizationTechnique.Suppression);
        entry.FieldName.ShouldBe("Email");
        entry.SubjectId.ShouldBe("subject-1");
        entry.KeyId.ShouldBe("key-1");
        entry.PerformedByUserId.ShouldBe("admin");
        entry.PerformedAtUtc.ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }

    [Fact]
    public void AnonymizationAuditEntry_Create_WithMinimalParameters_ReturnsEntry()
    {
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.KeyRotated);

        entry.ShouldNotBeNull();
        entry.SubjectId.ShouldBeNull();
        entry.Technique.ShouldBeNull();
        entry.FieldName.ShouldBeNull();
        entry.KeyId.ShouldBeNull();
        entry.PerformedByUserId.ShouldBeNull();
    }

    #endregion

    #region AnonymizationResult

    [Fact]
    public void AnonymizationResult_RequiredProperties_CanBeSet()
    {
        var techniques = new Dictionary<string, AnonymizationTechnique>
        {
            ["Name"] = AnonymizationTechnique.Suppression,
            ["Age"] = AnonymizationTechnique.Generalization
        };

        var result = new AnonymizationResult
        {
            OriginalFieldCount = 5,
            AnonymizedFieldCount = 2,
            SkippedFieldCount = 3,
            TechniqueApplied = techniques
        };

        result.OriginalFieldCount.ShouldBe(5);
        result.AnonymizedFieldCount.ShouldBe(2);
        result.SkippedFieldCount.ShouldBe(3);
        result.TechniqueApplied.Count.ShouldBe(2);
    }

    #endregion

    #region RiskAssessmentResult

    [Fact]
    public void RiskAssessmentResult_RequiredProperties_CanBeSet()
    {
        var result = new RiskAssessmentResult
        {
            KAnonymityValue = 5,
            LDiversityValue = 3,
            TClosenessDistance = 0.12,
            ReIdentificationProbability = 0.2,
            IsAcceptable = true,
            AssessedAtUtc = DateTimeOffset.UtcNow,
            Recommendations = []
        };

        result.KAnonymityValue.ShouldBe(5);
        result.LDiversityValue.ShouldBe(3);
        result.TClosenessDistance.ShouldBe(0.12);
        result.ReIdentificationProbability.ShouldBe(0.2);
        result.IsAcceptable.ShouldBeTrue();
        result.Recommendations.ShouldBeEmpty();
    }

    [Fact]
    public void RiskAssessmentResult_WithRecommendations_CanBeSet()
    {
        var result = new RiskAssessmentResult
        {
            KAnonymityValue = 2,
            LDiversityValue = 1,
            TClosenessDistance = 0.5,
            ReIdentificationProbability = 0.5,
            IsAcceptable = false,
            AssessedAtUtc = DateTimeOffset.UtcNow,
            Recommendations = ["Increase generalization granularity"]
        };

        result.IsAcceptable.ShouldBeFalse();
        result.Recommendations.Count.ShouldBe(1);
    }

    #endregion

    #region AnonymizationProfile

    [Fact]
    public void AnonymizationProfile_Create_ReturnsValidProfile()
    {
        var rules = new List<FieldAnonymizationRule>
        {
            new()
            {
                FieldName = "Name",
                Technique = AnonymizationTechnique.Suppression
            }
        };

        var profile = AnonymizationProfile.Create(
            name: "test-profile",
            fieldRules: rules,
            description: "A test profile");

        profile.ShouldNotBeNull();
        profile.Id.ShouldNotBeNullOrWhiteSpace();
        profile.Name.ShouldBe("test-profile");
        profile.Description.ShouldBe("A test profile");
        profile.FieldRules.Count.ShouldBe(1);
        profile.CreatedAtUtc.ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }

    [Fact]
    public void AnonymizationProfile_Create_WithoutDescription_ReturnsProfile()
    {
        var profile = AnonymizationProfile.Create(
            name: "minimal",
            fieldRules: []);

        profile.Description.ShouldBeNull();
        profile.FieldRules.ShouldBeEmpty();
    }

    #endregion

    #region FieldAnonymizationRule

    [Fact]
    public void FieldAnonymizationRule_RequiredProperties_CanBeSet()
    {
        var rule = new FieldAnonymizationRule
        {
            FieldName = "Age",
            Technique = AnonymizationTechnique.Generalization,
            Parameters = new Dictionary<string, object> { ["Granularity"] = 10 }
        };

        rule.FieldName.ShouldBe("Age");
        rule.Technique.ShouldBe(AnonymizationTechnique.Generalization);
        rule.Parameters.ShouldNotBeNull();
        rule.Parameters!.Count.ShouldBe(1);
    }

    [Fact]
    public void FieldAnonymizationRule_NullParameters_IsAllowed()
    {
        var rule = new FieldAnonymizationRule
        {
            FieldName = "Name",
            Technique = AnonymizationTechnique.Suppression
        };

        rule.Parameters.ShouldBeNull();
    }

    #endregion

    #region KeyInfo

    [Fact]
    public void KeyInfo_RequiredProperties_CanBeSet()
    {
        var keyInfo = new KeyInfo
        {
            KeyId = "key-2025-01",
            Algorithm = PseudonymizationAlgorithm.Aes256Gcm,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsActive = true,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(90)
        };

        keyInfo.KeyId.ShouldBe("key-2025-01");
        keyInfo.Algorithm.ShouldBe(PseudonymizationAlgorithm.Aes256Gcm);
        keyInfo.IsActive.ShouldBeTrue();
        keyInfo.ExpiresAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void KeyInfo_NullExpiration_IsAllowed()
    {
        var keyInfo = new KeyInfo
        {
            KeyId = "key-no-expiry",
            Algorithm = PseudonymizationAlgorithm.HmacSha256,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsActive = false
        };

        keyInfo.ExpiresAtUtc.ShouldBeNull();
        keyInfo.IsActive.ShouldBeFalse();
    }

    #endregion

    #region TokenMapping

    [Fact]
    public void TokenMapping_Create_ReturnsValidMapping()
    {
        var encryptedValue = new byte[] { 0x01, 0x02, 0x03 };

        var mapping = TokenMapping.Create(
            token: "tok_abc123",
            originalValueHash: "hash-value",
            encryptedOriginalValue: encryptedValue,
            keyId: "key-1",
            expiresAtUtc: DateTimeOffset.UtcNow.AddDays(30));

        mapping.ShouldNotBeNull();
        mapping.Id.ShouldNotBeNullOrWhiteSpace();
        mapping.Token.ShouldBe("tok_abc123");
        mapping.OriginalValueHash.ShouldBe("hash-value");
        mapping.EncryptedOriginalValue.ShouldBe(encryptedValue);
        mapping.KeyId.ShouldBe("key-1");
        mapping.CreatedAtUtc.ShouldBeGreaterThan(DateTimeOffset.MinValue);
        mapping.ExpiresAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void TokenMapping_Create_WithoutExpiration_ReturnsMapping()
    {
        var mapping = TokenMapping.Create(
            token: "tok_xyz",
            originalValueHash: "hash",
            encryptedOriginalValue: [0xFF],
            keyId: "key-2");

        mapping.ExpiresAtUtc.ShouldBeNull();
    }

    #endregion

    #region AnonymizationOperation enum

    [Fact]
    public void AnonymizationOperation_AllValues_AreDefined()
    {
        Enum.IsDefined(AnonymizationOperation.Anonymized).ShouldBeTrue();
        Enum.IsDefined(AnonymizationOperation.Pseudonymized).ShouldBeTrue();
        Enum.IsDefined(AnonymizationOperation.Depseudonymized).ShouldBeTrue();
        Enum.IsDefined(AnonymizationOperation.Tokenized).ShouldBeTrue();
        Enum.IsDefined(AnonymizationOperation.Detokenized).ShouldBeTrue();
        Enum.IsDefined(AnonymizationOperation.KeyRotated).ShouldBeTrue();
        Enum.IsDefined(AnonymizationOperation.RiskAssessed).ShouldBeTrue();
    }

    #endregion
}

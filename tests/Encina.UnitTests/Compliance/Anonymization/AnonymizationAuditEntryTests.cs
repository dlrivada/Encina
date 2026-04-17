using Encina.Compliance.Anonymization.Model;
using Shouldly;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="AnonymizationAuditEntry"/> factory method and record behavior.
/// </summary>
public class AnonymizationAuditEntryTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_ShouldGenerateNonEmptyId()
    {
        // Act
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Anonymized);

        // Assert
        entry.Id.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Create_ShouldSetOperation()
    {
        // Act
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Anonymized);

        // Assert
        entry.Operation.ShouldBe(AnonymizationOperation.Anonymized);
    }

    [Fact]
    public void Create_WithTechnique_ShouldSetTechnique()
    {
        // Act
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Anonymized,
            technique: AnonymizationTechnique.DataMasking);

        // Assert
        entry.Technique.ShouldBe(AnonymizationTechnique.DataMasking);
    }

    [Fact]
    public void Create_WithoutTechnique_ShouldBeNull()
    {
        // Act
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.KeyRotated);

        // Assert
        entry.Technique.ShouldBeNull();
    }

    [Fact]
    public void Create_WithFieldName_ShouldSetFieldName()
    {
        // Act
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Anonymized,
            technique: AnonymizationTechnique.Suppression,
            fieldName: "Email");

        // Assert
        entry.FieldName.ShouldBe("Email");
    }

    [Fact]
    public void Create_WithSubjectId_ShouldSetSubjectId()
    {
        // Act
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Pseudonymized,
            subjectId: "user-123");

        // Assert
        entry.SubjectId.ShouldBe("user-123");
    }

    [Fact]
    public void Create_WithKeyId_ShouldSetKeyId()
    {
        // Act
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Pseudonymized,
            keyId: "key-2025-01");

        // Assert
        entry.KeyId.ShouldBe("key-2025-01");
    }

    [Fact]
    public void Create_ShouldSetPerformedAtUtcToNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Tokenized);

        var after = DateTimeOffset.UtcNow;

        // Assert
        entry.PerformedAtUtc.ShouldBeGreaterThanOrEqualTo(before);
        entry.PerformedAtUtc.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_WithPerformedByUserId_ShouldSetPerformedByUserId()
    {
        // Act
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Depseudonymized,
            performedByUserId: "admin@company.com");

        // Assert
        entry.PerformedByUserId.ShouldBe("admin@company.com");
    }

    [Fact]
    public void Create_WithoutPerformedByUserId_ShouldBeNull()
    {
        // Act
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Anonymized);

        // Assert
        entry.PerformedByUserId.ShouldBeNull();
    }

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllProperties()
    {
        // Act
        var entry = AnonymizationAuditEntry.Create(
            operation: AnonymizationOperation.Pseudonymized,
            technique: AnonymizationTechnique.DataMasking,
            fieldName: "Email",
            subjectId: "user-123",
            keyId: "key-2025-01",
            performedByUserId: "admin@company.com");

        // Assert
        entry.Id.ShouldNotBeNullOrEmpty();
        entry.Operation.ShouldBe(AnonymizationOperation.Pseudonymized);
        entry.Technique.ShouldBe(AnonymizationTechnique.DataMasking);
        entry.FieldName.ShouldBe("Email");
        entry.SubjectId.ShouldBe("user-123");
        entry.KeyId.ShouldBe("key-2025-01");
        entry.PerformedByUserId.ShouldBe("admin@company.com");
    }

    #endregion

    #region Operation Coverage Tests

    [Theory]
    [InlineData(AnonymizationOperation.Anonymized)]
    [InlineData(AnonymizationOperation.Pseudonymized)]
    [InlineData(AnonymizationOperation.Depseudonymized)]
    [InlineData(AnonymizationOperation.Tokenized)]
    [InlineData(AnonymizationOperation.Detokenized)]
    [InlineData(AnonymizationOperation.KeyRotated)]
    [InlineData(AnonymizationOperation.RiskAssessed)]
    public void Create_WithEachOperation_ShouldSetOperationCorrectly(AnonymizationOperation operation)
    {
        // Act
        var entry = AnonymizationAuditEntry.Create(operation: operation);

        // Assert
        entry.Operation.ShouldBe(operation);
    }

    #endregion
}

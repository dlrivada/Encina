using Encina.Marten.GDPR;

using Shouldly;

namespace Encina.GuardTests.Marten.GDPR;

/// <summary>
/// Guard clause tests for the crypto-shredding exceptions and <see cref="CryptoShreddedPropertyIssue"/> (#1646, #1698).
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Marten")]
public sealed class CryptoShreddingEncryptionExceptionGuardTests
{
    [Fact]
    public void EncryptionException_NullDeclaringType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new CryptoShreddingEncryptionException(typeof(Sample), null!, "Email", CryptoShreddingEncryptionFailureReason.SubjectIdMissing));
        ex.ParamName.ShouldBe("declaringType");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EncryptionException_MissingPropertyName_ThrowsArgumentException(string? propertyName)
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new CryptoShreddingEncryptionException(typeof(Sample), typeof(Sample), propertyName!, CryptoShreddingEncryptionFailureReason.KeyUnavailable));
        ex.ParamName.ShouldBe("propertyName");
    }

    [Theory]
    [InlineData(CryptoShreddingEncryptionFailureReason.SubjectIdMissing, null, "subject id is missing")]
    [InlineData(CryptoShreddingEncryptionFailureReason.SubjectIdInvalid, null, "not of a supported subject-id type")]
    [InlineData(CryptoShreddingEncryptionFailureReason.KeyUnavailable, "crypto.key_store_error", "crypto.key_store_error")]
    [InlineData(CryptoShreddingEncryptionFailureReason.KeyUnavailable, null, "'unknown'")]
    public void EncryptionException_ValidArguments_ExposesTheFailureAndExplainsIt(
        CryptoShreddingEncryptionFailureReason reason, string? errorCode, string expectedText)
    {
        var ex = new CryptoShreddingEncryptionException(typeof(Root), typeof(Sample), "Email", reason, errorCode);

        ex.DocumentTypeName.ShouldBe(typeof(Root).FullName);
        ex.DeclaringTypeName.ShouldBe(typeof(Sample).FullName);
        ex.PropertyName.ShouldBe("Email");
        ex.Reason.ShouldBe(reason);
        ex.ErrorCode.ShouldBe(errorCode);
        ex.InnerException.ShouldBeNull();
        ex.Message.ShouldContain(expectedText);
        ex.Message.ShouldContain(typeof(Sample).FullName!);
    }

    [Fact]
    public void EncryptionException_UnknownDocumentType_IsNamedUnknown() =>
        new CryptoShreddingEncryptionException(null, typeof(Sample), "Email", CryptoShreddingEncryptionFailureReason.SubjectIdMissing)
            .DocumentTypeName.ShouldBe("unknown");

    [Fact]
    public void DecryptionException_NullDeclaringType_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            new CryptoShreddingDecryptionException(typeof(Root), null!, "Email", CryptoShreddingDecryptionFailureReason.EnvelopeMalformed))
            .ParamName.ShouldBe("declaringType");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DecryptionException_MissingPropertyName_ThrowsArgumentException(string? propertyName) =>
        Should.Throw<ArgumentException>(() =>
            new CryptoShreddingDecryptionException(typeof(Root), typeof(Sample), propertyName!, CryptoShreddingDecryptionFailureReason.KeyUnavailable))
            .ParamName.ShouldBe("propertyName");

    [Fact]
    public void DecryptionException_ValidArguments_ExposesTheFailure()
    {
        var ex = new CryptoShreddingDecryptionException(
            null, typeof(Sample), "Email", CryptoShreddingDecryptionFailureReason.IntegrityCheckFailed, "crypto.integrity_check_failed");

        ex.DocumentTypeName.ShouldBe("unknown");
        ex.DeclaringTypeName.ShouldBe(typeof(Sample).FullName);
        ex.PropertyName.ShouldBe("Email");
        ex.Reason.ShouldBe(CryptoShreddingDecryptionFailureReason.IntegrityCheckFailed);
        ex.ErrorCode.ShouldBe("crypto.integrity_check_failed");
        ex.InnerException.ShouldBeNull();
        ex.Message.ShouldContain("IntegrityCheckFailed");
    }

    [Fact]
    public void ConfigurationException_NullIssues_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            new CryptoShreddingConfigurationException(CryptoShreddingConfigurationProblem.MisconfiguredProperties, null!))
            .ParamName.ShouldBe("issues");

    [Fact]
    public void ConfigurationException_ListsEveryIssueAndTheComponent()
    {
        var issues = new[]
        {
            new CryptoShreddedPropertyIssue("A", "X", CryptoShreddedPropertyProblems.NoSetter),
            new CryptoShreddedPropertyIssue("B", "Y", CryptoShreddedPropertyProblems.NotString),
        };

        var ex = new CryptoShreddingConfigurationException(CryptoShreddingConfigurationProblem.ContractModifierMissing, issues, "Component");

        ex.Problem.ShouldBe(CryptoShreddingConfigurationProblem.ContractModifierMissing);
        ex.Issues.ShouldBe(issues);
        ex.ComponentType.ShouldBe("Component");
        ex.Message.ShouldContain("A.X");
        ex.Message.ShouldContain("B.Y");
        ex.Message.ShouldContain("(Component)");
    }

    [Fact]
    public void PropertyIssue_RecordEqualityAndDescribe()
    {
        var issue = new CryptoShreddedPropertyIssue("T", "P", CryptoShreddedPropertyProblems.MissingPersonalData);

        issue.ShouldBe(new CryptoShreddedPropertyIssue("T", "P", CryptoShreddedPropertyProblems.MissingPersonalData));
        issue.Describe().ShouldContain("[PersonalData]");
    }

    private sealed class Sample;

    private sealed class Root;
}

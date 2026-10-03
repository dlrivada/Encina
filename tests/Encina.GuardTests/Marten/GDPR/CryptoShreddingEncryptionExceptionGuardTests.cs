using Encina.Marten.GDPR;

using Shouldly;

namespace Encina.GuardTests.Marten.GDPR;

/// <summary>
/// Guard clause tests for <see cref="CryptoShreddingEncryptionException"/> (#1646).
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Marten")]
public sealed class CryptoShreddingEncryptionExceptionGuardTests
{
    [Fact]
    public void Constructor_NullEventType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new CryptoShreddingEncryptionException(null!, "Email", CryptoShreddingEncryptionFailureReason.SubjectIdMissing));
        ex.ParamName.ShouldBe("eventType");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_MissingPropertyName_ThrowsArgumentException(string? propertyName)
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new CryptoShreddingEncryptionException(typeof(string), propertyName!, CryptoShreddingEncryptionFailureReason.KeyUnavailable));
        ex.ParamName.ShouldBe("propertyName");
    }

    [Theory]
    [InlineData(CryptoShreddingEncryptionFailureReason.SubjectIdMissing, null, "subject id is missing")]
    [InlineData(CryptoShreddingEncryptionFailureReason.KeyUnavailable, "crypto.key_store_error", "crypto.key_store_error")]
    [InlineData(CryptoShreddingEncryptionFailureReason.KeyUnavailable, null, "'unknown'")]
    [InlineData(CryptoShreddingEncryptionFailureReason.PropertyMisconfigured, null, "misconfigured")]
    public void Constructor_ValidArguments_ExposesTheFailureAndExplainsIt(
        CryptoShreddingEncryptionFailureReason reason, string? errorCode, string expectedText)
    {
        var ex = new CryptoShreddingEncryptionException(typeof(Sample), "Email", reason, errorCode);

        ex.EventTypeName.ShouldBe(typeof(Sample).FullName);
        ex.PropertyName.ShouldBe("Email");
        ex.Reason.ShouldBe(reason);
        ex.ErrorCode.ShouldBe(errorCode);
        ex.InnerException.ShouldBeNull();
        ex.Message.ShouldContain(expectedText);
        ex.Message.ShouldContain(typeof(Sample).FullName!);
    }

    private sealed class Sample;
}

using System.Collections.Frozen;

using Encina;
using Encina.Compliance.Attestation.Model;
using Encina.Compliance.Attestation.Providers;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Compliance.Attestation;

[Trait("Category", "Unit")]
[Trait("Feature", "Attestation")]
public sealed class InMemoryAttestationProviderTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly InMemoryAttestationProvider _sut;

    public InMemoryAttestationProviderTests()
    {
        _sut = new InMemoryAttestationProvider(
            _timeProvider,
            NullLogger<InMemoryAttestationProvider>.Instance);
    }

    [Fact]
    public void ProviderName_ShouldBeInMemory()
    {
        _sut.ProviderName.ShouldBe("InMemory");
    }

    [Fact]
    public async Task AttestAsync_ValidRecord_ReturnsRightWithReceipt()
    {
        // Arrange
        var record = CreateRecord();

        // Act
        var result = await _sut.AttestAsync(record);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(receipt =>
        {
            receipt.AuditRecordId.ShouldBe(record.RecordId);
            receipt.ProviderName.ShouldBe("InMemory");
            receipt.ContentHash.ShouldNotBeNullOrWhiteSpace();
            receipt.Signature.ShouldNotBeNullOrWhiteSpace();
            receipt.AttestedAtUtc.ShouldBe(_timeProvider.GetUtcNow());
        });
    }

    [Fact]
    public async Task AttestAsync_SameRecordTwice_ReturnsIdempotentReceipt()
    {
        // Arrange
        var record = CreateRecord();

        // Act
        var first = await _sut.AttestAsync(record);
        var second = await _sut.AttestAsync(record);

        // Assert
        first.IsRight.ShouldBeTrue();
        second.IsRight.ShouldBeTrue();

        var firstReceipt = GetRight(first);
        var secondReceipt = GetRight(second);

        firstReceipt.AttestationId.ShouldBe(secondReceipt.AttestationId);
        firstReceipt.Signature.ShouldBe(secondReceipt.Signature);
    }

    [Fact]
    public async Task AttestAsync_DifferentRecords_ProduceDifferentReceipts()
    {
        // Arrange
        var record1 = CreateRecord();
        var record2 = CreateRecord();

        // Act
        var result1 = await _sut.AttestAsync(record1);
        var result2 = await _sut.AttestAsync(record2);

        // Assert
        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();
        var receipt1 = GetRight(result1);
        var receipt2 = GetRight(result2);

        receipt1.AttestationId.ShouldNotBe(receipt2.AttestationId);
    }

    [Fact]
    public async Task VerifyAsync_ValidReceipt_ReturnsValid()
    {
        // Arrange
        var record = CreateRecord();
        var attestResult = await _sut.AttestAsync(record);
        attestResult.IsRight.ShouldBeTrue();
        var receipt = GetRight(attestResult);

        // Act
        var verifyResult = await _sut.VerifyAsync(receipt);

        // Assert
        verifyResult.IsRight.ShouldBeTrue();
        verifyResult.IfRight(v =>
        {
            v.IsValid.ShouldBeTrue();
            v.FailureReason.ShouldBeNull();
        });
    }

    [Fact]
    public async Task VerifyAsync_UnknownReceipt_ReturnsInvalid()
    {
        // Arrange
        var fakeReceipt = new AttestationReceipt
        {
            AttestationId = Guid.NewGuid(),
            AuditRecordId = Guid.NewGuid(),
            ContentHash = "fake",
            AttestedAtUtc = _timeProvider.GetUtcNow(),
            ProviderName = "InMemory",
            Signature = "fake-sig"
        };

        // Act
        var result = await _sut.VerifyAsync(fakeReceipt);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(v =>
        {
            v.IsValid.ShouldBeFalse();
            v.FailureReason.ShouldNotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public async Task VerifyAsync_TamperedContentHash_ReturnsInvalid()
    {
        // Arrange
        var record = CreateRecord();
        var attestResult = await _sut.AttestAsync(record);
        attestResult.IsRight.ShouldBeTrue();
        var originalReceipt = GetRight(attestResult);

        var tamperedReceipt = originalReceipt with { ContentHash = "tampered-hash" };

        // Act
        var verifyResult = await _sut.VerifyAsync(tamperedReceipt);

        // Assert
        verifyResult.IsRight.ShouldBeTrue();
        verifyResult.IfRight(v =>
        {
            v.IsValid.ShouldBeFalse();
            v.FailureReason.ShouldContain("forgery");
        });
    }

    [Fact]
    public async Task AttestAsync_ReceiptContainsProofMetadata()
    {
        // Arrange
        var record = CreateRecord();

        // Act
        var result = await _sut.AttestAsync(record);

        // Assert
        result.IsRight.ShouldBeTrue();
        var receipt = GetRight(result);
        receipt.ProofMetadata.ShouldNotBeNull();
        receipt.ProofMetadata.ShouldContainKey("storage");
        receipt.ProofMetadata!["storage"].ShouldBe("in-memory");
    }

    [Fact]
    public async Task VerifyAsync_ForgedReceipt_ValidAuditRecordId_TamperedAttestedAtUtc_ReturnsInvalid()
    {
        var record = CreateRecord();
        var attestResult = await _sut.AttestAsync(record);
        attestResult.IsRight.ShouldBeTrue();
        var receipt = GetRight(attestResult);

        var forged = receipt with { AttestedAtUtc = receipt.AttestedAtUtc.AddHours(1) };

        var verifyResult = await _sut.VerifyAsync(forged);

        verifyResult.IsRight.ShouldBeTrue();
        verifyResult.IfRight(v =>
        {
            v.IsValid.ShouldBeFalse();
            v.FailureReason.ShouldContain("forgery");
        });
    }

    [Fact]
    public async Task VerifyAsync_ForgedReceipt_ValidAuditRecordId_TamperedProviderName_ReturnsInvalid()
    {
        var record = CreateRecord();
        var attestResult = await _sut.AttestAsync(record);
        attestResult.IsRight.ShouldBeTrue();
        var receipt = GetRight(attestResult);

        var forged = receipt with { ProviderName = "Forged" };

        var verifyResult = await _sut.VerifyAsync(forged);

        verifyResult.IsRight.ShouldBeTrue();
        verifyResult.IfRight(v =>
        {
            v.IsValid.ShouldBeFalse();
            v.FailureReason.ShouldContain("forgery");
        });
    }

    [Fact]
    public async Task VerifyAsync_ForgedReceipt_ValidAuditRecordId_TamperedProofMetadata_ReturnsInvalid()
    {
        var record = CreateRecord();
        var attestResult = await _sut.AttestAsync(record);
        attestResult.IsRight.ShouldBeTrue();
        var receipt = GetRight(attestResult);

        var forged = receipt with
        {
            ProofMetadata = new Dictionary<string, string>
            {
                ["storage"] = "tampered"
            }.ToFrozenDictionary()
        };

        var verifyResult = await _sut.VerifyAsync(forged);

        verifyResult.IsRight.ShouldBeTrue();
        verifyResult.IfRight(v =>
        {
            v.IsValid.ShouldBeFalse();
            v.FailureReason.ShouldContain("forgery");
        });
    }

    private AuditRecord CreateRecord() => new()
    {
        RecordId = Guid.NewGuid(),
        RecordType = "TestDecision",
        OccurredAtUtc = _timeProvider.GetUtcNow(),
        SerializedContent = $"{{\"test\":\"{Guid.NewGuid()}\"}}"
    };

    private static T GetRight<T>(Either<EncinaError, T> either) =>
        either.Match(r => r, e => throw new InvalidOperationException($"Expected Right but got Left: {e}"));
}

using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Model;
using Encina.Compliance.Attestation.Providers;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Compliance.Attestation;

[Trait("Category", "Unit")]
[Trait("Feature", "Attestation")]
public sealed class HashChainAttestationProviderTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly HashChainAttestationProvider _sut;

    public HashChainAttestationProviderTests()
    {
        _sut = new HashChainAttestationProvider(
            _timeProvider,
            NullLogger<HashChainAttestationProvider>.Instance,
            Options.Create(new HashChainOptions()));
    }

    [Fact]
    public void ProviderName_ShouldBeHashChain()
    {
        _sut.ProviderName.ShouldBe("HashChain");
    }

    [Fact]
    public async Task AttestAsync_ValidRecord_ReturnsRightWithReceipt()
    {
        var record = CreateRecord();

        var result = await _sut.AttestAsync(record);

        result.IsRight.ShouldBeTrue();
        result.IfRight(receipt =>
        {
            receipt.AuditRecordId.ShouldBe(record.RecordId);
            receipt.ProviderName.ShouldBe("HashChain");
            receipt.ProofMetadata.ShouldNotBeNull();
            receipt.ProofMetadata!.ShouldContainKey("chain_index");
            receipt.ProofMetadata["chain_index"].ShouldBe("0");
            receipt.ProofMetadata.ShouldContainKey("previous_signature");
            receipt.ProofMetadata["previous_signature"].ShouldBe("genesis");
        });
    }

    [Fact]
    public async Task AttestAsync_MultipleRecords_ChainIndexIncrements()
    {
        var records = Enumerable.Range(0, 5).Select(_ => CreateRecord()).ToList();

        for (var i = 0; i < records.Count; i++)
        {
            var result = await _sut.AttestAsync(records[i]);
            result.IsRight.ShouldBeTrue();

            var receipt = result.Match(r => r, _ => throw new InvalidOperationException());
            receipt.ProofMetadata!["chain_index"].ShouldBe(i.ToString());
        }
    }

    [Fact]
    public async Task AttestAsync_SameRecordTwice_ReturnsIdempotentReceipt()
    {
        var record = CreateRecord();

        var first = await _sut.AttestAsync(record);
        var second = await _sut.AttestAsync(record);

        var r1 = first.Match(r => r, _ => throw new InvalidOperationException());
        var r2 = second.Match(r => r, _ => throw new InvalidOperationException());

        r1.AttestationId.ShouldBe(r2.AttestationId);
    }

    [Fact]
    public async Task VerifyAsync_ValidReceipt_ReturnsValid()
    {
        var record = CreateRecord();
        var attestResult = await _sut.AttestAsync(record);
        var receipt = attestResult.Match(r => r, _ => throw new InvalidOperationException());

        var verifyResult = await _sut.VerifyAsync(receipt);

        verifyResult.IsRight.ShouldBeTrue();
        verifyResult.IfRight(v => v.IsValid.ShouldBeTrue());
    }

    [Fact]
    public async Task VerifyAsync_TamperedSignature_ReturnsInvalid()
    {
        var record = CreateRecord();
        var attestResult = await _sut.AttestAsync(record);
        var receipt = attestResult.Match(r => r, _ => throw new InvalidOperationException());

        var tampered = receipt with { Signature = "tampered" };

        var verifyResult = await _sut.VerifyAsync(tampered);

        verifyResult.IsRight.ShouldBeTrue();
        verifyResult.IfRight(v =>
        {
            v.IsValid.ShouldBeFalse();
            v.FailureReason.ShouldContain("Chain integrity");
        });
    }

    [Fact]
    public async Task VerifyAsync_UnknownReceipt_ReturnsInvalid()
    {
        var fakeReceipt = new AttestationReceipt
        {
            AttestationId = Guid.NewGuid(),
            AuditRecordId = Guid.NewGuid(),
            ContentHash = "fake",
            AttestedAtUtc = _timeProvider.GetUtcNow(),
            ProviderName = "HashChain",
            Signature = "fake"
        };

        var result = await _sut.VerifyAsync(fakeReceipt);

        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.IsValid.ShouldBeFalse());
    }

    [Fact]
    public void VerifyChainIntegrity_EmptyChain_ReturnsTrue()
    {
        _sut.VerifyChainIntegrity().ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyChainIntegrity_AfterMultipleAttestations_ReturnsTrue()
    {
        for (var i = 0; i < 10; i++)
        {
            await _sut.AttestAsync(CreateRecord());
        }

        _sut.VerifyChainIntegrity().ShouldBeTrue();
    }

    [Fact]
    public async Task AttestAsync_ChainLinksSignatures_EachEntryDependsOnPrevious()
    {
        var r1 = (await _sut.AttestAsync(CreateRecord())).Match(r => r, _ => throw new InvalidOperationException());
        var r2 = (await _sut.AttestAsync(CreateRecord())).Match(r => r, _ => throw new InvalidOperationException());

        // Second entry's previous_signature should be first entry's signature
        r2.ProofMetadata!["previous_signature"].ShouldBe(r1.Signature);
    }

    private static AuditRecord CreateRecord() => new()
    {
        RecordId = Guid.NewGuid(),
        RecordType = "TestDecision",
        OccurredAtUtc = DateTimeOffset.UtcNow,
        SerializedContent = $"{{\"test\":\"{Guid.NewGuid()}\"}}"
    };
}

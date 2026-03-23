using System.Collections.Frozen;

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
            v.FailureReason.ShouldContain("forgery");
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
    public async Task AttestAsync_ChainLinksSignatures_BothEntriesVerifyAndChainIsIntact()
    {
        var r1 = (await _sut.AttestAsync(CreateRecord())).Match(r => r, _ => throw new InvalidOperationException());
        var r2 = (await _sut.AttestAsync(CreateRecord())).Match(r => r, _ => throw new InvalidOperationException());

        // Both receipts must individually verify (their HMAC chains are correctly linked)
        var v1 = await _sut.VerifyAsync(r1);
        var v2 = await _sut.VerifyAsync(r2);
        v1.IsRight.ShouldBeTrue();
        v1.IfRight(v => v.IsValid.ShouldBeTrue());
        v2.IsRight.ShouldBeTrue();
        v2.IfRight(v => v.IsValid.ShouldBeTrue());

        // Full chain integrity check must pass
        _sut.VerifyChainIntegrity().ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyAsync_ForgedReceipt_ValidAuditRecordId_TamperedAttestedAtUtc_ReturnsInvalid()
    {
        var record = CreateRecord();
        var attestResult = await _sut.AttestAsync(record);
        var receipt = attestResult.Match(r => r, _ => throw new InvalidOperationException());

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
        var receipt = attestResult.Match(r => r, _ => throw new InvalidOperationException());

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
        var receipt = attestResult.Match(r => r, _ => throw new InvalidOperationException());

        var forged = receipt with
        {
            ProofMetadata = new Dictionary<string, string>
            {
                ["chain_index"] = "999",
                ["hash_algorithm"] = "HMAC-SHA256"
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

    [Fact]
    public async Task AttestAsync_WithSha512Algorithm_UsesCorrectHashAlgorithm()
    {
        var provider = new HashChainAttestationProvider(
            _timeProvider,
            NullLogger<HashChainAttestationProvider>.Instance,
            Options.Create(new HashChainOptions { HashAlgorithm = HashAlgorithmName.SHA512 }));

        var record = CreateRecord();
        var result = await provider.AttestAsync(record);

        result.IsRight.ShouldBeTrue();
        result.IfRight(receipt =>
        {
            receipt.ProofMetadata.ShouldNotBeNull();
            receipt.ProofMetadata!["hash_algorithm"].ShouldBe("HMAC-SHA512");
            // SHA-512 produces 128-hex-char hashes (64 bytes)
            receipt.ContentHash.Length.ShouldBe(128);
            // HMAC-SHA512 produces 128-hex-char signatures
            receipt.Signature.Length.ShouldBe(128);
        });
    }

    [Fact]
    public async Task AttestAsync_WithSha384Algorithm_UsesCorrectHashAlgorithm()
    {
        var provider = new HashChainAttestationProvider(
            _timeProvider,
            NullLogger<HashChainAttestationProvider>.Instance,
            Options.Create(new HashChainOptions { HashAlgorithm = HashAlgorithmName.SHA384 }));

        var record = CreateRecord();
        var result = await provider.AttestAsync(record);

        result.IsRight.ShouldBeTrue();
        result.IfRight(receipt =>
        {
            receipt.ProofMetadata.ShouldNotBeNull();
            receipt.ProofMetadata!["hash_algorithm"].ShouldBe("HMAC-SHA384");
            // SHA-384 produces 96-hex-char hashes (48 bytes)
            receipt.ContentHash.Length.ShouldBe(96);
            // HMAC-SHA384 produces 96-hex-char signatures
            receipt.Signature.Length.ShouldBe(96);
        });
    }

    [Fact]
    public async Task AttestAsync_DefaultOptions_UsesSha256()
    {
        var record = CreateRecord();
        var result = await _sut.AttestAsync(record);

        result.IsRight.ShouldBeTrue();
        result.IfRight(receipt =>
        {
            receipt.ProofMetadata!["hash_algorithm"].ShouldBe("HMAC-SHA256");
            // SHA-256 produces 64-hex-char hashes (32 bytes)
            receipt.ContentHash.Length.ShouldBe(64);
        });
    }

    [Fact]
    public async Task VerifyChainIntegrity_WithSha512_ReturnsTrue()
    {
        var provider = new HashChainAttestationProvider(
            _timeProvider,
            NullLogger<HashChainAttestationProvider>.Instance,
            Options.Create(new HashChainOptions { HashAlgorithm = HashAlgorithmName.SHA512 }));

        for (var i = 0; i < 5; i++)
            await provider.AttestAsync(CreateRecord());

        provider.VerifyChainIntegrity().ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyAsync_WithSha512_ValidReceipt_ReturnsValid()
    {
        var provider = new HashChainAttestationProvider(
            _timeProvider,
            NullLogger<HashChainAttestationProvider>.Instance,
            Options.Create(new HashChainOptions { HashAlgorithm = HashAlgorithmName.SHA512 }));

        var record = CreateRecord();
        var attestResult = await provider.AttestAsync(record);
        var receipt = attestResult.Match(r => r, _ => throw new InvalidOperationException());

        var verifyResult = await provider.VerifyAsync(receipt);

        verifyResult.IsRight.ShouldBeTrue();
        verifyResult.IfRight(v => v.IsValid.ShouldBeTrue());
    }

    private static AuditRecord CreateRecord() => new()
    {
        RecordId = Guid.NewGuid(),
        RecordType = "TestDecision",
        OccurredAtUtc = DateTimeOffset.UtcNow,
        SerializedContent = $"{{\"test\":\"{Guid.NewGuid()}\"}}"
    };
}

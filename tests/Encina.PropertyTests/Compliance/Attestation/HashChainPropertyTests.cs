using System.Globalization;
using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Model;
using Encina.Compliance.Attestation.Providers;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Encina.PropertyTests.Compliance.Attestation;

/// <summary>
/// Property-based tests for hash chain attestation invariants:
/// append-only, no gaps, tamper detection, idempotency.
/// </summary>
public class HashChainPropertyTests
{
    private static HashChainAttestationProvider CreateProvider(TimeProvider? timeProvider = null)
    {
        return new HashChainAttestationProvider(
            timeProvider ?? TimeProvider.System,
            NullLogger<HashChainAttestationProvider>.Instance,
            Options.Create(new HashChainOptions()));
    }

    private static AuditRecord CreateRecord(Guid? id = null, string? content = null) => new()
    {
        RecordId = id ?? Guid.NewGuid(),
        RecordType = "TestEvent",
        OccurredAtUtc = DateTimeOffset.UtcNow,
        SerializedContent = content ?? $"{{\"data\":\"{Guid.NewGuid()}\"}}"
    };

    #region Append-Only Chain Invariants

    /// <summary>
    /// Invariant: attesting N records always produces N distinct receipts with sequential chain indices.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property AppendOnly_NRecords_ProduceNDistinctReceipts()
    {
        return Prop.ForAll(
            Gen.Choose(1, 20).ToArbitrary(),
            count =>
            {
                var provider = CreateProvider();
                var receipts = new List<AttestationReceipt>();

                for (var i = 0; i < count; i++)
                {
                    var result = provider.AttestAsync(CreateRecord()).AsTask().Result;
                    result.Match(
                        Right: r => receipts.Add(r),
                        Left: e => throw new InvalidOperationException($"AttestAsync failed: {e}"));
                }

                // All receipts have distinct AttestationIds
                var distinctIds = receipts.Select(r => r.AttestationId).Distinct().Count();
                distinctIds.ShouldBe(count);

                // Chain indices are sequential (0..N-1)
                var indices = receipts
                    .Select(r => int.Parse(r.ProofMetadata!["chain_index"], CultureInfo.InvariantCulture))
                    .ToList();
                indices.ShouldBe(Enumerable.Range(0, count).ToList());
            });
    }

    /// <summary>
    /// Invariant: after attesting N records the full chain integrity check passes.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property ChainIntegrity_AlwaysHoldsAfterAppend()
    {
        return Prop.ForAll(
            Gen.Choose(1, 25).ToArbitrary(),
            count =>
            {
                var provider = CreateProvider();

                for (var i = 0; i < count; i++)
                {
                    provider.AttestAsync(CreateRecord()).AsTask().Result.IsRight.ShouldBeTrue();
                }

                provider.VerifyChainIntegrity().ShouldBeTrue();
            });
    }

    #endregion

    #region No Gaps Invariant

    /// <summary>
    /// Invariant: each receipt is individually verifiable, and the full chain passes integrity
    /// after N appends — proving the chain has no gaps (each entry's HMAC includes the previous
    /// entry's signature, verified by <see cref="HashChainAttestationProvider.VerifyChainIntegrity"/>).
    /// </summary>
    [Property(MaxTest = 20)]
    public Property NoGaps_ChainIntegrityHoldsAndAllReceiptsVerify()
    {
        return Prop.ForAll(
            Gen.Choose(2, 15).ToArbitrary(),
            count =>
            {
                var provider = CreateProvider();
                var receipts = new List<AttestationReceipt>();

                for (var i = 0; i < count; i++)
                {
                    var result = provider.AttestAsync(CreateRecord()).AsTask().Result;
                    result.Match(
                        Right: r => receipts.Add(r),
                        Left: e => throw new InvalidOperationException($"AttestAsync failed: {e}"));
                }

                // Full chain integrity check must pass (verifies all HMAC links from genesis)
                provider.VerifyChainIntegrity().ShouldBeTrue();

                // Each receipt must individually verify (its HMAC is correctly anchored in the chain)
                foreach (var receipt in receipts)
                {
                    var verifyResult = provider.VerifyAsync(receipt).AsTask().Result;
                    verifyResult.IsRight.ShouldBeTrue();
                    verifyResult.IfRight(v => v.IsValid.ShouldBeTrue());
                }
            });
    }

    #endregion

    #region Tamper Detection

    /// <summary>
    /// Invariant: verifying a receipt with a modified content hash always fails.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool TamperDetection_ModifiedContentHash_FailsVerification(NonEmptyString content)
    {
        var provider = CreateProvider();
        var record = CreateRecord(content: content.Get);

        var attestResult = provider.AttestAsync(record).AsTask().Result;
        if (!attestResult.IsRight) return false;

        var receipt = (AttestationReceipt)attestResult;

        // Tamper with the content hash
        var tampered = receipt with { ContentHash = "0000000000000000000000000000000000000000000000000000000000000000" };

        var verifyResult = provider.VerifyAsync(tampered).AsTask().Result;
        return verifyResult.Match(
            Right: v => !v.IsValid,
            Left: _ => false);
    }

    /// <summary>
    /// Invariant: verifying an untampered receipt always succeeds.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Verification_UntamperedReceipt_AlwaysSucceeds(NonEmptyString content)
    {
        var provider = CreateProvider();
        var record = CreateRecord(content: content.Get);

        var attestResult = provider.AttestAsync(record).AsTask().Result;
        if (!attestResult.IsRight) return false;

        var receipt = (AttestationReceipt)attestResult;
        var verifyResult = provider.VerifyAsync(receipt).AsTask().Result;

        return verifyResult.Match(
            Right: v => v.IsValid,
            Left: _ => false);
    }

    #endregion

    #region Idempotency

    /// <summary>
    /// Invariant: attesting the same RecordId multiple times returns the same receipt.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property Idempotency_SameRecordId_ReturnsSameReceipt()
    {
        return Prop.ForAll(
            Gen.Choose(2, 10).ToArbitrary(),
            repeatCount =>
            {
                var provider = CreateProvider();
                var recordId = Guid.NewGuid();
                var record = CreateRecord(id: recordId, content: "{\"stable\":true}");

                AttestationReceipt? first = null;

                for (var i = 0; i < repeatCount; i++)
                {
                    var result = provider.AttestAsync(record).AsTask().Result;
                    result.IsRight.ShouldBeTrue();
                    var receipt = (AttestationReceipt)result;

                    if (first is null)
                    {
                        first = receipt;
                    }
                    else
                    {
                        receipt.AttestationId.ShouldBe(first.AttestationId);
                        receipt.Signature.ShouldBe(first.Signature);
                        receipt.ContentHash.ShouldBe(first.ContentHash);
                    }
                }
            });
    }

    #endregion

    #region Determinism

    /// <summary>
    /// Invariant: same content always produces the same content hash.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ContentHash_IsDeterministic(NonEmptyString content)
    {
        var hash1 = AttestationHasher.ComputeSha256(content.Get);
        var hash2 = AttestationHasher.ComputeSha256(content.Get);
        return hash1 == hash2;
    }

    /// <summary>
    /// Invariant: different content produces different hashes (collision resistance).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ContentHash_DifferentInputs_DifferentHashes(NonEmptyString a, NonEmptyString b)
    {
        if (a.Get == b.Get) return true; // skip equal inputs

        var hashA = AttestationHasher.ComputeSha256(a.Get);
        var hashB = AttestationHasher.ComputeSha256(b.Get);
        return hashA != hashB;
    }

    #endregion
}

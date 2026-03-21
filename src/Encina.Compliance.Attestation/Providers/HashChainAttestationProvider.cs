using System.Collections.Concurrent;

using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Diagnostics;
using Encina.Compliance.Attestation.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Attestation.Providers;

/// <summary>
/// Hash chain attestation provider that creates a tamper-evident, append-only chain
/// of attestations. Each entry's signature incorporates the previous entry's hash,
/// ensuring that any retroactive modification breaks the chain.
/// </summary>
/// <remarks>
/// <para>
/// The chain uses SHA-256 to compute: <c>signature = SHA256(contentHash + ":" + previousSignature + ":" + chainIndex)</c>.
/// The genesis entry uses <c>"genesis"</c> as the previous signature.
/// </para>
/// <para>
/// This provider is self-hosted and has zero external dependencies.
/// For cloud-backed immutable ledgers, see future Azure/AWS providers.
/// </para>
/// </remarks>
public sealed class HashChainAttestationProvider : IAuditAttestationProvider
{
    private readonly ConcurrentDictionary<Guid, (AttestationReceipt Receipt, int ChainIndex)> _receipts = new();
    private readonly List<AttestationReceipt> _chain = [];
    private readonly Lock _chainLock = new();
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HashChainAttestationProvider> _logger;
    private readonly HashChainOptions _options;
    private string _previousSignature = "genesis";

    /// <inheritdoc />
    public string ProviderName => "HashChain";

    /// <summary>
    /// Initializes a new instance of the <see cref="HashChainAttestationProvider"/> class.
    /// </summary>
    public HashChainAttestationProvider(
        TimeProvider timeProvider,
        ILogger<HashChainAttestationProvider> logger,
        IOptions<HashChainOptions> options)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        _timeProvider = timeProvider;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, AttestationReceipt>> AttestAsync(
        AuditRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        // Idempotency: return existing receipt for same RecordId
        if (_receipts.TryGetValue(record.RecordId, out var existing))
        {
            AttestationLogMessages.IdempotentAttestationReturned(_logger, record.RecordId, ProviderName);
            return ValueTask.FromResult(Right<EncinaError, AttestationReceipt>(existing.Receipt));
        }

        var contentHash = ContentHasher.ComputeSha256(record.SerializedContent);
        var now = _timeProvider.GetUtcNow();

        lock (_chainLock)
        {
            // Double-check after acquiring lock
            if (_receipts.TryGetValue(record.RecordId, out existing))
            {
                return ValueTask.FromResult(Right<EncinaError, AttestationReceipt>(existing.Receipt));
            }

            var chainIndex = _chain.Count;
            var signature = ContentHasher.ComputeSha256($"{contentHash}:{_previousSignature}:{chainIndex}");

            var receipt = new AttestationReceipt
            {
                AttestationId = Guid.NewGuid(),
                AuditRecordId = record.RecordId,
                ContentHash = contentHash,
                AttestedAtUtc = now,
                ProviderName = ProviderName,
                Signature = signature,
                ProofMetadata = new Dictionary<string, string>
                {
                    ["chain_index"] = chainIndex.ToString(),
                    ["previous_signature"] = _previousSignature,
                    ["hash_algorithm"] = _options.HashAlgorithm.Name ?? "SHA256"
                }
            };

            _chain.Add(receipt);
            _receipts[record.RecordId] = (receipt, chainIndex);
            _previousSignature = signature;

            AttestationLogMessages.AttestationCreated(_logger, record.RecordId, ProviderName);
            return ValueTask.FromResult(Right<EncinaError, AttestationReceipt>(receipt));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, AttestationVerification>> VerifyAsync(
        AttestationReceipt receipt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        var now = _timeProvider.GetUtcNow();

        if (!_receipts.TryGetValue(receipt.AuditRecordId, out var stored))
        {
            return ValueTask.FromResult(Right<EncinaError, AttestationVerification>(
                new AttestationVerification
                {
                    IsValid = false,
                    VerifiedAtUtc = now,
                    FailureReason = "Receipt not found in hash chain."
                }));
        }

        // Verify the chain link: recompute signature from stored data
        lock (_chainLock)
        {
            var chainIndex = stored.ChainIndex;
            var previousSig = chainIndex == 0
                ? "genesis"
                : _chain[chainIndex - 1].Signature;

            var expectedSignature = ContentHasher.ComputeSha256(
                $"{receipt.ContentHash}:{previousSig}:{chainIndex}");

            var isValid = expectedSignature == receipt.Signature;

            AttestationLogMessages.VerificationCompleted(_logger, receipt.AuditRecordId, isValid, ProviderName);

            return ValueTask.FromResult(Right<EncinaError, AttestationVerification>(
                new AttestationVerification
                {
                    IsValid = isValid,
                    VerifiedAtUtc = now,
                    FailureReason = isValid
                        ? null
                        : "Chain integrity broken — signature does not match expected value for this chain position."
                }));
        }
    }

    /// <summary>
    /// Verifies the integrity of the entire hash chain from genesis to the latest entry.
    /// </summary>
    /// <returns>True if the chain is intact; false if any link is broken.</returns>
    public bool VerifyChainIntegrity()
    {
        lock (_chainLock)
        {
            var previousSig = "genesis";

            for (var i = 0; i < _chain.Count; i++)
            {
                var entry = _chain[i];
                var expected = ContentHasher.ComputeSha256(
                    $"{entry.ContentHash}:{previousSig}:{i}");

                if (expected != entry.Signature)
                {
                    AttestationLogMessages.ChainIntegrityBroken(_logger, i, _chain.Count);
                    return false;
                }

                previousSig = entry.Signature;
            }

            return true;
        }
    }
}

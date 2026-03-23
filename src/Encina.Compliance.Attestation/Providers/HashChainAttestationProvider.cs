using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

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
/// The chain uses an HMAC matching the configured <see cref="HashChainOptions.HashAlgorithm"/> (default SHA-256) to compute:
/// <c>signature = HMAC(key, contentHash + ":" + previousSignature + ":" + chainIndex)</c>.
/// The genesis entry uses <c>"genesis"</c> as the previous signature.
/// </para>
/// <para>
/// Provide a persistent <see cref="HashChainOptions.HmacKey"/> to enable chain
/// verification across process restarts. When no key is configured, a random key
/// is generated at startup (ephemeral — suitable for testing and single-process deployments).
/// </para>
/// <para>
/// This provider is self-hosted and has zero external dependencies.
/// For cloud-backed immutable ledgers, see future Azure/AWS providers.
/// </para>
/// </remarks>
public sealed class HashChainAttestationProvider : IAuditAttestationProvider, IAttestationReceiptStore
{
    private readonly ConcurrentDictionary<Guid, (AttestationReceipt Receipt, int ChainIndex)> _receipts = new();
    private readonly List<AttestationReceipt> _chain = [];
    private readonly Lock _chainLock = new();
    private readonly byte[] _hmacKey;
    private readonly HashAlgorithmName _hashAlgorithm;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HashChainAttestationProvider> _logger;
    // Reserved for future persistence implementation
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
        _hashAlgorithm = _options.HashAlgorithm;
        // Validate algorithm eagerly to fail fast on misconfiguration
        _ = ContentHasher.GetRecommendedKeySize(_hashAlgorithm);
        _hmacKey = _options.HmacKey ?? RandomNumberGenerator.GetBytes(
            ContentHasher.GetRecommendedKeySize(_hashAlgorithm));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, AttestationReceipt>> AttestAsync(
        AuditRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var activity = AttestationDiagnostics.StartAttestation(ProviderName, record.RecordType);
        try
        {
            var sw = Stopwatch.StartNew();
            AttestationDiagnostics.AttestationTotal.Add(1,
                new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });

            // Idempotency: return existing receipt for same RecordId
            if (_receipts.TryGetValue(record.RecordId, out var existing))
            {
                sw.Stop();
                AttestationDiagnostics.AttestationDuration.Record(sw.Elapsed.TotalMilliseconds,
                    new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
                AttestationLogMessages.IdempotentAttestationReturned(_logger, record.RecordId, ProviderName);
                AttestationDiagnostics.RecordSuccess(activity);
                return ValueTask.FromResult(Right<EncinaError, AttestationReceipt>(existing.Receipt));
            }

            var contentHash = ContentHasher.ComputeHash(record.SerializedContent, _hashAlgorithm);
            var now = _timeProvider.GetUtcNow();

            lock (_chainLock)
            {
                // Double-check after acquiring lock
                if (_receipts.TryGetValue(record.RecordId, out existing))
                {
                    sw.Stop();
                    AttestationDiagnostics.AttestationDuration.Record(sw.Elapsed.TotalMilliseconds,
                        new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
                    AttestationLogMessages.IdempotentAttestationReturned(_logger, record.RecordId, ProviderName);
                    AttestationDiagnostics.RecordSuccess(activity);
                    return ValueTask.FromResult(Right<EncinaError, AttestationReceipt>(existing.Receipt));
                }

                var chainIndex = _chain.Count;
                var signature = ComputeHmac(_hmacKey, $"{contentHash}:{_previousSignature}:{chainIndex}", _hashAlgorithm);

                var receipt = new AttestationReceipt
                {
                    AttestationId = Guid.NewGuid(),
                    AuditRecordId = record.RecordId,
                    ContentHash = contentHash,
                    AttestedAtUtc = now,
                    ProviderName = ProviderName,
                    Signature = signature,
                    CorrelationId = record.CorrelationId,
                    ProofMetadata = new Dictionary<string, string>
                    {
                        ["chain_index"] = chainIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["hash_algorithm"] = $"HMAC-{_hashAlgorithm.Name}"
                    }.ToFrozenDictionary()
                };

                _chain.Add(receipt);
                _receipts[record.RecordId] = (receipt, chainIndex);
                _previousSignature = signature;

                sw.Stop();
                AttestationDiagnostics.AttestationDuration.Record(sw.Elapsed.TotalMilliseconds,
                    new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
                AttestationDiagnostics.AttestationSucceeded.Add(1,
                    new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
                AttestationLogMessages.AttestationCreated(_logger, record.RecordId, ProviderName);
                AttestationDiagnostics.RecordSuccess(activity);
                return ValueTask.FromResult(Right<EncinaError, AttestationReceipt>(receipt));
            }
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, AttestationVerification>> VerifyAsync(
        AttestationReceipt receipt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        var activity = AttestationDiagnostics.StartVerification(ProviderName);
        try
        {
            AttestationDiagnostics.VerificationTotal.Add(1,
                new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });

            var now = _timeProvider.GetUtcNow();

            if (!_receipts.TryGetValue(receipt.AuditRecordId, out var stored))
            {
                AttestationDiagnostics.RecordFailure(activity, "Receipt not found");

                return ValueTask.FromResult(Right<EncinaError, AttestationVerification>(
                    new AttestationVerification
                    {
                        IsValid = false,
                        VerifiedAtUtc = now,
                        FailureReason = "Receipt not found in hash chain.",
                        AttestationId = receipt.AttestationId,
                        ProviderName = ProviderName
                    }));
            }

            // Verify the receipt matches the stored receipt (prevents forged receipts)
            lock (_chainLock)
            {
                var storedReceipt = stored.Receipt;
                var signaturesMatch = SignaturesEqual(receipt.Signature, storedReceipt.Signature);
                var receiptMatchesStored = receipt.AttestationId == storedReceipt.AttestationId
                    && signaturesMatch
                    && receipt.ContentHash == storedReceipt.ContentHash
                    && receipt.AttestedAtUtc == storedReceipt.AttestedAtUtc
                    && receipt.ProviderName == storedReceipt.ProviderName
                    && ProofMetadataEquals(receipt.ProofMetadata, storedReceipt.ProofMetadata);

                if (!receiptMatchesStored)
                {
                    const string reason = "Receipt does not match stored attestation — possible forgery.";
                    AttestationLogMessages.VerificationCompleted(_logger, receipt.AuditRecordId, false, ProviderName);
                    AttestationDiagnostics.RecordFailure(activity, reason);

                    return ValueTask.FromResult(Right<EncinaError, AttestationVerification>(
                        new AttestationVerification
                        {
                            IsValid = false,
                            VerifiedAtUtc = now,
                            FailureReason = reason,
                            AttestationId = storedReceipt.AttestationId,
                            ProviderName = ProviderName
                        }));
                }

                // Verify chain integrity: recompute HMAC from stored data
                var chainIndex = stored.ChainIndex;
                var previousSig = chainIndex == 0
                    ? "genesis"
                    : _chain[chainIndex - 1].Signature;

                var expectedSignature = ComputeHmac(_hmacKey,
                    $"{storedReceipt.ContentHash}:{previousSig}:{chainIndex}", _hashAlgorithm);

                var isValid = SignaturesEqual(expectedSignature, storedReceipt.Signature);

                AttestationLogMessages.VerificationCompleted(_logger, receipt.AuditRecordId, isValid, ProviderName);

                if (isValid)
                    AttestationDiagnostics.RecordSuccess(activity);
                else
                    AttestationDiagnostics.RecordFailure(activity, "Chain integrity broken");

                return ValueTask.FromResult(Right<EncinaError, AttestationVerification>(
                    new AttestationVerification
                    {
                        IsValid = isValid,
                        VerifiedAtUtc = now,
                        FailureReason = isValid
                            ? null
                            : "Chain integrity broken — signature does not match expected value for this chain position.",
                        AttestationId = storedReceipt.AttestationId,
                        ProviderName = ProviderName
                    }));
            }
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, AttestationReceipt>> GetReceiptAsync(
        Guid recordId, CancellationToken ct = default)
    {
        if (_receipts.TryGetValue(recordId, out var stored))
            return ValueTask.FromResult(Right<EncinaError, AttestationReceipt>(stored.Receipt));

        return ValueTask.FromResult(
            Left<EncinaError, AttestationReceipt>(AttestationErrors.ReceiptNotFound(recordId, ProviderName)));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<AttestationReceipt>>> GetReceiptsAsync(
        IEnumerable<Guid> recordIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(recordIds);

        var results = recordIds
            .Select(id => _receipts.TryGetValue(id, out var entry) ? entry.Receipt : (AttestationReceipt?)null)
            .Where(r => r is not null)
            .Select(r => r!)
            .ToList();

        return ValueTask.FromResult(
            Right<EncinaError, IReadOnlyList<AttestationReceipt>>(results));
    }

    /// <inheritdoc />
    ValueTask IAttestationReceiptStore.StoreReceiptAsync(AttestationReceipt receipt, CancellationToken ct)
    {
        // HashChain manages its own storage — external store not applicable
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask<AttestationReceipt?> IAttestationReceiptStore.GetReceiptAsync(Guid recordId, CancellationToken ct)
    {
        if (_receipts.TryGetValue(recordId, out var stored))
            return ValueTask.FromResult<AttestationReceipt?>(stored.Receipt);

        return ValueTask.FromResult<AttestationReceipt?>(null);
    }

    /// <inheritdoc />
    ValueTask<IReadOnlyList<AttestationReceipt>> IAttestationReceiptStore.GetReceiptsAsync(
        IEnumerable<Guid> recordIds, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(recordIds);

        IReadOnlyList<AttestationReceipt> results = recordIds
            .Select(id => _receipts.TryGetValue(id, out var entry) ? entry.Receipt : (AttestationReceipt?)null)
            .Where(r => r is not null)
            .Select(r => r!)
            .ToList();

        return ValueTask.FromResult(results);
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
                var expected = ComputeHmac(_hmacKey,
                    $"{entry.ContentHash}:{previousSig}:{i}", _hashAlgorithm);

                if (!SignaturesEqual(expected, entry.Signature))
                {
                    AttestationLogMessages.ChainIntegrityBroken(_logger, i, _chain.Count);
                    return false;
                }

                previousSig = entry.Signature;
            }

            return true;
        }
    }

    private static string ComputeHmac(byte[] key, string content, HashAlgorithmName algorithm)
    {
        using var hmac = ContentHasher.CreateHmac(key, algorithm);
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }

    private static bool SignaturesEqual(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }

    private static bool ProofMetadataEquals(
        FrozenDictionary<string, string>? left,
        FrozenDictionary<string, string>? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        if (left.Count != right.Count) return false;

        foreach (var kvp in left)
        {
            if (!right.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
                return false;
        }

        return true;
    }
}

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

using static LanguageExt.Prelude;

namespace Encina.Compliance.Attestation.Providers;

/// <summary>
/// In-memory attestation provider for testing and development.
/// Stores attestation receipts in a <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// with idempotent attestation (same <see cref="AuditRecord.RecordId"/> returns existing receipt).
/// </summary>
public sealed class InMemoryAttestationProvider : IAuditAttestationProvider, IAttestationReceiptStore
{
    private readonly ConcurrentDictionary<Guid, AttestationReceipt> _receipts = new();
    private readonly ConcurrentDictionary<Guid, AuditRecord> _records = new();
    private readonly byte[] _hmacKey;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryAttestationProvider> _logger;

    /// <inheritdoc />
    public string ProviderName => "InMemory";

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAttestationProvider"/> class.
    /// A cryptographically random HMAC key is generated at startup.
    /// </summary>
    public InMemoryAttestationProvider(TimeProvider timeProvider, ILogger<InMemoryAttestationProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _timeProvider = timeProvider;
        _logger = logger;
        _hmacKey = RandomNumberGenerator.GetBytes(32);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, AttestationReceipt>> AttestAsync(
        AuditRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var activity = AttestationDiagnostics.StartAttestation(ProviderName, record.RecordType);
        var sw = Stopwatch.StartNew();
        AttestationDiagnostics.AttestationTotal.Add(1,
            new(AttestationDiagnostics.TagProviderName, ProviderName));

        // Idempotent: return existing receipt if already attested
        if (_receipts.TryGetValue(record.RecordId, out var existingReceipt))
        {
            AttestationLogMessages.IdempotentAttestationReturned(_logger, record.RecordId, ProviderName);
            sw.Stop();
            AttestationDiagnostics.AttestationDuration.Record(sw.Elapsed.TotalMilliseconds,
                new(AttestationDiagnostics.TagProviderName, ProviderName));
            AttestationDiagnostics.RecordSuccess(activity);
            activity?.Dispose();
            return ValueTask.FromResult(Right<EncinaError, AttestationReceipt>(existingReceipt));
        }

        var contentHash = ContentHasher.ComputeSha256(record.SerializedContent);
        var now = _timeProvider.GetUtcNow();
        var signature = ComputeHmac(_hmacKey, $"{contentHash}:{now:O}:{ProviderName}");

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
                ["storage"] = "in-memory"
            }.ToFrozenDictionary()
        };

        // TryAdd is atomic — if another thread won the race, return its receipt
        if (_receipts.TryAdd(record.RecordId, receipt))
        {
            _records.TryAdd(record.RecordId, record);
            AttestationLogMessages.AttestationCreated(_logger, record.RecordId, ProviderName);
            AttestationDiagnostics.AttestationSucceeded.Add(1,
                new(AttestationDiagnostics.TagProviderName, ProviderName));
        }
        else
        {
            receipt = _receipts[record.RecordId];
            AttestationLogMessages.IdempotentAttestationReturned(_logger, record.RecordId, ProviderName);
        }

        sw.Stop();
        AttestationDiagnostics.AttestationDuration.Record(sw.Elapsed.TotalMilliseconds,
            new(AttestationDiagnostics.TagProviderName, ProviderName));
        AttestationDiagnostics.RecordSuccess(activity);
        activity?.Dispose();

        return ValueTask.FromResult(Right<EncinaError, AttestationReceipt>(receipt));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, AttestationVerification>> VerifyAsync(
        AttestationReceipt receipt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        var activity = AttestationDiagnostics.StartVerification(ProviderName);
        AttestationDiagnostics.VerificationTotal.Add(1,
            new(AttestationDiagnostics.TagProviderName, ProviderName));

        var now = _timeProvider.GetUtcNow();

        if (!_records.TryGetValue(receipt.AuditRecordId, out var originalRecord) ||
            !_receipts.TryGetValue(receipt.AuditRecordId, out var storedReceipt))
        {
            AttestationDiagnostics.RecordFailure(activity, "Record not found");
            activity?.Dispose();

            return ValueTask.FromResult(Right<EncinaError, AttestationVerification>(
                new AttestationVerification
                {
                    IsValid = false,
                    VerifiedAtUtc = now,
                    FailureReason = "Original audit record not found in this provider.",
                    AttestationId = receipt.AttestationId,
                    ProviderName = ProviderName
                }));
        }

        // Verify the receipt matches the stored receipt (prevents forged receipts)
        var signaturesMatch = SignaturesEqual(receipt.Signature, storedReceipt.Signature);
        var receiptMatchesStored = receipt.AttestationId == storedReceipt.AttestationId
            && signaturesMatch
            && receipt.ContentHash == storedReceipt.ContentHash
            && receipt.AttestedAtUtc == storedReceipt.AttestedAtUtc
            && receipt.ProviderName == storedReceipt.ProviderName
            && ProofMetadataEquals(receipt.ProofMetadata, storedReceipt.ProofMetadata);

        // Verify content integrity: recompute hash from stored record
        var currentHash = ContentHasher.ComputeSha256(originalRecord.SerializedContent);
        var contentIntact = currentHash == storedReceipt.ContentHash;

        var isValid = receiptMatchesStored && contentIntact;

        string? failureReason = null;
        if (!receiptMatchesStored)
            failureReason = "Receipt does not match stored attestation — possible forgery.";
        else if (!contentIntact)
            failureReason = "Content hash mismatch — audit record may have been tampered with.";

        AttestationLogMessages.VerificationCompleted(_logger, receipt.AuditRecordId, isValid, ProviderName);

        if (isValid)
            AttestationDiagnostics.RecordSuccess(activity);
        else
            AttestationDiagnostics.RecordFailure(activity, failureReason ?? "Verification failed");

        activity?.Dispose();

        return ValueTask.FromResult(Right<EncinaError, AttestationVerification>(
            new AttestationVerification
            {
                IsValid = isValid,
                VerifiedAtUtc = now,
                FailureReason = failureReason,
                AttestationId = storedReceipt.AttestationId,
                ProviderName = ProviderName
            }));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, AttestationReceipt>> GetReceiptAsync(
        Guid recordId, CancellationToken ct = default)
    {
        if (_receipts.TryGetValue(recordId, out var receipt))
            return ValueTask.FromResult(Right<EncinaError, AttestationReceipt>(receipt));

        return ValueTask.FromResult(
            Left<EncinaError, AttestationReceipt>(AttestationErrors.ReceiptNotFound(recordId, ProviderName)));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<AttestationReceipt>>> GetReceiptsAsync(
        IEnumerable<Guid> recordIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(recordIds);

        var results = recordIds
            .Where(id => _receipts.ContainsKey(id))
            .Select(id => _receipts[id])
            .ToList();

        return ValueTask.FromResult(
            Right<EncinaError, IReadOnlyList<AttestationReceipt>>(results));
    }

    /// <inheritdoc />
    ValueTask IAttestationReceiptStore.StoreReceiptAsync(AttestationReceipt receipt, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        _receipts.TryAdd(receipt.AuditRecordId, receipt);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask<AttestationReceipt?> IAttestationReceiptStore.GetReceiptAsync(Guid recordId, CancellationToken ct)
    {
        _receipts.TryGetValue(recordId, out var receipt);
        return ValueTask.FromResult(receipt);
    }

    /// <inheritdoc />
    ValueTask<IReadOnlyList<AttestationReceipt>> IAttestationReceiptStore.GetReceiptsAsync(
        IEnumerable<Guid> recordIds, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(recordIds);

        IReadOnlyList<AttestationReceipt> results = recordIds
            .Where(id => _receipts.ContainsKey(id))
            .Select(id => _receipts[id])
            .ToList();

        return ValueTask.FromResult(results);
    }

    private static string ComputeHmac(byte[] key, string content)
    {
        using var hmac = new HMACSHA256(key);
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
        IReadOnlyDictionary<string, string>? left,
        IReadOnlyDictionary<string, string>? right)
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

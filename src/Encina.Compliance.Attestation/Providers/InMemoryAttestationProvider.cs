using System.Collections.Concurrent;
using System.Diagnostics;

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
public sealed class InMemoryAttestationProvider : IAuditAttestationProvider
{
    private readonly ConcurrentDictionary<Guid, AttestationReceipt> _receipts = new();
    private readonly ConcurrentDictionary<Guid, AuditRecord> _records = new();
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryAttestationProvider> _logger;

    /// <inheritdoc />
    public string ProviderName => "InMemory";

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAttestationProvider"/> class.
    /// </summary>
    public InMemoryAttestationProvider(TimeProvider timeProvider, ILogger<InMemoryAttestationProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _timeProvider = timeProvider;
        _logger = logger;
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

        // Thread-safe idempotent attestation using GetOrAdd
        var isNew = false;
        var receipt = _receipts.GetOrAdd(record.RecordId, _ =>
        {
            isNew = true;
            var contentHash = ContentHasher.ComputeSha256(record.SerializedContent);
            var now = _timeProvider.GetUtcNow();

            return new AttestationReceipt
            {
                AttestationId = Guid.NewGuid(),
                AuditRecordId = record.RecordId,
                ContentHash = contentHash,
                AttestedAtUtc = now,
                ProviderName = ProviderName,
                Signature = ContentHasher.ComputeSha256($"{contentHash}:{now:O}:{ProviderName}"),
                ProofMetadata = new Dictionary<string, string>
                {
                    ["storage"] = "in-memory"
                }
            };
        });

        if (isNew)
        {
            _records.TryAdd(record.RecordId, record);
            AttestationLogMessages.AttestationCreated(_logger, record.RecordId, ProviderName);
            AttestationDiagnostics.AttestationSucceeded.Add(1,
                new(AttestationDiagnostics.TagProviderName, ProviderName));
        }
        else
        {
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

        if (!_records.TryGetValue(receipt.AuditRecordId, out var originalRecord))
        {
            AttestationDiagnostics.RecordFailure(activity, "Record not found");
            activity?.Dispose();

            return ValueTask.FromResult(Right<EncinaError, AttestationVerification>(
                new AttestationVerification
                {
                    IsValid = false,
                    VerifiedAtUtc = now,
                    FailureReason = "Original audit record not found in this provider."
                }));
        }

        var currentHash = ContentHasher.ComputeSha256(originalRecord.SerializedContent);
        var isValid = currentHash == receipt.ContentHash;

        AttestationLogMessages.VerificationCompleted(_logger, receipt.AuditRecordId, isValid, ProviderName);

        if (isValid)
            AttestationDiagnostics.RecordSuccess(activity);
        else
            AttestationDiagnostics.RecordFailure(activity, "Content hash mismatch");

        activity?.Dispose();

        return ValueTask.FromResult(Right<EncinaError, AttestationVerification>(
            new AttestationVerification
            {
                IsValid = isValid,
                VerifiedAtUtc = now,
                FailureReason = isValid ? null : "Content hash mismatch — audit record may have been tampered with."
            }));
    }
}

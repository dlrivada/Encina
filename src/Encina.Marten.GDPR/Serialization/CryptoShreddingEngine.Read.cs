using System.Security.Cryptography;

using Encina.Diagnostics;
using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.Marten.GDPR;

/// <summary>How one stored value was resolved on read.</summary>
internal enum DecryptOutcome
{
    /// <summary>The value was decrypted.</summary>
    Decrypted,

    /// <summary>The subject is forgotten; the placeholder was applied.</summary>
    Forgotten,

    /// <summary>The value is the tombstone of a confirmed forgotten subject; the placeholder was applied.</summary>
    Tombstone,
}

internal sealed partial class CryptoShreddingEngine
{
    /// <summary>
    /// Called by <c>JsonTypeInfo.OnDeserialized</c> for every constructed owner at any depth. Enqueues it into the
    /// read frame, or decrypts it at once in an implicit frame when the options are used outside the serializer.
    /// </summary>
    internal void OnOwnerDeserialized(object owner, CryptoShreddingTypePlan plan)
    {
        if (CryptoShreddingCallScope.CurrentRead is { } frame)
        {
            frame.Enqueue(owner, plan);
            return;
        }

        _logger.ImplicitCryptoScopeUsed(nameof(CryptoOperation.Decrypt), plan.Type.Name);
        using var implicitFrame = Begin(CryptoOperation.Decrypt, plan.Type);
        implicitFrame.Enqueue(owner, plan);
        DecryptPending(implicitFrame, owner);
    }

    /// <summary>Decrypts every pending owner of the frame (synchronous path, sync-over-async key access).</summary>
    internal void DecryptPending(CryptoShreddingFrame frame, object? root)
    {
        if (frame.Pending.Count == 0)
        {
            return;
        }

        frame.Root = root;
        ApplyPending(frame);
        foreach (var (subject, path) in frame.ForgottenToNotify)
        {
            NotifyForgotten(frame, subject, path, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Decrypts every pending owner of the frame (asynchronous path): keys and tombstone confirmations are fetched
    /// first with the token, then applied.
    /// </summary>
    internal async ValueTask DecryptPendingAsync(CryptoShreddingFrame frame, object? root, CancellationToken cancellationToken)
    {
        if (frame.Pending.Count == 0)
        {
            return;
        }

        frame.Root = root;
        await PrefetchAsync(frame, cancellationToken).ConfigureAwait(false);
        ApplyPending(frame);
        foreach (var (subject, path) in frame.ForgottenToNotify)
        {
            await NotifyForgotten(frame, subject, path, cancellationToken).ConfigureAwait(false);
        }
    }

    private void ApplyPending(CryptoShreddingFrame frame)
    {
        foreach (var (owner, plan) in frame.Pending)
        {
            foreach (var field in plan.Fields)
            {
                DecryptField(frame, owner, field);
            }
        }
    }

    private void DecryptField(CryptoShreddingFrame frame, object owner, CryptoShreddedField field)
    {
        if (field.Getter(owner) is not string stored || ReadSubject(frame, owner, field) is not { } subjectId)
        {
            return;
        }

        frame.EnsureActivity();
        var (plaintext, outcome) = CryptoShreddingToken.IsTombstone(stored)
            ? (ReadTombstone(frame, field, subjectId), DecryptOutcome.Tombstone)
            : ReadToken(frame, field, subjectId, stored);

        field.Setter(owner, plaintext);
        if (!ReferenceEquals(field.Getter(owner), plaintext))
        {
            throw ReadFailure(frame, field, CryptoShreddingDecryptionFailureReason.PropertyNotWritable, CryptoShreddingErrors.DecryptionFailedCode, null);
        }

        RecordOutcome(frame, owner, field, subjectId, outcome);
    }

    /// <summary>
    /// Resolves the subject of a field on read: checked before every other branch. Returns <c>null</c> when a
    /// subject filter excludes the field (it keeps its token).
    /// </summary>
    private string? ReadSubject(CryptoShreddingFrame frame, object owner, CryptoShreddedField field)
    {
        string? subjectId;
        try
        {
            subjectId = field.ResolveSubjectId(owner);
        }
        catch (InvalidOperationException)
        {
            return frame.SubjectFilter is null
                ? throw ReadFailure(frame, field, CryptoShreddingDecryptionFailureReason.SubjectIdInvalid, CryptoShreddingErrors.InvalidSubjectIdCode, null)
                : null;
        }

        if (frame.SubjectFilter is { } filter)
        {
            return string.Equals(subjectId, filter, StringComparison.Ordinal) ? subjectId : null;
        }

        if (subjectId is null)
        {
            _logger.SubjectIdMissing(nameof(CryptoOperation.Decrypt), field.DeclaringType.Name, field.Name, field.SubjectIdProperty.Name);
            throw ReadFailure(frame, field, CryptoShreddingDecryptionFailureReason.SubjectIdMissing, CryptoShreddingErrors.InvalidSubjectIdCode, null, log: false);
        }

        return subjectId;
    }

    private string ReadTombstone(CryptoShreddingFrame frame, CryptoShreddedField field, string subjectId)
    {
        var check = GetForgottenCheck(frame, subjectId);
        if (check.FailureCode is { } code)
        {
            throw ReadFailure(frame, field, CryptoShreddingDecryptionFailureReason.KeyUnavailable, code, null);
        }

        return check.Forgotten
            ? Placeholder
            : throw ReadFailure(frame, field, CryptoShreddingDecryptionFailureReason.IntegrityCheckFailed, CryptoShreddingErrors.IntegrityCheckFailedCode, null);
    }

    private (string Plaintext, DecryptOutcome Outcome) ReadToken(CryptoShreddingFrame frame, CryptoShreddedField field, string subjectId, string stored)
    {
        if (!CryptoShreddingToken.TryParse(stored, out var token))
        {
            throw ReadFailure(frame, field, CryptoShreddingDecryptionFailureReason.EnvelopeMalformed, CryptoShreddingErrors.EnvelopeMalformedCode, null);
        }

        var key = GetReadKey(frame, field, subjectId, token.Version);
        return key.Forgotten
            ? (Placeholder, DecryptOutcome.Forgotten)
            : (Decrypt(frame, field, key.Aes!, subjectId, token), DecryptOutcome.Decrypted);
    }

    private string Decrypt(CryptoShreddingFrame frame, CryptoShreddedField field, AesGcm aes, string subjectId, CryptoShreddingToken token)
    {
        try
        {
            return CryptoShreddingFieldCipher.Decrypt(aes, subjectId, token);
        }
        catch (AuthenticationTagMismatchException)
        {
            throw ReadFailure(frame, field, CryptoShreddingDecryptionFailureReason.IntegrityCheckFailed, CryptoShreddingErrors.IntegrityCheckFailedCode, null);
        }
        catch (CryptographicException ex)
        {
            throw ReadFailure(frame, field, CryptoShreddingDecryptionFailureReason.KeyUnavailable, CryptoShreddingErrors.DecryptionFailedCode, ex);
        }
    }

    private void RecordOutcome(CryptoShreddingFrame frame, object owner, CryptoShreddedField field, string subjectId, DecryptOutcome outcome)
    {
        if (outcome == DecryptOutcome.Decrypted)
        {
            CryptoShreddingDiagnostics.DecryptionTotal.Add(1);
            _logger.PiiFieldDecrypted(field.DeclaringType.Name, field.Name);
            return;
        }

        CryptoShreddingDiagnostics.ForgottenAccessTotal.Add(1);
        CryptoShreddingDiagnostics.RecordForgottenAccess(frame.Activity);
        _logger.ForgottenSubjectAccessed(field.DeclaringType.Name, field.Name);
        if (frame.NotifiedSubjects.Add(subjectId))
        {
            frame.ForgottenToNotify.Add((subjectId, FieldPathOf(frame, owner, field)));
        }
    }

    // The path of the field in the locator's FieldName format; the bare name when the root cannot be walked.
    private string FieldPathOf(CryptoShreddingFrame frame, object owner, CryptoShreddedField field) =>
        frame.Root is { } root && PathOptions is { } options
            ? FindPath(root, options, owner, field) ?? field.Name
            : field.Name;

    private string? FindPath(object root, System.Text.Json.JsonSerializerOptions options, object owner, CryptoShreddedField field) =>
        CryptoShreddedGraphWalker.Walk(root, options, Registry)
            .Where(occurrence => ReferenceEquals(occurrence.Owner, owner) && occurrence.Field.Name == field.Name)
            .Select(occurrence => occurrence.Path)
            .FirstOrDefault();

    private async ValueTask NotifyForgotten(CryptoShreddingFrame frame, string subjectId, string fieldPath, CancellationToken cancellationToken)
    {
        var documentType = frame.RootType ?? typeof(object);
        try
        {
            await frame.Services.GetRequiredService<IForgottenSubjectHandler>()
                .HandleForgottenSubjectAsync(subjectId, fieldPath, documentType, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A notification hook must not break reads; the subject id is never logged.
            _logger.ForgottenSubjectHandlerFailed(ex.ForLogging(), documentType.Name);
        }
    }

    // -- Key access --------------------------------------------------------------------------------------------

    private CryptoReadKey GetReadKey(CryptoShreddingFrame frame, CryptoShreddedField field, string subjectId, int version)
    {
        if (!frame.ReadKeys.TryGetValue((subjectId, version), out var key))
        {
            key = ToReadKey(frame, field, FetchReadKeySync(frame, subjectId, version));
            frame.ReadKeys[(subjectId, version)] = key;
        }

        return key;
    }

    private static Either<EncinaError, byte[]> FetchReadKeySync(CryptoShreddingFrame frame, string subjectId, int version)
    {
        try
        {
            return frame.Services.GetRequiredService<ISubjectKeyProvider>()
                .GetSubjectKeyAsync(subjectId, version).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            return CryptoShreddingErrors.KeyStoreError("GetSubjectKey", ex);
        }
    }

    private CryptoReadKey ToReadKey(CryptoShreddingFrame frame, CryptoShreddedField field, Either<EncinaError, byte[]> result) =>
        result.Match(
            Right: material => CryptoShreddingFieldCipher.CreateAes(material) is { } aes
                ? new CryptoReadKey(aes, Forgotten: false)
                : throw ReadFailure(frame, field, CryptoShreddingDecryptionFailureReason.KeyUnavailable, CryptoShreddingErrors.DecryptionFailedCode, null),
            Left: error => ToForgottenReadKey(frame, field, error));

    private CryptoReadKey ToForgottenReadKey(CryptoShreddingFrame frame, CryptoShreddedField field, EncinaError error)
    {
        var code = ErrorCode(error, CryptoShreddingErrors.KeyStoreErrorCode);
        return code == CryptoShreddingErrors.SubjectForgottenCode
            ? new CryptoReadKey(null, Forgotten: true)
            : throw ReadFailure(frame, field, CryptoShreddingDecryptionFailureReason.KeyUnavailable, code, error.Exception.MatchUnsafe(e => e, () => (Exception?)null));
    }

    private static CryptoForgottenCheck GetForgottenCheck(CryptoShreddingFrame frame, string subjectId)
    {
        if (!frame.ForgottenChecks.TryGetValue(subjectId, out var check))
        {
            check = ToForgottenCheck(FetchForgottenSync(frame, subjectId));
            frame.ForgottenChecks[subjectId] = check;
        }

        return check;
    }

    private static Either<EncinaError, bool> FetchForgottenSync(CryptoShreddingFrame frame, string subjectId)
    {
        try
        {
            return frame.Services.GetRequiredService<ISubjectKeyProvider>()
                .IsSubjectForgottenAsync(subjectId).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            return CryptoShreddingErrors.KeyStoreError("IsSubjectForgotten", ex);
        }
    }

    private static CryptoForgottenCheck ToForgottenCheck(Either<EncinaError, bool> result) =>
        result.Match(
            Right: forgotten => new CryptoForgottenCheck(forgotten, null),
            Left: error => new CryptoForgottenCheck(false, ErrorCode(error, CryptoShreddingErrors.KeyStoreErrorCode)));

    private CryptoShreddingDecryptionException ReadFailure(
        CryptoShreddingFrame frame,
        CryptoShreddedField field,
        CryptoShreddingDecryptionFailureReason reason,
        string errorCode,
        Exception? exception,
        bool log = true)
    {
        var reasonName = reason.ToString();
        CryptoShreddingDiagnostics.DecryptionFailedTotal.Add(1, CryptoShreddingDiagnostics.FailureReasonTag(reasonName));
        frame.RecordFailure(reasonName);
        if (log)
        {
            _logger.DecryptionFailed(field.DeclaringType.Name, field.Name, reasonName, errorCode, exception?.ForLogging());
        }

        return new CryptoShreddingDecryptionException(frame.RootType, field.DeclaringType, field.Name, reason, errorCode);
    }
}

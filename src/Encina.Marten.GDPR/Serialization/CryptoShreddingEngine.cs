using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;

using Encina.Diagnostics;
using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.Marten.GDPR;

/// <summary>
/// Encrypts <c>[CryptoShredded]</c> values as STJ writes them and decrypts deserialized owners, inside the frame
/// of the current serializer call.
/// </summary>
/// <remarks>
/// <para>
/// <b>Write</b>: the wrapped <c>JsonPropertyInfo.Get</c> calls <see cref="EncryptForWrite"/> with the declaring
/// object; the subject comes from that object's subject-id sibling and the token replaces the value in the JSON
/// only, never in the caller's object.
/// </para>
/// <para>
/// <b>Read</b>: <c>JsonTypeInfo.OnDeserialized</c> calls <see cref="OnOwnerDeserialized"/>, which enqueues the
/// owner; the serializer then calls <see cref="DecryptPending"/> or <see cref="DecryptPendingAsync"/>. Only a
/// genuinely forgotten subject (or a tombstone the provider confirms) reads as the placeholder; every other
/// failure throws <see cref="CryptoShreddingDecryptionException"/>.
/// </para>
/// </remarks>
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly",
    Justification = "Sync-over-async is intentional on the synchronous paths: Marten's ISerializer write members and "
                    + "FromJson are synchronous while ISubjectKeyProvider is async; Marten calls them without a "
                    + "SynchronizationContext.")]
internal sealed partial class CryptoShreddingEngine
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    internal CryptoShreddingEngine(IServiceScopeFactory scopeFactory, ILogger logger, string anonymizedPlaceholder)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        Placeholder = anonymizedPlaceholder;
    }

    /// <summary>The placeholder a forgotten subject's fields read as.</summary>
    internal string Placeholder { get; }

    /// <summary>The plans built by the modifier.</summary>
    internal CryptoShreddingTypePlanRegistry Registry { get; } = new();

    /// <summary>The options used to compute field paths for the forgotten-subject handler.</summary>
    internal JsonSerializerOptions? PathOptions { get; set; }

    /// <summary>Creates a frame for a serializer call and pushes it.</summary>
    internal CryptoShreddingFrame Begin(CryptoOperation operation, Type? rootType)
    {
        var filter = operation == CryptoOperation.Decrypt ? CryptoShreddingCallScope.CurrentSubjectFilter : null;
        var frame = new CryptoShreddingFrame(_scopeFactory, operation, rootType, filter);
        CryptoShreddingCallScope.Push(frame);
        return frame;
    }

    // -- Write ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// Returns the token that replaces <paramref name="value"/> in the JSON, or throws
    /// <see cref="CryptoShreddingEncryptionException"/>. A <c>null</c> value stays <c>null</c> with no key lookup.
    /// </summary>
    internal string? EncryptForWrite(object owner, CryptoShreddedField field, string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (CryptoShreddingCallScope.CurrentWrite is { } frame)
        {
            return Encrypt(frame, owner, field, value);
        }

        _logger.ImplicitCryptoScopeUsed(nameof(CryptoOperation.Encrypt), field.DeclaringType.Name);
        using var implicitFrame = Begin(CryptoOperation.Encrypt, rootType: null);
        return Encrypt(implicitFrame, owner, field, value);
    }

    private string Encrypt(CryptoShreddingFrame frame, object owner, CryptoShreddedField field, string value)
    {
        frame.EnsureActivity();
        var subjectId = ResolveWriteSubject(frame, owner, field);
        var key = GetWriteKey(frame, subjectId, field);
        if (key.Forgotten)
        {
            return WriteForgotten(frame, field, value);
        }

        var token = CryptoShreddingFieldCipher.Encrypt(key.Aes!, subjectId, key.Version, value);
        CryptoShreddingDiagnostics.EncryptionTotal.Add(1);
        _logger.PiiFieldEncrypted(field.DeclaringType.Name, field.Name);
        return token;
    }

    private string ResolveWriteSubject(CryptoShreddingFrame frame, object owner, CryptoShreddedField field)
    {
        string? subjectId;
        try
        {
            subjectId = field.ResolveSubjectId(owner);
        }
        catch (InvalidOperationException)
        {
            throw WriteFailure(frame, field, CryptoShreddingEncryptionFailureReason.SubjectIdInvalid, CryptoShreddingErrors.InvalidSubjectIdCode, null);
        }

        if (subjectId is null)
        {
            _logger.SubjectIdMissing(nameof(CryptoOperation.Encrypt), field.DeclaringType.Name, field.Name, field.SubjectIdProperty.Name);
            throw WriteFailure(frame, field, CryptoShreddingEncryptionFailureReason.SubjectIdMissing, CryptoShreddingErrors.InvalidSubjectIdCode, null, log: false);
        }

        return subjectId;
    }

    // The placeholder of a forgotten subject (a re-saved snapshot or read model) is written as the tombstone:
    // nothing personal is stored and no key is created (Art. 17). Any other value for a forgotten subject fails.
    private string WriteForgotten(CryptoShreddingFrame frame, CryptoShreddedField field, string value)
    {
        if (!string.Equals(value, Placeholder, StringComparison.Ordinal))
        {
            throw WriteFailure(frame, field, CryptoShreddingEncryptionFailureReason.KeyUnavailable, CryptoShreddingErrors.SubjectForgottenCode, null);
        }

        _logger.ForgottenSubjectTombstoneWritten(field.DeclaringType.Name, field.Name);
        return CryptoShreddingToken.Tombstone;
    }

    private CryptoWriteKey GetWriteKey(CryptoShreddingFrame frame, string subjectId, CryptoShreddedField field)
    {
        if (!frame.WriteKeys.TryGetValue(subjectId, out var key))
        {
            key = FetchWriteKey(frame, subjectId, field);
            frame.WriteKeys[subjectId] = key;
        }

        return key;
    }

    private CryptoWriteKey FetchWriteKey(CryptoShreddingFrame frame, string subjectId, CryptoShreddedField field)
    {
        Either<EncinaError, SubjectEncryptionKey> result;
        try
        {
            result = frame.Services.GetRequiredService<ISubjectKeyProvider>()
                .GetOrCreateSubjectKeyAsync(subjectId).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw WriteFailure(frame, field, CryptoShreddingEncryptionFailureReason.KeyUnavailable, CryptoShreddingErrors.KeyStoreErrorCode, ex);
        }

        return result.Match(
            Right: key => ToWriteKey(frame, field, key),
            Left: error => ToForgottenWriteKey(frame, field, error));
    }

    private CryptoWriteKey ToWriteKey(CryptoShreddingFrame frame, CryptoShreddedField field, SubjectEncryptionKey key)
    {
        var aes = key.Version >= 1 ? CryptoShreddingFieldCipher.CreateAes(key.KeyMaterial) : null;
        return aes is null
            ? throw WriteFailure(frame, field, CryptoShreddingEncryptionFailureReason.KeyUnavailable, CryptoShreddingErrors.EncryptionFailedCode, null)
            : new CryptoWriteKey(key.Version, aes, Forgotten: false);
    }

    private CryptoWriteKey ToForgottenWriteKey(CryptoShreddingFrame frame, CryptoShreddedField field, EncinaError error)
    {
        var code = ErrorCode(error, CryptoShreddingErrors.EncryptionFailedCode);
        return code == CryptoShreddingErrors.SubjectForgottenCode
            ? new CryptoWriteKey(0, null, Forgotten: true)
            : throw WriteFailure(frame, field, CryptoShreddingEncryptionFailureReason.KeyUnavailable, code, null);
    }

    private CryptoShreddingEncryptionException WriteFailure(
        CryptoShreddingFrame frame,
        CryptoShreddedField field,
        CryptoShreddingEncryptionFailureReason reason,
        string errorCode,
        Exception? exception,
        bool log = true)
    {
        var reasonName = reason.ToString();
        CryptoShreddingDiagnostics.EncryptionFailedTotal.Add(1, CryptoShreddingDiagnostics.FailureReasonTag(reasonName));
        frame.RecordFailure(reasonName);
        if (log)
        {
            _logger.EncryptionFailed(field.DeclaringType.Name, field.Name, reasonName, errorCode, exception?.ForLogging());
        }

        return new CryptoShreddingEncryptionException(frame.RootType, field.DeclaringType, field.Name, reason, errorCode);
    }

    internal static string ErrorCode(EncinaError error, string fallback) => error.GetCode().IfNone(fallback);
}

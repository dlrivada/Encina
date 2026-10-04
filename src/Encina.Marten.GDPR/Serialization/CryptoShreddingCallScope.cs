using System.Diagnostics;
using System.Security.Cryptography;

using Encina.Marten.GDPR.Diagnostics;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Marten.GDPR;

/// <summary>Which direction a frame serves.</summary>
internal enum CryptoOperation
{
    /// <summary>Serialization (encryption).</summary>
    Encrypt,

    /// <summary>Deserialization (decryption).</summary>
    Decrypt,
}

/// <summary>
/// The ambient frames of serializer calls. Write frames are <c>[ThreadStatic]</c> (STJ writes synchronously
/// inside the call); read frames and the subject filter are <c>AsyncLocal</c> so they flow into Marten's async
/// deserialization. Frames form a stack, so a re-entrant serializer call (the key store saving its own
/// documents) gets its own frame.
/// </summary>
internal static class CryptoShreddingCallScope
{
    [ThreadStatic]
    private static CryptoShreddingFrame? t_write;

    private static readonly AsyncLocal<CryptoShreddingFrame?> ReadFrame = new();
    private static readonly AsyncLocal<string?> SubjectFilter = new();

    /// <summary>Gets the innermost write frame of the current thread.</summary>
    internal static CryptoShreddingFrame? CurrentWrite => t_write;

    /// <summary>Gets the innermost read frame of the current async flow.</summary>
    internal static CryptoShreddingFrame? CurrentRead => ReadFrame.Value;

    /// <summary>Gets the subject filter of the current async flow, or <c>null</c>.</summary>
    internal static string? CurrentSubjectFilter => SubjectFilter.Value;

    /// <summary>
    /// Restricts decryption in the current async flow to the fields of one subject: fields of other subjects
    /// keep their tokens and none of their keys is fetched. Used by the personal data locator.
    /// </summary>
    /// <param name="subjectId">The subject whose fields are decrypted.</param>
    /// <returns>A handle that restores the previous filter.</returns>
    internal static IDisposable FilterToSubject(string subjectId)
    {
        var previous = SubjectFilter.Value;
        SubjectFilter.Value = subjectId;
        return new Restore(() => SubjectFilter.Value = previous);
    }

    internal static void Push(CryptoShreddingFrame frame)
    {
        if (frame.Operation == CryptoOperation.Encrypt)
        {
            frame.Parent = t_write;
            t_write = frame;
        }
        else
        {
            frame.Parent = ReadFrame.Value;
            ReadFrame.Value = frame;
        }
    }

    internal static void Pop(CryptoShreddingFrame frame)
    {
        if (frame.Operation == CryptoOperation.Encrypt)
        {
            t_write = frame.Parent;
        }
        else
        {
            ReadFrame.Value = frame.Parent;
        }
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}

/// <summary>
/// The state of one serializer call: per-call key caches (never shared across calls, so an erased key is never
/// served from a cache), the lazily created DI scope that resolves the key provider and the forgotten-subject
/// handler, the owners waiting for decryption and a lazy activity.
/// </summary>
/// <remarks>
/// Only <see cref="AesGcm"/> instances are cached; a provider-owned key array is never kept or zeroed. At the
/// end of the call the ciphers and the DI scope are disposed.
/// </remarks>
internal sealed class CryptoShreddingFrame : IDisposable, IAsyncDisposable
{
    private readonly long _startedAt = Stopwatch.GetTimestamp();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HashSet<object> _pendingSet = new(ReferenceEqualityComparer.Instance);
    private IServiceScope? _scope;
    private bool _disposed;

    internal CryptoShreddingFrame(IServiceScopeFactory scopeFactory, CryptoOperation operation, Type? rootType, string? subjectFilter)
    {
        _scopeFactory = scopeFactory;
        Operation = operation;
        RootType = rootType;
        SubjectFilter = subjectFilter;
    }

    internal CryptoOperation Operation { get; }

    internal Type? RootType { get; }

    internal string? SubjectFilter { get; }

    internal CryptoShreddingFrame? Parent { get; set; }

    /// <summary>The deserialized root, set before pending owners are decrypted (used for field paths).</summary>
    internal object? Root { get; set; }

    internal Dictionary<string, CryptoWriteKey> WriteKeys { get; } = new(StringComparer.Ordinal);

    internal Dictionary<(string Subject, int Version), CryptoReadKey> ReadKeys { get; } = [];

    internal Dictionary<string, CryptoForgottenCheck> ForgottenChecks { get; } = new(StringComparer.Ordinal);

    internal List<(object Owner, CryptoShreddingTypePlan Plan)> Pending { get; } = [];

    /// <summary>Forgotten subjects met in this call, in order, with the path of their first field.</summary>
    internal List<(string Subject, string FieldPath)> ForgottenToNotify { get; } = [];

    internal HashSet<string> NotifiedSubjects { get; } = new(StringComparer.Ordinal);

    internal Activity? Activity { get; private set; }

    /// <summary>Gets the services of the call's DI scope, created on first use.</summary>
    internal IServiceProvider Services => (_scope ??= _scopeFactory.CreateScope()).ServiceProvider;

    /// <summary>Gets the display name of the root type.</summary>
    internal string RootTypeName => RootType?.Name ?? "unknown";

    /// <summary>Enqueues a deserialized owner once (by reference).</summary>
    internal void Enqueue(object owner, CryptoShreddingTypePlan plan)
    {
        if (_pendingSet.Add(owner))
        {
            Pending.Add((owner, plan));
        }
    }

    /// <summary>Starts the call's activity on the first crypto field (documents without PII get none).</summary>
    internal Activity? EnsureActivity() =>
        Activity ??= Operation == CryptoOperation.Encrypt
            ? CryptoShreddingDiagnostics.StartEncryption(RootTypeName)
            : CryptoShreddingDiagnostics.StartDecryption(RootTypeName);

    /// <summary>Marks the call's activity as failed with a low-cardinality reason (never a message).</summary>
    internal void RecordFailure(string reason) => CryptoShreddingDiagnostics.RecordFailed(EnsureActivity(), reason);

    public void Dispose()
    {
        if (!Release())
        {
            return;
        }

        _scope?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (!Release())
        {
            return;
        }

        if (_scope is IAsyncDisposable asyncScope)
        {
            await asyncScope.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _scope?.Dispose();
        }
    }

    private bool Release()
    {
        if (_disposed)
        {
            return false;
        }

        _disposed = true;
        CryptoShreddingCallScope.Pop(this);
        DisposeCiphers();
        FinishActivity();
        return true;
    }

    private void DisposeCiphers()
    {
        foreach (var key in WriteKeys.Values)
        {
            key.Aes?.Dispose();
        }

        foreach (var key in ReadKeys.Values)
        {
            key.Aes?.Dispose();
        }
    }

    private void FinishActivity()
    {
        if (Activity is null)
        {
            return;
        }

        if (Activity.Status == ActivityStatusCode.Unset)
        {
            CryptoShreddingDiagnostics.RecordSuccess(Activity);
        }

        var histogram = Operation == CryptoOperation.Encrypt
            ? CryptoShreddingDiagnostics.EncryptionDuration
            : CryptoShreddingDiagnostics.DecryptionDuration;
        histogram.Record(Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds);
        Activity.Dispose();
    }
}

/// <summary>A cached write key: a cipher for a version, or the forgotten outcome.</summary>
internal sealed record CryptoWriteKey(int Version, AesGcm? Aes, bool Forgotten);

/// <summary>A cached read key: a cipher, or the forgotten outcome.</summary>
internal sealed record CryptoReadKey(AesGcm? Aes, bool Forgotten);

/// <summary>A cached tombstone confirmation: forgotten, live, or the provider failed with an error code.</summary>
internal sealed record CryptoForgottenCheck(bool Forgotten, string? FailureCode);

/// <summary>Helper to build a frame-owned ephemeral key (installation check only).</summary>
internal static class CryptoShreddingEphemeralKey
{
    internal static CryptoWriteKey Create()
    {
        var material = RandomNumberGenerator.GetBytes(CryptoShreddingFieldCipher.KeySize);
        try
        {
            return new CryptoWriteKey(1, CryptoShreddingFieldCipher.CreateAes(material), Forgotten: false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(material);
        }
    }
}

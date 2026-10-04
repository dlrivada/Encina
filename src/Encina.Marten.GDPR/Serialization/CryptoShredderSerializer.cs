using System.Buffers;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Encina.Marten.GDPR.Diagnostics;

using Marten;
using Marten.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Npgsql;

using Weasel.Core;

using ISerializer = Marten.ISerializer;
using SystemTextJsonSerializer = Marten.Services.SystemTextJsonSerializer;

namespace Encina.Marten.GDPR;

/// <summary>
/// The Marten <see cref="ISerializer"/> that crypto-shreds every <see cref="CryptoShreddedAttribute"/> property
/// Marten serializes, at any depth: nested objects, collection elements, dictionary values, polymorphic members,
/// records and non-public properties with <c>[JsonInclude]</c>.
/// </summary>
/// <remarks>
/// <para>
/// It wraps Marten's <see cref="SystemTextJsonSerializer"/> and installs a System.Text.Json contract modifier on
/// each of its <see cref="JsonSerializerOptions"/>. Encryption happens in the wrapped property getter, which
/// receives the declaring object, so the subject is the value of that object's
/// <see cref="CryptoShreddedAttribute.SubjectIdProperty"/> sibling and the caller's object is never mutated.
/// Decryption runs after deserialization for every constructed owner.
/// </para>
/// <para>
/// <b>Fail closed.</b> A value that cannot be encrypted throws <see cref="CryptoShreddingEncryptionException"/>
/// before any byte reaches the caller (the buffer-writer members stage their output). A misconfigured type throws
/// <see cref="CryptoShreddingConfigurationException"/>. A stored value that cannot be read throws
/// <see cref="CryptoShreddingDecryptionException"/>; only a forgotten subject reads as the placeholder.
/// </para>
/// <para>
/// <b>Keys</b> are resolved through <see cref="IServiceScopeFactory"/> in a DI scope created lazily per call, and
/// cached for that call only. <b>Thread safety</b>: the serializer is thread-safe; per-call state lives in
/// <c>[ThreadStatic]</c> write frames and <c>AsyncLocal</c> read frames.
/// </para>
/// </remarks>
[SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
    Justification = "Overloads are required by Marten's ISerializer interface contract.")]
public sealed class CryptoShredderSerializer : ISerializer
{
    [ThreadStatic]
    private static ArrayBufferWriter<byte>? t_staging;

    [ThreadStatic]
    private static bool t_stagingInUse;

    private static readonly ConditionalWeakTable<Type, StrongBox<bool>> StagingNeeded = new();

    private readonly SystemTextJsonSerializer _inner;
    private readonly ILogger<CryptoShredderSerializer> _logger;
    private readonly CryptoShreddingEngine _engine;
    private readonly CryptoShreddingContractModifier _modifier;
    private readonly List<InstalledResolver> _installed = [];
    private readonly Lock _verifyLock = new();
    private CryptoShreddingConfigurationException? _verificationFailure;
    private bool _verified;

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoShredderSerializer"/> class and installs the contract
    /// modifier on every <see cref="JsonSerializerOptions"/> of <paramref name="inner"/>.
    /// </summary>
    /// <param name="inner">Marten's System.Text.Json serializer.</param>
    /// <param name="scopeFactory">Creates the per-call DI scope that resolves the key provider and the forgotten-subject handler.</param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    /// <param name="anonymizedPlaceholder">The placeholder a forgotten subject's fields read as. Defaults to <c>"[REDACTED]"</c>.</param>
    /// <exception cref="CryptoShreddingConfigurationException">An options object is already read-only, so the modifier cannot be installed.</exception>
    public CryptoShredderSerializer(
        SystemTextJsonSerializer inner,
        IServiceScopeFactory scopeFactory,
        ILogger<CryptoShredderSerializer> logger,
        string anonymizedPlaceholder = CryptoShredderSerializerFactory.DefaultAnonymizedPlaceholder)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(anonymizedPlaceholder);

        _inner = inner;
        _logger = logger;
        _engine = new CryptoShreddingEngine(scopeFactory, logger, anonymizedPlaceholder);
        _modifier = new CryptoShreddingContractModifier(_engine, logger);
        inner.Configure(Install);
        _engine.PathOptions = _installed.Count > 0 ? _installed[0].Options : null;
    }

    /// <summary>Gets the number of options objects the modifier was installed on.</summary>
    internal int InstalledOptionsCount => _installed.Count;

    /// <summary>Gets the plans of the contracts built so far.</summary>
    internal CryptoShreddingTypePlanRegistry Registry => _engine.Registry;

    /// <summary>Gets the options the graph walker uses (the first captured options object).</summary>
    internal JsonSerializerOptions? WalkOptions => _engine.PathOptions;

    /// <summary>Gets the inner serializer.</summary>
    internal SystemTextJsonSerializer Inner => _inner;

    /// <inheritdoc />
    public EnumStorage EnumStorage => _inner.EnumStorage;

    /// <inheritdoc />
    public Casing Casing => _inner.Casing;

    /// <inheritdoc />
    public ValueCasting ValueCasting => _inner.ValueCasting;

    /// <inheritdoc />
    public string ToJson(object? document) => Write(document, d => _inner.ToJson(d));

    /// <inheritdoc />
    public string ToCleanJson(object? document) => Write(document, d => _inner.ToCleanJson(d));

    /// <inheritdoc />
    public string ToJsonWithTypes(object document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Write(document, d => _inner.ToJsonWithTypes(d!));
    }

    /// <inheritdoc />
    public void WriteTo(IBufferWriter<byte> writer, object? value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        WriteStaged(writer, value, static (inner, w, v) => inner.WriteTo(w, v));
    }

    /// <inheritdoc />
    public void WriteToCleanJson(IBufferWriter<byte> writer, object? value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        WriteStaged(writer, value, static (inner, w, v) => inner.WriteToCleanJson(w, v));
    }

    /// <inheritdoc />
    public void WriteToJsonWithTypes(IBufferWriter<byte> writer, object value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        WriteStaged(writer, value, static (inner, w, v) => inner.WriteToJsonWithTypes(w, v!));
    }

    /// <inheritdoc />
    public void WriteToParameter(DbParameter parameter, object? value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        Write(value, v => { ((ISerializer)_inner).WriteToParameter(parameter, v); return 0; });
    }

    /// <inheritdoc />
    public void WriteToParameter(NpgsqlParameter parameter, object? value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        Write(value, v => { _inner.WriteToParameter(parameter, v); return 0; });
    }

    /// <inheritdoc />
    public T FromJson<T>(Stream stream) => Read(typeof(T), () => _inner.FromJson<T>(stream));

    /// <inheritdoc />
    public T FromJson<T>(DbDataReader reader, int index) => Read(typeof(T), () => _inner.FromJson<T>(reader, index));

    /// <inheritdoc />
    public object FromJson(Type type, Stream stream) => Read(type, () => _inner.FromJson(type, stream));

    /// <inheritdoc />
    public object FromJson(Type type, DbDataReader reader, int index) => Read(type, () => _inner.FromJson(type, reader, index));

    /// <inheritdoc />
    public ValueTask<T> FromJsonAsync<T>(Stream stream, CancellationToken cancellationToken = default) =>
        ReadAsync(typeof(T), ct => _inner.FromJsonAsync<T>(stream, ct), cancellationToken);

    /// <inheritdoc />
    public ValueTask<T> FromJsonAsync<T>(DbDataReader reader, int index, CancellationToken cancellationToken = default) =>
        ReadAsync(typeof(T), ct => _inner.FromJsonAsync<T>(reader, index, ct), cancellationToken);

    /// <inheritdoc />
    public ValueTask<object> FromJsonAsync(Type type, Stream stream, CancellationToken cancellationToken = default) =>
        ReadAsync(type, ct => _inner.FromJsonAsync(type, stream, ct), cancellationToken);

    /// <inheritdoc />
    public ValueTask<object> FromJsonAsync(Type type, DbDataReader reader, int index, CancellationToken cancellationToken = default) =>
        ReadAsync(type, ct => _inner.FromJsonAsync(type, reader, index, ct), cancellationToken);

    /// <summary>
    /// Verifies that crypto-shredding is still installed: for each captured options object the resolver is the
    /// exact instance installed and the resolver chain is unchanged, and a nested canary serializes to a
    /// <c>cs2</c> token without its plaintext.
    /// </summary>
    /// <exception cref="CryptoShreddingConfigurationException">The modifier is missing, replaced or bypassed.</exception>
    internal void VerifyContractModifierInstalled()
    {
        foreach (var installed in _installed)
        {
            if (!installed.IsIntact() || !CanaryEncrypts(installed.Options))
            {
                throw InfrastructureFailure(CryptoShreddingConfigurationProblem.ContractModifierMissing, nameof(JsonSerializerOptions));
            }
        }
    }

    /// <summary>
    /// Resolves the contract of <paramref name="type"/> through every captured options object (running the
    /// modifier) and reports whether the modifier saw the type.
    /// </summary>
    /// <exception cref="CryptoShreddingConfigurationException">The type is misconfigured.</exception>
    internal JsonTypeInfo ResolveContract(Type type, out bool modifierSawType)
    {
        JsonTypeInfo? first = null;
        foreach (var installed in _installed)
        {
            first ??= installed.Options.GetTypeInfo(type);
            installed.Options.GetTypeInfo(type);
        }

        modifierSawType = _engine.Registry.WasSeen(type);
        return first ?? throw InfrastructureFailure(CryptoShreddingConfigurationProblem.ContractModifierMissing, nameof(JsonSerializerOptions));
    }

    private void Install(JsonSerializerOptions options)
    {
        if (options.IsReadOnly)
        {
            throw InfrastructureFailure(CryptoShreddingConfigurationProblem.ContractModifierMissing, nameof(JsonSerializerOptions));
        }

        var resolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver()).WithAddedModifier(_modifier.Modify);
        options.TypeInfoResolver = resolver;
        _installed.Add(new InstalledResolver(options, resolver, [.. options.TypeInfoResolverChain]));
    }

    private T Write<T>(object? document, Func<object?, T> write)
    {
        EnsureVerified();
        using var frame = _engine.Begin(CryptoOperation.Encrypt, document?.GetType());
        return write(document);
    }

    private void WriteStaged(IBufferWriter<byte> writer, object? value, Action<SystemTextJsonSerializer, IBufferWriter<byte>, object?> write)
    {
        if (value is null || t_stagingInUse || !NeedsStaging(value.GetType()))
        {
            Write(value, v => { write(_inner, writer, v); return 0; });
            return;
        }

        // A failed write must leave the caller's buffer untouched, so the output is staged and copied on success.
        var staging = t_staging ??= new ArrayBufferWriter<byte>();
        staging.ResetWrittenCount();
        t_stagingInUse = true;
        try
        {
            Write(value, v => { write(_inner, staging, v); return 0; });
            writer.Write(staging.WrittenSpan);
        }
        finally
        {
            staging.ResetWrittenCount();
            t_stagingInUse = false;
        }
    }

    /// <summary>
    /// Gets whether a root type can reach crypto data: an owner, a container of owners, or an open slot
    /// (<c>object</c>, interface, abstract or polymorphic member) whose runtime value may be one.
    /// </summary>
    internal static bool NeedsStaging(Type type) =>
        StagingNeeded.GetValue(type, static t => new StrongBox<bool>(ComputeNeedsStaging(t))).Value;

    private static bool ComputeNeedsStaging(Type type) =>
        CryptoShreddedPropertyClassifier.IsOwner(type)
        || CryptoShreddedPropertyClassifier.ReachesCryptoOwner(type)
        || HasOpenSlot(type, new HashSet<Type>());

    private static bool HasOpenSlot(Type type, HashSet<Type> visited)
    {
        if (IsOpenSlot(type))
        {
            return true;
        }

        if (CryptoShreddedPropertyClassifier.IsTerminal(type) || !visited.Add(type))
        {
            return false;
        }

        return CryptoShreddedPropertyClassifier.ComponentTypes(type).Any(component => HasOpenSlot(component, visited));
    }

    private static bool IsOpenSlot(Type type) =>
        type == typeof(object)
        || (type.IsInterface && !type.IsGenericType)
        || (type.IsAbstract && !type.IsSealed && !type.IsInterface)
        || type.IsDefined(typeof(System.Text.Json.Serialization.JsonDerivedTypeAttribute), inherit: false);

    private T Read<T>(Type rootType, Func<T> read)
    {
        using var frame = _engine.Begin(CryptoOperation.Decrypt, rootType);
        var result = read();
        _engine.DecryptPending(frame, result);
        return result;
    }

    private async ValueTask<T> ReadAsync<T>(Type rootType, Func<CancellationToken, ValueTask<T>> read, CancellationToken cancellationToken)
    {
        var frame = _engine.Begin(CryptoOperation.Decrypt, rootType);
        await using (frame.ConfigureAwait(false))
        {
            var result = await read(cancellationToken).ConfigureAwait(false);
            await _engine.DecryptPendingAsync(frame, result, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }

    // The installation is verified once, lazily, on the first write of the process; a failure keeps failing.
    private void EnsureVerified()
    {
        if (Volatile.Read(ref _verified))
        {
            return;
        }

        lock (_verifyLock)
        {
            if (!_verified && _verificationFailure is null)
            {
                RunVerification();
            }
        }

        if (_verificationFailure is { } failure)
        {
            throw failure;
        }
    }

    private void RunVerification()
    {
        try
        {
            VerifyContractModifierInstalled();
            Volatile.Write(ref _verified, true);
        }
        catch (CryptoShreddingConfigurationException ex)
        {
            _verificationFailure = ex;
        }
    }

    private bool CanaryEncrypts(JsonSerializerOptions options)
    {
        using var frame = _engine.Begin(CryptoOperation.Encrypt, typeof(CryptoShreddingCanaryHolder));
        frame.WriteKeys[CryptoShreddingCanary.Subject] = CryptoShreddingEphemeralKey.Create();
        try
        {
            var json = JsonSerializer.Serialize(new CryptoShreddingCanaryHolder(), options);
            return json.Contains(CryptoShreddingToken.Prefix, StringComparison.Ordinal)
                && !json.Contains(CryptoShreddingCanary.Plaintext, StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException)
        {
            return false;
        }
    }

    private CryptoShreddingConfigurationException InfrastructureFailure(CryptoShreddingConfigurationProblem problem, string componentType)
    {
        _logger.CryptoShreddingInfrastructureInvalid(problem.ToString(), componentType);
        return new CryptoShreddingConfigurationException(problem, [], componentType);
    }

    private sealed record InstalledResolver(JsonSerializerOptions Options, IJsonTypeInfoResolver Resolver, IJsonTypeInfoResolver[] Chain)
    {
        internal bool IsIntact() =>
            ReferenceEquals(Options.TypeInfoResolver, Resolver)
            && Options.TypeInfoResolverChain.SequenceEqual(Chain);
    }
}

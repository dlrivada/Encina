using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;
using Encina.Security.Encryption;

using Marten;

using Microsoft.Extensions.Logging;

using Weasel.Core;

namespace Encina.Marten.GDPR;

/// <summary>
/// A Marten <see cref="ISerializer"/> decorator that transparently encrypts and decrypts
/// properties marked with <see cref="CryptoShreddedAttribute"/> during event serialization
/// and deserialization.
/// </summary>
/// <remarks>
/// <para>
/// This serializer wraps an existing Marten <see cref="ISerializer"/> and intercepts the
/// serialization pipeline to apply field-level AES-256-GCM encryption to PII properties.
/// Non-PII events are passed through to the inner serializer without modification.
/// </para>
/// <para>
/// <b>Serialize flow</b> (<c>ToJson</c>, <c>ToCleanJson</c>):
/// </para>
/// <list type="number">
/// <item><description>Fast-path check via <see cref="CryptoShreddedPropertyCache.HasCryptoShreddedFields"/>
///   — if no crypto-shredded properties, delegate directly to inner serializer</description></item>
/// <item><description>For each <c>[CryptoShredded]</c> property: extract subject ID, obtain
///   encryption key via <see cref="ISubjectKeyProvider.GetOrCreateSubjectKeyAsync"/>,
///   encrypt with AES-256-GCM, replace property value with encrypted JSON envelope</description></item>
/// <item><description>Serialize modified object with inner serializer, then restore original
///   property values</description></item>
/// </list>
/// <para>
/// <b>Deserialize flow</b> (<c>FromJson</c>, <c>FromJsonAsync</c>):
/// </para>
/// <list type="number">
/// <item><description>Deserialize with inner serializer first</description></item>
/// <item><description>For each <c>[CryptoShredded]</c> property whose value starts with
///   <c>{"__enc":true</c>: parse the encrypted envelope, retrieve the key, and decrypt</description></item>
/// <item><description>For forgotten subjects (key not found): invoke
///   <see cref="IForgottenSubjectHandler"/> and apply the anonymized placeholder</description></item>
/// </list>
/// <para>
/// Encryption uses <see cref="AesGcm"/> directly with key material from
/// <see cref="ISubjectKeyProvider"/>, providing authenticated encryption with
/// 12-byte nonces and 16-byte authentication tags.
/// </para>
/// <para>
/// <b>Thread safety</b>: This class is thread-safe. Marten may invoke serializer methods
/// concurrently from multiple threads. The <see cref="ISubjectKeyProvider"/> and
/// <see cref="IForgottenSubjectHandler"/> implementations must also be thread-safe.
/// </para>
/// </remarks>
[SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
    Justification = "Overloads are required by Marten's ISerializer interface contract.")]
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly",
    Justification = "Sync-over-async is intentional: Marten's ISerializer.ToJson/FromJson are sync methods, " +
                    "but ISubjectKeyProvider is async. Marten invokes serializers from within its async pipeline " +
                    "(no SynchronizationContext), making .GetAwaiter().GetResult() safe here.")]
public sealed class CryptoShredderSerializer : ISerializer
{
    private const int NonceSizeInBytes = 12;
    private const int TagSizeInBytes = 16;

    private readonly ISerializer _inner;
    private readonly ISubjectKeyProvider _subjectKeyProvider;
    private readonly IForgottenSubjectHandler _forgottenSubjectHandler;
    private readonly ILogger<CryptoShredderSerializer> _logger;
    private readonly string _anonymizedPlaceholder;

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoShredderSerializer"/> class.
    /// </summary>
    /// <param name="inner">The inner Marten serializer to delegate to for actual JSON processing.</param>
    /// <param name="subjectKeyProvider">The provider for per-subject encryption keys.</param>
    /// <param name="forgottenSubjectHandler">
    /// The handler invoked when a forgotten subject's data is encountered during deserialization.
    /// </param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    /// <param name="anonymizedPlaceholder">
    /// The placeholder value substituted for PII properties of forgotten subjects.
    /// Defaults to <c>"[REDACTED]"</c>.
    /// </param>
    public CryptoShredderSerializer(
        ISerializer inner,
        ISubjectKeyProvider subjectKeyProvider,
        IForgottenSubjectHandler forgottenSubjectHandler,
        ILogger<CryptoShredderSerializer> logger,
        string anonymizedPlaceholder = "[REDACTED]")
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(subjectKeyProvider);
        ArgumentNullException.ThrowIfNull(forgottenSubjectHandler);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(anonymizedPlaceholder);

        _inner = inner;
        _subjectKeyProvider = subjectKeyProvider;
        _forgottenSubjectHandler = forgottenSubjectHandler;
        _logger = logger;
        _anonymizedPlaceholder = anonymizedPlaceholder;
    }

    /// <inheritdoc />
    public EnumStorage EnumStorage => _inner.EnumStorage;

    /// <inheritdoc />
    public Casing Casing => _inner.Casing;

    /// <inheritdoc />
    public ValueCasting ValueCasting => _inner.ValueCasting;

    /// <inheritdoc />
    public string ToJson(object? document)
    {
        if (document is null || !CryptoShreddedPropertyCache.HasCryptoShreddedFields(document.GetType()))
        {
            return _inner.ToJson(document);
        }

        return SerializeWithEncryption(document, d => _inner.ToJson(d));
    }

    /// <inheritdoc />
    public string ToCleanJson(object? document)
    {
        if (document is null || !CryptoShreddedPropertyCache.HasCryptoShreddedFields(document.GetType()))
        {
            return _inner.ToCleanJson(document);
        }

        return SerializeWithEncryption(document, d => _inner.ToCleanJson(d));
    }

    /// <inheritdoc />
    public string ToJsonWithTypes(object document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!CryptoShreddedPropertyCache.HasCryptoShreddedFields(document.GetType()))
        {
            return _inner.ToJsonWithTypes(document);
        }

        return SerializeWithEncryption(document, d => _inner.ToJsonWithTypes(d));
    }

    /// <inheritdoc />
    public T FromJson<T>(Stream stream)
    {
        var result = _inner.FromJson<T>(stream);
        return DecryptIfNeeded(result);
    }

    /// <inheritdoc />
    public T FromJson<T>(DbDataReader reader, int index)
    {
        var result = _inner.FromJson<T>(reader, index);
        return DecryptIfNeeded(result);
    }

    /// <inheritdoc />
    public object FromJson(Type type, Stream stream)
    {
        var result = _inner.FromJson(type, stream);
        return DecryptIfNeeded(result, type);
    }

    /// <inheritdoc />
    public object FromJson(Type type, DbDataReader reader, int index)
    {
        var result = _inner.FromJson(type, reader, index);
        return DecryptIfNeeded(result, type);
    }

    /// <inheritdoc />
    public async ValueTask<T> FromJsonAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        var result = await _inner.FromJsonAsync<T>(stream, cancellationToken).ConfigureAwait(false);
        return await DecryptIfNeededAsync(result, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<T> FromJsonAsync<T>(DbDataReader reader, int index, CancellationToken cancellationToken = default)
    {
        var result = await _inner.FromJsonAsync<T>(reader, index, cancellationToken).ConfigureAwait(false);
        return await DecryptIfNeededAsync(result, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<object> FromJsonAsync(Type type, Stream stream, CancellationToken cancellationToken = default)
    {
        var result = await _inner.FromJsonAsync(type, stream, cancellationToken).ConfigureAwait(false);
        return await DecryptIfNeededAsync(result, type, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<object> FromJsonAsync(Type type, DbDataReader reader, int index, CancellationToken cancellationToken = default)
    {
        var result = await _inner.FromJsonAsync(type, reader, index, cancellationToken).ConfigureAwait(false);
        return await DecryptIfNeededAsync(result, type, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Encrypts PII fields on the document, serializes with the inner serializer,
    /// then restores original values.
    /// </summary>
    private string SerializeWithEncryption(object document, Func<object, string> innerSerialize)
    {
        var eventType = document.GetType();
        var eventTypeName = eventType.Name;
        var fields = CryptoShreddedPropertyCache.GetFields(eventType);

        using var activity = CryptoShreddingDiagnostics.StartEncryption(eventTypeName);
        var stopwatch = Stopwatch.GetTimestamp();

        // Save original values for restore
        var originalValues = new (CryptoShreddedFieldInfo Field, object? Value)[fields.Length];
        for (var i = 0; i < fields.Length; i++)
        {
            originalValues[i] = (fields[i], fields[i].GetValue(document));
        }

        try
        {
            // Encrypt each PII field
            foreach (var field in fields)
            {
                var plaintext = field.GetValue(document) as string;
                if (plaintext is null)
                {
                    // Null values stay null — no encryption needed
                    continue;
                }

                var subjectId = GetSubjectId(document, eventType, field);
                if (subjectId is null)
                {
                    _logger.LogWarning(
                        "Cannot extract subject ID from property '{SubjectIdProperty}' on event type '{EventType}'. Skipping encryption for field '{FieldName}'",
                        field.SubjectIdProperty,
                        eventType.Name,
                        field.Property.Name);
                    continue;
                }

                var encryptedJson = EncryptField(subjectId, plaintext, field.Property.Name, eventType);
                if (encryptedJson is not null)
                {
                    field.SetValue(document, encryptedJson);
                    CryptoShreddingDiagnostics.EncryptionTotal.Add(1);
                    _logger.PiiFieldEncrypted(subjectId, field.Property.Name, eventTypeName);
                }
            }

            var result = innerSerialize(document);
            CryptoShreddingDiagnostics.RecordSuccess(activity);
            return result;
        }
        catch (Exception ex)
        {
            CryptoShreddingDiagnostics.RecordFailed(activity, ex.Message);
            throw;
        }
        finally
        {
            // Always restore original values to avoid mutating the caller's object
            foreach (var (field, value) in originalValues)
            {
                field.SetValue(document, value);
            }

            var elapsed = Stopwatch.GetElapsedTime(stopwatch);
            CryptoShreddingDiagnostics.EncryptionDuration.Record(elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Encrypts a single plaintext value using AES-256-GCM with the subject's key.
    /// </summary>
    /// <returns>The encrypted JSON envelope string, or <c>null</c> if encryption fails.</returns>
    private string? EncryptField(string subjectId, string plaintext, string propertyName, Type eventType)
    {
        try
        {
            // Get or create key — sync-over-async is safe here because Marten invokes
            // serializers from within its async pipeline (no SynchronizationContext)
            var keyResult = _subjectKeyProvider
                .GetOrCreateSubjectKeyAsync(subjectId)
                .GetAwaiter()
                .GetResult();

            return keyResult.Match<string?>(
                Right: keyMaterial =>
                {
                    // Get subject info for version number
                    var infoResult = _subjectKeyProvider
                        .GetSubjectInfoAsync(subjectId)
                        .GetAwaiter()
                        .GetResult();

                    var version = infoResult.Match(
                        Right: info => info.ActiveKeyVersion,
                        Left: _ => 1);

                    var keyId = $"subject:{subjectId}:v{version}";

                    // Perform AES-256-GCM encryption
                    var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                    var nonce = new byte[NonceSizeInBytes];
                    RandomNumberGenerator.Fill(nonce);

                    var ciphertext = new byte[plaintextBytes.Length];
                    var tag = new byte[TagSizeInBytes];

                    using var aesGcm = new AesGcm(keyMaterial, TagSizeInBytes);
                    aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

                    var encryptedValue = new EncryptedValue
                    {
                        KeyId = keyId,
                        Ciphertext = [.. ciphertext],
                        Nonce = [.. nonce],
                        Tag = [.. tag],
                        Algorithm = EncryptionAlgorithm.Aes256Gcm
                    };

                    return EncryptedFieldJsonConverter.Serialize(encryptedValue);
                },
                Left: error =>
                {
                    CryptoShreddingDiagnostics.EncryptionFailedTotal.Add(1);
                    _logger.EncryptionFailed(subjectId, propertyName, eventType.Name);
                    return null;
                });
        }
        catch (Exception ex)
        {
            CryptoShreddingDiagnostics.EncryptionFailedTotal.Add(1);
            _logger.EncryptionFailed(subjectId, propertyName, eventType.Name, ex);
            return null;
        }
    }

    /// <summary>
    /// Decrypts PII fields on a deserialized object (sync path).
    /// </summary>
    private T DecryptIfNeeded<T>(T result)
    {
        if (result is null)
        {
            return result;
        }

        var type = result.GetType();
        if (!CryptoShreddedPropertyCache.HasCryptoShreddedFields(type))
        {
            return result;
        }

        DecryptFields(result, type);
        return result;
    }

    /// <summary>
    /// Decrypts PII fields on a deserialized object (sync path with explicit type).
    /// </summary>
    private object DecryptIfNeeded(object result, Type type)
    {
        if (!CryptoShreddedPropertyCache.HasCryptoShreddedFields(type))
        {
            return result;
        }

        DecryptFields(result, type);
        return result;
    }

    /// <summary>
    /// Decrypts PII fields on a deserialized object (async path).
    /// </summary>
    private async ValueTask<T> DecryptIfNeededAsync<T>(T result, CancellationToken cancellationToken)
    {
        if (result is null)
        {
            return result;
        }

        var type = result.GetType();
        if (!CryptoShreddedPropertyCache.HasCryptoShreddedFields(type))
        {
            return result;
        }

        await DecryptFieldsAsync(result, type, cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Decrypts PII fields on a deserialized object (async path with explicit type).
    /// </summary>
    private async ValueTask<object> DecryptIfNeededAsync(object result, Type type, CancellationToken cancellationToken)
    {
        if (!CryptoShreddedPropertyCache.HasCryptoShreddedFields(type))
        {
            return result;
        }

        await DecryptFieldsAsync(result, type, cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Iterates PII fields and decrypts their values in-place (sync path).
    /// </summary>
    private void DecryptFields(object target, Type eventType)
    {
        var eventTypeName = eventType.Name;
        var fields = CryptoShreddedPropertyCache.GetFields(eventType);

        using var activity = CryptoShreddingDiagnostics.StartDecryption(eventTypeName);
        var stopwatch = Stopwatch.GetTimestamp();

        try
        {
            foreach (var field in fields)
            {
                var currentValue = field.GetValue(target) as string;
                if (currentValue is null || !EncryptedFieldJsonConverter.IsEncryptedField(currentValue))
                {
                    continue;
                }

                var subjectId = GetSubjectId(target, eventType, field);
                if (subjectId is null)
                {
                    continue;
                }

                var decrypted = DecryptField(subjectId, currentValue, field.Property.Name, eventType);
                field.SetValue(target, decrypted);

                if (decrypted == _anonymizedPlaceholder)
                {
                    CryptoShreddingDiagnostics.ForgottenAccessTotal.Add(1);
                    _logger.ForgottenSubjectAccessed(subjectId, field.Property.Name, eventTypeName);
                }
                else
                {
                    CryptoShreddingDiagnostics.DecryptionTotal.Add(1);
                    _logger.PiiFieldDecrypted(subjectId, field.Property.Name, eventTypeName);
                }
            }

            CryptoShreddingDiagnostics.RecordSuccess(activity);
        }
        catch (Exception ex)
        {
            CryptoShreddingDiagnostics.RecordFailed(activity, ex.Message);
            throw;
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(stopwatch);
            CryptoShreddingDiagnostics.DecryptionDuration.Record(elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Iterates PII fields and decrypts their values in-place (async path).
    /// </summary>
    private async ValueTask DecryptFieldsAsync(object target, Type eventType, CancellationToken cancellationToken)
    {
        var eventTypeName = eventType.Name;
        var fields = CryptoShreddedPropertyCache.GetFields(eventType);

        using var activity = CryptoShreddingDiagnostics.StartDecryption(eventTypeName);
        var stopwatch = Stopwatch.GetTimestamp();

        try
        {
            foreach (var field in fields)
            {
                var currentValue = field.GetValue(target) as string;
                if (currentValue is null || !EncryptedFieldJsonConverter.IsEncryptedField(currentValue))
                {
                    continue;
                }

                var subjectId = GetSubjectId(target, eventType, field);
                if (subjectId is null)
                {
                    continue;
                }

                var decrypted = await DecryptFieldAsync(subjectId, currentValue, field.Property.Name, eventType, cancellationToken)
                    .ConfigureAwait(false);
                field.SetValue(target, decrypted);

                if (decrypted == _anonymizedPlaceholder)
                {
                    CryptoShreddingDiagnostics.ForgottenAccessTotal.Add(1);
                    _logger.ForgottenSubjectAccessed(subjectId, field.Property.Name, eventTypeName);
                }
                else
                {
                    CryptoShreddingDiagnostics.DecryptionTotal.Add(1);
                    _logger.PiiFieldDecrypted(subjectId, field.Property.Name, eventTypeName);
                }
            }

            CryptoShreddingDiagnostics.RecordSuccess(activity);
        }
        catch (Exception ex)
        {
            CryptoShreddingDiagnostics.RecordFailed(activity, ex.Message);
            throw;
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(stopwatch);
            CryptoShreddingDiagnostics.DecryptionDuration.Record(elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Decrypts a single encrypted field value (sync path — uses sync-over-async).
    /// </summary>
    private string? DecryptField(string subjectId, string encryptedJson, string propertyName, Type eventType)
    {
        try
        {
            var encryptedValue = EncryptedFieldJsonConverter.TryParse(encryptedJson);
            if (encryptedValue is null)
            {
                _logger.LogWarning(
                    "Failed to parse encrypted envelope for field '{FieldName}' on event type '{EventType}'",
                    propertyName,
                    eventType.Name);
                return encryptedJson;
            }

            // Extract version from key ID: "subject:{subjectId}:v{version}"
            var version = ExtractKeyVersion(encryptedValue.Value.KeyId);

            var keyResult = _subjectKeyProvider
                .GetSubjectKeyAsync(subjectId, version)
                .GetAwaiter()
                .GetResult();

            return keyResult.Match(
                Right: keyMaterial => DecryptWithKey(encryptedValue.Value, keyMaterial),
                Left: error =>
                {
                    // Subject is forgotten or key not found — apply placeholder
                    HandleForgottenSubjectSync(subjectId, propertyName, eventType);
                    return _anonymizedPlaceholder;
                });
        }
        catch (Exception ex)
        {
            CryptoShreddingDiagnostics.DecryptionFailedTotal.Add(1);
            _logger.DecryptionFailed(subjectId, propertyName, eventType.Name, ex);
            return _anonymizedPlaceholder;
        }
    }

    /// <summary>
    /// Decrypts a single encrypted field value (async path).
    /// </summary>
    private async ValueTask<string?> DecryptFieldAsync(
        string subjectId,
        string encryptedJson,
        string propertyName,
        Type eventType,
        CancellationToken cancellationToken)
    {
        try
        {
            var encryptedValue = EncryptedFieldJsonConverter.TryParse(encryptedJson);
            if (encryptedValue is null)
            {
                _logger.LogWarning(
                    "Failed to parse encrypted envelope for field '{FieldName}' on event type '{EventType}'",
                    propertyName,
                    eventType.Name);
                return encryptedJson;
            }

            var version = ExtractKeyVersion(encryptedValue.Value.KeyId);

            var keyResult = await _subjectKeyProvider
                .GetSubjectKeyAsync(subjectId, version, cancellationToken)
                .ConfigureAwait(false);

            if (keyResult.IsRight)
            {
                var keyMaterial = (byte[])keyResult;
                return DecryptWithKey(encryptedValue.Value, keyMaterial);
            }

            // Subject is forgotten or key not found — apply placeholder
            await HandleForgottenSubjectAsync(subjectId, propertyName, eventType, cancellationToken)
                .ConfigureAwait(false);
            return _anonymizedPlaceholder;
        }
        catch (Exception ex)
        {
            CryptoShreddingDiagnostics.DecryptionFailedTotal.Add(1);
            _logger.DecryptionFailed(subjectId, propertyName, eventType.Name, ex);
            return _anonymizedPlaceholder;
        }
    }

    /// <summary>
    /// Performs AES-256-GCM decryption using the provided key material.
    /// </summary>
    private static string DecryptWithKey(EncryptedValue encryptedValue, byte[] keyMaterial)
    {
        var ciphertext = encryptedValue.Ciphertext.AsSpan();
        var nonce = encryptedValue.Nonce.AsSpan();
        var tag = encryptedValue.Tag.AsSpan();

        var plaintext = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(keyMaterial, TagSizeInBytes);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>
    /// Extracts the subject ID from the event object using the field's <see cref="CryptoShreddedAttribute.SubjectIdProperty"/>.
    /// </summary>
    private static string? GetSubjectId(object document, Type eventType, CryptoShreddedFieldInfo field)
    {
        var subjectIdProp = eventType.GetProperty(
            field.SubjectIdProperty,
            BindingFlags.Public | BindingFlags.Instance);

        return subjectIdProp?.GetValue(document) as string;
    }

    /// <summary>
    /// Extracts the key version number from a key ID string.
    /// </summary>
    /// <param name="keyId">The key ID in format <c>"subject:{subjectId}:v{version}"</c>.</param>
    /// <returns>The version number, or <c>null</c> if parsing fails.</returns>
    private static int? ExtractKeyVersion(string keyId)
    {
        // Format: "subject:{subjectId}:v{version}"
        var lastColon = keyId.LastIndexOf(":v", StringComparison.Ordinal);
        if (lastColon < 0)
        {
            return null;
        }

        var versionStr = keyId.AsSpan(lastColon + 2);
        return int.TryParse(versionStr, out var version) ? version : null;
    }

    /// <summary>
    /// Invokes the forgotten subject handler (sync path — fire-and-forget).
    /// </summary>
    private void HandleForgottenSubjectSync(string subjectId, string propertyName, Type eventType)
    {
        try
        {
            _forgottenSubjectHandler
                .HandleForgottenSubjectAsync(subjectId, propertyName, eventType)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Forgotten subject handler failed for subject '{SubjectId}', field '{FieldName}'",
                subjectId,
                propertyName);
        }
    }

    /// <summary>
    /// Invokes the forgotten subject handler (async path).
    /// </summary>
    private async ValueTask HandleForgottenSubjectAsync(
        string subjectId,
        string propertyName,
        Type eventType,
        CancellationToken cancellationToken)
    {
        try
        {
            await _forgottenSubjectHandler
                .HandleForgottenSubjectAsync(subjectId, propertyName, eventType, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Forgotten subject handler failed for subject '{SubjectId}', field '{FieldName}'",
                subjectId,
                propertyName);
        }
    }
}

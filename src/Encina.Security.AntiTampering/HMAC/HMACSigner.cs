using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Security.AntiTampering.HMAC;

/// <summary>
/// Default HMAC-based implementation of <see cref="IRequestSigner"/>.
/// </summary>
/// <remarks>
/// <para>
/// Computes HMAC signatures over a canonical string representation of request components.
/// The canonical format is: <c>"Method|Path|PayloadHash|Timestamp|Nonce"</c>.
/// </para>
/// <para>
/// Supports HMAC-SHA256 (default), HMAC-SHA384, and HMAC-SHA512 algorithms.
/// Verification uses <see cref="CryptographicOperations.FixedTimeEquals"/> to prevent
/// timing attacks.
/// </para>
/// <para>
/// When <see cref="AntiTamperingOptions.EnableTracing"/> is <c>true</c>, operations emit
/// OpenTelemetry activities via the <c>Encina.Security.AntiTampering</c> ActivitySource.
/// When <see cref="AntiTamperingOptions.EnableMetrics"/> is <c>true</c>, counters and
/// histograms are emitted for monitoring.
/// </para>
/// <para>
/// Thread-safe: All operations are stateless and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class HMACSigner : IRequestSigner
{
    private readonly IKeyProvider _keyProvider;
    private readonly AntiTamperingOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="HMACSigner"/> class.
    /// </summary>
    /// <param name="keyProvider">The key provider for retrieving HMAC secret keys.</param>
    /// <param name="options">The anti-tampering configuration options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="keyProvider"/> or <paramref name="options"/> is null.
    /// </exception>
    public HMACSigner(
        IKeyProvider keyProvider,
        IOptions<AntiTamperingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(keyProvider);
        ArgumentNullException.ThrowIfNull(options);

        _keyProvider = keyProvider;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> SignAsync(
        ReadOnlyMemory<byte> payload,
        SigningContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        Activity? activity = null;

        if (_options.EnableTracing)
        {
            activity = AntiTamperingDiagnostics.StartSigning(context.KeyId, _options.Algorithm.ToString());
        }

        try
        {
            var keyResult = await _keyProvider.GetKeyAsync(context.KeyId, cancellationToken)
                .ConfigureAwait(false);

            return keyResult.Match<Either<EncinaError, string>>(
                Right: key =>
                {
                    var signature = ComputeSignature(payload, context, key);
                    AntiTamperingDiagnostics.RecordSuccess(activity, context.KeyId);
                    return Right(signature);
                },
                Left: error =>
                {
                    AntiTamperingDiagnostics.RecordFailure(activity, "key_not_found");
                    return Left<EncinaError, string>(error);
                });
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> VerifyAsync(
        ReadOnlyMemory<byte> payload,
        string signature,
        SigningContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(signature);

        Activity? activity = null;
        var stopwatch = _options.EnableMetrics ? Stopwatch.StartNew() : null;

        if (_options.EnableTracing)
        {
            activity = AntiTamperingDiagnostics.StartVerification(
                "signer-verify", _options.Algorithm.ToString(), !string.IsNullOrEmpty(context.Nonce));
        }

        try
        {
            var keyResult = await _keyProvider.GetKeyAsync(context.KeyId, cancellationToken)
                .ConfigureAwait(false);

            var result = keyResult.Match<Either<EncinaError, bool>>(
                Right: key =>
                {
                    var expectedSignature = ComputeSignature(payload, context, key);

                    var expectedBytes = Convert.FromBase64String(expectedSignature);
                    var actualBytes = Convert.FromBase64String(signature);

                    var isValid = CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);

                    if (isValid)
                    {
                        AntiTamperingDiagnostics.RecordSuccess(activity, context.KeyId);
                    }
                    else
                    {
                        AntiTamperingDiagnostics.RecordFailure(activity, "signature_mismatch");
                    }

                    return Right(isValid);
                },
                Left: error =>
                {
                    AntiTamperingDiagnostics.RecordFailure(activity, "key_not_found");
                    return Left<EncinaError, bool>(error);
                });

            if (_options.EnableMetrics && stopwatch is not null)
            {
                stopwatch.Stop();
                AntiTamperingDiagnostics.VerificationDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
            }

            return result;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <summary>
    /// Computes the HMAC signature for the given payload and context.
    /// </summary>
    private string ComputeSignature(ReadOnlyMemory<byte> payload, SigningContext context, byte[] key)
    {
        var payloadHash = ComputePayloadHash(payload.Span);

        var components = new SignatureComponents
        {
            HttpMethod = context.HttpMethod.ToUpperInvariant(),
            RequestPath = context.RequestPath,
            PayloadHash = payloadHash,
            Timestamp = context.Timestamp.ToString("O"),
            Nonce = context.Nonce,
            KeyId = context.KeyId
        };

        var canonicalString = components.ToCanonicalString();
        var canonicalBytes = Encoding.UTF8.GetBytes(canonicalString);

        var hmacBytes = ComputeHMAC(key, canonicalBytes);
        return Convert.ToBase64String(hmacBytes);
    }

    /// <summary>
    /// Computes the SHA-256 hash of the request payload.
    /// </summary>
    private static string ComputePayloadHash(ReadOnlySpan<byte> payload)
    {
        Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(payload, hashBytes);
        return Convert.ToHexStringLower(hashBytes);
    }

    /// <summary>
    /// Computes the HMAC using the configured algorithm.
    /// </summary>
    private byte[] ComputeHMAC(byte[] key, byte[] data)
    {
        return _options.Algorithm switch
        {
            HMACAlgorithm.SHA256 => HMACSHA256.HashData(key, data),
            HMACAlgorithm.SHA384 => HMACSHA384.HashData(key, data),
            HMACAlgorithm.SHA512 => HMACSHA512.HashData(key, data),
            _ => HMACSHA256.HashData(key, data)
        };
    }
}

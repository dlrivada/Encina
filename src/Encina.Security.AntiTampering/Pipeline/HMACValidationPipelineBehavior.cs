using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.Diagnostics;
using Encina.Security.AntiTampering.HMAC;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Security.AntiTampering.Pipeline;

/// <summary>
/// Pipeline behavior that validates HMAC signatures on incoming requests decorated with
/// <see cref="RequireSignatureAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The request type traversing the pipeline.</typeparam>
/// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
/// <remarks>
/// <para>
/// This behavior intercepts the Encina pipeline to enforce request integrity and replay protection:
/// <list type="number">
/// <item><description>Checks if <typeparamref name="TRequest"/> has <see cref="RequireSignatureAttribute"/>.</description></item>
/// <item><description>Extracts signature, timestamp, nonce, and key ID from HTTP headers.</description></item>
/// <item><description>Validates the request timestamp is within the configured tolerance window.</description></item>
/// <item><description>Validates the nonce has not been used before (replay protection).</description></item>
/// <item><description>Verifies the HMAC signature using <see cref="IRequestSigner"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// Requests without <see cref="RequireSignatureAttribute"/> pass through without validation.
/// When no <see cref="HttpContext"/> is available (non-HTTP scenarios), the behavior skips
/// validation to allow the same request types to be used in background jobs or tests.
/// </para>
/// <para>
/// When <see cref="AntiTamperingOptions.EnableTracing"/> is <c>true</c>, operations emit
/// OpenTelemetry activities via the <c>Encina.Security.AntiTampering</c> ActivitySource with
/// parent and child spans for each validation stage.
/// When <see cref="AntiTamperingOptions.EnableMetrics"/> is <c>true</c>, counters and
/// histograms are emitted for monitoring.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Decorate a command to require HMAC validation
/// [RequireSignature]
/// public sealed record ProcessPaymentCommand(decimal Amount) : ICommand;
///
/// // Register in DI
/// services.AddEncinaAntiTampering(options =>
/// {
///     options.AddKey("api-key-v1", "my-secret");
/// });
/// </code>
/// </example>
public sealed class HMACValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const string MetadataKeyRequestType = "requestType";
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageAntiTampering = "antitampering";

    /// <summary>
    /// Cache for reflection lookups of <see cref="RequireSignatureAttribute"/> on request types.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, RequireSignatureAttribute?> AttributeCache = new();

    private readonly IRequestSigner _requestSigner;
    private readonly INonceStore _nonceStore;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AntiTamperingOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HMACValidationPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HMACValidationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="requestSigner">The HMAC signer for signature verification.</param>
    /// <param name="nonceStore">The nonce store for replay protection.</param>
    /// <param name="httpContextAccessor">Accessor to get the current HTTP context.</param>
    /// <param name="options">The anti-tampering configuration options.</param>
    /// <param name="timeProvider">The time provider for timestamp validation.</param>
    /// <param name="logger">The logger for structured diagnostic messages.</param>
    public HMACValidationPipelineBehavior(
        IRequestSigner requestSigner,
        INonceStore nonceStore,
        IHttpContextAccessor httpContextAccessor,
        IOptions<AntiTamperingOptions> options,
        TimeProvider timeProvider,
        ILogger<HMACValidationPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(requestSigner);
        ArgumentNullException.ThrowIfNull(nonceStore);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _requestSigner = requestSigner;
        _nonceStore = nonceStore;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // 1. Check if TRequest has [RequireSignature] (cached)
        var attribute = GetRequireSignatureAttribute();

        if (attribute is null)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // 2. Get HTTP context — skip validation in non-HTTP scenarios
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return await nextStep().ConfigureAwait(false);
        }

        var requestTypeName = typeof(TRequest).Name;

        // Start parent activity for the entire validation flow
        Activity? activity = null;

        if (_options.EnableTracing)
        {
            activity = AntiTamperingDiagnostics.StartVerification(
                requestTypeName, _options.Algorithm.ToString(), _options.RequireNonce && !attribute.SkipReplayProtection);
        }

        var stopwatch = _options.EnableMetrics ? Stopwatch.StartNew() : null;

        AntiTamperingLogMessages.SignatureValidationStarted(_logger, requestTypeName, attribute.KeyId ?? "(any)");

        try
        {
            // 3. Extract headers
            var headers = httpContext.Request.Headers;

            var signature = headers[_options.SignatureHeader].ToString();
            var timestampRaw = headers[_options.TimestampHeader].ToString();
            var nonce = headers[_options.NonceHeader].ToString();
            var keyId = headers[_options.KeyIdHeader].ToString();

            // 4. Validate required headers
            if (string.IsNullOrWhiteSpace(signature))
            {
                return Fail(activity, stopwatch, requestTypeName, keyId,
                    "signature_missing",
                    AntiTamperingErrors.SignatureMissing(_options.SignatureHeader));
            }

            if (string.IsNullOrWhiteSpace(keyId))
            {
                return Fail(activity, stopwatch, requestTypeName, string.Empty,
                    "key_id_missing",
                    AntiTamperingErrors.SignatureMissing(_options.KeyIdHeader));
            }

            // If attribute restricts to a specific key ID, enforce it
            if (attribute.KeyId is not null &&
                !string.Equals(keyId, attribute.KeyId, StringComparison.Ordinal))
            {
                AntiTamperingLogMessages.KeyNotFound(_logger, keyId);

                return Fail(activity, stopwatch, requestTypeName, keyId,
                    "key_mismatch",
                    AntiTamperingErrors.KeyNotFound(keyId));
            }

            // 5. Validate timestamp
            Activity? timestampActivity = null;

            if (_options.EnableTracing)
            {
                timestampActivity = AntiTamperingDiagnostics.StartTimestampValidation();
            }

            try
            {
                if (string.IsNullOrWhiteSpace(timestampRaw) ||
                    !DateTimeOffset.TryParse(timestampRaw, out var timestamp))
                {
                    timestampActivity?.SetStatus(ActivityStatusCode.Error, "invalid_format");

                    return Fail(activity, stopwatch, requestTypeName, keyId,
                        "timestamp_invalid",
                        AntiTamperingErrors.TimestampExpired(DateTimeOffset.MinValue, _options.TimestampToleranceMinutes));
                }

                var now = _timeProvider.GetUtcNow();
                var age = now - timestamp;

                if (Math.Abs(age.TotalMinutes) > _options.TimestampToleranceMinutes)
                {
                    timestampActivity?.SetStatus(ActivityStatusCode.Error, "timestamp_expired");

                    AntiTamperingLogMessages.TimestampExpired(
                        _logger,
                        timestamp.ToString("O"),
                        _options.TimestampToleranceMinutes,
                        now.ToString("O"));

                    return Fail(activity, stopwatch, requestTypeName, keyId,
                        "timestamp_expired",
                        AntiTamperingErrors.TimestampExpired(timestamp, _options.TimestampToleranceMinutes));
                }

                timestampActivity?.SetStatus(ActivityStatusCode.Ok);

                // 6. Validate nonce (replay protection)
                if (_options.RequireNonce && !attribute.SkipReplayProtection)
                {
                    Activity? nonceActivity = null;

                    if (_options.EnableTracing)
                    {
                        nonceActivity = AntiTamperingDiagnostics.StartNonceValidation();
                    }

                    try
                    {
                        if (string.IsNullOrWhiteSpace(nonce))
                        {
                            nonceActivity?.SetStatus(ActivityStatusCode.Error, "nonce_missing");

                            return Fail(activity, stopwatch, requestTypeName, keyId,
                                "nonce_missing",
                                AntiTamperingErrors.NonceMissing(_options.NonceHeader));
                        }

                        var nonceExpiry = TimeSpan.FromMinutes(_options.NonceExpiryMinutes);
                        var nonceAdded = await _nonceStore.TryAddAsync(nonce, nonceExpiry, cancellationToken)
                            .ConfigureAwait(false);

                        if (!nonceAdded)
                        {
                            nonceActivity?.SetStatus(ActivityStatusCode.Error, "nonce_reused");

                            var noncePrefix = nonce.Length > 8 ? nonce[..8] : nonce;
                            AntiTamperingLogMessages.NonceRejected(_logger, noncePrefix);

                            if (_options.EnableMetrics)
                            {
                                AntiTamperingDiagnostics.NonceRejectionsTotal.Add(1);
                            }

                            return Fail(activity, stopwatch, requestTypeName, keyId,
                                "nonce_reused",
                                AntiTamperingErrors.NonceReused(nonce));
                        }

                        nonceActivity?.SetStatus(ActivityStatusCode.Ok);
                    }
                    finally
                    {
                        nonceActivity?.Dispose();
                    }
                }

                // 7. Read request body and verify signature
                httpContext.Request.EnableBuffering();
                httpContext.Request.Body.Position = 0;

                using var memoryStream = new MemoryStream();
                await httpContext.Request.Body.CopyToAsync(memoryStream, cancellationToken)
                    .ConfigureAwait(false);

                httpContext.Request.Body.Position = 0;

                var payload = memoryStream.ToArray();

                var signingContext = new SigningContext
                {
                    KeyId = keyId,
                    Nonce = nonce ?? string.Empty,
                    Timestamp = timestamp,
                    HttpMethod = httpContext.Request.Method,
                    RequestPath = httpContext.Request.Path.Value ?? "/"
                };

                var verifyResult = await _requestSigner.VerifyAsync(
                    payload.AsMemory(),
                    signature,
                    signingContext,
                    cancellationToken).ConfigureAwait(false);

                return await verifyResult.MatchAsync<Either<EncinaError, TResponse>>(
                    RightAsync: async isValid =>
                    {
                        if (!isValid)
                        {
                            AntiTamperingLogMessages.SignatureValidationFailed(
                                _logger, keyId, "signature_mismatch", requestTypeName);

                            return Fail(activity, stopwatch, requestTypeName, keyId,
                                "signature_mismatch",
                                AntiTamperingErrors.SignatureInvalid(keyId));
                        }

                        RecordValidationSuccess(activity, stopwatch, requestTypeName, keyId);

                        return await nextStep().ConfigureAwait(false);
                    },
                    Left: error =>
                    {
                        return Fail(activity, stopwatch, requestTypeName, keyId,
                            "key_not_found", error);
                    }).ConfigureAwait(false); // NOSONAR S6966
            }
            finally
            {
                timestampActivity?.Dispose();
            }
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <summary>
    /// Records a validation failure in tracing, metrics, and logging, and returns the error.
    /// </summary>
    private Either<EncinaError, TResponse> Fail(
        Activity? activity,
        Stopwatch? stopwatch,
        string requestTypeName,
        string keyId,
        string reason,
        EncinaError error)
    {
        AntiTamperingDiagnostics.RecordFailure(activity, reason);
        RecordValidationFailure(stopwatch, requestTypeName, keyId, reason);

        return Left<EncinaError, TResponse>(error); // NOSONAR S6966: LanguageExt Left is a pure function
    }

    /// <summary>
    /// Records successful validation in activity, metrics, and logging.
    /// </summary>
    private void RecordValidationSuccess(
        Activity? activity,
        Stopwatch? stopwatch,
        string requestTypeName,
        string keyId)
    {
        AntiTamperingDiagnostics.RecordSuccess(activity, keyId);

        if (_options.EnableMetrics && stopwatch is not null)
        {
            stopwatch.Stop();
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            AntiTamperingDiagnostics.ValidationsTotal.Add(1,
                new KeyValuePair<string, object?>(AntiTamperingDiagnostics.TagResult, "success"),
                new KeyValuePair<string, object?>(AntiTamperingDiagnostics.TagKeyId, keyId));

            AntiTamperingDiagnostics.VerificationDuration.Record(durationMs);

            AntiTamperingLogMessages.SignatureValidationSucceeded(_logger, keyId, durationMs);
        }
    }

    /// <summary>
    /// Records a validation failure in metrics and logging.
    /// </summary>
    private void RecordValidationFailure(
        Stopwatch? stopwatch,
        string requestTypeName,
        string keyId,
        string reason)
    {
        if (_options.EnableMetrics)
        {
            if (stopwatch is not null)
            {
                stopwatch.Stop();
                AntiTamperingDiagnostics.VerificationDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
            }

            AntiTamperingDiagnostics.ValidationsTotal.Add(1,
                new KeyValuePair<string, object?>(AntiTamperingDiagnostics.TagResult, "failure"),
                new KeyValuePair<string, object?>(AntiTamperingDiagnostics.TagKeyId, keyId));

            AntiTamperingDiagnostics.FailuresTotal.Add(1,
                new KeyValuePair<string, object?>(AntiTamperingDiagnostics.TagFailureReason, reason));
        }

        AntiTamperingLogMessages.SignatureValidationFailed(_logger, keyId, reason, requestTypeName);
    }

    /// <summary>
    /// Gets the <see cref="RequireSignatureAttribute"/> for <typeparamref name="TRequest"/>,
    /// using a cached lookup to avoid repeated reflection.
    /// </summary>
    private static RequireSignatureAttribute? GetRequireSignatureAttribute()
    {
        return AttributeCache.GetOrAdd(
            typeof(TRequest),
            static type => type.GetCustomAttribute<RequireSignatureAttribute>(inherit: true));
    }
}

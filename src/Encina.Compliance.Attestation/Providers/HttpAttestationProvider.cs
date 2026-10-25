using System.Collections.Frozen;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Diagnostics;
using Encina.Compliance.Attestation.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Attestation.Providers;

/// <summary>
/// Attestation provider that delegates to an external HTTP endpoint.
/// Supports configurable authentication, payload mapping, and receipt parsing.
/// Compatible with services like Sigstore/Rekor or custom attestation APIs.
/// </summary>
public sealed class HttpAttestationProvider : IAuditAttestationProvider
{
    private const int MaxErrorBodyLength = 500;
    private const long MaxResponseContentBytes = 1_048_576; // 1 MB — SEC-7
    private const string DefaultFailureReason = "Remote verification failed.";

    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HttpAttestationProvider> _logger;
    private readonly HttpAttestationOptions _options;

    /// <inheritdoc />
    public string ProviderName => "Http";

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpAttestationProvider"/> class.
    /// </summary>
    public HttpAttestationProvider(
        HttpClient httpClient,
        TimeProvider timeProvider,
        ILogger<HttpAttestationProvider> logger,
        IOptions<HttpAttestationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        _httpClient = httpClient;
        _timeProvider = timeProvider;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, AttestationReceipt>> AttestAsync(
        AuditRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        using var activity = AttestationDiagnostics.StartAttestation(ProviderName, record.RecordType);
        var sw = Stopwatch.StartNew();
        AttestationDiagnostics.AttestationTotal.Add(1,
            new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });

        var contentHash = ContentHasher.ComputeSha256(record.SerializedContent);
        var now = _timeProvider.GetUtcNow();

        var payload = new
        {
            record_id = record.RecordId,
            record_type = record.RecordType,
            content_hash = contentHash,
            occurred_at_utc = record.OccurredAtUtc
        };

        try
        {
            using var request = CreateRequest(_options.AttestEndpointUrl, payload);
            using var response = await _httpClient.SendAsync(request, ct);

            return await ProcessAttestResponseAsync(response, record, contentHash, now, activity, sw, ct);
        }
        catch (HttpRequestException ex)
        {
            AttestationLogMessages.HttpEndpointError(_logger, _options.AttestEndpointUrl, 0);
            AttestationDiagnostics.AttestationFailed.Add(1,
                new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
            AttestationDiagnostics.RecordFailure(activity, "Endpoint unreachable");
            return AttestationErrors.ProviderUnavailable(ProviderName, $"HTTP endpoint unreachable: {ex.Message}");
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            AttestationDiagnostics.RecordFailure(activity, "Cancelled");
            return AttestationErrors.ProviderUnavailable(ProviderName, "Attestation request was cancelled.");
        }
        catch (TaskCanceledException ex)
        {
            AttestationDiagnostics.AttestationFailed.Add(1,
                new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
            AttestationDiagnostics.RecordFailure(activity, "Timeout");
            return AttestationErrors.ProviderUnavailable(ProviderName, $"HTTP attestation endpoint timed out: {ex.Message}");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// A verdict the endpoint actually gave is a <c>Right</c>: <c>IsValid = true</c>, or
    /// <c>IsValid = false</c> for a non-2xx status, a 2xx response without <c>is_valid</c>, an
    /// explicit <c>is_valid: false</c> or a missing <see cref="HttpAttestationOptions.VerifyEndpointUrl"/>.
    /// When no verdict can be read from a 2xx response (empty body, not JSON, unusable content
    /// type or charset, a root that is not an object, or an <c>is_valid</c> that is not a JSON
    /// boolean) the method returns <c>Left</c> with
    /// <see cref="AttestationErrors.ProviderUnavailableCode"/> because the answer is unknown, not negative.
    /// </remarks>
    public async ValueTask<Either<EncinaError, AttestationVerification>> VerifyAsync(
        AttestationReceipt receipt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        using var activity = AttestationDiagnostics.StartVerification(ProviderName);
        AttestationDiagnostics.VerificationTotal.Add(1,
            new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });

        var now = _timeProvider.GetUtcNow();

        if (_options.VerifyEndpointUrl is null)
        {
            AttestationDiagnostics.RecordFailure(activity, "Verification endpoint not configured");

            return Right<EncinaError, AttestationVerification>(Invalid(receipt, now,
                "Verification endpoint not configured. Set VerifyEndpointUrl to enable remote verification."));
        }

        var payload = new
        {
            attestation_id = receipt.AttestationId,
            audit_record_id = receipt.AuditRecordId,
            content_hash = receipt.ContentHash,
            signature = receipt.Signature
        };

        try
        {
            using var request = CreateRequest(_options.VerifyEndpointUrl, payload);
            using var response = await _httpClient.SendAsync(request, ct);

            return await ProcessVerifyResponseAsync(response, receipt, now, activity, ct);
        }
        catch (HttpRequestException ex)
        {
            AttestationDiagnostics.RecordFailure(activity, "Verification endpoint unreachable");
            return AttestationErrors.ProviderUnavailable(ProviderName, $"Verification endpoint unreachable: {ex.Message}");
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            AttestationDiagnostics.RecordFailure(activity, "Cancelled");
            return AttestationErrors.ProviderUnavailable(ProviderName, "Verification request was cancelled.");
        }
        catch (TaskCanceledException ex)
        {
            AttestationDiagnostics.RecordFailure(activity, "Timeout");
            return AttestationErrors.ProviderUnavailable(ProviderName, $"Verification endpoint timed out: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, AttestationReceipt>> GetReceiptAsync(
        Guid recordId, CancellationToken ct = default)
    {
        // HTTP provider does not store receipts locally.
        // Use IAttestationReceiptStore for persistent receipt retrieval with this provider.
        return ValueTask.FromResult(
            Left<EncinaError, AttestationReceipt>(AttestationErrors.ReceiptNotFound(recordId, ProviderName)));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<AttestationReceipt>>> GetReceiptsAsync(
        IEnumerable<Guid> recordIds, CancellationToken ct = default)
    {
        // HTTP provider does not store receipts locally.
        // Use IAttestationReceiptStore for persistent receipt retrieval with this provider.
        return ValueTask.FromResult(
            Right<EncinaError, IReadOnlyList<AttestationReceipt>>(System.Array.Empty<AttestationReceipt>()));
    }

    private readonly record struct FieldFailure(string ActivityReason, string Message);

    private enum FieldState
    {
        Missing,
        WrongType,
        Present
    }

    private HttpRequestMessage CreateRequest(Uri url, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };

        if (!string.IsNullOrWhiteSpace(_options.AuthHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", _options.AuthHeader);
        }

        return request;
    }

    private async Task<Either<EncinaError, AttestationReceipt>> ProcessAttestResponseAsync(
        HttpResponseMessage response,
        AuditRecord record,
        string contentHash,
        DateTimeOffset now,
        Activity? activity,
        Stopwatch sw,
        CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            return await FailAttestationStatusAsync(response, activity, ct);
        }

        var parsed = await ReadJsonObjectAsync(response, ct);
        var result = ParseReceipt(parsed, record, contentHash, now, activity);

        if (result.IsRight)
        {
            sw.Stop();
            AttestationDiagnostics.AttestationDuration.Record(sw.Elapsed.TotalMilliseconds,
                new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
            AttestationDiagnostics.AttestationSucceeded.Add(1,
                new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
            AttestationLogMessages.AttestationCreated(_logger, record.RecordId, ProviderName);
            AttestationDiagnostics.RecordSuccess(activity);
        }

        return result;
    }

    private async Task<Either<EncinaError, AttestationReceipt>> FailAttestationStatusAsync(
        HttpResponseMessage response, Activity? activity, CancellationToken ct)
    {
        var fullBody = await response.Content.ReadAsStringAsync(ct);
        var truncatedBody = fullBody.Length > MaxErrorBodyLength
            ? fullBody[..MaxErrorBodyLength] + "…"
            : fullBody;

        AttestationLogMessages.HttpEndpointError(_logger, _options.AttestEndpointUrl, (int)response.StatusCode);
        _logger.LogDebug("HTTP attestation error body: {Body}", fullBody);

        AttestationDiagnostics.AttestationFailed.Add(1,
            new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
        AttestationDiagnostics.RecordFailure(activity, $"HTTP {(int)response.StatusCode}");
        return AttestationErrors.HttpEndpointError(
            _options.AttestEndpointUrl, (int)response.StatusCode, truncatedBody);
    }

    private Either<EncinaError, AttestationReceipt> ParseReceipt(
        (JsonElement Root, string? Error) parsed,
        AuditRecord record,
        string contentHash,
        DateTimeOffset now,
        Activity? activity)
    {
        if (parsed.Error is not null)
        {
            return FailAttestation(activity, "Malformed response", parsed.Error);
        }

        // SEC-3: reject incomplete responses — fabricated receipts are worse than no receipt
        if (ReadAttestationId(parsed.Root, out var attestationId) is { } idFailure)
        {
            return FailAttestation(activity, idFailure.ActivityReason, idFailure.Message);
        }

        if (ReadSignature(parsed.Root, out var signature) is { } signatureFailure)
        {
            return FailAttestation(activity, signatureFailure.ActivityReason, signatureFailure.Message);
        }

        return new AttestationReceipt
        {
            AttestationId = attestationId,
            AuditRecordId = record.RecordId,
            ContentHash = contentHash,
            AttestedAtUtc = now,
            ProviderName = ProviderName,
            Signature = signature,
            CorrelationId = record.CorrelationId,
            ProofMetadata = ExtractProofMetadata(parsed.Root)
        };
    }

    private async Task<Either<EncinaError, AttestationVerification>> ProcessVerifyResponseAsync(
        HttpResponseMessage response,
        AttestationReceipt receipt,
        DateTimeOffset now,
        Activity? activity,
        CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            AttestationDiagnostics.RecordFailure(activity, $"HTTP {(int)response.StatusCode}");

            return Right<EncinaError, AttestationVerification>(Invalid(receipt, now,
                $"Verification endpoint returned HTTP {(int)response.StatusCode}."));
        }

        // SEC-7: reject responses that exceed 1 MB to prevent memory exhaustion
        if (response.Content.Headers.ContentLength is { } contentLength
            && contentLength > MaxResponseContentBytes)
        {
            AttestationDiagnostics.RecordFailure(activity, "Response too large");
            return AttestationErrors.HttpResponseTooLarge(
                ProviderName, contentLength, MaxResponseContentBytes);
        }

        var parsed = await ReadJsonObjectAsync(response, ct);
        return BuildVerification(parsed, receipt, now, activity);
    }

    private Either<EncinaError, AttestationVerification> BuildVerification(
        (JsonElement Root, string? Error) parsed,
        AttestationReceipt receipt,
        DateTimeOffset now,
        Activity? activity)
    {
        if (parsed.Error is not null)
        {
            return FailVerification(activity, parsed.Error);
        }

        if (!TryReadVerdict(parsed.Root, out var isValid))
        {
            return FailVerification(activity, WrongTypeMessage("is_valid"));
        }

        AttestationLogMessages.VerificationCompleted(_logger, receipt.AuditRecordId, isValid, ProviderName);

        if (isValid)
            AttestationDiagnostics.RecordSuccess(activity);
        else
            AttestationDiagnostics.RecordFailure(activity, "Remote verification failed");

        return Right<EncinaError, AttestationVerification>(new AttestationVerification
        {
            IsValid = isValid,
            VerifiedAtUtc = now,
            FailureReason = isValid ? null : ReadFailureReason(parsed.Root),
            AttestationId = receipt.AttestationId,
            ProviderName = ProviderName
        });
    }

    private AttestationVerification Invalid(AttestationReceipt receipt, DateTimeOffset now, string reason) => new()
    {
        IsValid = false,
        VerifiedAtUtc = now,
        FailureReason = reason,
        AttestationId = receipt.AttestationId,
        ProviderName = ProviderName
    };

    private static string WrongTypeMessage(string field) =>
        $"Attestation endpoint returned a response whose '{field}' has an unexpected type.";

    private EncinaError FailAttestation(Activity? activity, string activityReason, string message)
    {
        AttestationDiagnostics.AttestationFailed.Add(1,
            new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
        AttestationDiagnostics.RecordFailure(activity, activityReason);
        return AttestationErrors.ProviderUnavailable(ProviderName, message);
    }

    private EncinaError FailVerification(Activity? activity, string message)
    {
        AttestationDiagnostics.RecordFailure(activity, "Malformed response");
        return AttestationErrors.ProviderUnavailable(ProviderName, message);
    }

    /// <summary>
    /// Reads the response body as a JSON object. Returns a constant error text (never the
    /// exception message) when the body is empty, not JSON, undecodable or not an object.
    /// </summary>
    private static async Task<(JsonElement Root, string? Error)> ReadJsonObjectAsync(
        HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var root = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            return root.ValueKind == JsonValueKind.Object
                ? (root, null)
                : (default, "Attestation endpoint returned a response that is not a JSON object.");
        }
        // InvalidOperationException: HttpContent rejects an unknown charset in the Content-Type header
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            return (default, $"Attestation endpoint returned a malformed response ({ex.GetType().Name}).");
        }
    }

    private static FieldState ReadString(JsonElement root, string name, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
        {
            return FieldState.Missing;
        }

        if (prop.ValueKind != JsonValueKind.String)
        {
            return FieldState.WrongType;
        }

        value = prop.GetString();
        return FieldState.Present;
    }

    private static FieldFailure? ReadAttestationId(JsonElement root, out Guid attestationId)
    {
        attestationId = default;
        var state = ReadString(root, "attestation_id", out var text);
        if (state == FieldState.WrongType)
        {
            return new FieldFailure("Malformed attestation_id", WrongTypeMessage("attestation_id"));
        }

        if (state == FieldState.Missing || !Guid.TryParse(text, out attestationId))
        {
            return new FieldFailure("Missing attestation_id",
                "External attestation endpoint returned a response missing 'attestation_id'. "
                + "An attestation receipt without a real external identifier provides no compliance value.");
        }

        return null;
    }

    private static FieldFailure? ReadSignature(JsonElement root, out string signature)
    {
        signature = string.Empty;
        var state = ReadString(root, "signature", out var text);
        if (state == FieldState.WrongType)
        {
            return new FieldFailure("Malformed signature", WrongTypeMessage("signature"));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new FieldFailure("Missing signature",
                "External attestation endpoint returned a response missing 'signature'. "
                + "An attestation receipt without a real external signature provides no compliance value.");
        }

        signature = text;
        return null;
    }

    // An absent is_valid keeps its documented meaning (not valid); a present value must be a JSON boolean.
    private static bool TryReadVerdict(JsonElement root, out bool isValid)
    {
        isValid = false;
        if (!root.TryGetProperty("is_valid", out var prop))
        {
            return true;
        }

        isValid = prop.ValueKind == JsonValueKind.True;
        return isValid || prop.ValueKind == JsonValueKind.False;
    }

    private static string ReadFailureReason(JsonElement root) =>
        ReadString(root, "failure_reason", out var reason) == FieldState.Present && reason is not null
            ? reason
            : DefaultFailureReason;

    private static FrozenDictionary<string, string> ExtractProofMetadata(JsonElement json)
    {
        var metadata = new Dictionary<string, string> { ["transport"] = "http" };

        if (json.TryGetProperty("proof_metadata", out var pm) && pm.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in pm.EnumerateObject())
            {
                metadata[prop.Name] = prop.Value.ToString();
            }
        }

        return metadata.ToFrozenDictionary();
    }
}

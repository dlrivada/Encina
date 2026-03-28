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
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.AttestEndpointUrl);
            request.Content = JsonContent.Create(payload);

            if (!string.IsNullOrWhiteSpace(_options.AuthHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", _options.AuthHeader);
            }

            using var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
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

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

            // SEC-3: reject incomplete responses — fabricated receipts are worse than no receipt
            if (!responseJson.TryGetProperty("attestation_id", out var aidProp)
                || !Guid.TryParse(aidProp.GetString(), out var attestationId))
            {
                const string msg = "External attestation endpoint returned a response missing 'attestation_id'. "
                    + "An attestation receipt without a real external identifier provides no compliance value.";
                AttestationDiagnostics.AttestationFailed.Add(1,
                    new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
                AttestationDiagnostics.RecordFailure(activity, "Missing attestation_id");
                return AttestationErrors.ProviderUnavailable(ProviderName, msg);
            }

            if (!responseJson.TryGetProperty("signature", out var sigProp)
                || string.IsNullOrWhiteSpace(sigProp.GetString()))
            {
                const string msg = "External attestation endpoint returned a response missing 'signature'. "
                    + "An attestation receipt without a real external signature provides no compliance value.";
                AttestationDiagnostics.AttestationFailed.Add(1,
                    new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
                AttestationDiagnostics.RecordFailure(activity, "Missing signature");
                return AttestationErrors.ProviderUnavailable(ProviderName, msg);
            }

            var receipt = new AttestationReceipt
            {
                AttestationId = attestationId,
                AuditRecordId = record.RecordId,
                ContentHash = contentHash,
                AttestedAtUtc = now,
                ProviderName = ProviderName,
                Signature = sigProp.GetString()!,
                CorrelationId = record.CorrelationId,
                ProofMetadata = ExtractProofMetadata(responseJson)
            };

            sw.Stop();
            AttestationDiagnostics.AttestationDuration.Record(sw.Elapsed.TotalMilliseconds,
                new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
            AttestationDiagnostics.AttestationSucceeded.Add(1,
                new TagList { { AttestationDiagnostics.TagProviderName, ProviderName } });
            AttestationLogMessages.AttestationCreated(_logger, record.RecordId, ProviderName);
            AttestationDiagnostics.RecordSuccess(activity);
            return Right<EncinaError, AttestationReceipt>(receipt);
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

            return Right<EncinaError, AttestationVerification>(new AttestationVerification
            {
                IsValid = false,
                VerifiedAtUtc = now,
                FailureReason = "Verification endpoint not configured. Set VerifyEndpointUrl to enable remote verification.",
                AttestationId = receipt.AttestationId,
                ProviderName = ProviderName
            });
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
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.VerifyEndpointUrl);
            request.Content = JsonContent.Create(payload);

            if (!string.IsNullOrWhiteSpace(_options.AuthHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", _options.AuthHeader);
            }

            using var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                AttestationDiagnostics.RecordFailure(activity, $"HTTP {(int)response.StatusCode}");

                return Right<EncinaError, AttestationVerification>(new AttestationVerification
                {
                    IsValid = false,
                    VerifiedAtUtc = now,
                    FailureReason = $"Verification endpoint returned HTTP {(int)response.StatusCode}.",
                    AttestationId = receipt.AttestationId,
                    ProviderName = ProviderName
                });
            }

            // SEC-7: reject responses that exceed 1 MB to prevent memory exhaustion
            if (response.Content.Headers.ContentLength is { } contentLength
                && contentLength > MaxResponseContentBytes)
            {
                AttestationDiagnostics.RecordFailure(activity, "Response too large");
                return AttestationErrors.HttpResponseTooLarge(
                ProviderName, contentLength, MaxResponseContentBytes);
            }

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var isValid = responseJson.TryGetProperty("is_valid", out var validProp) && validProp.GetBoolean();

            AttestationLogMessages.VerificationCompleted(_logger, receipt.AuditRecordId, isValid, ProviderName);

            if (isValid)
                AttestationDiagnostics.RecordSuccess(activity);
            else
                AttestationDiagnostics.RecordFailure(activity, "Remote verification failed");


            return Right<EncinaError, AttestationVerification>(new AttestationVerification
            {
                IsValid = isValid,
                VerifiedAtUtc = now,
                FailureReason = isValid
                    ? null
                    : responseJson.TryGetProperty("failure_reason", out var frProp)
                        ? frProp.GetString()
                        : "Remote verification failed.",
                AttestationId = receipt.AttestationId,
                ProviderName = ProviderName
            });
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

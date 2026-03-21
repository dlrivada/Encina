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
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                AttestationLogMessages.HttpEndpointError(_logger, _options.AttestEndpointUrl, (int)response.StatusCode);
                return EncinaError.New(
                    $"HTTP attestation endpoint returned {(int)response.StatusCode}: {errorBody}");
            }

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

            var receipt = new AttestationReceipt
            {
                AttestationId = responseJson.TryGetProperty("attestation_id", out var aidProp)
                    && Guid.TryParse(aidProp.GetString(), out var aid)
                        ? aid
                        : Guid.NewGuid(),
                AuditRecordId = record.RecordId,
                ContentHash = contentHash,
                AttestedAtUtc = now,
                ProviderName = ProviderName,
                Signature = responseJson.TryGetProperty("signature", out var sigProp)
                    ? sigProp.GetString() ?? ContentHasher.ComputeSha256($"{contentHash}:{now:O}")
                    : ContentHasher.ComputeSha256($"{contentHash}:{now:O}"),
                ProofMetadata = ExtractProofMetadata(responseJson)
            };

            AttestationLogMessages.AttestationCreated(_logger, record.RecordId, ProviderName);
            return Right<EncinaError, AttestationReceipt>(receipt);
        }
        catch (HttpRequestException ex)
        {
            AttestationLogMessages.HttpEndpointError(_logger, _options.AttestEndpointUrl, 0);
            return EncinaError.New("HTTP attestation endpoint unreachable.", ex);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            return EncinaError.New("Attestation request was cancelled.");
        }
        catch (TaskCanceledException ex)
        {
            return EncinaError.New("HTTP attestation endpoint timed out.", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, AttestationVerification>> VerifyAsync(
        AttestationReceipt receipt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        var now = _timeProvider.GetUtcNow();

        if (_options.VerifyEndpointUrl is null)
        {
            return Right<EncinaError, AttestationVerification>(new AttestationVerification
            {
                IsValid = true,
                VerifiedAtUtc = now,
                FailureReason = null
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
                return Right<EncinaError, AttestationVerification>(new AttestationVerification
                {
                    IsValid = false,
                    VerifiedAtUtc = now,
                    FailureReason = $"Verification endpoint returned HTTP {(int)response.StatusCode}."
                });
            }

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var isValid = responseJson.TryGetProperty("is_valid", out var validProp) && validProp.GetBoolean();

            AttestationLogMessages.VerificationCompleted(_logger, receipt.AuditRecordId, isValid, ProviderName);

            return Right<EncinaError, AttestationVerification>(new AttestationVerification
            {
                IsValid = isValid,
                VerifiedAtUtc = now,
                FailureReason = isValid
                    ? null
                    : responseJson.TryGetProperty("failure_reason", out var frProp)
                        ? frProp.GetString()
                        : "Remote verification failed."
            });
        }
        catch (HttpRequestException ex)
        {
            return EncinaError.New("Verification endpoint unreachable.", ex);
        }
    }

    private static Dictionary<string, string> ExtractProofMetadata(JsonElement json)
    {
        var metadata = new Dictionary<string, string> { ["transport"] = "http" };

        if (json.TryGetProperty("proof_metadata", out var pm) && pm.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in pm.EnumerateObject())
            {
                metadata[prop.Name] = prop.Value.ToString();
            }
        }

        return metadata;
    }
}

using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.HMAC;
using LanguageExt;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Security.AntiTampering.Http;

/// <summary>
/// Default implementation of <see cref="IRequestSigningClient"/> that signs outgoing
/// HTTP requests with HMAC signatures.
/// </summary>
/// <remarks>
/// <para>
/// Reads the request body, generates a nonce and timestamp, computes the HMAC signature
/// via <see cref="IRequestSigner"/>, and attaches all required headers to the request.
/// </para>
/// <para>
/// Thread-safe: All operations are stateless beyond injected dependencies.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var client = serviceProvider.GetRequiredService&lt;IRequestSigningClient&gt;();
///
/// var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
/// {
///     Content = JsonContent.Create(new { Amount = 99.99m })
/// };
///
/// var result = await client.SignRequestAsync(request, "api-key-v1");
/// // Headers X-Signature, X-Timestamp, X-Nonce, X-Key-Id are now set
/// </code>
/// </example>
public sealed class RequestSigningClient : IRequestSigningClient
{
    private readonly IRequestSigner _requestSigner;
    private readonly AntiTamperingOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestSigningClient"/> class.
    /// </summary>
    /// <param name="requestSigner">The HMAC signer for computing signatures.</param>
    /// <param name="options">The anti-tampering configuration options.</param>
    /// <param name="timeProvider">The time provider for timestamp generation.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public RequestSigningClient(
        IRequestSigner requestSigner,
        IOptions<AntiTamperingOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(requestSigner);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _requestSigner = requestSigner;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, HttpRequestMessage>> SignRequestAsync(
        HttpRequestMessage request,
        string keyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        // 1. Read request body
        byte[] payload;

        if (request.Content is not null)
        {
            payload = await request.Content.ReadAsByteArrayAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            payload = [];
        }

        // 2. Generate nonce
        var nonce = Guid.NewGuid().ToString("N");

        // 3. Get current timestamp
        var timestamp = _timeProvider.GetUtcNow();

        // 4. Build signing context
        var method = request.Method.Method.ToUpperInvariant();
        var path = request.RequestUri?.PathAndQuery ?? "/";

        var signingContext = new SigningContext
        {
            KeyId = keyId,
            Nonce = nonce,
            Timestamp = timestamp,
            HttpMethod = method,
            RequestPath = path
        };

        // 5. Compute HMAC signature
        var signResult = await _requestSigner.SignAsync(
            payload.AsMemory(),
            signingContext,
            cancellationToken).ConfigureAwait(false);

        return signResult.Match<Either<EncinaError, HttpRequestMessage>>(
            Right: signature =>
            {
                // 6. Add headers
                request.Headers.Remove(_options.SignatureHeader);
                request.Headers.Remove(_options.TimestampHeader);
                request.Headers.Remove(_options.NonceHeader);
                request.Headers.Remove(_options.KeyIdHeader);

                request.Headers.TryAddWithoutValidation(_options.SignatureHeader, signature);
                request.Headers.TryAddWithoutValidation(_options.TimestampHeader, timestamp.ToString("O"));
                request.Headers.TryAddWithoutValidation(_options.NonceHeader, nonce);
                request.Headers.TryAddWithoutValidation(_options.KeyIdHeader, keyId);

                return Right(request);
            },
            Left: Left<EncinaError, HttpRequestMessage>);
    }
}

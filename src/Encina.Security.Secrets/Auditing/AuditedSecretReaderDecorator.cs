using Encina.Security.Audit;
using Encina.Security.Secrets.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.Auditing;

/// <summary>
/// Decorator that records audit entries for secret read operations.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="SecretsOptions.EnableAccessAuditing"/> is <c>true</c>, creates an
/// <see cref="AuditEntry"/> for each secret read attempt, recording the user, outcome,
/// and timing information via <see cref="IAuditStore"/>.
/// </para>
/// <para>
/// <b>Resilience:</b> Audit failures are logged but never affect the secret retrieval result.
/// A secret read should always succeed even if auditing fails.
/// </para>
/// </remarks>
public sealed class AuditedSecretReaderDecorator : ISecretReader
{
    private readonly ISecretReader _inner;
    private readonly IAuditStore _auditStore;
    private readonly IRequestContext _requestContext;
    private readonly SecretsOptions _options;
    private readonly ILogger<AuditedSecretReaderDecorator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AuditedSecretReaderDecorator"/>.
    /// </summary>
    /// <param name="inner">The inner secret reader to delegate to.</param>
    /// <param name="auditStore">The audit store for recording access entries.</param>
    /// <param name="requestContext">The current request context for user information.</param>
    /// <param name="options">The secrets options controlling auditing behavior.</param>
    /// <param name="logger">The logger instance.</param>
    public AuditedSecretReaderDecorator(
        ISecretReader inner,
        IAuditStore auditStore,
        IRequestContext requestContext,
        SecretsOptions options,
        ILogger<AuditedSecretReaderDecorator> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _auditStore = auditStore;
        _requestContext = requestContext;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableAccessAuditing)
        {
            return await _inner.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        }

        var startedAt = DateTimeOffset.UtcNow;
        var result = await _inner.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        var completedAt = DateTimeOffset.UtcNow;

        await RecordAuditEntryAsync("SecretAccess", secretName, result.IsRight, result, startedAt, completedAt, cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, T>> GetSecretAsync<T>(
        string secretName,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.EnableAccessAuditing)
        {
            return await _inner.GetSecretAsync<T>(secretName, cancellationToken).ConfigureAwait(false);
        }

        var startedAt = DateTimeOffset.UtcNow;
        var result = await _inner.GetSecretAsync<T>(secretName, cancellationToken).ConfigureAwait(false);
        var completedAt = DateTimeOffset.UtcNow;

        await RecordAuditEntryAsync("SecretAccess", secretName, result.IsRight, null, startedAt, completedAt, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private async ValueTask RecordAuditEntryAsync(
        string action,
        string secretName,
        bool isSuccess,
        Either<EncinaError, string>? errorResult,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            string? errorMessage = null;
            if (!isSuccess && errorResult.HasValue)
            {
                errorMessage = errorResult.Value.MatchUnsafe(Right: _ => (string?)null, Left: e => e.Message);
            }

            var entry = new AuditEntry
            {
                Id = Guid.NewGuid(),
                CorrelationId = _requestContext.CorrelationId,
                UserId = _requestContext.UserId,
                TenantId = _requestContext.TenantId,
                Action = action,
                EntityType = "Secret",
                EntityId = secretName,
                Outcome = isSuccess ? AuditOutcome.Success : AuditOutcome.Failure,
                ErrorMessage = errorMessage,
                TimestampUtc = completedAt.UtcDateTime,
                StartedAtUtc = startedAt,
                CompletedAtUtc = completedAt,
                Metadata = new Dictionary<string, object?>
                {
                    ["secretName"] = secretName,
                    ["result"] = isSuccess ? "success" : "failure"
                }
            };

            var auditResult = await _auditStore.RecordAsync(entry, cancellationToken).ConfigureAwait(false);
            auditResult.Match(
                Right: _ => Log.AuditEntryRecorded(_logger, secretName),
                Left: _ => Log.AuditEntryFailed(_logger, secretName, new InvalidOperationException("Audit store returned error")));
        }
        catch (Exception ex)
        {
            // Audit failures must never block secret operations
            Log.AuditEntryFailed(_logger, secretName, ex);
        }
    }
}

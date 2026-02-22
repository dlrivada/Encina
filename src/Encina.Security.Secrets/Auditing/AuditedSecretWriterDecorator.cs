using Encina.Security.Audit;
using Encina.Security.Secrets.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.Auditing;

/// <summary>
/// Decorator that records audit entries for secret write operations.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="SecretsOptions.EnableAccessAuditing"/> is <c>true</c>, creates an
/// <see cref="AuditEntry"/> for each secret write attempt with <c>Action = "SecretWrite"</c>.
/// </para>
/// <para>
/// <b>Resilience:</b> Audit failures are logged but never affect the write result.
/// </para>
/// </remarks>
public sealed class AuditedSecretWriterDecorator : ISecretWriter
{
    private readonly ISecretWriter _inner;
    private readonly IAuditStore _auditStore;
    private readonly IRequestContext _requestContext;
    private readonly SecretsOptions _options;
    private readonly ILogger<AuditedSecretWriterDecorator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AuditedSecretWriterDecorator"/>.
    /// </summary>
    /// <param name="inner">The inner secret writer to delegate to.</param>
    /// <param name="auditStore">The audit store for recording write entries.</param>
    /// <param name="requestContext">The current request context for user information.</param>
    /// <param name="options">The secrets options controlling auditing behavior.</param>
    /// <param name="logger">The logger instance.</param>
    public AuditedSecretWriterDecorator(
        ISecretWriter inner,
        IAuditStore auditStore,
        IRequestContext requestContext,
        SecretsOptions options,
        ILogger<AuditedSecretWriterDecorator> logger)
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
    public async ValueTask<Either<EncinaError, Unit>> SetSecretAsync(
        string secretName,
        string value,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableAccessAuditing)
        {
            return await _inner.SetSecretAsync(secretName, value, cancellationToken).ConfigureAwait(false);
        }

        var startedAt = DateTimeOffset.UtcNow;
        var result = await _inner.SetSecretAsync(secretName, value, cancellationToken).ConfigureAwait(false);
        var completedAt = DateTimeOffset.UtcNow;

        await RecordAuditEntryAsync(secretName, result.IsRight, result, startedAt, completedAt, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private async ValueTask RecordAuditEntryAsync(
        string secretName,
        bool isSuccess,
        Either<EncinaError, Unit> result,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            string? errorMessage = null;
            errorMessage = result.MatchUnsafe(Right: _ => (string?)null, Left: e => e.Message);

            var entry = new AuditEntry
            {
                Id = Guid.NewGuid(),
                CorrelationId = _requestContext.CorrelationId,
                UserId = _requestContext.UserId,
                TenantId = _requestContext.TenantId,
                Action = "SecretWrite",
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
            Log.AuditEntryFailed(_logger, secretName, ex);
        }
    }
}

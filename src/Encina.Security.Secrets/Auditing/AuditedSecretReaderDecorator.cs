using Encina.Security.Audit;
using Encina.Security.Secrets.Abstractions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.Auditing;

/// <summary>
/// Decorator that records audit entries for secret read operations.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="SecretsOptions.EnableAccessAuditing"/> is <c>true</c>, creates an
/// <see cref="OperationAuditEntry"/> for each secret read attempt, recording the user, outcome,
/// and timing information via <see cref="IOperationAuditStore"/>.
/// </para>
/// <para>
/// <b>Resilience:</b> Audit failures are logged but never affect the secret retrieval result.
/// A secret read should always succeed even if auditing fails.
/// </para>
/// </remarks>
public sealed class AuditedSecretReaderDecorator : ISecretReader
{
    private readonly ISecretReader _inner;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly SecretsOptions _options;
    private readonly ILogger<AuditedSecretReaderDecorator> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="AuditedSecretReaderDecorator"/>.
    /// </summary>
    /// <param name="inner">The inner secret reader to delegate to.</param>
    /// <param name="scopeFactory">
    /// Scope factory used to resolve the <see cref="IOperationAuditStore"/> for each audit write
    /// (this decorator is a singleton and database stores are scoped).
    /// </param>
    /// <param name="requestContextAccessor">
    /// Accessor for the ambient request context, read at the moment each audit entry is recorded
    /// (this decorator is registered as a singleton, so the context cannot be captured once at
    /// construction time).
    /// </param>
    /// <param name="options">The secrets options controlling auditing behavior.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">Optional time provider for the entry timestamps. Defaults to system time.</param>
    public AuditedSecretReaderDecorator(
        ISecretReader inner,
        IServiceScopeFactory scopeFactory,
        IRequestContextAccessor requestContextAccessor,
        SecretsOptions options,
        ILogger<AuditedSecretReaderDecorator> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(requestContextAccessor);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _scopeFactory = scopeFactory;
        _requestContextAccessor = requestContextAccessor;
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
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

        var startedAt = _timeProvider.GetUtcNow();
        var result = await _inner.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        var completedAt = _timeProvider.GetUtcNow();

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

        var startedAt = _timeProvider.GetUtcNow();
        var result = await _inner.GetSecretAsync<T>(secretName, cancellationToken).ConfigureAwait(false);
        var completedAt = _timeProvider.GetUtcNow();

        await RecordAuditEntryAsync("SecretAccess", secretName, result.IsRight, null, startedAt, completedAt, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private ValueTask RecordAuditEntryAsync(
        string action,
        string secretName,
        bool isSuccess,
        Either<EncinaError, string>? errorResult,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
        => SecretAuditRecorder.RecordAsync(
            _scopeFactory, _requestContextAccessor, _logger, action, secretName, isSuccess,
            errorResult, startedAt, completedAt, cancellationToken);
}

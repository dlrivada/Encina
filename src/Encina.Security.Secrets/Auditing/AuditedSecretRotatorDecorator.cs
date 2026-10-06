using Encina.Security.Audit;
using Encina.Security.Secrets.Abstractions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.Auditing;

/// <summary>
/// Decorator that records audit entries for secret rotation operations.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="SecretsOptions.EnableAccessAuditing"/> is <c>true</c>, creates an
/// <see cref="OperationAuditEntry"/> for each secret rotation attempt with <c>Action = "SecretRotation"</c>.
/// </para>
/// <para>
/// <b>Resilience:</b> Audit failures are logged but never affect the rotation result.
/// </para>
/// </remarks>
public sealed class AuditedSecretRotatorDecorator : ISecretRotator
{
    private readonly ISecretRotator _inner;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly SecretsOptions _options;
    private readonly ILogger<AuditedSecretRotatorDecorator> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="AuditedSecretRotatorDecorator"/>.
    /// </summary>
    /// <param name="inner">The inner secret rotator to delegate to.</param>
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
    public AuditedSecretRotatorDecorator(
        ISecretRotator inner,
        IServiceScopeFactory scopeFactory,
        IRequestContextAccessor requestContextAccessor,
        SecretsOptions options,
        ILogger<AuditedSecretRotatorDecorator> logger,
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
    public async ValueTask<Either<EncinaError, Unit>> RotateSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableAccessAuditing)
        {
            return await _inner.RotateSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        }

        var startedAt = _timeProvider.GetUtcNow();
        var result = await _inner.RotateSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        var completedAt = _timeProvider.GetUtcNow();

        await RecordAuditEntryAsync(secretName, result.IsRight, result, startedAt, completedAt, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private ValueTask RecordAuditEntryAsync(
        string secretName,
        bool isSuccess,
        Either<EncinaError, Unit> result,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
        => SecretAuditRecorder.RecordAsync<Unit>(
            _scopeFactory, _requestContextAccessor, _logger, "SecretRotation", secretName, isSuccess,
            result, startedAt, completedAt, cancellationToken);
}

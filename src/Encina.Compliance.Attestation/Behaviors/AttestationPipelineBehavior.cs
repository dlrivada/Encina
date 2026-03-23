using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Attributes;
using Encina.Compliance.Attestation.Diagnostics;
using Encina.Compliance.Attestation.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Attestation.Behaviors;

/// <summary>
/// Pipeline behavior that creates tamper-evident attestation receipts for requests decorated
/// with <see cref="AttestDecisionAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior intercepts successful responses from commands or queries marked with
/// <see cref="AttestDecisionAttribute"/> and attests the decision outcome via the configured
/// <see cref="IAuditAttestationProvider"/>.
/// </para>
/// <para>
/// Only <c>Right</c> (successful) results are attested. Failed pipeline outcomes are passed through
/// unchanged to preserve error context.
/// </para>
/// <para>
/// Attestation failure handling is controlled by <see cref="AttestDecisionAttribute.FailureMode"/>:
/// <list type="bullet">
/// <item><see cref="AttestationFailureMode.Enforce"/> — attestation failure blocks the pipeline (returns Left).</item>
/// <item><see cref="AttestationFailureMode.LogOnly"/> — attestation failure is logged as a warning but the result is returned.</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Command that requires attestation
/// [AttestDecision(RecordType = "ApprovalDecision", Reason = "Required for EU AI Act Art. 14")]
/// public sealed record ApproveModelDeploymentCommand(Guid ModelId) : ICommand;
/// </code>
/// </example>
public sealed class AttestationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, AttestDecisionAttribute?> AttributeCache = new();

    /// <summary>
    /// Deterministic serializer options used when building the <see cref="AuditRecord.SerializedContent"/>
    /// snapshot. Property names are sorted alphabetically to guarantee a stable hash regardless of
    /// runtime property order.
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    private readonly IAuditAttestationProvider _provider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AttestationPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttestationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="provider">The attestation provider used to create receipts.</param>
    /// <param name="timeProvider">Time provider for capturing occurrence timestamps.</param>
    /// <param name="logger">Logger for structured log messages.</param>
    public AttestationPipelineBehavior(
        IAuditAttestationProvider provider,
        TimeProvider timeProvider,
        ILogger<AttestationPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _provider = provider;
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
        ArgumentNullException.ThrowIfNull(request);

        var attr = AttributeCache.GetOrAdd(
            typeof(TRequest),
            static t => t.GetCustomAttribute<AttestDecisionAttribute>());

        // No [AttestDecision] attribute — pass through unchanged
        if (attr is null)
            return await nextStep().ConfigureAwait(false);

        var result = await nextStep().ConfigureAwait(false);

        // Only attest successful outcomes; failed pipeline results are returned unchanged
        if (result.IsLeft)
            return result;

        var outcome = result.IfRight(r => (object?)r);
        var serializedContent = JsonSerializer.Serialize(
            new { request, outcome },
            SerializerOptions);

        var record = new AuditRecord
        {
            RecordId = Guid.NewGuid(),
            RecordType = attr.RecordType ?? typeof(TRequest).Name,
            SerializedContent = serializedContent,
            OccurredAtUtc = _timeProvider.GetUtcNow(),
            CorrelationId = context.CorrelationId
        };

        var attestResult = await _provider
            .AttestAsync(record, cancellationToken)
            .ConfigureAwait(false);

        if (attestResult.IsLeft)
        {
            var error = (EncinaError)attestResult;

            if (attr.FailureMode == AttestationFailureMode.Enforce)
            {
                AttestationLogMessages.AttestationEnforced(_logger, typeof(TRequest).Name, error.Message);
                return Left<EncinaError, TResponse>(error);
            }

            AttestationLogMessages.AttestationLogOnly(_logger, typeof(TRequest).Name, error.Message);
        }

        return result;
    }
}

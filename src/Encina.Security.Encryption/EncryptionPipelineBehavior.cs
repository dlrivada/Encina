using System.Diagnostics;
using System.Diagnostics.Metrics;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.Encryption;

/// <summary>
/// Pipeline behavior that provides bidirectional field-level encryption and decryption
/// around CQRS handler execution.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior operates in two phases:
/// <list type="number">
/// <item><description><b>Pre-handler (decrypt)</b>: If the request type or its properties are decorated
/// with <see cref="DecryptOnReceiveAttribute"/>, encrypted property values are decrypted before
/// the handler executes.</description></item>
/// <item><description><b>Post-handler (encrypt)</b>: If the request has properties with <see cref="EncryptAttribute"/>,
/// they are encrypted after the handler returns. If the response type is decorated with
/// <see cref="EncryptedResponseAttribute"/>, response properties are also encrypted.</description></item>
/// </list>
/// </para>
/// <para>
/// The behavior short-circuits on encryption/decryption failure when
/// <see cref="EncryptionAttribute.FailOnError"/> is <c>true</c> (default).
/// </para>
/// <para>
/// <b>Attribute discovery</b>: Uses <see cref="EncryptedPropertyCache"/> for cached, reflection-free
/// property discovery with compiled setters.
/// </para>
/// <para>
/// <b>Observability</b>: Emits OpenTelemetry traces via <c>Encina.Security.Encryption</c> ActivitySource
/// and metrics via <c>Encina.Security.Encryption</c> Meter when enabled via <see cref="EncryptionOptions"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Request with properties that need encryption before persistence
/// public sealed record CreateUserCommand(
///     string Username,
///     [property: Encrypt(Purpose = "User.Email")] string Email,
///     [property: Encrypt(Purpose = "User.SSN")] string SocialSecurityNumber
/// ) : ICommand&lt;UserId&gt;;
///
/// // Request with pre-decryption of incoming encrypted data
/// [DecryptOnReceive]
/// public sealed record ProcessEncryptedPayloadCommand(
///     [property: Encrypt(Purpose = "Payload.Data")] string EncryptedData
/// ) : ICommand;
/// </code>
/// </example>
public sealed class EncryptionPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEncryptionOrchestrator _orchestrator;
    private readonly EncryptionOptions _options;
    private readonly ILogger<EncryptionPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="orchestrator">The encryption orchestrator for field-level operations.</param>
    /// <param name="options">The encryption configuration options.</param>
    /// <param name="logger">The logger for structured logging.</param>
    public EncryptionPipelineBehavior(
        IEncryptionOrchestrator orchestrator,
        IOptions<EncryptionOptions> options,
        ILogger<EncryptionPipelineBehavior<TRequest, TResponse>> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var requestType = typeof(TRequest);
        var requestTypeName = requestType.Name;
        var startedAt = Stopwatch.GetTimestamp();

        // Start tracing if enabled
        using var activity = _options.EnableTracing
            ? EncryptionDiagnostics.StartProcess(requestTypeName)
            : null;

        // --- Pre-handler: Decrypt incoming data if [DecryptOnReceive] is present ---
        if (RequiresDecryptOnReceive(requestType))
        {
            _logger.LogDebug("Pre-handler decryption for {RequestType}", requestTypeName);

            var decryptProperties = EncryptedPropertyCache.GetProperties(requestType);
            var decryptResult = await _orchestrator.DecryptAsync(request, context, cancellationToken)
                .ConfigureAwait(false);

            if (decryptResult.IsLeft)
            {
                RecordFailure(activity, startedAt, "decrypt", requestTypeName,
                    decryptResult.Match(Right: _ => string.Empty, Left: e => e.Message));
                return decryptResult.Match<Either<EncinaError, TResponse>>(
                    Right: _ => default!,
                    Left: e => e);
            }

            RecordOperationEvent(activity, "Decrypt", decryptProperties.Length);
        }

        // --- Pre-handler: Encrypt request properties with [Encrypt] before handler executes ---
        var encryptedProperties = EncryptedPropertyCache.GetProperties(requestType);
        var hasRequestEncryption = encryptedProperties.Length > 0;

        if (hasRequestEncryption)
        {
            _logger.LogDebug("Pre-handler encryption for {RequestType} ({Count} properties)",
                requestTypeName, encryptedProperties.Length);

            var encryptResult = await _orchestrator.EncryptAsync(request, context, cancellationToken)
                .ConfigureAwait(false);

            if (encryptResult.IsLeft)
            {
                RecordFailure(activity, startedAt, "encrypt", requestTypeName,
                    encryptResult.Match(Right: _ => string.Empty, Left: e => e.Message));
                return encryptResult.Match<Either<EncinaError, TResponse>>(
                    Right: _ => default!,
                    Left: e => e);
            }

            RecordOperationEvent(activity, "Encrypt", encryptedProperties.Length);
        }

        var response = await nextStep().ConfigureAwait(false);

        // --- Post-handler: Encrypt response if [EncryptedResponse] is present ---
        if (response.IsRight && RequiresResponseEncryption())
        {
            var responseResult = await response.MatchAsync(
                RightAsync: async responseValue =>
                {
                    _logger.LogDebug("Post-handler response encryption for {ResponseType}", typeof(TResponse).Name);

                    var responseProperties = EncryptedPropertyCache.GetProperties(typeof(TResponse));
                    var encryptResult = await _orchestrator.EncryptAsync(responseValue, context, cancellationToken)
                        .ConfigureAwait(false);

                    if (encryptResult.IsRight)
                    {
                        RecordOperationEvent(activity, "EncryptResponse", responseProperties.Length);
                    }
                    else
                    {
                        RecordFailure(activity, startedAt, "encrypt_response", requestTypeName,
                            encryptResult.Match(Right: _ => string.Empty, Left: e => e.Message));
                    }

                    return encryptResult;
                },
                Left: e => e).ConfigureAwait(false);

            RecordSuccess(activity, startedAt, requestTypeName);
            return responseResult;
        }

        RecordSuccess(activity, startedAt, requestTypeName);
        return response;
    }

    /// <summary>
    /// Records a successful pipeline completion with tracing and metrics.
    /// </summary>
    private void RecordSuccess(Activity? activity, long startedAt, string requestTypeName)
    {
        if (_options.EnableTracing)
        {
            EncryptionDiagnostics.RecordSuccess(activity);
        }

        if (_options.EnableMetrics)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var tags = new TagList
            {
                { EncryptionDiagnostics.TagRequestType, requestTypeName },
                { EncryptionDiagnostics.TagOutcome, "success" }
            };

            EncryptionDiagnostics.OperationsTotal.Add(1, tags);
            EncryptionDiagnostics.OperationDuration.Record(elapsed.TotalMilliseconds, tags);
        }
    }

    /// <summary>
    /// Records a failed pipeline operation with tracing and metrics.
    /// </summary>
    private void RecordFailure(Activity? activity, long startedAt, string operation, string requestTypeName, string errorMessage)
    {
        if (_options.EnableTracing)
        {
            EncryptionDiagnostics.RecordFailure(activity, operation, errorMessage);
        }

        if (_options.EnableMetrics)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var tags = new TagList
            {
                { EncryptionDiagnostics.TagRequestType, requestTypeName },
                { EncryptionDiagnostics.TagOperation, operation },
                { EncryptionDiagnostics.TagOutcome, "failure" }
            };

            EncryptionDiagnostics.OperationsTotal.Add(1, tags);
            EncryptionDiagnostics.FailuresTotal.Add(1, tags);
            EncryptionDiagnostics.OperationDuration.Record(elapsed.TotalMilliseconds, tags);
        }

        _logger.LogWarning("Encryption pipeline {Operation} failed for {RequestType}: {ErrorMessage}",
            operation, requestTypeName, errorMessage);
    }

    /// <summary>
    /// Records an operation event on the activity.
    /// </summary>
    private void RecordOperationEvent(Activity? activity, string operation, int propertyCount)
    {
        if (_options.EnableTracing)
        {
            EncryptionDiagnostics.RecordOperationEvent(activity, operation, propertyCount);
        }
    }

    /// <summary>
    /// Checks if the request type requires pre-handler decryption.
    /// </summary>
    private static bool RequiresDecryptOnReceive(Type requestType) =>
        requestType.GetCustomAttributes(typeof(DecryptOnReceiveAttribute), inherit: true).Length > 0;

    /// <summary>
    /// Checks if the response type requires post-handler encryption.
    /// </summary>
    private static bool RequiresResponseEncryption() =>
        typeof(TResponse).GetCustomAttributes(typeof(EncryptedResponseAttribute), inherit: true).Length > 0;
}

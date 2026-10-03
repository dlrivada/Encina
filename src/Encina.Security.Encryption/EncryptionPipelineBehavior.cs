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

        var requestTypeName = typeof(TRequest).Name;
        var startedAt = Stopwatch.GetTimestamp();

        // Start tracing if enabled
        using var activity = StartActivity(requestTypeName);

        // --- Pre-handler: decrypt [DecryptOnReceive] data, then encrypt [Encrypt] properties ---
        var preHandler = await RunPreHandlerAsync(request, context, activity, startedAt, cancellationToken)
            .ConfigureAwait(false);
        if (preHandler.IsLeft)
        {
            return preHandler.Match<Either<EncinaError, TResponse>>(Right: _ => default!, Left: e => e);
        }

        var response = await nextStep().ConfigureAwait(false);

        // --- Post-handler: Encrypt response if [EncryptedResponse] is present ---
        response = await EncryptResponseWhenRequiredAsync(response, context, activity, startedAt, cancellationToken)
            .ConfigureAwait(false);

        RecordSuccess(activity, startedAt, requestTypeName);
        return response;
    }

    private ValueTask<Either<EncinaError, TResponse>> EncryptResponseWhenRequiredAsync(
        Either<EncinaError, TResponse> response,
        IRequestContext context,
        Activity? activity,
        long startedAt,
        CancellationToken cancellationToken) =>
        response.IsRight && RequiresResponseEncryption()
            ? EncryptResponseAsync(response, context, activity, startedAt, cancellationToken)
            : ValueTask.FromResult(response);

    private Activity? StartActivity(string requestTypeName) =>
        _options.EnableTracing ? EncryptionDiagnostics.StartProcess(requestTypeName) : null;

    private async ValueTask<Either<EncinaError, Unit>> RunPreHandlerAsync(
        TRequest request,
        IRequestContext context,
        Activity? activity,
        long startedAt,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);

        if (RequiresDecryptOnReceive(requestType))
        {
            var decrypted = await DecryptRequestAsync(request, context, activity, startedAt, cancellationToken)
                .ConfigureAwait(false);
            if (decrypted.IsLeft)
            {
                return decrypted;
            }
        }

        if (EncryptedPropertyCache.GetProperties(requestType).Length > 0)
        {
            return await EncryptRequestAsync(request, context, activity, startedAt, cancellationToken)
                .ConfigureAwait(false);
        }

        return Unit.Default;
    }

    private async ValueTask<Either<EncinaError, Unit>> DecryptRequestAsync(
        TRequest request,
        IRequestContext context,
        Activity? activity,
        long startedAt,
        CancellationToken cancellationToken)
    {
        var requestTypeName = typeof(TRequest).Name;
        _logger.LogDebug("Pre-handler decryption for {RequestType}", requestTypeName);

        var properties = EncryptedPropertyCache.GetProperties(typeof(TRequest));
        var result = await _orchestrator.DecryptAsync(request, context, cancellationToken).ConfigureAwait(false);

        return AfterStep(result, activity, startedAt, "decrypt", "Decrypt", properties.Length);
    }

    private async ValueTask<Either<EncinaError, Unit>> EncryptRequestAsync(
        TRequest request,
        IRequestContext context,
        Activity? activity,
        long startedAt,
        CancellationToken cancellationToken)
    {
        var requestTypeName = typeof(TRequest).Name;
        var properties = EncryptedPropertyCache.GetProperties(typeof(TRequest));
        _logger.LogDebug("Pre-handler encryption for {RequestType} ({Count} properties)",
            requestTypeName, properties.Length);

        var result = await _orchestrator.EncryptAsync(request, context, cancellationToken).ConfigureAwait(false);

        return AfterStep(result, activity, startedAt, "encrypt", "Encrypt", properties.Length);
    }

    private async ValueTask<Either<EncinaError, TResponse>> EncryptResponseAsync(
        Either<EncinaError, TResponse> response,
        IRequestContext context,
        Activity? activity,
        long startedAt,
        CancellationToken cancellationToken) =>
        await response.MatchAsync(
            RightAsync: async responseValue =>
            {
                _logger.LogDebug("Post-handler response encryption for {ResponseType}", typeof(TResponse).Name);

                var properties = EncryptedPropertyCache.GetProperties(typeof(TResponse));
                var encrypted = await _orchestrator.EncryptAsync(responseValue, context, cancellationToken)
                    .ConfigureAwait(false);

                AfterStep(encrypted, activity, startedAt, "encrypt_response", "EncryptResponse", properties.Length);
                return encrypted;
            },
            Left: e => e).ConfigureAwait(false);

    /// <summary>
    /// Records the outcome of one orchestrator step (event on success, failure telemetry otherwise)
    /// and maps the result to a unit result.
    /// </summary>
    private Either<EncinaError, Unit> AfterStep<T>(
        Either<EncinaError, T> result,
        Activity? activity,
        long startedAt,
        string failureOperation,
        string eventOperation,
        int propertyCount)
    {
        if (result.IsLeft)
        {
            RecordFailure(activity, startedAt, failureOperation, typeof(TRequest).Name, result);
        }
        else
        {
            RecordOperationEvent(activity, eventOperation, propertyCount);
        }

        return result.Map(_ => Unit.Default);
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
    private void RecordFailure<T>(Activity? activity, long startedAt, string operation, string requestTypeName, Either<EncinaError, T> failed)
    {
        var (errorMessage, errorCode) = failed.Match(
            Right: _ => (string.Empty, string.Empty),
            Left: e => (e.Message, e.GetCode().IfNone("encina.unknown")));

        if (_options.EnableTracing)
        {
            EncryptionDiagnostics.RecordFailure(activity, operation, errorMessage);
        }

        RecordFailureMetrics(startedAt, operation, requestTypeName);

        _logger.LogWarning("Encryption pipeline {Operation} failed for {RequestType}: {ErrorCode}",
            operation, requestTypeName, errorCode);
    }

    private void RecordFailureMetrics(long startedAt, string operation, string requestTypeName)
    {
        if (!_options.EnableMetrics)
        {
            return;
        }

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

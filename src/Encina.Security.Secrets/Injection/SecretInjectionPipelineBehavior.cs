using System.Diagnostics;
using System.Diagnostics.Metrics;
using Encina.Diagnostics;
using Encina.Security.Secrets.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.Secrets.Injection;

/// <summary>
/// Pipeline behavior that injects secrets into request properties decorated with
/// <see cref="InjectSecretAttribute"/> before handler execution.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior operates as a pre-handler phase:
/// <list type="number">
/// <item><description>Discovers properties decorated with <see cref="InjectSecretAttribute"/>
/// via <see cref="SecretPropertyCache"/>.</description></item>
/// <item><description>Delegates to <see cref="SecretInjectionOrchestrator"/> to fetch and inject
/// secret values.</description></item>
/// <item><description>If injection fails for a required secret
/// (<see cref="InjectSecretAttribute.FailOnError"/> is <c>true</c>), the pipeline short-circuits
/// with an error.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Observability</b>: Emits OpenTelemetry traces via <c>Encina.Security.Secrets</c> ActivitySource
/// and metrics via <c>Encina.Security.Secrets</c> Meter when enabled via <see cref="SecretsOptions"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record ProcessPaymentCommand(
///     decimal Amount,
///     [property: InjectSecret("stripe-api-key")] string? StripeKey
/// ) : ICommand&lt;PaymentResult&gt;;
/// </code>
/// </example>
internal sealed class SecretInjectionPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly SecretInjectionOrchestrator _orchestrator;
    private readonly SecretsOptions _options;
    private readonly ILogger<SecretInjectionPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretInjectionPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="orchestrator">The secret injection orchestrator.</param>
    /// <param name="options">The secrets configuration options.</param>
    /// <param name="logger">The logger for structured logging.</param>
    public SecretInjectionPipelineBehavior(
        SecretInjectionOrchestrator orchestrator,
        IOptions<SecretsOptions> options,
        ILogger<SecretInjectionPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _orchestrator = orchestrator;
        _options = options.Value;
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
        ArgumentNullException.ThrowIfNull(context);

        var requestType = typeof(TRequest);
        var requestTypeName = requestType.Name;

        // Quick exit if no injectable properties
        var properties = SecretPropertyCache.GetProperties(requestType);
        if (properties.Length == 0)
        {
            Log.SecretInjectionSkipped(_logger, requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        var startedAt = Stopwatch.GetTimestamp();

        // Start tracing if enabled
        using var activity = _options.EnableTracing
            ? SecretsDiagnostics.StartSecretInjection(requestTypeName)
            : null;

        Log.SecretInjectionStarted(_logger, requestTypeName, properties.Length);

        var failure = await InjectAsync(request, activity, startedAt, requestTypeName, cancellationToken).ConfigureAwait(false);
        if (failure.IsSome)
        {
            return failure.Match<Either<EncinaError, TResponse>>(Some: e => e, None: () => default!);
        }

        return await nextStep().ConfigureAwait(false);
    }

    /// <summary>
    /// Injects the secrets and records the outcome; returns the error when the injection failed.
    /// </summary>
    private async ValueTask<Option<EncinaError>> InjectAsync(
        TRequest request,
        Activity? activity,
        long startedAt,
        string requestTypeName,
        CancellationToken cancellationToken)
    {
        var injectionResult = await _orchestrator.InjectAsync(request, cancellationToken).ConfigureAwait(false);

        return injectionResult.Match(
            Right: injectedCount =>
            {
                RecordSuccess(activity, startedAt, requestTypeName, injectedCount);
                return Option<EncinaError>.None;
            },
            Left: error =>
            {
                RecordFailure(activity, startedAt, requestTypeName, ActivityStatusDescriptions.ForError(error));
                return Option<EncinaError>.Some(error);
            });
    }

    /// <summary>
    /// Records a successful pipeline completion with tracing and metrics.
    /// </summary>
    private void RecordSuccess(Activity? activity, long startedAt, string requestTypeName, int injectedCount)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);

        Log.SecretInjectionCompleted(_logger, requestTypeName, injectedCount, elapsed.TotalMilliseconds);

        if (_options.EnableTracing)
        {
            SecretsDiagnostics.RecordSuccess(activity);
        }

        if (_options.EnableMetrics)
        {
            var tags = new TagList
            {
                { SecretsDiagnostics.TagRequestType, requestTypeName },
                { SecretsDiagnostics.TagOperation, "inject" },
                { SecretsDiagnostics.TagOutcome, "success" }
            };

            SecretsDiagnostics.InjectionsTotal.Add(1, tags);
            SecretsDiagnostics.PropertiesInjected.Add(injectedCount, tags);
            SecretsDiagnostics.OperationDuration.Record(elapsed.TotalMilliseconds, tags);
        }
    }

    /// <summary>
    /// Records a failed pipeline operation with tracing and metrics.
    /// </summary>
    private void RecordFailure(Activity? activity, long startedAt, string requestTypeName, string errorCode)
    {
        if (_options.EnableTracing)
        {
            SecretsDiagnostics.RecordFailure(activity, errorCode);
        }

        if (_options.EnableMetrics)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var tags = new TagList
            {
                { SecretsDiagnostics.TagRequestType, requestTypeName },
                { SecretsDiagnostics.TagOperation, "inject" },
                { SecretsDiagnostics.TagOutcome, "failure" }
            };

            SecretsDiagnostics.InjectionsTotal.Add(1, tags);
            SecretsDiagnostics.FailuresTotal.Add(1, tags);
            SecretsDiagnostics.OperationDuration.Record(elapsed.TotalMilliseconds, tags);
        }
    }
}

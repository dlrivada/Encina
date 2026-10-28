using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Recoverability;

/// <summary>
/// Checks, when the host starts, that delayed retries have what they need: with
/// <see cref="RecoverabilityOptions.EnableDelayedRetries"/> on, an <see cref="IDelayedRetryStore"/> and an
/// <see cref="IDelayedRetryMessageFactory"/> must be registered, otherwise the application fails to start
/// (ADR-050, Critical log with EventId 5457, see EventIdRanges.MessagingRecoverability).
/// </summary>
/// <remarks>
/// <para>
/// The check asks the built container (<see cref="IServiceProviderIsService"/>), so the order in which the store
/// and <c>AddEncinaRecoverability</c> / <c>UseRecoverability</c> were registered never matters. It returns at
/// once when delayed retries are off. The flag alone decides: an empty
/// <see cref="RecoverabilityOptions.DelayedRetries"/> array with the flag on still needs the store.
/// </para>
/// <para>
/// It is registered before <see cref="DelayedRetryProcessor"/>, so a failed start prevents the processor from
/// running. Without a generic host this check never runs; <see cref="DelayedRetryScheduler"/>'s registration
/// then fails when it is resolved, with the same message. Interim implementation of the ADR-050 row
/// "Recoverability / delayed retries"; it migrates into the startup validator of #2382.
/// </para>
/// </remarks>
internal sealed class DelayedRetryStartupCheck : IHostedService
{
    private readonly RecoverabilityOptions _options;
    private readonly IServiceProviderIsService _serviceProviderIsService;
    private readonly ILogger<DelayedRetryStartupCheck> _logger;

    public DelayedRetryStartupCheck(
        RecoverabilityOptions options,
        IServiceProviderIsService serviceProviderIsService,
        ILogger<DelayedRetryStartupCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProviderIsService);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _serviceProviderIsService = serviceProviderIsService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.EnableDelayedRetries)
        {
            return Task.CompletedTask;
        }

        var missing = DescribeMissing(
            _serviceProviderIsService.IsService(typeof(IDelayedRetryStore)),
            _serviceProviderIsService.IsService(typeof(IDelayedRetryMessageFactory)));
        if (missing is null)
        {
            return Task.CompletedTask;
        }

        DelayedRetryStartupCheckLog.StoreMissing(_logger, missing);
        throw new InvalidOperationException(BuildMessage(missing));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Names what is missing for delayed retries, or <c>null</c> when both services are registered.
    /// </summary>
    internal static string? DescribeMissing(bool hasStore, bool hasFactory) => (hasStore, hasFactory) switch
    {
        (true, true) => null,
        (false, false) => $"{nameof(IDelayedRetryStore)} and {nameof(IDelayedRetryMessageFactory)}",
        (false, true) => nameof(IDelayedRetryStore),
        (true, false) => nameof(IDelayedRetryMessageFactory),
    };

    /// <summary>
    /// Builds the message of the failure: the module, what is missing, ADR-050 and how to fix it.
    /// </summary>
    internal static string BuildMessage(string missing) =>
        $"Recoverability delayed retries are enabled (RecoverabilityOptions.EnableDelayedRetries) but {missing} " +
        "is not registered (ADR-050). Register the store and its message factory with " +
        "AddEncinaDelayedRetryStore<TStore, TFactory>() or through a database provider package, " +
        "or set RecoverabilityOptions.EnableDelayedRetries to false.";
}

/// <summary>
/// LoggerMessage definitions for <see cref="DelayedRetryStartupCheck"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class DelayedRetryStartupCheckLog
{
    /// <remarks>
    /// Event IDs: 5450-5499 (see EventIdRanges.MessagingRecoverability).
    /// </remarks>
    [LoggerMessage(
        EventId = 5457,
        Level = LogLevel.Critical,
        Message = "Delayed retries are enabled but {Missing} is not registered. Register a delayed retry store and factory (AddEncinaDelayedRetryStore) or disable EnableDelayedRetries")]
    public static partial void StoreMissing(ILogger logger, string missing);
}

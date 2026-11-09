using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Recoverability;

/// <summary>
/// Keeps a reference to the service collection Recoverability was registered in, so the duplicate checks can
/// count its descriptors. The collection is the live one: registrations added after Recoverability are visible.
/// </summary>
/// <remarks>
/// Messaging owns this snapshot because the Encina pipeline snapshot is internal to Encina and only exists
/// after <c>AddEncina</c>, which a host using only a database provider may never call.
/// </remarks>
internal sealed class RecoverabilityDescriptorSnapshot(IServiceCollection services)
{
    /// <summary>
    /// Describes the duplicate Recoverability registration in the collection, or <c>null</c> when there is none.
    /// </summary>
    public string? DescribeDuplicate() => RecoverabilityRegistrationCheck.DescribeDuplicate(services, pending: 0);
}

/// <summary>
/// Checks, when the host starts, that Recoverability is registered once: the behavior
/// (<see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/>) exactly once and no
/// <see cref="RecoverabilityOptions"/> instance registered directly
/// (ADR-050, Critical log with EventId 5458, see EventIdRanges.MessagingRecoverability).
/// </summary>
/// <remarks>
/// <para>
/// The registration methods already throw when a second Encina call would duplicate Recoverability. This
/// check catches what is added afterwards, directly in the service collection (a raw
/// <c>AddScoped(typeof(IPipelineBehavior&lt;,&gt;), typeof(RecoverabilityPipelineBehavior&lt;,&gt;))</c> or
/// <c>AddSingleton(RecoverabilityOptions)</c>). It always runs, whatever
/// <see cref="RecoverabilityOptions.EnableDelayedRetries"/> says, and only open generic registrations of the
/// behavior are counted. Without a generic host it never runs; the registration-time error still does.
/// </para>
/// </remarks>
internal sealed class RecoverabilityRegistrationCheck : IHostedService
{
    private readonly RecoverabilityDescriptorSnapshot _snapshot;
    private readonly ILogger<RecoverabilityRegistrationCheck> _logger;

    public RecoverabilityRegistrationCheck(
        RecoverabilityDescriptorSnapshot snapshot,
        ILogger<RecoverabilityRegistrationCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(logger);

        _snapshot = snapshot;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var duplicate = _snapshot.DescribeDuplicate();
        if (duplicate is null)
        {
            return Task.CompletedTask;
        }

        RecoverabilityRegistrationCheckLog.DuplicateRegistration(_logger, duplicate);
        throw new InvalidOperationException(BuildMessage(duplicate));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Throws when registering Recoverability once more would duplicate it: the behavior is already registered,
    /// or a <see cref="RecoverabilityOptions"/> instance is. Runs before anything is added.
    /// </summary>
    internal static void ThrowIfAlreadyRegistered(IServiceCollection services)
    {
        var duplicate = DescribeDuplicate(services, pending: 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(BuildMessage(duplicate));
        }
    }

    /// <summary>
    /// Names the duplicate in the collection, or <c>null</c> when there is none. <paramref name="pending"/> is the
    /// number of behavior registrations about to be added, so the same rule serves the registration-time check
    /// (one pending) and the startup check (none).
    /// </summary>
    internal static string? DescribeDuplicate(IServiceCollection services, int pending)
    {
        var behaviors = pending + services.Count(IsRecoverabilityBehavior);
        if (behaviors > 1)
        {
            return $"RecoverabilityPipelineBehavior is registered {behaviors} times";
        }

        return services.Any(IsOptionsInstance) ? "a RecoverabilityOptions instance is registered directly" : null;
    }

    private static bool IsRecoverabilityBehavior(ServiceDescriptor descriptor)
        => !descriptor.IsKeyedService
            && descriptor.ServiceType == typeof(IPipelineBehavior<,>)
            && descriptor.ImplementationType == typeof(RecoverabilityPipelineBehavior<,>);

    private static bool IsOptionsInstance(ServiceDescriptor descriptor)
        => !descriptor.IsKeyedService && descriptor.ServiceType == typeof(RecoverabilityOptions);

    /// <summary>
    /// Builds the message of the failure: the module, the duplicate, the requirement, ADR-050 and how to fix it.
    /// </summary>
    internal static string BuildMessage(string duplicate) =>
        $"Recoverability (Encina.Messaging) must be registered once, but {duplicate} (ADR-050, Pipeline core: " +
        "the same behavior registered twice is a registration error). Register Recoverability once: use either " +
        "AddEncinaRecoverability() or MessagingConfiguration.UseRecoverability = true, in one provider " +
        "registration only; remove any extra AddScoped(typeof(IPipelineBehavior<,>), typeof(RecoverabilityPipelineBehavior<,>)); " +
        "RecoverabilityOptions is read through IOptions<RecoverabilityOptions>, so remove any AddSingleton(RecoverabilityOptions) " +
        "and configure it with services.Configure<RecoverabilityOptions>(...).";
}

/// <summary>
/// LoggerMessage definitions for <see cref="RecoverabilityRegistrationCheck"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class RecoverabilityRegistrationCheckLog
{
    /// <remarks>
    /// Event IDs: 5450-5499 (see EventIdRanges.MessagingRecoverability).
    /// </remarks>
    [LoggerMessage(
        EventId = 5458,
        Level = LogLevel.Critical,
        Message = "Recoverability is registered more than once: {Duplicate}. Register it once with AddEncinaRecoverability or UseRecoverability and configure RecoverabilityOptions with Configure")]
    public static partial void DuplicateRegistration(ILogger logger, string duplicate);
}

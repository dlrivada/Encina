using System.Collections.Concurrent;
using System.Reflection;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.GDPR;

/// <summary>
/// In-memory implementation of <see cref="IProcessingActivityRegistry"/> for development, testing, and simple deployments.
/// </summary>
/// <remarks>
/// <para>
/// Activities are stored in a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// <see cref="ProcessingActivity.RequestType"/>, ensuring thread-safe concurrent access.
/// </para>
/// <para>
/// This implementation also supports auto-registration from <see cref="ProcessingActivityAttribute"/>
/// decorations via <see cref="AutoRegisterFromAssemblies"/>. Call this method at startup
/// to scan assemblies and populate the registry automatically.
/// </para>
/// <para>
/// For production systems requiring durable storage, consider a database-backed implementation
/// of <see cref="IProcessingActivityRegistry"/>.
/// </para>
/// </remarks>
public sealed class InMemoryProcessingActivityRegistry : IProcessingActivityRegistry
{
    private readonly ConcurrentDictionary<Type, ProcessingActivity> _activities = new();

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RegisterActivityAsync(
        ProcessingActivity activity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (!_activities.TryAdd(activity.RequestType, activity))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                EncinaError.New($"A processing activity is already registered for request type '{activity.RequestType.FullName}'."));
        }

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<ProcessingActivity>>> GetAllActivitiesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProcessingActivity> result = _activities.Values.ToList().AsReadOnly();
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ProcessingActivity>>>(Right(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<ProcessingActivity>>> GetActivityByRequestTypeAsync(
        Type requestType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        Option<ProcessingActivity> result = _activities.TryGetValue(requestType, out var activity)
            ? Some(activity)
            : None;

        return ValueTask.FromResult<Either<EncinaError, Option<ProcessingActivity>>>(result);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> UpdateActivityAsync(
        ProcessingActivity activity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (!_activities.ContainsKey(activity.RequestType))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                EncinaError.New($"No processing activity is registered for request type '{activity.RequestType.FullName}'."));
        }

        _activities[activity.RequestType] = activity;
        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <summary>
    /// Scans the specified assemblies for types decorated with <see cref="ProcessingActivityAttribute"/>
    /// and registers them automatically.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan for processing activity attributes.</param>
    /// <param name="timeProvider">Time provider for timestamps. Uses <see cref="TimeProvider.System"/> if <c>null</c>.</param>
    /// <returns>The number of processing activities auto-registered.</returns>
    /// <remarks>
    /// <para>
    /// Call this method at application startup when <c>GDPROptions.AutoRegisterFromAttributes</c> is enabled.
    /// Types already registered are silently skipped.
    /// </para>
    /// </remarks>
    public int AutoRegisterFromAssemblies(IEnumerable<Assembly> assemblies, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var clock = timeProvider ?? TimeProvider.System;
        var now = clock.GetUtcNow();
        var count = 0;

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttribute<ProcessingActivityAttribute>();
                if (attr is null)
                {
                    continue;
                }

                var activity = new ProcessingActivity
                {
                    Id = Guid.NewGuid(),
                    Name = type.Name,
                    Purpose = attr.Purpose,
                    LawfulBasis = attr.LawfulBasis,
                    CategoriesOfDataSubjects = attr.DataSubjects,
                    CategoriesOfPersonalData = attr.DataCategories,
                    Recipients = attr.Recipients,
                    RetentionPeriod = TimeSpan.FromDays(attr.RetentionDays),
                    SecurityMeasures = attr.SecurityMeasures,
                    ThirdCountryTransfers = attr.ThirdCountryTransfers,
                    Safeguards = attr.Safeguards,
                    RequestType = type,
                    CreatedAtUtc = now,
                    LastUpdatedAtUtc = now
                };

                if (_activities.TryAdd(type, activity))
                {
                    count++;
                }
            }
        }

        return count;
    }
}

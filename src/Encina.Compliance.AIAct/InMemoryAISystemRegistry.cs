using System.Collections.Concurrent;

using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Model;
using Encina.Compliance.AIAct.Notifications;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.AIAct;

/// <summary>
/// In-memory implementation of <see cref="IAISystemRegistry"/> for development, testing,
/// and simple deployments.
/// </summary>
/// <remarks>
/// <para>
/// Registrations are stored in a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// <see cref="AISystemRegistration.SystemId"/>, ensuring thread-safe concurrent access.
/// </para>
/// <para>
/// When an optional <see cref="IEncina"/> is provided, reclassification operations publish
/// an <see cref="AISystemReclassifiedNotification"/> for audit trail purposes.
/// </para>
/// <para>
/// For production systems requiring durable storage, register a database-backed
/// implementation of <see cref="IAISystemRegistry"/> via DI.
/// </para>
/// </remarks>
public sealed class InMemoryAISystemRegistry : IAISystemRegistry
{
    private readonly ConcurrentDictionary<string, AISystemRegistration> _systems = new(StringComparer.Ordinal);
    private readonly IEncina? _encina;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="InMemoryAISystemRegistry"/>.
    /// </summary>
    /// <param name="timeProvider">Time provider for timestamps.</param>
    /// <param name="encina">
    /// Optional Encina mediator for publishing reclassification notifications.
    /// When <c>null</c>, reclassifications succeed silently without notification.
    /// </param>
    public InMemoryAISystemRegistry(TimeProvider timeProvider, IEncina? encina = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
        _encina = encina;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, AISystemRegistration>> GetSystemAsync(
        string systemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(systemId);

        return _systems.TryGetValue(systemId, out var registration)
            ? ValueTask.FromResult(Right<EncinaError, AISystemRegistration>(registration))
            : ValueTask.FromResult<Either<EncinaError, AISystemRegistration>>(
                EncinaError.New($"AI system '{systemId}' is not registered."));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RegisterSystemAsync(
        AISystemRegistration registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);

        if (!_systems.TryAdd(registration.SystemId, registration))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                EncinaError.New($"AI system '{registration.SystemId}' is already registered."));
        }

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ReclassifyAsync(
        string systemId,
        AIRiskLevel newLevel,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(systemId);
        ArgumentNullException.ThrowIfNull(reason);

        if (!_systems.TryGetValue(systemId, out var current))
        {
            return EncinaError.New($"AI system '{systemId}' is not registered.");
        }

        var previousLevel = current.RiskLevel;
        var updated = current with { RiskLevel = newLevel };
        _systems[systemId] = updated;

        if (_encina is not null)
        {
            var notification = new AISystemReclassifiedNotification(
                systemId,
                previousLevel,
                newLevel,
                reason,
                _timeProvider.GetUtcNow());

            await _encina.Publish(notification, cancellationToken);
        }

        return unit;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<AISystemRegistration>>> GetSystemsByRiskLevelAsync(
        AIRiskLevel level,
        CancellationToken cancellationToken = default)
    {
        var result = _systems.Values
            .Where(s => s.RiskLevel == level)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<AISystemRegistration>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<AISystemRegistration>>> GetAllSystemsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = _systems.Values.ToList().AsReadOnly();
        return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<AISystemRegistration>>(result));
    }

    /// <inheritdoc />
    public bool IsRegistered(string systemId)
    {
        ArgumentNullException.ThrowIfNull(systemId);
        return _systems.ContainsKey(systemId);
    }
}

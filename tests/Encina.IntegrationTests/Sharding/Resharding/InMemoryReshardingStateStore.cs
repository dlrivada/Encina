using System.Collections.Concurrent;
using Encina.Sharding.Resharding;
using LanguageExt;

namespace Encina.IntegrationTests.Sharding.Resharding;

/// <summary>
/// In-memory implementation of <see cref="IReshardingStateStore"/> for integration testing.
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread-safe state storage
/// without requiring a real database.
/// </summary>
internal sealed class InMemoryReshardingStateStore : IReshardingStateStore
{
    private readonly ConcurrentDictionary<Guid, ReshardingState> _states = new();

    public Task<Either<EncinaError, Unit>> SaveStateAsync(
        ReshardingState state,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _states[state.Id] = state;

        return Task.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
    }

    public Task<Either<EncinaError, ReshardingState>> GetStateAsync(
        Guid reshardingId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_states.TryGetValue(reshardingId, out var state))
        {
            return Task.FromResult(Either<EncinaError, ReshardingState>.Right(state));
        }

        return Task.FromResult(Either<EncinaError, ReshardingState>.Left(
            EncinaErrors.Create(
                ReshardingErrorCodes.ReshardingNotFound,
                $"Resharding state not found for ID '{reshardingId}'.")));
    }

    public Task<Either<EncinaError, IReadOnlyList<ReshardingState>>> GetActiveReshardingsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var terminalPhases = new System.Collections.Generic.HashSet<ReshardingPhase>
        {
            ReshardingPhase.Completed,
            ReshardingPhase.RolledBack,
            ReshardingPhase.Failed
        };

        var activeStates = _states.Values
            .Where(s => !terminalPhases.Contains(s.CurrentPhase))
            .ToList();

        return Task.FromResult(
            Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(activeStates));
    }

    public Task<Either<EncinaError, Unit>> DeleteStateAsync(
        Guid reshardingId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _states.TryRemove(reshardingId, out _);

        return Task.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
    }

    /// <summary>
    /// Gets all stored states for test assertions.
    /// </summary>
    internal IReadOnlyDictionary<Guid, ReshardingState> GetAllStates() =>
        new Dictionary<Guid, ReshardingState>(_states);

    /// <summary>
    /// Clears all stored states.
    /// </summary>
    internal void Clear() => _states.Clear();
}

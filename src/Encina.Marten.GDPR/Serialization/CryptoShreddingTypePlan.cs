using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Encina.Marten.GDPR;

/// <summary>
/// The crypto fields the contract modifier installed for one closed owner type.
/// </summary>
/// <param name="Type">The owner type.</param>
/// <param name="Fields">Its valid crypto fields.</param>
internal sealed record CryptoShreddingTypePlan(Type Type, ImmutableArray<CryptoShreddedField> Fields);

/// <summary>
/// The plans built by one contract modifier, and every type the modifier was called for.
/// </summary>
internal sealed class CryptoShreddingTypePlanRegistry
{
    private readonly ConcurrentDictionary<Type, CryptoShreddingTypePlan> _plans = new();
    private readonly ConcurrentDictionary<Type, byte> _seen = new();
    private readonly ConcurrentDictionary<Type, byte> _rejected = new();

    /// <summary>Gets the number of contracts with crypto fields built so far.</summary>
    internal int CryptoContractCount => _plans.Count;

    /// <summary>Gets the number of types rejected at runtime so far.</summary>
    internal int MisconfiguredTypeCount => _rejected.Count;

    /// <summary>Records that the modifier was called for a type.</summary>
    internal void MarkSeen(Type type) => _seen.TryAdd(type, 0);

    /// <summary>Records that the modifier rejected a type.</summary>
    internal void MarkRejected(Type type) => _rejected.TryAdd(type, 0);

    /// <summary>Gets whether the modifier was called for a type (with or without crypto fields).</summary>
    internal bool WasSeen(Type type) => _seen.ContainsKey(type);

    /// <summary>Registers the plan of an owner type.</summary>
    internal void Register(CryptoShreddingTypePlan plan) => _plans[plan.Type] = plan;

    /// <summary>Gets the plan of an owner type, or <c>null</c>.</summary>
    internal CryptoShreddingTypePlan? Find(Type type) => _plans.GetValueOrDefault(type);
}

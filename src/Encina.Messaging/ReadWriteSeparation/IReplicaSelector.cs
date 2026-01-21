namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// Defines the contract for selecting a read replica from a collection of available replicas.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface determine which read replica connection string
/// should be used for a given read request. The selection strategy can be based on
/// round-robin, random selection, least connections, or custom logic.
/// </para>
/// <para>
/// All implementations must be thread-safe as they may be called concurrently
/// from multiple request processing threads.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class WeightedReplicaSelector : IReplicaSelector
/// {
///     private readonly IReadOnlyList&lt;string&gt; _replicas;
///     private readonly IReadOnlyList&lt;int&gt; _weights;
///
///     public WeightedReplicaSelector(IReadOnlyList&lt;string&gt; replicas, IReadOnlyList&lt;int&gt; weights)
///     {
///         _replicas = replicas;
///         _weights = weights;
///     }
///
///     public string SelectReplica()
///     {
///         // Custom weighted selection logic
///         var totalWeight = _weights.Sum();
///         var selection = Random.Shared.Next(totalWeight);
///         // ... weighted selection implementation
///     }
/// }
/// </code>
/// </example>
public interface IReplicaSelector
{
    /// <summary>
    /// Selects a read replica connection string from the available replicas.
    /// </summary>
    /// <returns>
    /// The connection string for the selected read replica.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no replicas are available for selection.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method must be thread-safe as it may be called concurrently
    /// from multiple request processing threads.
    /// </para>
    /// <para>
    /// The returned connection string should be used immediately as the
    /// selection state may change between calls.
    /// </para>
    /// </remarks>
    string SelectReplica();
}

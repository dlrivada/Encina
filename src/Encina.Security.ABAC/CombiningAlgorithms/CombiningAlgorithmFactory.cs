namespace Encina.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Factory that maps <see cref="CombiningAlgorithmId"/> values to their
/// <see cref="ICombiningAlgorithm"/> implementations.
/// </summary>
/// <remarks>
/// <para>
/// All eight standard XACML 3.0 combining algorithms are pre-registered at construction time.
/// The factory is stateless after initialization and thread-safe for concurrent reads.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var factory = new CombiningAlgorithmFactory();
/// var algorithm = factory.GetAlgorithm(CombiningAlgorithmId.DenyOverrides);
/// var effect = algorithm.CombineRuleResults(ruleResults);
/// </code>
/// </example>
public sealed class CombiningAlgorithmFactory
{
    private readonly Dictionary<CombiningAlgorithmId, ICombiningAlgorithm> _algorithms;

    /// <summary>
    /// Initializes a new <see cref="CombiningAlgorithmFactory"/> with all eight standard
    /// XACML 3.0 combining algorithms registered.
    /// </summary>
    public CombiningAlgorithmFactory()
    {
        _algorithms = new Dictionary<CombiningAlgorithmId, ICombiningAlgorithm>
        {
            [CombiningAlgorithmId.DenyOverrides] = new DenyOverridesAlgorithm(),
            [CombiningAlgorithmId.PermitOverrides] = new PermitOverridesAlgorithm(),
            [CombiningAlgorithmId.FirstApplicable] = new FirstApplicableAlgorithm(),
            [CombiningAlgorithmId.OnlyOneApplicable] = new OnlyOneApplicableAlgorithm(),
            [CombiningAlgorithmId.DenyUnlessPermit] = new DenyUnlessPermitAlgorithm(),
            [CombiningAlgorithmId.PermitUnlessDeny] = new PermitUnlessDenyAlgorithm(),
            [CombiningAlgorithmId.OrderedDenyOverrides] = new OrderedDenyOverridesAlgorithm(),
            [CombiningAlgorithmId.OrderedPermitOverrides] = new OrderedPermitOverridesAlgorithm()
        };
    }

    /// <summary>
    /// Gets the <see cref="ICombiningAlgorithm"/> implementation for the specified algorithm identifier.
    /// </summary>
    /// <param name="algorithmId">The combining algorithm identifier.</param>
    /// <returns>The combining algorithm implementation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="algorithmId"/> is not a recognized algorithm.
    /// </exception>
    public ICombiningAlgorithm GetAlgorithm(CombiningAlgorithmId algorithmId)
    {
        if (!_algorithms.TryGetValue(algorithmId, out var algorithm))
        {
            throw new ArgumentOutOfRangeException(
                nameof(algorithmId),
                algorithmId,
                $"Combining algorithm '{algorithmId}' is not registered.");
        }

        return algorithm;
    }
}

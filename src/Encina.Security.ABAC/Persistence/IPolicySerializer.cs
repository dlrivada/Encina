using LanguageExt;

namespace Encina.Security.ABAC.Persistence;

/// <summary>
/// Serializes and deserializes XACML policy graphs for persistent storage.
/// </summary>
/// <remarks>
/// <para>
/// The serializer handles the full depth of the XACML object graph, including
/// polymorphic <see cref="IExpression"/> trees (<see cref="Apply"/>,
/// <see cref="AttributeDesignator"/>, <see cref="AttributeValue"/>,
/// <see cref="VariableReference"/>), recursive <see cref="PolicySet"/> nesting,
/// and all enum types (<see cref="Effect"/>, <see cref="CombiningAlgorithmId"/>,
/// <see cref="AttributeCategory"/>, <see cref="FulfillOn"/>).
/// </para>
/// <para>
/// The default implementation (<see cref="DefaultPolicySerializer"/>) uses
/// <c>System.Text.Json</c> with custom converters. Alternative implementations
/// (e.g., XACML 3.0 XML for standards compliance) can be plugged in via
/// dependency injection.
/// </para>
/// </remarks>
public interface IPolicySerializer
{
    /// <summary>
    /// Serializes a <see cref="PolicySet"/> to its string representation.
    /// </summary>
    /// <param name="policySet">The policy set to serialize.</param>
    /// <returns>The serialized string representation of the policy set.</returns>
    string Serialize(PolicySet policySet);

    /// <summary>
    /// Serializes a standalone <see cref="Policy"/> to its string representation.
    /// </summary>
    /// <param name="policy">The policy to serialize.</param>
    /// <returns>The serialized string representation of the policy.</returns>
    string Serialize(Policy policy);

    /// <summary>
    /// Deserializes a string representation into a <see cref="PolicySet"/>.
    /// </summary>
    /// <param name="data">The serialized string to deserialize.</param>
    /// <returns>
    /// A <c>Right</c> containing the deserialized policy set on success,
    /// or a <c>Left</c> containing an <see cref="EncinaError"/> if deserialization fails.
    /// </returns>
    Either<EncinaError, PolicySet> DeserializePolicySet(string data);

    /// <summary>
    /// Deserializes a string representation into a standalone <see cref="Policy"/>.
    /// </summary>
    /// <param name="data">The serialized string to deserialize.</param>
    /// <returns>
    /// A <c>Right</c> containing the deserialized policy on success,
    /// or a <c>Left</c> containing an <see cref="EncinaError"/> if deserialization fails.
    /// </returns>
    Either<EncinaError, Policy> DeserializePolicy(string data);
}

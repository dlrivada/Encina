using System.Text.Json;
using System.Text.Json.Serialization;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Security.ABAC.Persistence;

/// <summary>
/// Default <see cref="IPolicySerializer"/> implementation using <see cref="System.Text.Json"/>.
/// </summary>
/// <remarks>
/// <para>
/// Serializes the full XACML policy graph — including polymorphic <see cref="IExpression"/>
/// trees, recursive <see cref="PolicySet"/> nesting, and all enum types — to compact JSON.
/// </para>
/// <para>
/// Configuration:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="JsonSerializerOptions.PropertyNamingPolicy"/> = <see cref="JsonNamingPolicy.CamelCase"/></description></item>
/// <item><description><see cref="JsonSerializerOptions.WriteIndented"/> = <c>false</c> (compact storage)</description></item>
/// <item><description><see cref="ExpressionJsonConverter"/> for polymorphic <see cref="IExpression"/> with <c>$type</c> discriminator</description></item>
/// <item><description><see cref="JsonStringEnumConverter"/> for all enums as camelCase strings</description></item>
/// <item><description><see cref="JsonSerializerOptions.DefaultIgnoreCondition"/> = <see cref="JsonIgnoreCondition.WhenWritingNull"/> (omit null properties)</description></item>
/// </list>
/// </remarks>
public sealed class DefaultPolicySerializer : IPolicySerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPolicySerializer"/> class
    /// with pre-configured JSON serializer options.
    /// </summary>
    public DefaultPolicySerializer()
    {
        _options = CreateSerializerOptions();
    }

    /// <inheritdoc />
    public string Serialize(PolicySet policySet)
    {
        ArgumentNullException.ThrowIfNull(policySet);

        return JsonSerializer.Serialize(policySet, _options);
    }

    /// <inheritdoc />
    public string Serialize(Policy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return JsonSerializer.Serialize(policy, _options);
    }

    /// <inheritdoc />
    public Either<EncinaError, PolicySet> DeserializePolicySet(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return Left(ABACErrors.DeserializationFailed("PolicySet", "Input data is null or empty."));
        }

        try
        {
            var result = JsonSerializer.Deserialize<PolicySet>(data, _options);
            if (result is null)
            {
                return Left(ABACErrors.DeserializationFailed("PolicySet", "Deserialization produced a null result."));
            }

            return Right(result);
        }
        catch (JsonException ex)
        {
            return Left(ABACErrors.DeserializationFailed("PolicySet", ex.Message));
        }
    }

    /// <inheritdoc />
    public Either<EncinaError, Policy> DeserializePolicy(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return Left(ABACErrors.DeserializationFailed("Policy", "Input data is null or empty."));
        }

        try
        {
            var result = JsonSerializer.Deserialize<Policy>(data, _options);
            if (result is null)
            {
                return Left(ABACErrors.DeserializationFailed("Policy", "Deserialization produced a null result."));
            }

            return Right(result);
        }
        catch (JsonException ex)
        {
            return Left(ABACErrors.DeserializationFailed("Policy", ex.Message));
        }
    }

    /// <summary>
    /// Creates the <see cref="JsonSerializerOptions"/> configured for ABAC policy serialization.
    /// </summary>
    internal static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        options.Converters.Add(new ExpressionJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}

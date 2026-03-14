using System.Reflection;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Cached reflection metadata for a request type's properties and privacy attributes.
/// </summary>
/// <remarks>
/// Built once per request type via the static <see cref="DefaultDataMinimizationAnalyzer.MetadataCache"/>
/// and reused on all subsequent calls, eliminating reflection overhead after the first invocation.
/// </remarks>
internal sealed record FieldMetadataCache(
    PropertyInfo[] Properties,
    NotStrictlyNecessaryAttribute?[] NotStrictlyNecessary,
    PurposeLimitationAttribute?[] PurposeLimitation,
    PrivacyDefaultAttribute?[] PrivacyDefault)
{
    /// <summary>
    /// Builds the metadata cache for a given type by inspecting all public instance properties.
    /// </summary>
    internal static FieldMetadataCache Build(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var notStrictlyNecessary = new NotStrictlyNecessaryAttribute?[properties.Length];
        var purposeLimitation = new PurposeLimitationAttribute?[properties.Length];
        var privacyDefault = new PrivacyDefaultAttribute?[properties.Length];

        for (var i = 0; i < properties.Length; i++)
        {
            notStrictlyNecessary[i] = properties[i].GetCustomAttribute<NotStrictlyNecessaryAttribute>();
            purposeLimitation[i] = properties[i].GetCustomAttribute<PurposeLimitationAttribute>();
            privacyDefault[i] = properties[i].GetCustomAttribute<PrivacyDefaultAttribute>();
        }

        return new FieldMetadataCache(properties, notStrictlyNecessary, purposeLimitation, privacyDefault);
    }
}

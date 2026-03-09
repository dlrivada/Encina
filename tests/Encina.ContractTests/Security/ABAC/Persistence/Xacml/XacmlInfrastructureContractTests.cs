using System.Collections.Frozen;
using System.Reflection;

using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence.Xacml;

namespace Encina.ContractTests.Security.ABAC.Persistence.Xacml;

/// <summary>
/// Contract tests for the XACML serialization infrastructure classes.
/// Verifies that internal helper types maintain their expected shapes,
/// registry symmetry, and API contracts.
/// </summary>
[Trait("Category", "Contract")]
public sealed class XacmlInfrastructureContractTests
{
    /// <summary>
    /// Binding flags for finding internal static members via reflection.
    /// Internal members appear as NonPublic in reflection even with InternalsVisibleTo.
    /// </summary>
    private const BindingFlags InternalStatic =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    // ── XacmlFunctionRegistry Contract ────────────────────────────────

    #region XacmlFunctionRegistry Contract

    [Fact]
    public void XacmlFunctionRegistry_ShouldBeStatic()
    {
        var type = typeof(XacmlFunctionRegistry);

        type.IsAbstract.ShouldBeTrue(
            "XacmlFunctionRegistry must be a static class (IsAbstract)");
        type.IsSealed.ShouldBeTrue(
            "XacmlFunctionRegistry must be a static class (IsSealed)");
    }

    [Fact]
    public void XacmlFunctionRegistry_ShouldBeInternal()
    {
        typeof(XacmlFunctionRegistry).IsNotPublic.ShouldBeTrue(
            "XacmlFunctionRegistry must be internal (not part of public API)");
    }

    [Fact]
    public void XacmlFunctionRegistry_ShouldExpose_ShortIdToUrn_AsFrozenDictionary()
    {
        var field = typeof(XacmlFunctionRegistry)
            .GetField("ShortIdToUrn", InternalStatic);

        field.ShouldNotBeNull("ShortIdToUrn field must exist");
        field!.FieldType.ShouldBe(typeof(FrozenDictionary<string, string>),
            "ShortIdToUrn must be FrozenDictionary<string, string>");
    }

    [Fact]
    public void XacmlFunctionRegistry_ShouldExpose_UrnToShortId_AsFrozenDictionary()
    {
        var field = typeof(XacmlFunctionRegistry)
            .GetField("UrnToShortId", InternalStatic);

        field.ShouldNotBeNull("UrnToShortId field must exist");
        field!.FieldType.ShouldBe(typeof(FrozenDictionary<string, string>),
            "UrnToShortId must be FrozenDictionary<string, string>");
    }

    [Fact]
    public void XacmlFunctionRegistry_ShouldHave_ToUrn_Method()
    {
        var method = typeof(XacmlFunctionRegistry)
            .GetMethod("ToUrn", InternalStatic);

        method.ShouldNotBeNull("ToUrn method must exist");
        method!.ReturnType.ShouldBe(typeof(string));
        method.GetParameters().Length.ShouldBe(1);
        method.GetParameters()[0].ParameterType.ShouldBe(typeof(string));
    }

    [Fact]
    public void XacmlFunctionRegistry_ShouldHave_ToShortId_Method()
    {
        var method = typeof(XacmlFunctionRegistry)
            .GetMethod("ToShortId", InternalStatic);

        method.ShouldNotBeNull("ToShortId method must exist");
        method!.ReturnType.ShouldBe(typeof(string));
        method.GetParameters().Length.ShouldBe(1);
        method.GetParameters()[0].ParameterType.ShouldBe(typeof(string));
    }

    [Fact]
    public void XacmlFunctionRegistry_ShouldHave_IsKnownUrn_Method()
    {
        var method = typeof(XacmlFunctionRegistry)
            .GetMethod("IsKnownUrn", InternalStatic);

        method.ShouldNotBeNull("IsKnownUrn method must exist");
        method!.ReturnType.ShouldBe(typeof(bool));
    }

    [Fact]
    public void XacmlFunctionRegistry_Dictionaries_ShouldBe_Symmetric()
    {
        XacmlFunctionRegistry.ShortIdToUrn.Count
            .ShouldBe(XacmlFunctionRegistry.UrnToShortId.Count,
                "ShortIdToUrn and UrnToShortId must have equal entry counts");
    }

    [Fact]
    public void XacmlFunctionRegistry_ShouldMap_AllXACMLFunctionIds()
    {
        // All constants from XACMLFunctionIds should be present in the registry
        var functionIdFields = typeof(XACMLFunctionIds)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        functionIdFields.Count.ShouldBeGreaterThan(0,
            "XACMLFunctionIds must define at least one function constant");

        foreach (var shortId in functionIdFields)
        {
            XacmlFunctionRegistry.ShortIdToUrn.ContainsKey(shortId).ShouldBeTrue(
                $"XACMLFunctionIds.{shortId} must be mapped in XacmlFunctionRegistry");
        }
    }

    #endregion

    // ── XacmlMappingExtensions Contract ───────────────────────────────

    #region XacmlMappingExtensions Contract

    [Fact]
    public void XacmlMappingExtensions_ShouldBeStatic()
    {
        var type = typeof(XacmlMappingExtensions);

        type.IsAbstract.ShouldBeTrue(
            "XacmlMappingExtensions must be a static class (IsAbstract)");
        type.IsSealed.ShouldBeTrue(
            "XacmlMappingExtensions must be a static class (IsSealed)");
    }

    [Fact]
    public void XacmlMappingExtensions_ShouldBeInternal()
    {
        typeof(XacmlMappingExtensions).IsNotPublic.ShouldBeTrue(
            "XacmlMappingExtensions must be internal (not part of public API)");
    }

    [Fact]
    public void XacmlMappingExtensions_ShouldHave_AttributeCategory_RoundTrip_Methods()
    {
        var toUrn = typeof(XacmlMappingExtensions)
            .GetMethod("ToXacmlUrn", InternalStatic, null, [typeof(AttributeCategory)], null);
        var toCategory = typeof(XacmlMappingExtensions)
            .GetMethod("ToAttributeCategory", InternalStatic, null, [typeof(string)], null);

        toUrn.ShouldNotBeNull("ToXacmlUrn(AttributeCategory) must exist");
        toCategory.ShouldNotBeNull("ToAttributeCategory(string) must exist");
    }

    [Fact]
    public void XacmlMappingExtensions_ShouldHave_CombiningAlgorithm_RoundTrip_Methods()
    {
        var toUrn = typeof(XacmlMappingExtensions)
            .GetMethod("ToXacmlUrn", InternalStatic,
                null, [typeof(CombiningAlgorithmId), typeof(bool)], null);
        var toAlg = typeof(XacmlMappingExtensions)
            .GetMethod("ToCombiningAlgorithmId", InternalStatic,
                null, [typeof(string)], null);

        toUrn.ShouldNotBeNull("ToXacmlUrn(CombiningAlgorithmId, bool) must exist");
        toAlg.ShouldNotBeNull("ToCombiningAlgorithmId(string) must exist");
    }

    [Fact]
    public void XacmlMappingExtensions_ShouldHave_Effect_RoundTrip_Methods()
    {
        var toStr = typeof(XacmlMappingExtensions)
            .GetMethod("ToXacmlString", InternalStatic, null, [typeof(Effect)], null);
        var toEff = typeof(XacmlMappingExtensions)
            .GetMethod("ToEffect", InternalStatic, null, [typeof(string)], null);

        toStr.ShouldNotBeNull("ToXacmlString(Effect) must exist");
        toEff.ShouldNotBeNull("ToEffect(string) must exist");
    }

    [Fact]
    public void XacmlMappingExtensions_ShouldHave_FulfillOn_RoundTrip_Methods()
    {
        var toStr = typeof(XacmlMappingExtensions)
            .GetMethod("ToXacmlString", InternalStatic, null, [typeof(FulfillOn)], null);
        var toFulfill = typeof(XacmlMappingExtensions)
            .GetMethod("ToFulfillOn", InternalStatic, null, [typeof(string)], null);

        toStr.ShouldNotBeNull("ToXacmlString(FulfillOn) must exist");
        toFulfill.ShouldNotBeNull("ToFulfillOn(string) must exist");
    }

    [Fact]
    public void XacmlMappingExtensions_ShouldHave_FormatXacmlValue_Method()
    {
        var method = typeof(XacmlMappingExtensions)
            .GetMethod("FormatXacmlValue", InternalStatic);

        method.ShouldNotBeNull("FormatXacmlValue must exist");
        method!.ReturnType.ShouldBe(typeof(string));
    }

    [Fact]
    public void XacmlMappingExtensions_ShouldHave_ParseXacmlValue_Method()
    {
        var method = typeof(XacmlMappingExtensions)
            .GetMethod("ParseXacmlValue", InternalStatic);

        method.ShouldNotBeNull("ParseXacmlValue must exist");
    }

    [Fact]
    public void XacmlMappingExtensions_AllAttributeCategories_ShouldRoundTrip()
    {
        foreach (var category in Enum.GetValues<AttributeCategory>())
        {
            var urn = category.ToXacmlUrn();
            var roundTripped = XacmlMappingExtensions.ToAttributeCategory(urn);
            roundTripped.ShouldBe(category,
                $"AttributeCategory.{category} must survive URN round-trip");
        }
    }

    [Fact]
    public void XacmlMappingExtensions_AllCombiningAlgorithms_ShouldRoundTrip_RuleCombining()
    {
        foreach (var algorithm in Enum.GetValues<CombiningAlgorithmId>())
        {
            var urn = algorithm.ToXacmlUrn(isRuleCombining: true);
            var roundTripped = XacmlMappingExtensions.ToCombiningAlgorithmId(urn);
            roundTripped.ShouldBe(algorithm,
                $"CombiningAlgorithmId.{algorithm} must survive rule-combining URN round-trip");
        }
    }

    [Fact]
    public void XacmlMappingExtensions_AllCombiningAlgorithms_ShouldRoundTrip_PolicyCombining()
    {
        foreach (var algorithm in Enum.GetValues<CombiningAlgorithmId>())
        {
            var urn = algorithm.ToXacmlUrn(isRuleCombining: false);
            var roundTripped = XacmlMappingExtensions.ToCombiningAlgorithmId(urn);
            roundTripped.ShouldBe(algorithm,
                $"CombiningAlgorithmId.{algorithm} must survive policy-combining URN round-trip");
        }
    }

    #endregion

    // ── XacmlDataTypeMap Contract ─────────────────────────────────────

    #region XacmlDataTypeMap Contract

    [Fact]
    public void XacmlDataTypeMap_ShouldBeStatic()
    {
        var type = typeof(XacmlDataTypeMap);

        type.IsAbstract.ShouldBeTrue(
            "XacmlDataTypeMap must be a static class (IsAbstract)");
        type.IsSealed.ShouldBeTrue(
            "XacmlDataTypeMap must be a static class (IsSealed)");
    }

    [Fact]
    public void XacmlDataTypeMap_ShouldBeInternal()
    {
        typeof(XacmlDataTypeMap).IsNotPublic.ShouldBeTrue(
            "XacmlDataTypeMap must be internal (not part of public API)");
    }

    [Fact]
    public void XacmlDataTypeMap_ShouldHave_InferDataType_Method()
    {
        var method = typeof(XacmlDataTypeMap)
            .GetMethod("InferDataType", InternalStatic);

        method.ShouldNotBeNull("InferDataType must exist");
        method!.ReturnType.ShouldBe(typeof(string));
    }

    [Fact]
    public void XacmlDataTypeMap_ShouldHave_IsKnownDataType_Method()
    {
        var method = typeof(XacmlDataTypeMap)
            .GetMethod("IsKnownDataType", InternalStatic);

        method.ShouldNotBeNull("IsKnownDataType must exist");
        method!.ReturnType.ShouldBe(typeof(bool));
    }

    [Fact]
    public void XacmlDataTypeMap_ShouldRecognize_AllStandardXACMLDataTypes()
    {
        // All XACML data type URIs from XACMLDataTypes should be recognized
        var dataTypeFields = typeof(XACMLDataTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        dataTypeFields.Count.ShouldBeGreaterThan(0,
            "XACMLDataTypes must define at least one data type constant");

        foreach (var dataType in dataTypeFields)
        {
            XacmlDataTypeMap.IsKnownDataType(dataType).ShouldBeTrue(
                $"XACMLDataTypes.{dataType} must be recognized by XacmlDataTypeMap");
        }
    }

    #endregion

    // ── XacmlNamespaces Contract ──────────────────────────────────────

    #region XacmlNamespaces Contract

    [Fact]
    public void XacmlNamespaces_ShouldBeStatic()
    {
        var type = typeof(XacmlNamespaces);

        type.IsAbstract.ShouldBeTrue(
            "XacmlNamespaces must be a static class (IsAbstract)");
        type.IsSealed.ShouldBeTrue(
            "XacmlNamespaces must be a static class (IsSealed)");
    }

    [Fact]
    public void XacmlNamespaces_ShouldBeInternal()
    {
        typeof(XacmlNamespaces).IsNotPublic.ShouldBeTrue(
            "XacmlNamespaces must be internal (not part of public API)");
    }

    [Fact]
    public void XacmlNamespaces_ShouldDefine_XacmlCoreNamespace()
    {
        var field = typeof(XacmlNamespaces)
            .GetField("XacmlCore", InternalStatic);

        field.ShouldNotBeNull("XacmlCore namespace field must exist");

        var value = field!.GetValue(null);
        value.ShouldNotBeNull("XacmlCore namespace must have a value");
        value!.ToString().ShouldBe("urn:oasis:names:tc:xacml:3.0:core:schema:wd-17",
            "XacmlCore must be the XACML 3.0 WD-17 namespace URI");
    }

    [Fact]
    public void XacmlNamespaces_ShouldDefine_EncinaExtensionNamespace()
    {
        var field = typeof(XacmlNamespaces)
            .GetField("Encina", InternalStatic);

        field.ShouldNotBeNull("Encina extension namespace field must exist");

        var value = field!.GetValue(null);
        value.ShouldNotBeNull("Encina namespace must have a value");
        value!.ToString().ShouldBe("urn:encina:xacml:extensions:1.0",
            "Encina must be the Encina extension namespace URI");
    }

    #endregion
}

using System.Reflection;

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.Persistence;

namespace Encina.ContractTests.Security.ABAC;

/// <summary>
/// Contract tests for the Persistent PAP public surface area.
/// Verifies interface shapes, implementation contracts, data type structures,
/// and error code conventions for the ABAC persistence module.
/// </summary>
[Trait("Category", "Contract")]
public sealed class PersistentPAPContractTests
{
    // ── IPolicyAdministrationPoint Interface Shape ─────────────────────

    #region IPolicyAdministrationPoint Interface Shape

    [Fact]
    public void IPolicyAdministrationPoint_ShouldHave_TenMethods()
    {
        // Arrange
        var type = typeof(IPolicyAdministrationPoint);

        // Act
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        methods.Length.ShouldBe(10,
            "IPolicyAdministrationPoint must define exactly 10 methods: " +
            "5 PolicySet CRUD + 5 Policy CRUD");
    }

    [Fact]
    public void IPolicyAdministrationPoint_AllMethods_ShouldReturn_ValueTaskEither()
    {
        // Arrange
        var type = typeof(IPolicyAdministrationPoint);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Act & Assert
        foreach (var method in methods)
        {
            var returnType = method.ReturnType;

            returnType.IsGenericType.ShouldBeTrue(
                $"Method '{method.Name}' must return a generic type (ValueTask<Either<...>>)");

            var outerGenericDef = returnType.GetGenericTypeDefinition();
            outerGenericDef.ShouldBe(typeof(ValueTask<>),
                $"Method '{method.Name}' must return ValueTask<T>, got {returnType.Name}");

            var innerType = returnType.GetGenericArguments()[0];
            innerType.IsGenericType.ShouldBeTrue(
                $"Method '{method.Name}' inner type must be generic (Either<EncinaError, T>)");

            var fullName = innerType.GetGenericTypeDefinition().FullName ?? string.Empty;
            fullName.StartsWith("LanguageExt.Either", StringComparison.Ordinal).ShouldBeTrue(
                $"Method '{method.Name}' must return ValueTask<Either<EncinaError, T>>, got {innerType.Name}");

            var eitherArgs = innerType.GetGenericArguments();
            eitherArgs[0].ShouldBe(typeof(EncinaError),
                $"Method '{method.Name}' Either left type must be EncinaError");
        }
    }

    [Fact]
    public void IPolicyAdministrationPoint_AllMethods_ShouldAccept_CancellationToken()
    {
        // Arrange
        var type = typeof(IPolicyAdministrationPoint);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Act & Assert
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var lastParam = parameters[^1];

            lastParam.ParameterType.ShouldBe(typeof(CancellationToken),
                $"Method '{method.Name}' must accept CancellationToken as last parameter");
            lastParam.HasDefaultValue.ShouldBeTrue(
                $"Method '{method.Name}' CancellationToken must have a default value");
        }
    }

    [Fact]
    public void IPolicyAdministrationPoint_GetPolicySetsAsync_ShouldExist()
    {
        var method = typeof(IPolicyAdministrationPoint).GetMethod("GetPolicySetsAsync");
        method.ShouldNotBeNull("GetPolicySetsAsync must exist");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1, "GetPolicySetsAsync must accept (CancellationToken)");
    }

    [Fact]
    public void IPolicyAdministrationPoint_GetPolicySetAsync_ShouldAccept_StringId()
    {
        var method = typeof(IPolicyAdministrationPoint).GetMethod("GetPolicySetAsync");
        method.ShouldNotBeNull("GetPolicySetAsync must exist");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2, "GetPolicySetAsync must accept (string, CancellationToken)");
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("policySetId");
    }

    [Fact]
    public void IPolicyAdministrationPoint_AddPolicySetAsync_ShouldAccept_PolicySet()
    {
        var method = typeof(IPolicyAdministrationPoint).GetMethod("AddPolicySetAsync");
        method.ShouldNotBeNull("AddPolicySetAsync must exist");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2, "AddPolicySetAsync must accept (PolicySet, CancellationToken)");
        parameters[0].ParameterType.ShouldBe(typeof(PolicySet));
        parameters[0].Name.ShouldBe("policySet");
    }

    [Fact]
    public void IPolicyAdministrationPoint_AddPolicyAsync_ShouldAccept_PolicyAndOptionalParent()
    {
        var method = typeof(IPolicyAdministrationPoint).GetMethod("AddPolicyAsync");
        method.ShouldNotBeNull("AddPolicyAsync must exist");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3,
            "AddPolicyAsync must accept (Policy, string?, CancellationToken)");
        parameters[0].ParameterType.ShouldBe(typeof(Policy));
        parameters[0].Name.ShouldBe("policy");
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("parentPolicySetId");
    }

    [Fact]
    public void IPolicyAdministrationPoint_GetPoliciesAsync_ShouldAccept_OptionalPolicySetId()
    {
        var method = typeof(IPolicyAdministrationPoint).GetMethod("GetPoliciesAsync");
        method.ShouldNotBeNull("GetPoliciesAsync must exist");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2,
            "GetPoliciesAsync must accept (string?, CancellationToken)");
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("policySetId");
    }

    #endregion

    // ── Implementation Contracts ───────────────────────────────────────

    #region Implementation Contracts

    [Fact]
    public void PersistentPolicyAdministrationPoint_ShouldImplement_IPolicyAdministrationPoint()
    {
        typeof(IPolicyAdministrationPoint)
            .IsAssignableFrom(typeof(PersistentPolicyAdministrationPoint))
            .ShouldBeTrue(
                "PersistentPolicyAdministrationPoint must implement IPolicyAdministrationPoint");
    }

    [Fact]
    public void PersistentPolicyAdministrationPoint_ShouldBeSealed()
    {
        typeof(PersistentPolicyAdministrationPoint).IsSealed.ShouldBeTrue(
            "PersistentPolicyAdministrationPoint must be sealed for performance");
    }

    [Fact]
    public void InMemoryPolicyAdministrationPoint_ShouldImplement_IPolicyAdministrationPoint()
    {
        typeof(IPolicyAdministrationPoint)
            .IsAssignableFrom(typeof(InMemoryPolicyAdministrationPoint))
            .ShouldBeTrue(
                "InMemoryPolicyAdministrationPoint must implement IPolicyAdministrationPoint");
    }

    [Fact]
    public void InMemoryPolicyAdministrationPoint_ShouldBeSealed()
    {
        typeof(InMemoryPolicyAdministrationPoint).IsSealed.ShouldBeTrue(
            "InMemoryPolicyAdministrationPoint must be sealed for performance");
    }

    #endregion

    // ── IPolicyStore Interface Shape ───────────────────────────────────

    #region IPolicyStore Interface Shape

    [Fact]
    public void IPolicyStore_ShouldHave_TwelveMethods()
    {
        var type = typeof(IPolicyStore);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Length.ShouldBe(12,
            "IPolicyStore must define exactly 12 methods: " +
            "6 PolicySet ops + 6 Policy ops");
    }

    [Fact]
    public void IPolicyStore_AllMethods_ShouldReturn_ValueTaskEither()
    {
        var type = typeof(IPolicyStore);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var returnType = method.ReturnType;
            returnType.IsGenericType.ShouldBeTrue(
                $"Method '{method.Name}' must return a generic type");

            var outerGenericDef = returnType.GetGenericTypeDefinition();
            outerGenericDef.ShouldBe(typeof(ValueTask<>),
                $"Method '{method.Name}' must return ValueTask<T>");
        }
    }

    #endregion

    // ── IPolicySerializer Interface Shape ──────────────────────────────

    #region IPolicySerializer Interface Shape

    [Fact]
    public void IPolicySerializer_ShouldHave_FourMethods()
    {
        var type = typeof(IPolicySerializer);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Length.ShouldBe(4,
            "IPolicySerializer must define 4 methods: " +
            "Serialize(PolicySet), Serialize(Policy), DeserializePolicySet, DeserializePolicy");
    }

    [Fact]
    public void DefaultPolicySerializer_ShouldImplement_IPolicySerializer()
    {
        typeof(IPolicySerializer)
            .IsAssignableFrom(typeof(DefaultPolicySerializer))
            .ShouldBeTrue(
                "DefaultPolicySerializer must implement IPolicySerializer");
    }

    [Fact]
    public void DefaultPolicySerializer_ShouldBeSealed()
    {
        typeof(DefaultPolicySerializer).IsSealed.ShouldBeTrue(
            "DefaultPolicySerializer must be sealed for performance");
    }

    #endregion

    // ── Entity Type Contracts ──────────────────────────────────────────

    #region Entity Type Contracts

    [Fact]
    public void PolicySetEntity_ShouldHave_RequiredProperties()
    {
        var type = typeof(PolicySetEntity);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var expectedNames = new[]
        {
            "Id", "Version", "Description", "IsEnabled", "Priority",
            "PolicyJson", "CreatedAtUtc", "UpdatedAtUtc"
        };

        foreach (var name in expectedNames)
        {
            var prop = properties.FirstOrDefault(p => p.Name == name);
            prop.ShouldNotBeNull($"PolicySetEntity must have '{name}' property");
        }
    }

    [Fact]
    public void PolicyEntity_ShouldHave_RequiredProperties()
    {
        var type = typeof(PolicyEntity);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var expectedNames = new[]
        {
            "Id", "Version", "Description", "IsEnabled", "Priority",
            "PolicyJson", "CreatedAtUtc", "UpdatedAtUtc"
        };

        foreach (var name in expectedNames)
        {
            var prop = properties.FirstOrDefault(p => p.Name == name);
            prop.ShouldNotBeNull($"PolicyEntity must have '{name}' property");
        }
    }

    [Fact]
    public void PolicySetEntity_TimestampProperties_ShouldBeDateTime()
    {
        var type = typeof(PolicySetEntity);

        type.GetProperty("CreatedAtUtc")!.PropertyType.ShouldBe(typeof(DateTime));
        type.GetProperty("UpdatedAtUtc")!.PropertyType.ShouldBe(typeof(DateTime));
    }

    [Fact]
    public void PolicyEntity_TimestampProperties_ShouldBeDateTime()
    {
        var type = typeof(PolicyEntity);

        type.GetProperty("CreatedAtUtc")!.PropertyType.ShouldBe(typeof(DateTime));
        type.GetProperty("UpdatedAtUtc")!.PropertyType.ShouldBe(typeof(DateTime));
    }

    #endregion

    // ── PolicyEntityMapper Contract ────────────────────────────────────

    #region PolicyEntityMapper Contract

    [Fact]
    public void PolicyEntityMapper_ShouldBeStatic()
    {
        typeof(PolicyEntityMapper).IsAbstract.ShouldBeTrue(
            "PolicyEntityMapper must be a static class (IsAbstract + IsSealed)");
        typeof(PolicyEntityMapper).IsSealed.ShouldBeTrue(
            "PolicyEntityMapper must be a static class (IsAbstract + IsSealed)");
    }

    [Fact]
    public void PolicyEntityMapper_ShouldHave_ToPolicySetEntity_Method()
    {
        var methods = typeof(PolicyEntityMapper)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "ToPolicySetEntity")
            .ToList();

        methods.Count.ShouldBeGreaterThan(0,
            "PolicyEntityMapper must have at least one ToPolicySetEntity method");
    }

    [Fact]
    public void PolicyEntityMapper_ShouldHave_ToPolicySet_Method()
    {
        var method = typeof(PolicyEntityMapper)
            .GetMethod("ToPolicySet", BindingFlags.Public | BindingFlags.Static);

        method.ShouldNotBeNull("PolicyEntityMapper must have ToPolicySet method");
    }

    [Fact]
    public void PolicyEntityMapper_ShouldHave_ToPolicyEntity_Method()
    {
        var methods = typeof(PolicyEntityMapper)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "ToPolicyEntity")
            .ToList();

        methods.Count.ShouldBeGreaterThan(0,
            "PolicyEntityMapper must have at least one ToPolicyEntity method");
    }

    [Fact]
    public void PolicyEntityMapper_ShouldHave_ToPolicy_Method()
    {
        var method = typeof(PolicyEntityMapper)
            .GetMethod("ToPolicy", BindingFlags.Public | BindingFlags.Static);

        method.ShouldNotBeNull("PolicyEntityMapper must have ToPolicy method");
    }

    #endregion

    // ── XACML Domain Model Contracts ───────────────────────────────────

    #region XACML Domain Model Contracts

    [Fact]
    public void PolicySet_ShouldBe_SealedRecord()
    {
        typeof(PolicySet).IsSealed.ShouldBeTrue("PolicySet must be sealed");
        typeof(PolicySet).GetMethod("<Clone>$").ShouldNotBeNull("PolicySet must be a record (has <Clone>$ method)");
    }

    [Fact]
    public void Policy_ShouldBe_SealedRecord()
    {
        typeof(Policy).IsSealed.ShouldBeTrue("Policy must be sealed");
        typeof(Policy).GetMethod("<Clone>$").ShouldNotBeNull("Policy must be a record (has <Clone>$ method)");
    }

    [Fact]
    public void Rule_ShouldBe_SealedRecord()
    {
        typeof(Rule).IsSealed.ShouldBeTrue("Rule must be sealed");
        typeof(Rule).GetMethod("<Clone>$").ShouldNotBeNull("Rule must be a record (has <Clone>$ method)");
    }

    [Fact]
    public void PolicySet_ShouldHave_RequiredProperties()
    {
        var type = typeof(PolicySet);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var expectedNames = new[]
        {
            "Id", "Target", "Algorithm", "Policies", "PolicySets",
            "Obligations", "Advice"
        };

        foreach (var name in expectedNames)
        {
            var prop = properties.FirstOrDefault(p => p.Name == name);
            prop.ShouldNotBeNull($"PolicySet must have '{name}' property");
        }
    }

    [Fact]
    public void Policy_ShouldHave_RequiredProperties()
    {
        var type = typeof(Policy);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var expectedNames = new[]
        {
            "Id", "Target", "Algorithm", "Rules",
            "Obligations", "Advice", "VariableDefinitions"
        };

        foreach (var name in expectedNames)
        {
            var prop = properties.FirstOrDefault(p => p.Name == name);
            prop.ShouldNotBeNull($"Policy must have '{name}' property");
        }
    }

    #endregion

    // ── CombiningAlgorithmId Enum Contract ─────────────────────────────

    #region CombiningAlgorithmId Enum Contract

    [Fact]
    public void CombiningAlgorithmId_ShouldHave_AtLeastFourValues()
    {
        var values = Enum.GetValues<CombiningAlgorithmId>();

        values.Length.ShouldBeGreaterThanOrEqualTo(4,
            "CombiningAlgorithmId must define at least DenyOverrides, PermitOverrides, FirstApplicable, OnlyOneApplicable");
    }

    [Fact]
    public void CombiningAlgorithmId_ShouldContain_DenyOverrides()
    {
        Enum.IsDefined(CombiningAlgorithmId.DenyOverrides).ShouldBeTrue(
            "DenyOverrides must exist in CombiningAlgorithmId");
    }

    [Fact]
    public void CombiningAlgorithmId_ShouldContain_PermitOverrides()
    {
        Enum.IsDefined(CombiningAlgorithmId.PermitOverrides).ShouldBeTrue(
            "PermitOverrides must exist in CombiningAlgorithmId");
    }

    [Fact]
    public void CombiningAlgorithmId_ShouldContain_FirstApplicable()
    {
        Enum.IsDefined(CombiningAlgorithmId.FirstApplicable).ShouldBeTrue(
            "FirstApplicable must exist in CombiningAlgorithmId");
    }

    #endregion

    // ── Effect Enum Contract ───────────────────────────────────────────

    #region Effect Enum Contract

    [Fact]
    public void Effect_ShouldHave_ExactlyFour_Values()
    {
        var values = Enum.GetValues<Effect>();

        values.Length.ShouldBe(4,
            "Effect must define exactly 4 XACML 3.0 values: Permit, Deny, NotApplicable, Indeterminate");
    }

    [Fact]
    public void Effect_ShouldContain_AllXACMLEffects()
    {
        Enum.IsDefined(Effect.Permit).ShouldBeTrue("Effect.Permit must exist");
        Enum.IsDefined(Effect.Deny).ShouldBeTrue("Effect.Deny must exist");
        Enum.IsDefined(Effect.NotApplicable).ShouldBeTrue("Effect.NotApplicable must exist");
        Enum.IsDefined(Effect.Indeterminate).ShouldBeTrue("Effect.Indeterminate must exist");
    }

    #endregion
}

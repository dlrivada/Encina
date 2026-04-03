using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;

using Shouldly;

using Rule = Encina.Security.ABAC.Rule;

namespace Encina.GuardTests.Security.ABAC.Persistence;

/// <summary>
/// Guard clause tests for <see cref="DefaultPolicySerializer"/>.
/// </summary>
public class DefaultPolicySerializerGuardTests
{
    private readonly DefaultPolicySerializer _serializer = new();

    private static Policy CreatePolicy(string id = "p-1") => new()
    {
        Id = id,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [new Rule { Id = "r-1", Effect = Effect.Permit, Obligations = [], Advice = [] }],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    private static PolicySet CreatePolicySet(string id = "ps-1") => new()
    {
        Id = id,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies = [CreatePolicy()],
        PolicySets = [],
        Obligations = [],
        Advice = []
    };

    #region Serialize PolicySet Guards

    [Fact]
    public void Serialize_NullPolicySet_ThrowsArgumentNullException()
    {
        var act = () => _serializer.Serialize((PolicySet)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("policySet");
    }

    [Fact]
    public void Serialize_ValidPolicySet_ReturnsJson()
    {
        var policySet = CreatePolicySet();
        var json = _serializer.Serialize(policySet);
        json.ShouldNotBeNullOrWhiteSpace();
        json.ShouldContain("ps-1");
    }

    #endregion

    #region Serialize Policy Guards

    [Fact]
    public void Serialize_NullPolicy_ThrowsArgumentNullException()
    {
        var act = () => _serializer.Serialize((Policy)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("policy");
    }

    [Fact]
    public void Serialize_ValidPolicy_ReturnsJson()
    {
        var policy = CreatePolicy();
        var json = _serializer.Serialize(policy);
        json.ShouldNotBeNullOrWhiteSpace();
        json.ShouldContain("p-1");
    }

    #endregion

    #region DeserializePolicySet Guards

    [Fact]
    public void DeserializePolicySet_NullData_ReturnsLeft()
    {
        var result = _serializer.DeserializePolicySet(null!);
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicySet_EmptyData_ReturnsLeft()
    {
        var result = _serializer.DeserializePolicySet("");
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicySet_WhitespaceData_ReturnsLeft()
    {
        var result = _serializer.DeserializePolicySet("   ");
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicySet_InvalidJson_ReturnsLeft()
    {
        var result = _serializer.DeserializePolicySet("{not valid json");
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicySet_ValidJson_ReturnsRight()
    {
        var policySet = CreatePolicySet();
        var json = _serializer.Serialize(policySet);
        var result = _serializer.DeserializePolicySet(json);
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region DeserializePolicy Guards

    [Fact]
    public void DeserializePolicy_NullData_ReturnsLeft()
    {
        var result = _serializer.DeserializePolicy(null!);
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicy_EmptyData_ReturnsLeft()
    {
        var result = _serializer.DeserializePolicy("");
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicy_WhitespaceData_ReturnsLeft()
    {
        var result = _serializer.DeserializePolicy("   ");
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicy_InvalidJson_ReturnsLeft()
    {
        var result = _serializer.DeserializePolicy("{not valid json");
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicy_ValidJson_ReturnsRight()
    {
        var policy = CreatePolicy();
        var json = _serializer.Serialize(policy);
        var result = _serializer.DeserializePolicy(json);
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Round-trip

    [Fact]
    public void Roundtrip_PolicySet_PreservesData()
    {
        var policySet = new PolicySet
        {
            Id = "roundtrip-ps",
            Version = "1.0",
            Description = "Test policy set",
            Algorithm = CombiningAlgorithmId.PermitOverrides,
            IsEnabled = true,
            Priority = 5,
            Policies = [CreatePolicy("p-inner")],
            PolicySets = [],
            Obligations = [],
            Advice = []
        };
        var json = _serializer.Serialize(policySet);
        var result = _serializer.DeserializePolicySet(json);
        var deserialized = result.Match(Left: _ => null!, Right: ps => ps);
        deserialized.Id.ShouldBe("roundtrip-ps");
        deserialized.Version.ShouldBe("1.0");
        deserialized.Description.ShouldBe("Test policy set");
    }

    #endregion
}

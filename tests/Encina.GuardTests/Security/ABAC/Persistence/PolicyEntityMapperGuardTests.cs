using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;

using NSubstitute;

using Shouldly;

using Rule = Encina.Security.ABAC.Rule;

namespace Encina.GuardTests.Security.ABAC.Persistence;

/// <summary>
/// Guard clause tests for <see cref="PolicyEntityMapper"/>.
/// </summary>
public class PolicyEntityMapperGuardTests
{
    private static readonly IPolicySerializer Serializer = new DefaultPolicySerializer();
    private static readonly TimeProvider TimeProvider = TimeProvider.System;

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

    #region ToPolicySetEntity Guards

    [Fact]
    public void ToPolicySetEntity_NullPolicySet_ThrowsArgumentNullException()
    {
        var act = () => PolicyEntityMapper.ToPolicySetEntity(null!, Serializer, TimeProvider);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("policySet");
    }

    [Fact]
    public void ToPolicySetEntity_NullSerializer_ThrowsArgumentNullException()
    {
        var ps = CreatePolicySet();
        var act = () => PolicyEntityMapper.ToPolicySetEntity(ps, null!, TimeProvider);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("serializer");
    }

    [Fact]
    public void ToPolicySetEntity_NullTimeProvider_ThrowsArgumentNullException()
    {
        var ps = CreatePolicySet();
        var act = () => PolicyEntityMapper.ToPolicySetEntity(ps, Serializer, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void ToPolicySetEntity_ValidInput_ReturnsEntity()
    {
        var ps = new PolicySet { Id = "ps-1", Version = "1.0", Description = "desc", Algorithm = CombiningAlgorithmId.DenyOverrides, IsEnabled = true, Priority = 3, Policies = [CreatePolicy()], PolicySets = [], Obligations = [], Advice = [] };
        var entity = PolicyEntityMapper.ToPolicySetEntity(ps, Serializer, TimeProvider);
        entity.Id.ShouldBe("ps-1");
        entity.Version.ShouldBe("1.0");
        entity.Description.ShouldBe("desc");
        entity.IsEnabled.ShouldBeTrue();
        entity.Priority.ShouldBe(3);
        entity.PolicyJson.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ToPolicySetEntity_WithExistingEntity_PreservesCreatedAt()
    {
        var ps = CreatePolicySet();
        var existing = new PolicySetEntity { Id = "ps-1", PolicyJson = "{}", CreatedAtUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var entity = PolicyEntityMapper.ToPolicySetEntity(ps, Serializer, TimeProvider, existing);
        entity.CreatedAtUtc.ShouldBe(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    #endregion

    #region ToPolicySet Guards

    [Fact]
    public void ToPolicySet_NullEntity_ThrowsArgumentNullException()
    {
        Action act = () => PolicyEntityMapper.ToPolicySet(null!, Serializer);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("entity");
    }

    [Fact]
    public void ToPolicySet_NullSerializer_ThrowsArgumentNullException()
    {
        var entity = new PolicySetEntity { Id = "ps-1", PolicyJson = "{}" };
        Action act = () => PolicyEntityMapper.ToPolicySet(entity, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("serializer");
    }

    #endregion

    #region ToPolicyEntity Guards

    [Fact]
    public void ToPolicyEntity_NullPolicy_ThrowsArgumentNullException()
    {
        var act = () => PolicyEntityMapper.ToPolicyEntity(null!, Serializer, TimeProvider);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("policy");
    }

    [Fact]
    public void ToPolicyEntity_NullSerializer_ThrowsArgumentNullException()
    {
        var policy = CreatePolicy();
        var act = () => PolicyEntityMapper.ToPolicyEntity(policy, null!, TimeProvider);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("serializer");
    }

    [Fact]
    public void ToPolicyEntity_NullTimeProvider_ThrowsArgumentNullException()
    {
        var policy = CreatePolicy();
        var act = () => PolicyEntityMapper.ToPolicyEntity(policy, Serializer, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void ToPolicyEntity_ValidInput_ReturnsEntity()
    {
        var policy = new Policy { Id = "p-1", Version = "2.0", Description = "test policy", Algorithm = CombiningAlgorithmId.PermitOverrides, IsEnabled = true, Priority = 7, Rules = [new Rule { Id = "r-1", Effect = Effect.Deny, Obligations = [], Advice = [] }], Obligations = [], Advice = [], VariableDefinitions = [] };
        var entity = PolicyEntityMapper.ToPolicyEntity(policy, Serializer, TimeProvider);
        entity.Id.ShouldBe("p-1");
        entity.Version.ShouldBe("2.0");
        entity.Description.ShouldBe("test policy");
        entity.IsEnabled.ShouldBeTrue();
        entity.Priority.ShouldBe(7);
        entity.PolicyJson.ShouldNotBeNullOrWhiteSpace();
    }

    #endregion

    #region ToPolicy Guards

    [Fact]
    public void ToPolicy_NullEntity_ThrowsArgumentNullException()
    {
        Action act = () => PolicyEntityMapper.ToPolicy(null!, Serializer);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("entity");
    }

    [Fact]
    public void ToPolicy_NullSerializer_ThrowsArgumentNullException()
    {
        var entity = new PolicyEntity { Id = "p-1", PolicyJson = "{}" };
        Action act = () => PolicyEntityMapper.ToPolicy(entity, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("serializer");
    }

    #endregion
}

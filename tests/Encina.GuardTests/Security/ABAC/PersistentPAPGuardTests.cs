#pragma warning disable CA2012 // Use ValueTasks correctly — NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.Health;
using Encina.Security.ABAC.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard clause tests for Persistent PAP types in the Encina.Security.ABAC module.
/// Verifies that null and invalid arguments are properly rejected.
/// </summary>
public class PersistentPAPGuardTests
{
    #region PersistentPolicyAdministrationPoint — Constructor Guards

    [Fact]
    public void PersistentPAP_Constructor_NullStore_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<PersistentPolicyAdministrationPoint>();

        // Act
        var act = () => new PersistentPolicyAdministrationPoint(null!, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("store");
    }

    [Fact]
    public void PersistentPAP_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IPolicyStore>();

        // Act
        var act = () => new PersistentPolicyAdministrationPoint(store, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    #endregion

    #region PersistentPolicyAdministrationPoint — Method Guards

    [Fact]
    public void PersistentPAP_GetPolicySetAsync_NullId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.GetPolicySetAsync(null!);

        // Assert
        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void PersistentPAP_GetPolicySetAsync_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.GetPolicySetAsync("");

        // Assert
        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void PersistentPAP_GetPolicySetAsync_WhitespaceId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.GetPolicySetAsync("   ");

        // Assert
        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void PersistentPAP_AddPolicySetAsync_NullPolicySet_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.AddPolicySetAsync(null!);

        // Assert
        Should.ThrowAsync<ArgumentNullException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void PersistentPAP_UpdatePolicySetAsync_NullPolicySet_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.UpdatePolicySetAsync(null!);

        // Assert
        Should.ThrowAsync<ArgumentNullException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void PersistentPAP_RemovePolicySetAsync_NullId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.RemovePolicySetAsync(null!);

        // Assert
        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void PersistentPAP_RemovePolicySetAsync_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.RemovePolicySetAsync("");

        // Assert
        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void PersistentPAP_GetPolicyAsync_NullId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.GetPolicyAsync(null!);

        // Assert
        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void PersistentPAP_GetPolicyAsync_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.GetPolicyAsync("");

        // Assert
        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void PersistentPAP_AddPolicyAsync_NullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.AddPolicyAsync(null!, null);

        // Assert
        Should.ThrowAsync<ArgumentNullException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void PersistentPAP_UpdatePolicyAsync_NullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.UpdatePolicyAsync(null!);

        // Assert
        Should.ThrowAsync<ArgumentNullException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void PersistentPAP_RemovePolicyAsync_NullId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.RemovePolicyAsync(null!);

        // Assert
        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    [Fact]
    public void PersistentPAP_RemovePolicyAsync_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.RemovePolicyAsync("");

        // Assert
        Should.ThrowAsync<ArgumentException>(act).GetAwaiter().GetResult();
    }

    #endregion

    #region DefaultPolicySerializer — Guard Clauses

    [Fact]
    public void DefaultPolicySerializer_Serialize_NullPolicySet_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = () => serializer.Serialize((PolicySet)null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("policySet");
    }

    [Fact]
    public void DefaultPolicySerializer_Serialize_NullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = () => serializer.Serialize((Policy)null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("policy");
    }

    #endregion

    #region PolicyEntityMapper — Guard Clauses

    [Fact]
    public void PolicyEntityMapper_ToPolicySetEntity_NullPolicySet_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = () => PolicyEntityMapper.ToPolicySetEntity(null!, serializer, TimeProvider.System);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("policySet");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicySetEntity_NullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        var ps = CreateMinimalPolicySet();

        // Act
        var act = () => PolicyEntityMapper.ToPolicySetEntity(ps, null!, TimeProvider.System);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serializer");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicySetEntity_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var ps = CreateMinimalPolicySet();
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = () => PolicyEntityMapper.ToPolicySetEntity(ps, serializer, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicySet_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = (Action)(() => PolicyEntityMapper.ToPolicySet(null!, serializer));

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("entity");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicySet_NullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = new PolicySetEntity
        {
            Id = "ps-1",
            PolicyJson = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Act
        var act = (Action)(() => PolicyEntityMapper.ToPolicySet(entity, null!));

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serializer");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicyEntity_NullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = (Action)(() => PolicyEntityMapper.ToPolicyEntity(null!, serializer, TimeProvider.System));

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("policy");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicyEntity_NullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        var p = CreateMinimalPolicy();

        // Act
        var act = (Action)(() => PolicyEntityMapper.ToPolicyEntity(p, null!, TimeProvider.System));

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serializer");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicyEntity_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var p = CreateMinimalPolicy();
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = (Action)(() => PolicyEntityMapper.ToPolicyEntity(p, serializer, null!));

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicy_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = (Action)(() => PolicyEntityMapper.ToPolicy(null!, serializer));

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("entity");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicy_NullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = new PolicyEntity
        {
            Id = "p-1",
            PolicyJson = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Act
        var act = (Action)(() => PolicyEntityMapper.ToPolicy(entity, null!));

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serializer");
    }

    #endregion

    #region ABACHealthCheck — Constructor Guards

    [Fact]
    public void ABACHealthCheck_Constructor_NullPap_ThrowsArgumentNullException()
    {
        // Arrange
        var sp = new ServiceCollection().BuildServiceProvider();

        // Act
        var act = () => new ABACHealthCheck(null!, sp);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("pap");
    }

    [Fact]
    public void ABACHealthCheck_Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var pap = Substitute.For<IPolicyAdministrationPoint>();

        // Act
        var act = () => new ABACHealthCheck(pap, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serviceProvider");
    }

    #endregion

    #region ABACOptions — AddFunction Guards

    [Fact]
    public void ABACOptions_AddFunction_NullFunctionId_ThrowsArgumentException()
    {
        // Arrange
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        // Act
        var act = () => options.AddFunction(null!, function);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void ABACOptions_AddFunction_EmptyFunctionId_ThrowsArgumentException()
    {
        // Arrange
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        // Act
        var act = () => options.AddFunction("", function);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void ABACOptions_AddFunction_WhitespaceFunctionId_ThrowsArgumentException()
    {
        // Arrange
        var options = new ABACOptions();
        var function = Substitute.For<IXACMLFunction>();

        // Act
        var act = () => options.AddFunction("   ", function);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void ABACOptions_AddFunction_NullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ABACOptions();

        // Act
        var act = () => options.AddFunction("custom:test", null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("function");
    }

    #endregion

    // ── Helpers ──────────────────────────────────────────────────────

    private static PersistentPolicyAdministrationPoint CreatePersistentPAP()
    {
        var store = Substitute.For<IPolicyStore>();
        var logger = NullLoggerFactory.Instance.CreateLogger<PersistentPolicyAdministrationPoint>();
        return new PersistentPolicyAdministrationPoint(store, logger);
    }

    private static PolicySet CreateMinimalPolicySet() => new()
    {
        Id = "ps-1",
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies = [],
        PolicySets = [],
        Obligations = [],
        Advice = []
    };

    private static Policy CreateMinimalPolicy() => new()
    {
        Id = "p-1",
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };
}

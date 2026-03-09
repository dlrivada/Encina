#pragma warning disable CA2012 // Use ValueTasks correctly — NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.Health;
using Encina.Security.ABAC.Persistence;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

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
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("store");
    }

    [Fact]
    public void PersistentPAP_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IPolicyStore>();

        // Act
        var act = () => new PersistentPolicyAdministrationPoint(store, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
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
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void PersistentPAP_GetPolicySetAsync_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.GetPolicySetAsync("");

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void PersistentPAP_GetPolicySetAsync_WhitespaceId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.GetPolicySetAsync("   ");

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void PersistentPAP_AddPolicySetAsync_NullPolicySet_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.AddPolicySetAsync(null!);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void PersistentPAP_UpdatePolicySetAsync_NullPolicySet_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.UpdatePolicySetAsync(null!);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void PersistentPAP_RemovePolicySetAsync_NullId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.RemovePolicySetAsync(null!);

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void PersistentPAP_RemovePolicySetAsync_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.RemovePolicySetAsync("");

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void PersistentPAP_GetPolicyAsync_NullId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.GetPolicyAsync(null!);

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void PersistentPAP_GetPolicyAsync_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.GetPolicyAsync("");

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void PersistentPAP_AddPolicyAsync_NullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.AddPolicyAsync(null!, null);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void PersistentPAP_UpdatePolicyAsync_NullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.UpdatePolicyAsync(null!);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void PersistentPAP_RemovePolicyAsync_NullId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.RemovePolicyAsync(null!);

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void PersistentPAP_RemovePolicyAsync_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreatePersistentPAP();

        // Act
        var act = async () => await sut.RemovePolicyAsync("");

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
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
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policySet");
    }

    [Fact]
    public void DefaultPolicySerializer_Serialize_NullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = () => serializer.Serialize((Policy)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policy");
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
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policySet");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicySetEntity_NullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        var ps = CreateMinimalPolicySet();

        // Act
        var act = () => PolicyEntityMapper.ToPolicySetEntity(ps, null!, TimeProvider.System);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serializer");
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
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("timeProvider");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicySet_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = () => PolicyEntityMapper.ToPolicySet(null!, serializer);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entity");
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
        var act = () => PolicyEntityMapper.ToPolicySet(entity, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serializer");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicyEntity_NullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = () => PolicyEntityMapper.ToPolicyEntity(null!, serializer, TimeProvider.System);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policy");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicyEntity_NullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        var p = CreateMinimalPolicy();

        // Act
        var act = () => PolicyEntityMapper.ToPolicyEntity(p, null!, TimeProvider.System);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serializer");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicyEntity_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var p = CreateMinimalPolicy();
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = () => PolicyEntityMapper.ToPolicyEntity(p, serializer, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("timeProvider");
    }

    [Fact]
    public void PolicyEntityMapper_ToPolicy_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new DefaultPolicySerializer();

        // Act
        var act = () => PolicyEntityMapper.ToPolicy(null!, serializer);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entity");
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
        var act = () => PolicyEntityMapper.ToPolicy(entity, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serializer");
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
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pap");
    }

    [Fact]
    public void ABACHealthCheck_Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var pap = Substitute.For<IPolicyAdministrationPoint>();

        // Act
        var act = () => new ABACHealthCheck(pap, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ABACOptions_AddFunction_NullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ABACOptions();

        // Act
        var act = () => options.AddFunction("custom:test", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("function");
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

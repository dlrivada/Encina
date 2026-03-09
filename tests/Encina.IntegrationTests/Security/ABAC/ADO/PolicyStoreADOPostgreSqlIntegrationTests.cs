using System.Data;
using Encina.ADO.PostgreSQL.ABAC;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using Encina.TestInfrastructure.Fixtures;

using Rule = Encina.Security.ABAC.Rule;

namespace Encina.IntegrationTests.Security.ABAC.ADO;

/// <summary>
/// Integration tests for <see cref="PolicyStoreADO"/> with PostgreSQL.
/// Tests full CRUD and round-trip persistence of XACML 3.0 policy sets and policies.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("ADO-PostgreSQL")]
public sealed class PolicyStoreADOPostgreSqlIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private PolicyStoreADO _store = null!;

    public PolicyStoreADOPostgreSqlIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();

        var connection = _fixture.CreateConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        var serializer = new DefaultPolicySerializer();
        _store = new PolicyStoreADO(connection, serializer);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // ── PolicySet CRUD ──────────────────────────────────────────────

    [Fact]
    public async Task SavePolicySetAsync_NewPolicySet_ShouldPersist()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet("ps-ado-pg-1");

        // Act
        var result = await _store.SavePolicySetAsync(policySet);

        // Assert
        result.IsRight.ShouldBeTrue("SavePolicySetAsync should succeed");

        var getResult = await _store.GetPolicySetAsync("ps-ado-pg-1");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsSome.ShouldBeTrue("PolicySet should be found after save"));
    }

    [Fact]
    public async Task SavePolicySetAsync_UpdateExisting_ShouldUpsert()
    {
        // Arrange
        var original = CreateMinimalPolicySet("ps-ado-pg-upsert");
        await _store.SavePolicySetAsync(original);

        var updated = original with { Description = "Updated description" };

        // Act
        var result = await _store.SavePolicySetAsync(updated);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicySetAsync("ps-ado-pg-upsert");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IfSome(ps =>
            ps.Description.ShouldBe("Updated description")));
    }

    [Fact]
    public async Task GetAllPolicySetsAsync_WithMultipleSets_ShouldReturnAll()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ado-pg-all-1"));
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ado-pg-all-2"));
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ado-pg-all-3"));

        // Act
        var result = await _store.GetAllPolicySetsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(sets => sets.Count.ShouldBeGreaterThanOrEqualTo(3));
    }

    [Fact]
    public async Task GetPolicySetAsync_NonExistentId_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetPolicySetAsync("non-existent-ado-pg");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task DeletePolicySetAsync_ExistingSet_ShouldRemove()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ado-pg-delete"));

        // Act
        var result = await _store.DeletePolicySetAsync("ps-ado-pg-delete");

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicySetAsync("ps-ado-pg-delete");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicySetAsync_ExistingSet_ShouldReturnTrue()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ado-pg-exists"));

        // Act
        var result = await _store.ExistsPolicySetAsync("ps-ado-pg-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicySetAsync_NonExistent_ShouldReturnFalse()
    {
        // Act
        var result = await _store.ExistsPolicySetAsync("ps-ado-pg-not-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeFalse());
    }

    [Fact]
    public async Task GetPolicySetCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ado-pg-cnt-1"));
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ado-pg-cnt-2"));

        // Act
        var result = await _store.GetPolicySetCountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBeGreaterThanOrEqualTo(2));
    }

    // ── Policy CRUD ─────────────────────────────────────────────────

    [Fact]
    public async Task SavePolicyAsync_NewPolicy_ShouldPersist()
    {
        // Arrange
        var policy = CreateMinimalPolicy("p-ado-pg-1");

        // Act
        var result = await _store.SavePolicyAsync(policy);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicyAsync("p-ado-pg-1");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsSome.ShouldBeTrue());
    }

    [Fact]
    public async Task SavePolicyAsync_UpdateExisting_ShouldUpsert()
    {
        // Arrange
        var original = CreateMinimalPolicy("p-ado-pg-upsert");
        await _store.SavePolicyAsync(original);

        var updated = original with { Description = "Updated policy" };

        // Act
        var result = await _store.SavePolicyAsync(updated);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicyAsync("p-ado-pg-upsert");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IfSome(p =>
            p.Description.ShouldBe("Updated policy")));
    }

    [Fact]
    public async Task GetAllStandalonePoliciesAsync_ShouldReturnAllPolicies()
    {
        // Arrange
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-ado-pg-all-1"));
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-ado-pg-all-2"));

        // Act
        var result = await _store.GetAllStandalonePoliciesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(policies => policies.Count.ShouldBeGreaterThanOrEqualTo(2));
    }

    [Fact]
    public async Task GetPolicyAsync_NonExistentId_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetPolicyAsync("non-existent-ado-pg-p");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task DeletePolicyAsync_ExistingPolicy_ShouldRemove()
    {
        // Arrange
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-ado-pg-delete"));

        // Act
        var result = await _store.DeletePolicyAsync("p-ado-pg-delete");

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicyAsync("p-ado-pg-delete");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicyAsync_ExistingPolicy_ShouldReturnTrue()
    {
        // Arrange
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-ado-pg-exists"));

        // Act
        var result = await _store.ExistsPolicyAsync("p-ado-pg-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicyAsync_NonExistent_ShouldReturnFalse()
    {
        // Act
        var result = await _store.ExistsPolicyAsync("p-ado-pg-not-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeFalse());
    }

    // ── Round-Trip Tests ─────────────────────────────────────────────

    [Fact]
    public async Task PolicySet_RoundTrip_PreservesAllProperties()
    {
        // Arrange
        var rule = new Rule
        {
            Id = "rule-ado-pg-rt-1",
            Effect = Effect.Permit,
            Obligations = [],
            Advice = [],
            Target = new Target
            {
                AnyOfElements =
                [
                    new AnyOf
                    {
                        AllOfElements =
                        [
                            new AllOf
                            {
                                Matches =
                                [
                                    new Match
                                    {
                                        FunctionId = "urn:oasis:names:tc:xacml:1.0:function:string-equal",
                                        AttributeDesignator = new AttributeDesignator
                                        {
                                            AttributeId = "urn:oasis:names:tc:xacml:1.0:subject:subject-id",
                                            Category = AttributeCategory.Subject,
                                            DataType = "http://www.w3.org/2001/XMLSchema#string"
                                        },
                                        AttributeValue = new AttributeValue
                                        {
                                            DataType = "http://www.w3.org/2001/XMLSchema#string",
                                            Value = "admin"
                                        }
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var policy = new Policy
        {
            Id = "p-ado-pg-rt-1",
            Description = "Test round-trip policy",
            Version = "1.0",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules = [rule],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        };

        var policySet = new PolicySet
        {
            Id = "ps-ado-pg-rt-1",
            Description = "Test round-trip policy set",
            Version = "2.0",
            Target = null,
            Algorithm = CombiningAlgorithmId.PermitOverrides,
            Policies = [policy],
            PolicySets = [],
            Obligations = [],
            Advice = []
        };

        // Act
        await _store.SavePolicySetAsync(policySet);
        var result = await _store.GetPolicySetAsync("ps-ado-pg-rt-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IfSome(retrieved =>
        {
            retrieved.Id.ShouldBe("ps-ado-pg-rt-1");
            retrieved.Description.ShouldBe("Test round-trip policy set");
            retrieved.Version.ShouldBe("2.0");
            retrieved.Algorithm.ShouldBe(CombiningAlgorithmId.PermitOverrides);
            retrieved.Policies.Count.ShouldBe(1);

            var retrievedPolicy = retrieved.Policies[0];
            retrievedPolicy.Id.ShouldBe("p-ado-pg-rt-1");
            retrievedPolicy.Rules.Count.ShouldBe(1);
            retrievedPolicy.Rules[0].Effect.ShouldBe(Effect.Permit);
            retrievedPolicy.Rules[0].Target.ShouldNotBeNull();
        }));
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static PolicySet CreateMinimalPolicySet(string id) => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies = [],
        PolicySets = [],
        Obligations = [],
        Advice = []
    };

    private static Policy CreateMinimalPolicy(string id) => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };
}

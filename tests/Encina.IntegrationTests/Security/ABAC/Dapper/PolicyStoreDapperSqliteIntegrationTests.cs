using System.Data;
using Encina.Dapper.Sqlite.ABAC;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using Encina.TestInfrastructure.Fixtures;

using Rule = Encina.Security.ABAC.Rule;

namespace Encina.IntegrationTests.Security.ABAC.Dapper;

/// <summary>
/// Integration tests for <see cref="PolicyStoreDapper"/> with SQLite.
/// Tests full CRUD and round-trip persistence of XACML 3.0 policy sets and policies.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("Dapper-Sqlite")]
public sealed class PolicyStoreDapperSqliteIntegrationTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;
    private PolicyStoreDapper _store = null!;

    public PolicyStoreDapperSqliteIntegrationTests(SqliteFixture fixture)
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
        _store = new PolicyStoreDapper(connection, serializer);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // ── PolicySet CRUD ──────────────────────────────────────────────

    [Fact]
    public async Task SavePolicySetAsync_NewPolicySet_ShouldPersist()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet("ps-dap-1");

        // Act
        var result = await _store.SavePolicySetAsync(policySet);

        // Assert
        result.IsRight.ShouldBeTrue("SavePolicySetAsync should succeed");

        var getResult = await _store.GetPolicySetAsync("ps-dap-1");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsSome.ShouldBeTrue("PolicySet should be found after save"));
    }

    [Fact]
    public async Task SavePolicySetAsync_UpdateExisting_ShouldUpsert()
    {
        // Arrange
        var original = CreateMinimalPolicySet("ps-dap-upsert");
        await _store.SavePolicySetAsync(original);

        var updated = original with { Description = "Updated via Dapper" };

        // Act
        var result = await _store.SavePolicySetAsync(updated);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicySetAsync("ps-dap-upsert");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IfSome(ps =>
            ps.Description.ShouldBe("Updated via Dapper")));
    }

    [Fact]
    public async Task GetAllPolicySetsAsync_WithMultipleSets_ShouldReturnAll()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-dap-all-1"));
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-dap-all-2"));
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-dap-all-3"));

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
        var result = await _store.GetPolicySetAsync("non-existent-dap");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task DeletePolicySetAsync_ExistingSet_ShouldRemove()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-dap-delete"));

        // Act
        var result = await _store.DeletePolicySetAsync("ps-dap-delete");

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicySetAsync("ps-dap-delete");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicySetAsync_ExistingSet_ShouldReturnTrue()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-dap-exists"));

        // Act
        var result = await _store.ExistsPolicySetAsync("ps-dap-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicySetAsync_NonExistent_ShouldReturnFalse()
    {
        // Act
        var result = await _store.ExistsPolicySetAsync("ps-dap-not-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeFalse());
    }

    [Fact]
    public async Task GetPolicySetCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-dap-cnt-1"));
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-dap-cnt-2"));

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
        var policy = CreateMinimalPolicy("p-dap-1");

        // Act
        var result = await _store.SavePolicyAsync(policy);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicyAsync("p-dap-1");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsSome.ShouldBeTrue());
    }

    [Fact]
    public async Task SavePolicyAsync_UpdateExisting_ShouldUpsert()
    {
        // Arrange
        var original = CreateMinimalPolicy("p-dap-upsert");
        await _store.SavePolicyAsync(original);

        var updated = original with { Description = "Updated policy via Dapper" };

        // Act
        var result = await _store.SavePolicyAsync(updated);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicyAsync("p-dap-upsert");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IfSome(p =>
            p.Description.ShouldBe("Updated policy via Dapper")));
    }

    [Fact]
    public async Task GetAllStandalonePoliciesAsync_ShouldReturnAllPolicies()
    {
        // Arrange
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-dap-all-1"));
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-dap-all-2"));

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
        var result = await _store.GetPolicyAsync("non-existent-dap-p");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task DeletePolicyAsync_ExistingPolicy_ShouldRemove()
    {
        // Arrange
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-dap-delete"));

        // Act
        var result = await _store.DeletePolicyAsync("p-dap-delete");

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicyAsync("p-dap-delete");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicyAsync_ExistingPolicy_ShouldReturnTrue()
    {
        // Arrange
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-dap-exists"));

        // Act
        var result = await _store.ExistsPolicyAsync("p-dap-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicyAsync_NonExistent_ShouldReturnFalse()
    {
        // Act
        var result = await _store.ExistsPolicyAsync("p-dap-not-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeFalse());
    }

    // ── Round-Trip Tests ─────────────────────────────────────────────

    [Fact]
    public async Task PolicySet_RoundTrip_PreservesNestedPolicies()
    {
        // Arrange
        var policy = new Policy
        {
            Id = "p-dap-rt-nested",
            Description = "Nested policy for round-trip",
            Version = "1.0",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "rule-dap-1",
                    Effect = Effect.Permit,
                    Description = "Allow rule",
                    Obligations = [],
                    Advice = []
                },
                new Rule
                {
                    Id = "rule-dap-2",
                    Effect = Effect.Deny,
                    Description = "Deny rule",
                    Obligations = [],
                    Advice = []
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        };

        var policySet = new PolicySet
        {
            Id = "ps-dap-rt",
            Description = "Round-trip policy set",
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
        var result = await _store.GetPolicySetAsync("ps-dap-rt");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IfSome(retrieved =>
        {
            retrieved.Id.ShouldBe("ps-dap-rt");
            retrieved.Policies.Count.ShouldBe(1);
            retrieved.Policies[0].Rules.Count.ShouldBe(2);
            retrieved.Policies[0].Rules[0].Effect.ShouldBe(Effect.Permit);
            retrieved.Policies[0].Rules[1].Effect.ShouldBe(Effect.Deny);
        }));
    }

    [Fact]
    public async Task Policy_RoundTrip_WithTargetExpression()
    {
        // Arrange
        var policy = new Policy
        {
            Id = "p-dap-target-rt",
            Description = "Policy with target",
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
                                            AttributeId = "urn:custom:role",
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
            },
            Algorithm = CombiningAlgorithmId.FirstApplicable,
            Rules = [new Rule { Id = "r-dap-tgt", Effect = Effect.Permit, Obligations = [], Advice = [] }],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        };

        // Act
        await _store.SavePolicyAsync(policy);
        var result = await _store.GetPolicyAsync("p-dap-target-rt");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IfSome(retrieved =>
        {
            retrieved.Target.ShouldNotBeNull();
            retrieved.Target!.AnyOfElements.Count.ShouldBe(1);
            retrieved.Target.AnyOfElements[0].AllOfElements[0].Matches[0].AttributeValue.Value!.ToString().ShouldBe("admin");
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

using Encina.EntityFrameworkCore.ABAC;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Rule = Encina.Security.ABAC.Rule;

namespace Encina.IntegrationTests.Security.ABAC.EFCore;

/// <summary>
/// Integration tests for <see cref="PolicyStoreEF"/> with SQL Server via EF Core.
/// Tests full CRUD and round-trip persistence of XACML 3.0 policy sets and policies.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class PolicyStoreEFCoreSqlServerIntegrationTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;
    private PolicyStoreEF _store = null!;
    private ABACTestDbContext _dbContext = null!;

    public PolicyStoreEFCoreSqlServerIntegrationTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();

        _dbContext = _fixture.CreateDbContext<ABACTestDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        var serializer = new DefaultPolicySerializer();
        _store = new PolicyStoreEF(_dbContext, serializer);
    }

    public ValueTask DisposeAsync()
    {
        _dbContext?.Dispose();
        return ValueTask.CompletedTask;
    }

    // ── PolicySet CRUD ──────────────────────────────────────────────

    [Fact]
    public async Task SavePolicySetAsync_NewPolicySet_ShouldPersist()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet("ps-ef-ss-1");

        // Act
        var result = await _store.SavePolicySetAsync(policySet);

        // Assert
        result.IsRight.ShouldBeTrue("SavePolicySetAsync should succeed");

        var getResult = await _store.GetPolicySetAsync("ps-ef-ss-1");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsSome.ShouldBeTrue("PolicySet should be found after save"));
    }

    [Fact]
    public async Task SavePolicySetAsync_UpdateExisting_ShouldUpsert()
    {
        // Arrange
        var original = CreateMinimalPolicySet("ps-ef-ss-upsert");
        await _store.SavePolicySetAsync(original);

        var updated = original with { Description = "Updated via EF Core" };

        // Act
        var result = await _store.SavePolicySetAsync(updated);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicySetAsync("ps-ef-ss-upsert");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IfSome(ps =>
            ps.Description.ShouldBe("Updated via EF Core")));
    }

    [Fact]
    public async Task GetAllPolicySetsAsync_WithMultipleSets_ShouldReturnAll()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-ss-all-1"));
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-ss-all-2"));
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-ss-all-3"));

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
        var result = await _store.GetPolicySetAsync("non-existent-ef-ss");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task DeletePolicySetAsync_ExistingSet_ShouldRemove()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-ss-delete"));

        // Act
        var result = await _store.DeletePolicySetAsync("ps-ef-ss-delete");

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicySetAsync("ps-ef-ss-delete");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicySetAsync_ExistingSet_ShouldReturnTrue()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-ss-exists"));

        // Act
        var result = await _store.ExistsPolicySetAsync("ps-ef-ss-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicySetAsync_NonExistent_ShouldReturnFalse()
    {
        // Act
        var result = await _store.ExistsPolicySetAsync("ps-ef-ss-not-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeFalse());
    }

    [Fact]
    public async Task GetPolicySetCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-ss-cnt-1"));
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-ss-cnt-2"));

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
        var policy = CreateMinimalPolicy("p-ef-ss-1");

        // Act
        var result = await _store.SavePolicyAsync(policy);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicyAsync("p-ef-ss-1");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsSome.ShouldBeTrue());
    }

    [Fact]
    public async Task SavePolicyAsync_UpdateExisting_ShouldUpsert()
    {
        // Arrange
        var original = CreateMinimalPolicy("p-ef-ss-upsert");
        await _store.SavePolicyAsync(original);

        var updated = original with { Description = "Updated via EF Core" };

        // Act
        var result = await _store.SavePolicyAsync(updated);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicyAsync("p-ef-ss-upsert");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IfSome(p =>
            p.Description.ShouldBe("Updated via EF Core")));
    }

    [Fact]
    public async Task GetAllStandalonePoliciesAsync_ShouldReturnAllPolicies()
    {
        // Arrange
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-ef-ss-all-1"));
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-ef-ss-all-2"));

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
        var result = await _store.GetPolicyAsync("non-existent-ef-ss-p");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task DeletePolicyAsync_ExistingPolicy_ShouldRemove()
    {
        // Arrange
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-ef-ss-delete"));

        // Act
        var result = await _store.DeletePolicyAsync("p-ef-ss-delete");

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicyAsync("p-ef-ss-delete");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicyAsync_ExistingPolicy_ShouldReturnTrue()
    {
        // Arrange
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-ef-ss-exists"));

        // Act
        var result = await _store.ExistsPolicyAsync("p-ef-ss-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicyAsync_NonExistent_ShouldReturnFalse()
    {
        // Act
        var result = await _store.ExistsPolicyAsync("p-ef-ss-not-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeFalse());
    }

    // ── Round-Trip Tests ─────────────────────────────────────────────

    [Fact]
    public async Task PolicySet_RoundTrip_PreservesCompleteGraph()
    {
        // Arrange
        var policySet = new PolicySet
        {
            Id = "ps-ef-ss-rt",
            Description = "EF Core SQL Server round-trip test",
            Version = "1.0",
            Target = null,
            Algorithm = CombiningAlgorithmId.PermitOverrides,
            Policies =
            [
                new Policy
                {
                    Id = "p-ef-ss-rt-inner",
                    Description = "Inner policy",
                    Target = null,
                    Algorithm = CombiningAlgorithmId.DenyOverrides,
                    Rules =
                    [
                        new Rule
                        {
                            Id = "r-ef-ss-1",
                            Effect = Effect.Permit,
                            Description = "Permit rule",
                            Obligations = [],
                            Advice = []
                        },
                        new Rule
                        {
                            Id = "r-ef-ss-2",
                            Effect = Effect.Deny,
                            Description = "Deny rule",
                            Obligations = [],
                            Advice = []
                        }
                    ],
                    Obligations = [],
                    Advice = [],
                    VariableDefinitions = []
                }
            ],
            PolicySets = [],
            Obligations = [],
            Advice = []
        };

        // Act
        await _store.SavePolicySetAsync(policySet);
        var result = await _store.GetPolicySetAsync("ps-ef-ss-rt");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IfSome(retrieved =>
        {
            retrieved.Id.ShouldBe("ps-ef-ss-rt");
            retrieved.Policies.Count.ShouldBe(1);
            retrieved.Policies[0].Rules.Count.ShouldBe(2);
            retrieved.Policies[0].Rules[0].Effect.ShouldBe(Effect.Permit);
            retrieved.Policies[0].Rules[1].Effect.ShouldBe(Effect.Deny);
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

    /// <summary>
    /// Test DbContext configured with ABAC entity mappings.
    /// </summary>
    private sealed class ABACTestDbContext : DbContext
    {
        public ABACTestDbContext(DbContextOptions<ABACTestDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyABACConfiguration();
        }
    }
}

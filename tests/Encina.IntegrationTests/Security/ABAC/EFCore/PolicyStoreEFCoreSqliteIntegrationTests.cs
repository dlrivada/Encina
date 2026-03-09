using Encina.EntityFrameworkCore.ABAC;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Rule = Encina.Security.ABAC.Rule;

namespace Encina.IntegrationTests.Security.ABAC.EFCore;

/// <summary>
/// Integration tests for <see cref="PolicyStoreEF"/> with SQLite via EF Core.
/// Tests full CRUD and round-trip persistence of XACML 3.0 policy sets and policies.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("EFCore-Sqlite")]
public sealed class PolicyStoreEFCoreSqliteIntegrationTests : IAsyncLifetime
{
    private readonly EFCoreSqliteFixture _fixture;
    private PolicyStoreEF _store = null!;
    private ABACTestDbContext _dbContext = null!;

    public PolicyStoreEFCoreSqliteIntegrationTests(EFCoreSqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();

        // Create a test DbContext configured with ABAC entity mappings
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
        var policySet = CreateMinimalPolicySet("ps-ef-1");

        // Act
        var result = await _store.SavePolicySetAsync(policySet);

        // Assert
        result.IsRight.ShouldBeTrue("SavePolicySetAsync should succeed");

        var getResult = await _store.GetPolicySetAsync("ps-ef-1");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsSome.ShouldBeTrue("PolicySet should be found after save"));
    }

    [Fact]
    public async Task SavePolicySetAsync_UpdateExisting_ShouldUpsert()
    {
        // Arrange
        var original = CreateMinimalPolicySet("ps-ef-upsert");
        await _store.SavePolicySetAsync(original);

        var updated = original with { Description = "Updated via EF Core" };

        // Act
        var result = await _store.SavePolicySetAsync(updated);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicySetAsync("ps-ef-upsert");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IfSome(ps =>
            ps.Description.ShouldBe("Updated via EF Core")));
    }

    [Fact]
    public async Task GetAllPolicySetsAsync_WithMultipleSets_ShouldReturnAll()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-all-1"));
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-all-2"));
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-all-3"));

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
        var result = await _store.GetPolicySetAsync("non-existent-ef");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task DeletePolicySetAsync_ExistingSet_ShouldRemove()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-delete"));

        // Act
        var result = await _store.DeletePolicySetAsync("ps-ef-delete");

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicySetAsync("ps-ef-delete");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicySetAsync_ExistingSet_ShouldReturnTrue()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-exists"));

        // Act
        var result = await _store.ExistsPolicySetAsync("ps-ef-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicySetAsync_NonExistent_ShouldReturnFalse()
    {
        // Act
        var result = await _store.ExistsPolicySetAsync("ps-ef-not-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeFalse());
    }

    [Fact]
    public async Task GetPolicySetCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-cnt-1"));
        await _store.SavePolicySetAsync(CreateMinimalPolicySet("ps-ef-cnt-2"));

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
        var policy = CreateMinimalPolicy("p-ef-1");

        // Act
        var result = await _store.SavePolicyAsync(policy);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicyAsync("p-ef-1");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsSome.ShouldBeTrue());
    }

    [Fact]
    public async Task SavePolicyAsync_UpdateExisting_ShouldUpsert()
    {
        // Arrange
        var original = CreateMinimalPolicy("p-ef-upsert");
        await _store.SavePolicyAsync(original);

        var updated = original with { Description = "Updated via EF Core" };

        // Act
        var result = await _store.SavePolicyAsync(updated);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicyAsync("p-ef-upsert");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IfSome(p =>
            p.Description.ShouldBe("Updated via EF Core")));
    }

    [Fact]
    public async Task GetAllStandalonePoliciesAsync_ShouldReturnAllPolicies()
    {
        // Arrange
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-ef-all-1"));
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-ef-all-2"));

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
        var result = await _store.GetPolicyAsync("non-existent-ef-p");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task DeletePolicyAsync_ExistingPolicy_ShouldRemove()
    {
        // Arrange
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-ef-delete"));

        // Act
        var result = await _store.DeletePolicyAsync("p-ef-delete");

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await _store.GetPolicyAsync("p-ef-delete");
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(opt => opt.IsNone.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicyAsync_ExistingPolicy_ShouldReturnTrue()
    {
        // Arrange
        await _store.SavePolicyAsync(CreateMinimalPolicy("p-ef-exists"));

        // Act
        var result = await _store.ExistsPolicyAsync("p-ef-exists");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsPolicyAsync_NonExistent_ShouldReturnFalse()
    {
        // Act
        var result = await _store.ExistsPolicyAsync("p-ef-not-exists");

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
            Id = "ps-ef-rt",
            Description = "EF Core round-trip test",
            Version = "1.0",
            Target = null,
            Algorithm = CombiningAlgorithmId.PermitOverrides,
            Policies =
            [
                new Policy
                {
                    Id = "p-ef-rt-inner",
                    Description = "Inner policy",
                    Target = null,
                    Algorithm = CombiningAlgorithmId.DenyOverrides,
                    Rules =
                    [
                        new Rule
                        {
                            Id = "r-ef-1",
                            Effect = Effect.Permit,
                            Description = "Permit rule",
                            Obligations = [],
                            Advice = []
                        },
                        new Rule
                        {
                            Id = "r-ef-2",
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
        var result = await _store.GetPolicySetAsync("ps-ef-rt");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(opt => opt.IfSome(retrieved =>
        {
            retrieved.Id.ShouldBe("ps-ef-rt");
            retrieved.Description.ShouldBe("EF Core round-trip test");
            retrieved.Version.ShouldBe("1.0");
            retrieved.Algorithm.ShouldBe(CombiningAlgorithmId.PermitOverrides);
            retrieved.Policies.Count.ShouldBe(1);

            var inner = retrieved.Policies[0];
            inner.Id.ShouldBe("p-ef-rt-inner");
            inner.Rules.Count.ShouldBe(2);
            inner.Rules[0].Effect.ShouldBe(Effect.Permit);
            inner.Rules[1].Effect.ShouldBe(Effect.Deny);
        }));
    }

    [Fact]
    public async Task SavePolicySetAsync_UpdatePreservesCreatedAtUtc()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet("ps-ef-ts");
        await _store.SavePolicySetAsync(policySet);

        // Read entity directly to get CreatedAtUtc
        var entity = await _dbContext.Set<PolicySetEntity>().FindAsync("ps-ef-ts");
        var originalCreated = entity!.CreatedAtUtc;

        // Allow a small time gap
        await Task.Delay(50);

        // Update
        var updated = policySet with { Description = "After update" };
        await _store.SavePolicySetAsync(updated);

        // Act - Read again
        _dbContext.ChangeTracker.Clear();
        var updatedEntity = await _dbContext.Set<PolicySetEntity>().FindAsync("ps-ef-ts");

        // Assert
        updatedEntity!.CreatedAtUtc.ShouldBe(originalCreated, "CreatedAtUtc should be preserved on update");
        updatedEntity.UpdatedAtUtc.ShouldBeGreaterThanOrEqualTo(originalCreated);
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

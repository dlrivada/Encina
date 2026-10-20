#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using System.Collections.Concurrent;

using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using Encina.Security.Audit;
using Encina.Testing.Identity;
using LanguageExt;
using Microsoft.Extensions.Hosting;

namespace Encina.IntegrationTests.Security.ABAC;

/// <summary>
/// The scenario shared by the persistent-PAP registration tests of every provider family (#1707):
/// the provider's real registration (a scoped <see cref="IPolicyStore"/>) is combined with
/// <c>AddEncinaABAC(o =&gt; o.UsePersistentPAP = true)</c> under <c>ValidateOnBuild</c> and
/// <c>ValidateScopes</c>, the seeding hosted service runs under the built-in service identity, and
/// user changes go through the PAP inside an identity scope (#1705 Phase 4). No identity service is
/// substituted: the real <see cref="IRequestContextAccessor"/> and scope factory are used.
/// </summary>
internal static class PersistentPapScopeScenario
{
    private const string SeedingSubject = "service:encina.abac.policy-seeding";
    private const string UserSubject = "scope-test-user";

    /// <summary>Returns a policy with the given identifier.</summary>
    public static Policy CreatePolicy(string id, string? description = null) => new()
    {
        Id = id,
        Description = description,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    /// <summary>
    /// Adds the ABAC registration to <paramref name="services"/> (which already hold the provider's
    /// real registration), builds the provider with scope validation, seeds, changes a policy as a
    /// user, and checks the persisted policies and the audit actor of each change.
    /// </summary>
    public static async Task RunAsync(IServiceCollection services)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var seededId = $"seeded-{suffix}";
        var addedId = $"added-{suffix}";
        var audit = new ConcurrentQueue<OperationAuditEntry>();

        services.AddLogging();
        services.AddScoped(_ => RecordingAuditStore(audit));
        services.AddEncinaABAC(options =>
        {
            options.UsePersistentPAP = true;
            options.SeedPolicies.Add(CreatePolicy(seededId));
        });

        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var pap = provider.GetRequiredService<IPolicyAdministrationPoint>();
        var scopes = provider.GetRequiredService<IRequestContextScopeFactory>();
        var seeder = provider.GetServices<IHostedService>()
            .Single(s => s.GetType().Name == "ABACPolicySeedingHostedService");
        await seeder.StartAsync(CancellationToken.None);

        // Without an identity scope a change has no caller and is refused (fail closed).
        (await pap.AddPolicyAsync(CreatePolicy($"anonymous-{suffix}"), parentPolicySetId: null)).IsLeft
            .ShouldBeTrue("a policy change without an authenticated caller is refused");

        var asUser = await scopes.RunAsPrincipalAsync(
            TestIdentity.Principal(UserSubject),
            async (_, ct) =>
            {
                (await pap.AddPolicyAsync(CreatePolicy(addedId), parentPolicySetId: null, ct)).IsRight.ShouldBeTrue();
                (await pap.UpdatePolicyAsync(CreatePolicy(addedId, "updated"), ct)).IsRight.ShouldBeTrue();
                return Prelude.Right<EncinaError, LanguageExt.Unit>(Prelude.unit);
            });
        asUser.IsRight.ShouldBeTrue();

        var seeded = await pap.GetPolicyAsync(seededId);
        seeded.IsRight.ShouldBeTrue();
        seeded.IfRight(option => option.IsSome.ShouldBeTrue("the seeding hosted service persisted the policy"));

        var updated = await pap.GetPolicyAsync(addedId);
        updated.IfRight(option => option.IfSome(policy => policy.Description.ShouldBe("updated")));
        updated.IfRight(option => option.IsSome.ShouldBeTrue());

        var entries = audit.ToList();
        entries.ShouldContain(e => e.EntityId == seededId && e.UserId == SeedingSubject
            && (string?)e.Metadata["actor"] == "service");
        entries.Where(e => e.EntityId == addedId)
            .ShouldAllBe(e => e.UserId == UserSubject && (string?)e.Metadata["actor"] == "user");
        entries.Count(e => e.EntityId == addedId).ShouldBe(2);

        await scopes.RunAsPrincipalAsync(
            TestIdentity.Principal(UserSubject),
            async (_, ct) =>
            {
                (await pap.RemovePolicyAsync(addedId, ct)).IsRight.ShouldBeTrue();
                (await pap.RemovePolicyAsync(seededId, ct)).IsRight.ShouldBeTrue();
                return Prelude.Right<EncinaError, LanguageExt.Unit>(Prelude.unit);
            });
    }

    // An operation audit store that records every entry, so the scenario can check the actor of
    // each change; registered after the provider so it wins over any store the provider adds.
    private static IOperationAuditStore RecordingAuditStore(ConcurrentQueue<OperationAuditEntry> audit)
    {
        var store = Substitute.For<IOperationAuditStore>();
        store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            audit.Enqueue(call.Arg<OperationAuditEntry>());
            return new ValueTask<Either<EncinaError, LanguageExt.Unit>>(Prelude.Right<EncinaError, LanguageExt.Unit>(Prelude.unit));
        });
        return store;
    }
}

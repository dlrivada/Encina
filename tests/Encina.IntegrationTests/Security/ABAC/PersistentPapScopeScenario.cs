using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using Encina.Testing.Identity;
using Microsoft.Extensions.Hosting;

namespace Encina.IntegrationTests.Security.ABAC;

/// <summary>
/// The scenario shared by the persistent-PAP registration tests of every provider family (#1707):
/// the provider's real registration (a scoped <see cref="IPolicyStore"/>) is combined with
/// <c>AddEncinaABAC(o =&gt; o.UsePersistentPAP = true)</c> under <c>ValidateOnBuild</c> and
/// <c>ValidateScopes</c>, the seeding hosted service runs, and one change goes through the PAP.
/// </summary>
internal static class PersistentPapScopeScenario
{
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
    /// real registration), builds the provider with scope validation, seeds, changes a policy
    /// and checks the result.
    /// </summary>
    public static async Task RunAsync(IServiceCollection services)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var seededId = $"seeded-{suffix}";
        var addedId = $"added-{suffix}";

        services.AddLogging();
        services.AddSingleton(Substitute.For<global::Encina.Security.ISecurityContextAccessor>());
        var context = Substitute.For<IRequestContext>();
        context.Identity.Returns(TestIdentity.User("scope-test-user"));
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(context);
        services.AddSingleton(accessor);
        services.AddEncinaABAC(options =>
        {
            options.UsePersistentPAP = true;
            options.SeedPolicies.Add(CreatePolicy(seededId));
        });

        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var pap = provider.GetRequiredService<IPolicyAdministrationPoint>();
        var seeder = provider.GetServices<IHostedService>()
            .Single(s => s.GetType().Name == "ABACPolicySeedingHostedService");
        await seeder.StartAsync(CancellationToken.None);

        (await pap.AddPolicyAsync(CreatePolicy(addedId), parentPolicySetId: null)).IsRight.ShouldBeTrue();
        (await pap.UpdatePolicyAsync(CreatePolicy(addedId, "updated"))).IsRight.ShouldBeTrue();

        var seeded = await pap.GetPolicyAsync(seededId);
        seeded.IsRight.ShouldBeTrue();
        seeded.IfRight(option => option.IsSome.ShouldBeTrue("the seeding hosted service persisted the policy"));

        var updated = await pap.GetPolicyAsync(addedId);
        updated.IfRight(option => option.IfSome(policy => policy.Description.ShouldBe("updated")));
        updated.IfRight(option => option.IsSome.ShouldBeTrue());

        (await pap.RemovePolicyAsync(addedId)).IsRight.ShouldBeTrue();
        (await pap.RemovePolicyAsync(seededId)).IsRight.ShouldBeTrue();
    }
}

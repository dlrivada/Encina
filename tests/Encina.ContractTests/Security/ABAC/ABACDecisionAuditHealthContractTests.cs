using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Encina.ContractTests.Security.ABAC;

/// <summary>
/// Contract of the decision audit health state (#751 Phase 5): <c>AddEncinaABAC</c> registers one
/// shared singleton, and the default failure window is five minutes.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "ABAC")]
public sealed class ABACDecisionAuditHealthContractTests
{
    [Fact]
    public void AddEncinaABAC_RegistersOneSharedHealthStateSingleton()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddScoped<IOperationAuditStore, InMemoryOperationAuditStore>();
        services.AddEncinaABAC();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        var root = provider.GetRequiredService<ABACDecisionAuditHealthState>();
        scope.ServiceProvider.GetRequiredService<ABACDecisionAuditHealthState>().ShouldBeSameAs(root);
        root.LastFailureAtUtc.ShouldBeNull();
    }

    [Fact]
    public void DecisionAuditOptions_HealthFailureWindowDefaultsToFiveMinutes() =>
        new ABACDecisionAuditOptions().HealthFailureWindow.ShouldBe(TimeSpan.FromMinutes(5));
}

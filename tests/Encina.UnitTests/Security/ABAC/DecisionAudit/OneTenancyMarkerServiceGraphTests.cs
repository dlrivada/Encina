using Encina.Messaging;
using Encina.Messaging.DeadLetter;
using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Shouldly;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// One core tenancy marker for every tenant-aware component (#2106): <c>AddEncinaTenancy</c> registers
/// <see cref="MultiTenancyMarker"/> once, and both the dead letter manager and the ABAC decision audit
/// reader fail closed on it when no tenant is resolved. The provider builds with <c>ValidateOnBuild</c>
/// and <c>ValidateScopes</c> with and without <c>AddEncinaTenancy</c>.
/// </summary>
public sealed class OneTenancyMarkerServiceGraphTests
{
    private static readonly ServiceProviderOptions Validated = new() { ValidateOnBuild = true, ValidateScopes = true };

    private sealed class StubFactory : IDeadLetterMessageFactory
    {
        public IDeadLetterMessage Create(DeadLetterData data) => throw new NotSupportedException();
    }

    private static ServiceProvider Build(bool tenancy)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (tenancy)
        {
            services.AddEncinaTenancy();
        }

        services.AddScoped<IOperationAuditStore, InMemoryOperationAuditStore>();
        services.AddEncinaABAC();
        services.AddDeadLetterQueueServices<FakeDeadLetterStore, StubFactory>(true, new DeadLetterOptions());
        return services.BuildServiceProvider(Validated);
    }

    [Fact]
    public void AddEncinaTenancy_RegistersExactlyOneCoreMarkerThatBothConsumersShare()
    {
        var services = new ServiceCollection();
        services.AddEncinaTenancy();
        services.AddEncinaTenancy();

        services.Count(d => d.ServiceType == typeof(MultiTenancyMarker)).ShouldBe(1);
    }

    [Fact]
    public async Task WithAddEncinaTenancy_BothConsumersDenyWhenNoTenantIsResolved()
    {
        using var provider = Build(tenancy: true);
        using var scope = provider.CreateScope();

        var query = await scope.ServiceProvider.GetRequiredService<IABACDecisionAuditReader>()
            .QueryAsync(new ABACDecisionAuditQuery());
        var dead = await scope.ServiceProvider.GetRequiredService<IDeadLetterManager>().GetCountAsync();

        query.ShouldBeErrorWithCode(ABACErrors.DecisionAuditTenantRequiredCode);
        dead.ShouldBeErrorWithCode(DeadLetterErrorCodes.TenantRequired);
    }

    [Fact]
    public async Task WithoutAddEncinaTenancy_BothConsumersRunAsSingleTenant()
    {
        using var provider = Build(tenancy: false);
        using var scope = provider.CreateScope();

        var query = await scope.ServiceProvider.GetRequiredService<IABACDecisionAuditReader>()
            .QueryAsync(new ABACDecisionAuditQuery());
        var dead = await scope.ServiceProvider.GetRequiredService<IDeadLetterManager>().GetCountAsync();

        query.IsRight.ShouldBeTrue();
        dead.ShouldBeRight().ShouldBe(0);
    }

    [Fact]
    public async Task AnApplicationThatRegistersTheMarkerItselfMakesTheReaderTenantAware()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<MultiTenancyMarker>();
        services.AddScoped<IOperationAuditStore, InMemoryOperationAuditStore>();
        services.AddEncinaABAC();

        using var provider = services.BuildServiceProvider(Validated);
        using var scope = provider.CreateScope();

        var query = await scope.ServiceProvider.GetRequiredService<IABACDecisionAuditReader>()
            .QueryAsync(new ABACDecisionAuditQuery());

        query.ShouldBeErrorWithCode(ABACErrors.DecisionAuditTenantRequiredCode);
    }
}

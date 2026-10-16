using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// AddEncinaAudit registers everything the awaited, child-scope audit write resolves (#1869).
/// </summary>
public sealed class AuditPipelineBehaviorRegistrationTests
{
    [Fact]
    public async Task AddEncinaAudit_BuildsWithValidationAndResolvesTheClosedBehaviorFromAScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAudit(o => o.FailureMode = OperationAuditFailureMode.BestEffort);
        services.AddSingleton<IPiiMasker, NullPiiMasker>();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        await using var scope = provider.CreateAsyncScope();

        var behavior = scope.ServiceProvider.GetServices<IPipelineBehavior<AuditRegistrationCommand, Unit>>()
            .OfType<AuditPipelineBehavior<AuditRegistrationCommand, Unit>>()
            .Single();

        var result = await behavior.Handle(
            new AuditRegistrationCommand(),
            RequestContext.CreateForTest(),
            () => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        var store = scope.ServiceProvider.GetRequiredService<IOperationAuditStore>()
            .ShouldBeOfType<InMemoryOperationAuditStore>();
        store.Count.ShouldBe(1);
    }

    public sealed record AuditRegistrationCommand : ICommand<Unit>;
}

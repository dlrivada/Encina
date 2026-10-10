#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using System.Transactions;

using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Auditing;
using Encina.IntegrationTests.Security.Audit.EFCore;
using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.ABAC.EEL;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Encina.Testing.Identity;

using LanguageExt;

using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Security.ABAC.DecisionAudit;

/// <summary>
/// Isolation of the decision audit write on a real SQL Server (#751 Phase 5): the deny record of a request
/// survives the rollback of the transaction the request runs in, both when that transaction is an EF Core
/// transaction opened by <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> and when it is an
/// ambient <see cref="TransactionScope"/> (the recorder runs in its own DI scope with ambient
/// transactions suppressed).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class ABACDecisionAuditTransactionIsolationTests(EFCoreSqlServerFixture fixture) : IAsyncLifetime
{
    private const string PolicyName = "deny-policy";

    private readonly List<AuditTestDbContext> _contexts = [];

    [RequirePolicy(PolicyName)]
    private sealed record DenyingCommand : IRequest<string>, ITransactionalCommand;

    public async ValueTask InitializeAsync()
    {
        await fixture.EnsureSchemaCreatedAsync<AuditTestDbContext>();
        await fixture.ClearAllDataAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.DisposeAsync();
        }
    }

    private AuditTestDbContext NewContext()
    {
        var context = fixture.CreateDbContext<AuditTestDbContext>();
        _contexts.Add(context);
        return context;
    }

    private ABACPipelineBehavior<DenyingCommand, string> NewPep()
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, PolicyDecision>(new PolicyDecision
            {
                Effect = Effect.Deny,
                Obligations = [],
                Advice = [],
                EvaluationDuration = TimeSpan.FromMilliseconds(1)
            })));

        var attributes = Substitute.For<IAttributeProvider>();
        attributes.GetSubjectAttributesAsync(Arg.Any<RequestIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        attributes.GetResourceAttributesAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(new Dictionary<string, object>());
        attributes.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());

        var options = new ABACOptions();
        options.DecisionAudit.Enabled = true;

        // The recorder resolves its store from a fresh DI scope, so it gets a context and a connection of
        // its own: nothing it writes shares the request's transaction.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IOperationAuditStore>(_ => new OperationAuditStoreEF(NewContext()));
        var provider = services.BuildServiceProvider();

        var recorder = new AuditStoreABACDecisionRecorder(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(options),
            TimeProvider.System,
            NullLogger<AuditStoreABACDecisionRecorder>.Instance);

        return new ABACPipelineBehavior<DenyingCommand, string>(
            pdp,
            attributes,
            new ObligationExecutor([], NullLogger<ObligationExecutor>.Instance),
            new EELCompiler(),
            Microsoft.Extensions.Options.Options.Create(options),
            recorder,
            TimeProvider.System,
            NullLogger<ABACPipelineBehavior<DenyingCommand, string>>.Instance);
    }

    private static OperationAuditEntry Marker(string correlationId) => new()
    {
        Id = Guid.NewGuid(),
        CorrelationId = correlationId,
        Action = "TestMarker",
        EntityType = "TestMarker",
        Outcome = AuditOutcome.Success,
        TimestampUtc = DateTime.UtcNow,
        StartedAtUtc = DateTimeOffset.UtcNow,
        CompletedAtUtc = DateTimeOffset.UtcNow
    };

    private async Task<IReadOnlyList<OperationAuditEntry>> EntriesAsync(string correlationId)
    {
        var store = new OperationAuditStoreEF(NewContext());
        var found = await store.GetByCorrelationIdAsync(correlationId);
        return found.Match(Right: entries => entries, Left: error => throw new InvalidOperationException(error.GetEncinaCode()));
    }

    private static string Code(Either<EncinaError, string> result) =>
        result.Match(Right: _ => "<right>", Left: error => error.GetCode().IfNone("<none>"));

    [Fact]
    public async Task DenyRecord_SurvivesTheRollbackOfTheEfCoreTransactionOfTheRequest()
    {
        var correlationId = $"tx-{Guid.NewGuid():N}";
        var markerCorrelation = correlationId + "-marker";
        var outerContext = NewContext();
        var outerStore = new OperationAuditStoreEF(outerContext);
        var transaction = new TransactionPipelineBehavior<DenyingCommand, string>(
            outerContext, NullLogger<TransactionPipelineBehavior<DenyingCommand, string>>.Instance);
        var pep = NewPep();
        var context = TestRequestContext.For(TestIdentity.User("alice"), correlationId: correlationId);

        var result = await transaction.Handle(
            new DenyingCommand(),
            context,
            async () =>
            {
                // Work that belongs to the request's transaction, then the PEP denies.
                (await outerStore.RecordAsync(Marker(markerCorrelation))).IsRight.ShouldBeTrue();
                return await pep.Handle(new DenyingCommand(), context, () => ValueTask.FromResult(Right<EncinaError, string>("handled")), default);
            },
            default);

        Code(result).ShouldBe(ABACErrors.AccessDeniedCode);
        (await EntriesAsync(markerCorrelation)).ShouldBeEmpty();

        var decision = (await EntriesAsync(correlationId)).ShouldHaveSingleItem();
        decision.Action.ShouldBe(ABACDecisionAuditSchema.Action);
        decision.Outcome.ShouldBe(AuditOutcome.Denied);
    }

    [Fact]
    public async Task DenyRecord_SurvivesTheRollbackOfAnAmbientTransactionScope()
    {
        var correlationId = $"ts-{Guid.NewGuid():N}";
        var markerCorrelation = correlationId + "-marker";
        var pep = NewPep();
        var context = TestRequestContext.For(TestIdentity.User("alice"), correlationId: correlationId);

        Either<EncinaError, string> result;
        using (new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
        {
            var outerStore = new OperationAuditStoreEF(NewContext());
            (await outerStore.RecordAsync(Marker(markerCorrelation))).IsRight.ShouldBeTrue();
            result = await pep.Handle(new DenyingCommand(), context, () => ValueTask.FromResult(Right<EncinaError, string>("handled")), default);

            // The scope is disposed without Complete: the request's transaction rolls back.
        }

        Code(result).ShouldBe(ABACErrors.AccessDeniedCode);
        (await EntriesAsync(markerCorrelation)).ShouldBeEmpty();
        (await EntriesAsync(correlationId)).ShouldHaveSingleItem().Outcome.ShouldBe(AuditOutcome.Denied);
    }
}

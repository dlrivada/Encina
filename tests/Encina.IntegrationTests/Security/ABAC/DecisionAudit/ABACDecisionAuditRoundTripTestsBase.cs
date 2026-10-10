using System.Text;
using System.Text.Json;

using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Encina.IntegrationTests.Security.ABAC.DecisionAudit;

/// <summary>
/// The decision audit round trip on a real operation audit store (#751 Phase 3): the registered
/// recorder writes records through the provider's <see cref="IOperationAuditStore"/>, the reader
/// queries them back by subject, request type, resource, outcome and correlation id, and the
/// export writes them as JSON Lines. Each provider runs the same tests through its shared collection.
/// </summary>
public abstract class ABACDecisionAuditRoundTripTestsBase
{
    private static readonly DateTimeOffset Start = new(2026, 10, 9, 8, 0, 0, TimeSpan.Zero);

    /// <summary>Creates the provider's store for one DI scope (the recorder and the reader each open their own).</summary>
    protected abstract IOperationAuditStore CreateStore();

    /// <summary>Whether the provider's database is reachable; an unavailable one skips the test.</summary>
    protected virtual bool IsAvailable => true;

    /// <summary>Waits until written entries are readable (stores with asynchronous read models override it).</summary>
    protected virtual Task WaitForReadsAsync() => Task.CompletedTask;

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => CreateStore());
        services.AddEncinaABAC(options => options.DecisionAudit.Enabled = true);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static ABACDecisionRecord Record(string requestType, string correlationId, string userId, ABACEnforcedOutcome outcome, string reason, int second) => new()
    {
        DecisionId = Guid.CreateVersion7(Start.AddSeconds(second)),
        UserId = userId,
        IdentityKind = IdentityKind.User,
        CorrelationId = correlationId,
        ModuleId = "orders",
        IpAddress = "203.0.113.9",
        UserAgent = "integration/1.0",
        RequestType = requestType,
        ResourceId = "order-42",
        EnforcedOutcome = outcome,
        ReasonCode = reason,
        EnforcementMode = ABACEnforcementMode.Block,
        Effect = outcome == ABACEnforcedOutcome.Granted ? Effect.Permit : Effect.Deny,
        PolicyId = "orders-policy",
        RuleId = "rule-1",
        EvaluatedPolicies =
        [
            new PolicyEvaluationTrace
            {
                PolicyId = "orders-policy",
                IsPolicySet = false,
                Effect = outcome == ABACEnforcedOutcome.Granted ? Effect.Permit : Effect.Deny,
                Reason = PolicyTraceReason.Evaluated,
                DecisiveRuleIds = ["rule-1"]
            }
        ],
        AttributeNames = new Dictionary<AttributeCategory, IReadOnlyList<string>> { [AttributeCategory.Subject] = ["department"] },
        RecordedValues = new Dictionary<string, string> { ["department"] = "HR" },
        StartedAtUtc = Start.AddSeconds(second),
        CompletedAtUtc = Start.AddSeconds(second).AddMilliseconds(15)
    };

    [Fact]
    public async Task RecordQueryExport_RoundTripsTheDecisionsThroughTheStore()
    {
        if (!IsAvailable)
        {
            Assert.Skip("The database of this provider is not available.");
        }

        // Unique values per run, so the test never depends on rows other tests left behind.
        var run = Guid.NewGuid().ToString("N")[..12];
        var requestType = $"GetOrderQuery_{run}";
        var longUser = $"user-{run}-" + new string('u', ABACDecisionAuditSchema.UserIdMaxLength);
        var denied = Record(requestType, $"corr-{run}-1", longUser, ABACEnforcedOutcome.Denied, ABACErrors.AccessDeniedCode, 0);
        var granted = Record(requestType, $"corr-{run}-2", $"user-{run}", ABACEnforcedOutcome.Granted, ABACDecisionAuditSchema.PermitReasonCode, 1);

        await using var provider = BuildProvider();
        var recorder = provider.GetRequiredService<IABACDecisionRecorder>();
        ShouldBeWritten(await recorder.RecordAsync(denied));
        ShouldBeWritten(await recorder.RecordAsync(granted));
        await WaitForReadsAsync();

        await using var scope = provider.CreateAsyncScope();
        var reader = scope.ServiceProvider.GetRequiredService<IABACDecisionAuditReader>();

        // By request type: both decisions.
        var byType = await QueryAsync(reader, new ABACDecisionAuditQuery { RequestType = requestType });
        byType.Select(item => item.DecisionId).Order().ShouldBe(new[] { denied.DecisionId, granted.DecisionId }.Order());

        // By an over-limit subject: found through the hashed column.
        var byUser = (await QueryAsync(reader, new ABACDecisionAuditQuery { UserId = longUser })).ShouldHaveSingleItem();
        byUser.DecisionId.ShouldBe(denied.DecisionId);
        byUser.UserId.ShouldBe(ABACDecisionAuditEntryMapper.Hash(longUser));
        byUser.HashedFields.ShouldBe(["UserId"]);

        // By outcome, resource and correlation id: the denial with every stored field.
        var read = (await QueryAsync(reader, new ABACDecisionAuditQuery
        {
            RequestType = requestType,
            ResourceId = "order-42",
            Outcome = AuditOutcome.Denied,
            CorrelationId = denied.CorrelationId
        })).ShouldHaveSingleItem();
        read.DecisionId.ShouldBe(denied.DecisionId);
        read.ReasonCode.ShouldBe(ABACErrors.AccessDeniedCode);
        read.Enforced.ShouldBeTrue();
        read.EnforcementMode.ShouldBe(ABACEnforcementMode.Block);
        read.IdentityKind.ShouldBe(IdentityKind.User);
        read.Effect.ShouldBe(Effect.Deny);
        read.PolicyId.ShouldBe("orders-policy");
        read.RuleId.ShouldBe("rule-1");
        read.ModuleId.ShouldBe("orders");
        read.EvaluatedPolicies.ShouldHaveSingleItem().DecisiveRuleIds.ShouldBe(["rule-1"]);
        read.AttributeNames["Subject"].ShouldBe(["department"]);
        read.RecordedValues["department"].ShouldBe("HR");
        read.IpAddress.ShouldBe("203.0.113.9");
        read.UserAgent.ShouldBe("integration/1.0");
        read.StartedAtUtc.ShouldBe(denied.StartedAtUtc, TimeSpan.FromMilliseconds(1));
        read.CompletedAtUtc.ShouldBe(denied.CompletedAtUtc, TimeSpan.FromMilliseconds(1));

        // Export: one JSON line per decision of the request type.
        using var stream = new MemoryStream();
        var exported = await reader.ExportAsync(new ABACDecisionAuditQuery { RequestType = requestType }, stream);
        exported.Match(Right: count => count, Left: error => throw new ShouldAssertException(error.GetCode().IfNone("<none>"))).ShouldBe(2);
        var lines = Encoding.UTF8.GetString(stream.ToArray()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(2);
        lines.ShouldAllBe(line => JsonDocument.Parse(line).RootElement.GetProperty("schema").GetString() == ABACDecisionAuditSchema.SchemaVersion);
    }

    private static void ShouldBeWritten(LanguageExt.Either<EncinaError, LanguageExt.Unit> result) =>
        _ = result.Match(
            Right: _ => true,
            Left: error => throw new ShouldAssertException($"The record was not written: {error.GetCode().IfNone("<none>")} {error.Message}"));

    private static async Task<IReadOnlyList<ABACDecisionAuditRecord>> QueryAsync(IABACDecisionAuditReader reader, ABACDecisionAuditQuery query)
    {
        var result = await reader.QueryAsync(query);
        return result.Match(Right: page => page.Items, Left: error => throw new ShouldAssertException(error.GetCode().IfNone("<none>")));
    }
}

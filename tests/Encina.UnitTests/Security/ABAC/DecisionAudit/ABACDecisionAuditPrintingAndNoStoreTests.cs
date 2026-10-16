using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// The printed form of the decision audit query and record carries no personal data, and the
/// recorder and the reader report <c>DecisionAuditStoreUnavailable</c> when no operation audit store
/// is registered (#751 Phase 7).
/// </summary>
public sealed class ABACDecisionAuditPrintingAndNoStoreTests
{
    private static string CodeOf<T>(LanguageExt.Either<EncinaError, T> result) =>
        result.Match(Right: _ => string.Empty, Left: error => error.GetCode().IfNone(string.Empty));

    [Fact]
    public void Query_WithNoFilters_PrintsOnlyThePaging()
    {
        var printed = new ABACDecisionAuditQuery().ToString();

        printed.ShouldContain("PageNumber = 1");
        printed.ShouldNotContain("UserId");
        printed.ShouldNotContain("TenantId");
    }

    [Fact]
    public void Query_WithFilters_PrintsExactlyThePaging()
    {
        var query = new ABACDecisionAuditQuery { UserId = "user-secret", TenantId = "tenant-secret", ResourceId = "r", CorrelationId = "c", PageSize = 5 };

        query.ToString().ShouldBe("ABACDecisionAuditQuery { PageNumber = 1, PageSize = 5 }");
    }

    [Fact]
    public void Record_PrintsOnlyTheDecisionIdOutcomeAndReason()
    {
        var record = new ABACDecisionAuditRecord
        {
            DecisionId = Guid.Empty,
            CorrelationId = "corr-secret",
            RequestType = "SecretRequest",
            UserId = "user-secret",
            Outcome = AuditOutcome.Denied,
            Enforced = true,
            ReasonCode = "abac.access_denied",
            StartedAtUtc = DateTimeOffset.UnixEpoch,
            CompletedAtUtc = DateTimeOffset.UnixEpoch
        };

        var printed = record.ToString();

        printed.ShouldContain("abac.access_denied");
        printed.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task Reader_WithoutAStore_ReturnsStoreUnavailableForQueryAndExport()
    {
        await using var provider = new ServiceCollection().AddLogging().AddEncinaABAC().BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var reader = scope.ServiceProvider.GetRequiredService<IABACDecisionAuditReader>();

        var query = await reader.QueryAsync(new ABACDecisionAuditQuery());
        var export = await reader.ExportAsync(new ABACDecisionAuditQuery(), Stream.Null);

        CodeOf(query).ShouldBe(ABACErrors.DecisionAuditStoreUnavailableCode);
        CodeOf(export).ShouldBe(ABACErrors.DecisionAuditStoreUnavailableCode);
    }

    [Fact]
    public async Task Recorder_WithoutAStore_ReturnsStoreUnavailable()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var recorder = new AuditStoreABACDecisionRecorder(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ABACOptions()),
            TimeProvider.System,
            NullLogger<AuditStoreABACDecisionRecorder>.Instance);
        var record = new ABACDecisionRecord
        {
            DecisionId = Guid.CreateVersion7(),
            IdentityKind = IdentityKind.User,
            CorrelationId = "c",
            RequestType = "R",
            EnforcedOutcome = ABACEnforcedOutcome.Granted,
            ReasonCode = ABACDecisionAuditSchema.PermitReasonCode,
            EnforcementMode = ABACEnforcementMode.Block,
            StartedAtUtc = DateTimeOffset.UnixEpoch,
            CompletedAtUtc = DateTimeOffset.UnixEpoch
        };

        var result = await recorder.RecordAsync(record);

        CodeOf(result).ShouldBe(ABACErrors.DecisionAuditStoreUnavailableCode);
    }
}

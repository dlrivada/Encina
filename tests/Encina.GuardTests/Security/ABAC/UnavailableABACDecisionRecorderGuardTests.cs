using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard tests of the recorder <c>AddEncinaABAC</c> registers until a durable recorder exists.
/// </summary>
public sealed class UnavailableABACDecisionRecorderGuardTests
{
    private static IABACDecisionRecorder Resolve() =>
        new ServiceCollection().AddEncinaABAC().BuildServiceProvider().GetRequiredService<IABACDecisionRecorder>();

    [Fact]
    public async Task RecordAsync_NullRecord_ThrowsArgumentNullException()
    {
        var recorder = Resolve();

        var thrown = await Should.ThrowAsync<ArgumentNullException>(async () => await recorder.RecordAsync(null!));

        thrown.ParamName.ShouldBe("record");
    }

    [Fact]
    public async Task RecordAsync_ValidRecord_DoesNotThrowAndReturnsTheStoreUnavailableError()
    {
        var recorder = Resolve();
        var record = new ABACDecisionRecord
        {
            DecisionId = Guid.CreateVersion7(),
            IdentityKind = IdentityKind.User,
            CorrelationId = "guard",
            RequestType = "GuardRequest",
            EnforcedOutcome = ABACEnforcedOutcome.Granted,
            ReasonCode = ABACDecisionAuditSchema.PermitReasonCode,
            EnforcementMode = ABACEnforcementMode.Block,
            StartedAtUtc = DateTimeOffset.UnixEpoch,
            CompletedAtUtc = DateTimeOffset.UnixEpoch
        };

        var result = await recorder.RecordAsync(record);

        result.IsLeft.ShouldBeTrue();
    }
}

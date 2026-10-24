using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Testing.Identity;
using LanguageExt;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Encina.PropertyTests.Security.BreakTheGlass;

[BreakTheGlassEligible("clinical-record", SubjectIdProperty = nameof(PatientId))]
public sealed record PropertyGateRequest(string PatientId);

/// <summary>
/// FsCheck properties of <see cref="BreakTheGlassAccessGate"/>: for any duration and any check time,
/// the gate grants only when the check time is before the expiry and the status is Active.
/// </summary>
[Trait("Category", "Property")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassGateProperties
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

    private static IRequestContext Context()
    {
        var context = NSubstitute.Substitute.For<IRequestContext>();
        context.Identity.Returns(TestIdentity.User("clin-1"));
        context.TenantId.Returns("tenant-1");
        context.CorrelationId.Returns("corr-1");
        return context;
    }

    private static async Task<bool> GrantsAsync(int durationSeconds, int checkOffsetSeconds, BreakTheGlassStatus status)
    {
        var store = new InMemoryBreakTheGlassStore();
        var elevation = new BreakTheGlassElevation
        {
            ElevationId = Guid.NewGuid(),
            TenantId = "tenant-1",
            UserId = "clin-1",
            SubjectId = "patient-1",
            Scope = "clinical-record",
            Justification = "justification",
            OpenedAtUtc = OpenedAt,
            ExpiresAtUtc = OpenedAt.AddSeconds(durationSeconds),
            Status = status
        };
        await store.AddAsync(elevation);

        var time = new FakeTimeProvider(OpenedAt.AddSeconds(checkOffsetSeconds));
        var gate = new BreakTheGlassAccessGate(
            store,
            new InMemoryOperationAuditStore(),
            new ReadAuditContext(),
            new BreakTheGlassRequestGrants(),
            Options.Create(new BreakTheGlassOptions()),
            time);

        var result = await gate.TryElevateAsync(new PropertyGateRequest("patient-1"), Context(), EncinaErrors.Forbidden(), CancellationToken.None);
        return result.Match(Right: option => option.IsSome, Left: _ => false);
    }

    [Property(MaxTest = 200)]
    public bool Grants_OnlyWhenBeforeExpiryAndActive(PositiveInt duration, int checkOffset)
    {
        var durationSeconds = duration.Get % 86_400;
        var offset = checkOffset % 200_000;

        var granted = GrantsAsync(durationSeconds, offset, BreakTheGlassStatus.Active).GetAwaiter().GetResult();

        return granted == (offset < durationSeconds);
    }

    [Property(MaxTest = 100)]
    public bool NeverGrants_ForAClosedElevation(PositiveInt duration, int checkOffset, bool revoked)
    {
        var status = revoked ? BreakTheGlassStatus.Revoked : BreakTheGlassStatus.Expired;

        return !GrantsAsync(duration.Get % 86_400, checkOffset % 200_000, status).GetAwaiter().GetResult();
    }
}

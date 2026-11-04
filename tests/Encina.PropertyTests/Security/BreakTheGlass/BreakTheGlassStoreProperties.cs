using Encina.Security.BreakTheGlass;

namespace Encina.PropertyTests.Security.BreakTheGlass;

/// <summary>
/// FsCheck property of the <see cref="IBreakTheGlassStore"/> contract, run on the in-memory store: for any sequence of
/// add and revoke operations on one key, at most one row is effective at any instant, and
/// <c>FindEffectiveAsync</c> finds a row exactly when one is. This is an extra check of the in-memory reference only;
/// the same invariant runs against the real ADO, Dapper and EF Core SQL Server stores in the integration tests
/// (<c>BreakTheGlassStorePropertyIntegrationTests</c>).
/// </summary>
[Trait("Category", "Property")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassStoreProperties
{
    private const string Tenant = "tenant-1";
    private const string User = "clin-1";
    private const string Subject = "patient-1";
    private const string Scope = "clinical-record";

    private static readonly DateTimeOffset Start = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    private static BreakTheGlassElevation Elevation(DateTimeOffset openedAt, int durationSeconds) => new()
    {
        ElevationId = Guid.NewGuid(),
        TenantId = Tenant,
        UserId = User,
        SubjectId = Subject,
        Scope = Scope,
        Justification = "justification",
        OpenedAtUtc = openedAt,
        ExpiresAtUtc = openedAt.AddSeconds(durationSeconds)
    };

    private static async Task<bool> AtMostOneEffectiveAsync(IReadOnlyList<(byte Step, byte Duration, bool Revoke)> operations)
    {
        var store = new InMemoryBreakTheGlassStore();
        var now = Start;

        foreach (var (step, duration, revoke) in operations)
        {
            now = now.AddSeconds(step);
            if (revoke)
            {
                var effective = await store.FindEffectiveAsync(Tenant, User, Subject, Scope, now);
                var id = effective.Match(Right: option => option.Match(Some: e => e.ElevationId, None: () => Guid.Empty), Left: _ => Guid.Empty);
                if (id != Guid.Empty)
                {
                    await store.TryCloseAsync(Tenant, id, BreakTheGlassStatus.Revoked, "revoked", now);
                }
            }
            else
            {
                await store.AddAsync(Elevation(now, duration + 1));
            }

            var page = await store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery { PageSize = BreakTheGlassReviewQuery.MaxPageSize });
            var effectiveRows = page.Match(Right: p => p.Items.Count(e => e.IsEffectiveAt(now)), Left: _ => int.MaxValue);
            var found = await store.FindEffectiveAsync(Tenant, User, Subject, Scope, now);
            var foundOne = found.Match(Right: option => option.IsSome, Left: _ => false);

            if (effectiveRows > 1 || foundOne != (effectiveRows == 1))
            {
                return false;
            }
        }

        return true;
    }

    [Property(MaxTest = 200)]
    public bool AtMostOneRowIsEffective_ForAnySequenceOfAddsAndRevokesOnOneKey(byte[] steps, byte[] durations, bool[] revokes)
    {
        var length = new[] { steps.Length, durations.Length, revokes.Length }.Min();
        var operations = Enumerable.Range(0, length).Select(i => (steps[i], durations[i], revokes[i])).ToList();

        return AtMostOneEffectiveAsync(operations).GetAwaiter().GetResult();
    }
}

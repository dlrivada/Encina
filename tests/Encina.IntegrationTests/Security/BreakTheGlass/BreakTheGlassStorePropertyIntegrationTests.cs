using Encina.ADO.SqlServer.BreakTheGlass;
using Encina.Dapper.SqlServer.BreakTheGlass;
using Encina.EntityFrameworkCore.BreakTheGlass;
using Encina.Security.BreakTheGlass;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using FsCheck.Xunit;

namespace Encina.IntegrationTests.Security.BreakTheGlass;

/// <summary>
/// The FsCheck invariant of <see cref="IBreakTheGlassStore"/>, run against the real SQL Server stores: for any sequence of
/// add, close and expiry operations on one key, at most one elevation is effective at any instant, and
/// <c>FindEffectiveAsync</c> finds a row exactly when one is. Each case starts from a cleared table.
/// </summary>
internal static class BreakTheGlassStoreInvariant
{
    /// <summary>Cases per property; each case runs several statements against a real database.</summary>
    public const int Cases = 25;

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

    public static IReadOnlyList<(byte Step, byte Duration, bool Close)> Operations(byte[] steps, byte[] durations, bool[] closes)
    {
        var length = new[] { steps.Length, durations.Length, closes.Length }.Min();
        return Enumerable.Range(0, length).Select(i => (steps[i], durations[i], closes[i])).ToList();
    }

    public static async Task<bool> AtMostOneEffectiveAsync(
        IBreakTheGlassStore store, IReadOnlyList<(byte Step, byte Duration, bool Close)> operations)
    {
        var now = Start;

        foreach (var (step, duration, close) in operations)
        {
            now = now.AddSeconds(step);
            if (close)
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
}

/// <summary>The invariant against <see cref="BreakTheGlassStoreADO"/> on SQL Server.</summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class BreakTheGlassStoreADOPropertyIntegrationTests(SqlServerFixture fixture)
{
    [Property(MaxTest = BreakTheGlassStoreInvariant.Cases)]
    public bool AtMostOneElevationIsEffective_ForAnySequenceOnOneKey(byte[] steps, byte[] durations, bool[] closes)
    {
        BreakTheGlassSqlServerSchema.PrepareAsync(fixture, "Encina.ADO.SqlServer").GetAwaiter().GetResult();
        using var connection = fixture.CreateConnection();
        var store = new BreakTheGlassStoreADO(connection, BreakTheGlassSqlServerSchema.BusinessTransactionOn(connection));
        return BreakTheGlassStoreInvariant.AtMostOneEffectiveAsync(store, BreakTheGlassStoreInvariant.Operations(steps, durations, closes)).GetAwaiter().GetResult();
    }
}

/// <summary>The invariant against <see cref="BreakTheGlassStoreDapper"/> on SQL Server.</summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("Dapper-SqlServer")]
public sealed class BreakTheGlassStoreDapperPropertyIntegrationTests(SqlServerFixture fixture)
{
    [Property(MaxTest = BreakTheGlassStoreInvariant.Cases)]
    public bool AtMostOneElevationIsEffective_ForAnySequenceOnOneKey(byte[] steps, byte[] durations, bool[] closes)
    {
        BreakTheGlassSqlServerSchema.PrepareAsync(fixture, "Encina.Dapper.SqlServer").GetAwaiter().GetResult();
        using var connection = fixture.CreateConnection();
        var store = new BreakTheGlassStoreDapper(connection, BreakTheGlassSqlServerSchema.BusinessTransactionOn(connection));
        return BreakTheGlassStoreInvariant.AtMostOneEffectiveAsync(store, BreakTheGlassStoreInvariant.Operations(steps, durations, closes)).GetAwaiter().GetResult();
    }
}

/// <summary>The invariant against <see cref="BreakTheGlassStoreEF{TContext}"/> on SQL Server.</summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class BreakTheGlassStoreEFPropertyIntegrationTests(EFCoreSqlServerFixture fixture)
{
    [Property(MaxTest = BreakTheGlassStoreInvariant.Cases)]
    public bool AtMostOneElevationIsEffective_ForAnySequenceOnOneKey(byte[] steps, byte[] durations, bool[] closes)
    {
        BreakTheGlassEFSchema.PrepareAsync(fixture).GetAwaiter().GetResult();
        var store = new BreakTheGlassStoreEF<BreakTheGlassTestDbContext>(new BreakTheGlassTestContextFactory(fixture));
        return BreakTheGlassStoreInvariant.AtMostOneEffectiveAsync(store, BreakTheGlassStoreInvariant.Operations(steps, durations, closes)).GetAwaiter().GetResult();
    }
}

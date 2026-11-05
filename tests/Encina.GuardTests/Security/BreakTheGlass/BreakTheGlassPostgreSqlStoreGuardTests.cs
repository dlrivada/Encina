using System.Data;
using Encina.ADO.PostgreSQL.BreakTheGlass;
using Encina.Dapper.PostgreSQL.BreakTheGlass;
using Encina.Security.BreakTheGlass;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Security.BreakTheGlass;

/// <summary>Guard tests of the PostgreSQL break-the-glass stores (ADO.NET and Dapper): null, bad batch size or table name.</summary>
public sealed class BreakTheGlassPostgreSqlStoreGuardTests
{
    public static TheoryData<string> Providers => ["ADO", "Dapper"];

    private static IBreakTheGlassStore NewStore(string provider) =>
        provider == "ADO"
            ? new BreakTheGlassStoreADO(Substitute.For<IDbConnection>())
            : new BreakTheGlassStoreDapper(Substitute.For<IDbConnection>());

    [Fact]
    public void Constructors_NullConnectionOrInvalidTableName_Throw()
    {
        Should.Throw<ArgumentNullException>(() => new BreakTheGlassStoreADO(null!)).ParamName.ShouldBe("connection");
        Should.Throw<ArgumentNullException>(() => new BreakTheGlassStoreDapper(null!)).ParamName.ShouldBe("connection");
        Should.Throw<ArgumentException>(() => new BreakTheGlassStoreADO(Substitute.For<IDbConnection>(), tableName: "x; DROP TABLE y"));
        Should.Throw<ArgumentException>(() => new BreakTheGlassStoreDapper(Substitute.For<IDbConnection>(), tableName: "x; DROP TABLE y"));
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task AddAsync_NullElevation_ThrowsArgumentNullException(string provider)
    {
        (await Should.ThrowAsync<ArgumentNullException>(async () => await NewStore(provider).AddAsync(null!))).ParamName.ShouldBe("elevation");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task FindEffectiveAsync_NullKeyPart_ThrowsArgumentNullException(string provider)
    {
        var store = NewStore(provider);

        (await Should.ThrowAsync<ArgumentNullException>(async () => await store.FindEffectiveAsync(null, null!, "s", "c", DateTimeOffset.UnixEpoch))).ParamName.ShouldBe("userId");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await store.FindEffectiveAsync(null, "u", null!, "c", DateTimeOffset.UnixEpoch))).ParamName.ShouldBe("subjectId");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await store.FindEffectiveAsync(null, "u", "s", null!, DateTimeOffset.UnixEpoch))).ParamName.ShouldBe("scope");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task TryCloseAsync_NullReason_ThrowsArgumentNullException(string provider)
    {
        (await Should.ThrowAsync<ArgumentNullException>(
            async () => await NewStore(provider).TryCloseAsync(null, Guid.NewGuid(), BreakTheGlassStatus.Revoked, null!, DateTimeOffset.UnixEpoch))).ParamName.ShouldBe("reason");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task TryRecordReviewAsync_NullReviewer_ThrowsArgumentNullException(string provider)
    {
        (await Should.ThrowAsync<ArgumentNullException>(
            async () => await NewStore(provider).TryRecordReviewAsync(null, Guid.NewGuid(), BreakTheGlassReviewOutcome.Justified, null!, null, DateTimeOffset.UnixEpoch))).ParamName.ShouldBe("reviewerUserId");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task QueryReviewQueueAsync_NullQuery_ThrowsArgumentNullException(string provider)
    {
        (await Should.ThrowAsync<ArgumentNullException>(async () => await NewStore(provider).QueryReviewQueueAsync(null, null!))).ParamName.ShouldBe("query");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task GetExpiredUnrecordedAsync_NonPositiveBatchSize_ThrowsArgumentOutOfRangeException(string provider)
    {
        var store = NewStore(provider);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () => await store.GetExpiredUnrecordedAsync(DateTimeOffset.UnixEpoch, 0));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () => await store.GetExpiredUnrecordedAsync(DateTimeOffset.UnixEpoch, -1));
    }
}

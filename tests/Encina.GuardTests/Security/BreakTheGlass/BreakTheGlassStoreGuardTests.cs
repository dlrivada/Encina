using System.Data;
using Encina.ADO.SqlServer.BreakTheGlass;
using Encina.Dapper.SqlServer.BreakTheGlass;
using Encina.EntityFrameworkCore.BreakTheGlass;
using Encina.Security.BreakTheGlass;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Security.BreakTheGlass;

/// <summary>
/// Guard tests of the SQL Server break-the-glass stores (ADO.NET, Dapper and EF Core): a null constructor argument names its
/// parameter, and a null method argument throws before any database call.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassStoreGuardTests
{
    private sealed class GuardContext(DbContextOptions<GuardContext> options) : DbContext(options);

    private sealed class UnusedFactory : IDbContextFactory<GuardContext>
    {
        public GuardContext CreateDbContext() => throw new InvalidOperationException("The guards must throw before a context is created.");
    }

    public static TheoryData<string> Stores => ["ADO", "Dapper", "EF"];

    private static IBreakTheGlassStore NewStore(string store) => store switch
    {
        "ADO" => new BreakTheGlassStoreADO(Substitute.For<IDbConnection>()),
        "Dapper" => new BreakTheGlassStoreDapper(Substitute.For<IDbConnection>()),
        _ => new BreakTheGlassStoreEF<GuardContext>(new UnusedFactory())
    };

    [Fact]
    public void Constructors_NullDependency_NamesTheParameter()
    {
        Should.Throw<ArgumentNullException>(() => new BreakTheGlassStoreADO(null!)).ParamName.ShouldBe("connection");
        Should.Throw<ArgumentNullException>(() => new BreakTheGlassStoreDapper(null!)).ParamName.ShouldBe("connection");
        Should.Throw<ArgumentNullException>(() => new BreakTheGlassStoreEF<GuardContext>(null!)).ParamName.ShouldBe("contextFactory");
    }

    [Fact]
    public void SqlConstructors_InvalidTableName_Throws()
    {
        Should.Throw<ArgumentException>(() => new BreakTheGlassStoreADO(Substitute.For<IDbConnection>(), tableName: "x; DROP TABLE y"));
        Should.Throw<ArgumentException>(() => new BreakTheGlassStoreDapper(Substitute.For<IDbConnection>(), tableName: "x; DROP TABLE y"));
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task AddAsync_NullElevation_Throws(string store) =>
        (await Should.ThrowAsync<ArgumentNullException>(async () => await NewStore(store).AddAsync(null!))).ParamName.ShouldBe("elevation");

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task FindEffectiveAsync_NullKeyPart_Throws(string store)
    {
        var target = NewStore(store);
        (await Should.ThrowAsync<ArgumentNullException>(async () => await target.FindEffectiveAsync(null, null!, "s", "c", DateTimeOffset.UnixEpoch))).ParamName.ShouldBe("userId");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await target.FindEffectiveAsync(null, "u", null!, "c", DateTimeOffset.UnixEpoch))).ParamName.ShouldBe("subjectId");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await target.FindEffectiveAsync(null, "u", "s", null!, DateTimeOffset.UnixEpoch))).ParamName.ShouldBe("scope");
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task TryCloseAsync_NullReason_Throws(string store) =>
        (await Should.ThrowAsync<ArgumentNullException>(async () =>
            await NewStore(store).TryCloseAsync(null, Guid.NewGuid(), BreakTheGlassStatus.Revoked, null!, DateTimeOffset.UnixEpoch))).ParamName.ShouldBe("reason");

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task TryRecordReviewAsync_NullReviewer_Throws(string store) =>
        (await Should.ThrowAsync<ArgumentNullException>(async () =>
            await NewStore(store).TryRecordReviewAsync(null, Guid.NewGuid(), BreakTheGlassReviewOutcome.Justified, null!, null, DateTimeOffset.UnixEpoch))).ParamName.ShouldBe("reviewerUserId");

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task QueryReviewQueueAsync_NullQuery_Throws(string store) =>
        (await Should.ThrowAsync<ArgumentNullException>(async () => await NewStore(store).QueryReviewQueueAsync(null, null!))).ParamName.ShouldBe("query");

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task GetExpiredUnrecordedAsync_NonPositiveBatch_Throws(string store)
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () => await NewStore(store).GetExpiredUnrecordedAsync(DateTimeOffset.UnixEpoch, 0));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () => await NewStore(store).GetExpiredUnrecordedAsync(DateTimeOffset.UnixEpoch, -1));
    }

    [Fact]
    public void ModelBuilderExtension_NullBuilder_Throws() =>
        Should.Throw<ArgumentNullException>(() => BreakTheGlassModelBuilderExtensions.ApplyBreakTheGlassConfiguration(null!)).ParamName.ShouldBe("modelBuilder");

    [Fact]
    public void Configuration_NullBuilder_Throws() =>
        Should.Throw<ArgumentNullException>(() => new BreakTheGlassElevationEntityConfiguration().Configure(null!)).ParamName.ShouldBe("builder");
}

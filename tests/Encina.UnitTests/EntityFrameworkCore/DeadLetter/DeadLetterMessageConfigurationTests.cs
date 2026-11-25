using Encina.EntityFrameworkCore.DeadLetter;
using Encina.Messaging.DeadLetter;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Encina.UnitTests.EntityFrameworkCore.DeadLetter;

/// <summary>
/// A context that applies <see cref="DeadLetterMessageConfiguration"/>; shared by the configuration and store tests.
/// </summary>
internal sealed class DeadLetterTestDbContext : DbContext
{
    private readonly string? _collation;

    public DeadLetterTestDbContext(DbContextOptions<DeadLetterTestDbContext> options, string? collation = null)
        : base(options)
    {
        _collation = collation;
    }

    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfiguration(new DeadLetterMessageConfiguration(_collation));
}

/// <summary>
/// Unit tests for <see cref="DeadLetterMessageConfiguration"/>: the model it builds must keep the schema of the
/// 029 scripts of the ADO.NET and Dapper providers (table, key, indexes, lengths, required columns).
/// </summary>
public sealed class DeadLetterMessageConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    private IEntityType BuildEntityType(string? collation)
    {
        // The model cache is keyed by context type, so each collation needs its own service provider.
        var options = new DbContextOptionsBuilder<DeadLetterTestDbContext>()
            .UseSqlite(_connection)
            .EnableServiceProviderCaching(false)
            .Options;
        using var context = new DeadLetterTestDbContext(options, collation);

        // The design-time model keeps the relational annotations (collation, index filter) that the runtime model drops.
        return context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(DeadLetterMessage)).ShouldNotBeNull();
    }

    private static IIndex Index(IEntityType entityType, string name)
        => entityType.GetIndexes().Single(i => i.GetDatabaseName() == name);

    private static string[] Columns(IIndex index) => index.Properties.Select(p => p.Name).ToArray();

    public void Dispose() => _connection.Dispose();

    [Fact]
    public void Constants_AreTheBinaryCollationsOfSqlServerAndMySql()
    {
        DeadLetterMessageConfiguration.SqlServerBinaryCollation.ShouldBe("Latin1_General_100_BIN2");
        DeadLetterMessageConfiguration.MySqlBinaryCollation.ShouldBe("utf8mb4_bin");
    }

    [Fact]
    public void Configure_MapsToTheDeadLetterMessagesTable()
    {
        var entityType = BuildEntityType(null);

        entityType.GetTableName().ShouldBe("DeadLetterMessages");
    }

    [Fact]
    public void Configure_UsesIdAsThePrimaryKey()
    {
        var key = BuildEntityType(null).FindPrimaryKey().ShouldNotBeNull();

        key.Properties.Select(p => p.Name).ShouldBe([nameof(DeadLetterMessage.Id)]);
    }

    [Fact]
    public void Configure_IgnoresTheDerivedIsReplayedMember()
    {
        BuildEntityType(null).FindProperty(nameof(DeadLetterMessage.IsReplayed)).ShouldBeNull();
    }

    [Theory]
    [InlineData(nameof(DeadLetterMessage.RequestType), DeadLetterStoreLimits.RequestTypeMaxLength)]
    [InlineData(nameof(DeadLetterMessage.ErrorCode), DeadLetterStoreLimits.ErrorCodeMaxLength)]
    [InlineData(nameof(DeadLetterMessage.CorrelationId), DeadLetterStoreLimits.CorrelationIdMaxLength)]
    [InlineData(nameof(DeadLetterMessage.SourcePattern), DeadLetterStoreLimits.SourcePatternMaxLength)]
    [InlineData(nameof(DeadLetterMessage.SourceMessageId), DeadLetterStoreLimits.SourceMessageIdMaxLength)]
    [InlineData(nameof(DeadLetterMessage.TenantId), DeadLetterStoreLimits.TenantIdMaxLength)]
    [InlineData(nameof(DeadLetterMessage.ReplayResult), DeadLetterStoreLimits.ReplayResultMaxLength)]
    [InlineData(nameof(DeadLetterMessage.ExceptionType), 512)]
    public void Configure_BoundsTheColumnLength(string property, int expectedMaxLength)
    {
        BuildEntityType(null).FindProperty(property).ShouldNotBeNull().GetMaxLength().ShouldBe(expectedMaxLength);
    }

    [Theory]
    [InlineData(nameof(DeadLetterMessage.RequestContent))]
    [InlineData(nameof(DeadLetterMessage.ExceptionStackTrace))]
    public void Configure_LeavesUnboundedTextColumnsWithoutALength(string property)
    {
        BuildEntityType(null).FindProperty(property).ShouldNotBeNull().GetMaxLength().ShouldBeNull();
    }

    [Theory]
    [InlineData(nameof(DeadLetterMessage.RequestType), false)]
    [InlineData(nameof(DeadLetterMessage.RequestContent), false)]
    [InlineData(nameof(DeadLetterMessage.ErrorCode), false)]
    [InlineData(nameof(DeadLetterMessage.SourcePattern), false)]
    [InlineData(nameof(DeadLetterMessage.SourceMessageId), false)]
    [InlineData(nameof(DeadLetterMessage.TotalRetryAttempts), false)]
    [InlineData(nameof(DeadLetterMessage.FirstFailedAtUtc), false)]
    [InlineData(nameof(DeadLetterMessage.DeadLetteredAtUtc), false)]
    [InlineData(nameof(DeadLetterMessage.ExceptionType), true)]
    [InlineData(nameof(DeadLetterMessage.ExceptionStackTrace), true)]
    [InlineData(nameof(DeadLetterMessage.CorrelationId), true)]
    [InlineData(nameof(DeadLetterMessage.TenantId), true)]
    [InlineData(nameof(DeadLetterMessage.ExpiresAtUtc), true)]
    [InlineData(nameof(DeadLetterMessage.ReplayClaimedAtUtc), true)]
    [InlineData(nameof(DeadLetterMessage.ReplayedAtUtc), true)]
    [InlineData(nameof(DeadLetterMessage.ReplayResult), true)]
    public void Configure_MarksTheColumnNullability(string property, bool expectedNullable)
    {
        BuildEntityType(null).FindProperty(property).ShouldNotBeNull().IsNullable.ShouldBe(expectedNullable);
    }

    [Theory]
    [InlineData(nameof(DeadLetterMessage.FirstFailedAtUtc))]
    [InlineData(nameof(DeadLetterMessage.DeadLetteredAtUtc))]
    [InlineData(nameof(DeadLetterMessage.ExpiresAtUtc))]
    [InlineData(nameof(DeadLetterMessage.ReplayClaimedAtUtc))]
    [InlineData(nameof(DeadLetterMessage.ReplayedAtUtc))]
    public void Configure_ConvertsEveryInstantToUtc(string property)
    {
        var converter = BuildEntityType(null).FindProperty(property).ShouldNotBeNull().GetValueConverter().ShouldNotBeNull();

        var unspecified = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var read = (DateTime)converter.ConvertFromProvider(unspecified)!;
        var written = (DateTime)converter.ConvertToProvider(unspecified)!;

        read.Kind.ShouldBe(DateTimeKind.Utc);
        read.Ticks.ShouldBe(unspecified.Ticks);
        written.Kind.ShouldBe(DateTimeKind.Utc);
        written.Ticks.ShouldBe(unspecified.Ticks);
    }

    [Fact]
    public void Configure_ConvertsALocalInstantToItsUtcEquivalentOnWrite()
    {
        var converter = BuildEntityType(null).FindProperty(nameof(DeadLetterMessage.DeadLetteredAtUtc))
            .ShouldNotBeNull().GetValueConverter().ShouldNotBeNull();
        var local = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Local);

        var written = (DateTime)converter.ConvertToProvider(local)!;

        written.Kind.ShouldBe(DateTimeKind.Utc);
        written.ShouldBe(local.ToUniversalTime());
    }

    [Fact]
    public void Configure_DeclaresTheSixIndexesByName()
    {
        var names = BuildEntityType(null).GetIndexes().Select(i => i.GetDatabaseName()).ToList();

        names.ShouldBe(
            [
                "UX_DeadLetterMessages_Source",
                "IX_DeadLetterMessages_DeadLetteredAt",
                "IX_DeadLetterMessages_Pending",
                "IX_DeadLetterMessages_ExpiresAt",
                "IX_DeadLetterMessages_CorrelationId",
                "IX_DeadLetterMessages_Tenant"
            ],
            ignoreOrder: true);
    }

    [Fact]
    public void Configure_MakesTheSourceKeyUnique()
    {
        var index = Index(BuildEntityType(null), "UX_DeadLetterMessages_Source");

        index.IsUnique.ShouldBeTrue();
        Columns(index).ShouldBe([nameof(DeadLetterMessage.SourcePattern), nameof(DeadLetterMessage.SourceMessageId)]);
    }

    [Theory]
    [InlineData("IX_DeadLetterMessages_DeadLetteredAt", new[] { "DeadLetteredAtUtc", "Id" })]
    [InlineData("IX_DeadLetterMessages_Pending", new[] { "ReplayedAtUtc", "SourcePattern", "DeadLetteredAtUtc" })]
    [InlineData("IX_DeadLetterMessages_ExpiresAt", new[] { "ExpiresAtUtc" })]
    [InlineData("IX_DeadLetterMessages_CorrelationId", new[] { "CorrelationId" })]
    [InlineData("IX_DeadLetterMessages_Tenant", new[] { "TenantId", "DeadLetteredAtUtc" })]
    public void Configure_BuildsTheNonUniqueIndexesWithoutAFilter(string name, string[] expectedColumns)
    {
        var index = Index(BuildEntityType(null), name);

        index.IsUnique.ShouldBeFalse();
        index.GetFilter().ShouldBeNull();
        Columns(index).ShouldBe(expectedColumns);
    }

    [Fact]
    public void Configure_NullCollation_LeavesTheFilterColumnsOnTheDatabaseDefault()
    {
        var entityType = BuildEntityType(null);

        foreach (var name in FilterColumns)
        {
            entityType.FindProperty(name).ShouldNotBeNull().GetCollation().ShouldBeNull();
        }
    }

    [Fact]
    public void Configure_EmptyCollation_LeavesTheFilterColumnsOnTheDatabaseDefault()
    {
        var entityType = BuildEntityType(string.Empty);

        foreach (var name in FilterColumns)
        {
            entityType.FindProperty(name).ShouldNotBeNull().GetCollation().ShouldBeNull();
        }
    }

    [Theory]
    [InlineData(DeadLetterMessageConfiguration.SqlServerBinaryCollation)]
    [InlineData(DeadLetterMessageConfiguration.MySqlBinaryCollation)]
    public void Configure_WithCollation_AppliesItToEverySixFilterColumnsAndNoOther(string collation)
    {
        var entityType = BuildEntityType(collation);

        foreach (var name in FilterColumns)
        {
            entityType.FindProperty(name).ShouldNotBeNull().GetCollation().ShouldBe(collation);
        }

        entityType.GetProperties()
            .Where(p => !FilterColumns.Contains(p.Name))
            .ShouldAllBe(p => p.GetCollation() == null);
    }

    private static readonly string[] FilterColumns =
    [
        nameof(DeadLetterMessage.RequestType),
        nameof(DeadLetterMessage.ErrorCode),
        nameof(DeadLetterMessage.CorrelationId),
        nameof(DeadLetterMessage.SourcePattern),
        nameof(DeadLetterMessage.SourceMessageId),
        nameof(DeadLetterMessage.TenantId)
    ];
}

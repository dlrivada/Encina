using Encina.Security.BreakTheGlass;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.BreakTheGlass;

/// <summary>
/// Entity Framework Core configuration of <see cref="BreakTheGlassElevationEntity"/> for SQL Server.
/// </summary>
/// <remarks>
/// <para>
/// It mirrors the script <c>030_CreateBreakTheGlassElevationsTable.sql</c> that the ADO.NET and Dapper SQL Server packages
/// ship: the same table, column types, lengths (from <see cref="BreakTheGlassLimits"/>), binary collation on the key columns,
/// computed <c>ActiveKey</c> and indexes. The collation and the computed expression are SQL Server specific, so another
/// provider needs its own configuration.
/// </para>
/// <para>
/// <c>ActiveKey</c> is a PERSISTED computed column under a plain unique index: SQL Server rejects a filtered index over a
/// computed column, and a non-active row carries its own elevation id there, so only active rows can collide.
/// </para>
/// </remarks>
public sealed class BreakTheGlassElevationEntityConfiguration : IEntityTypeConfiguration<BreakTheGlassElevationEntity>
{
    /// <summary>The name of the table.</summary>
    public const string TableName = "BreakTheGlassElevations";

    /// <summary>The name of the unique index over <c>ActiveKey</c>.</summary>
    public const string ActiveKeyIndexName = "UX_BreakTheGlassElevations_ActiveKey";

    /// <summary>The collation of the key columns, so that they compare case-sensitively like the other providers.</summary>
    public const string KeyCollation = "Latin1_General_100_BIN2";

    // Must match the ActiveKey expression of the shipped script, and BreakTheGlassStoreEF.ActiveKeyOf.
    private const string ActiveKeyExpression =
        "CASE WHEN [Status] = 0 " +
        "THEN CAST(HASHBYTES('SHA2_256', CONCAT(COALESCE([TenantId], N''), NCHAR(31), [UserId], NCHAR(31), [SubjectId], NCHAR(31), [Scope])) AS BINARY(32)) " +
        "ELSE CAST([ElevationId] AS BINARY(32)) END";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BreakTheGlassElevationEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(TableName);
        builder.HasKey(x => x.ElevationId).HasName("PK_BreakTheGlassElevations");
        builder.Property(x => x.ElevationId).ValueGeneratedNever();

        builder.Property(x => x.TenantId).HasMaxLength(BreakTheGlassLimits.TenantIdMaxLength).UseCollation(KeyCollation);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(BreakTheGlassLimits.UserIdMaxLength).UseCollation(KeyCollation);
        builder.Property(x => x.SubjectId).IsRequired().HasMaxLength(BreakTheGlassLimits.SubjectIdMaxLength).UseCollation(KeyCollation);
        builder.Property(x => x.Scope).IsRequired().HasMaxLength(BreakTheGlassLimits.ScopeMaxLength).UseCollation(KeyCollation);
        builder.Property(x => x.Justification).IsRequired().HasMaxLength(BreakTheGlassLimits.JustificationMaxLength);
        builder.Property(x => x.CloseReason).HasMaxLength(BreakTheGlassLimits.CloseReasonMaxLength).UseCollation(KeyCollation);
        builder.Property(x => x.ReviewedByUserId).HasMaxLength(BreakTheGlassLimits.ReviewedByUserIdMaxLength).UseCollation(KeyCollation);
        builder.Property(x => x.ReviewNote).HasMaxLength(BreakTheGlassLimits.ReviewNoteMaxLength);

        builder.Property(x => x.OpenedAtUtc).IsRequired().HasColumnType("datetime2(7)");
        builder.Property(x => x.ExpiresAtUtc).IsRequired().HasColumnType("datetime2(7)");
        builder.Property(x => x.ClosedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(x => x.ExpiryRecordedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(x => x.ReviewedAtUtc).HasColumnType("datetime2(7)");

        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.ReviewOutcome).HasConversion<int?>();

        builder.Property(x => x.ActiveKey)
            .HasColumnType("binary(32)")
            .HasComputedColumnSql(ActiveKeyExpression, stored: true);

        // HasFilter(null): EF Core adds "WHERE [ActiveKey] IS NOT NULL" to a unique index over a nullable property on SQL Server,
        // and SQL Server rejects a filtered index over a computed column. The shipped script has a plain unique index.
        builder.HasIndex(x => x.ActiveKey).IsUnique().HasFilter(null).HasDatabaseName(ActiveKeyIndexName);
        builder.HasIndex(x => new { x.TenantId, x.ReviewOutcome, x.OpenedAtUtc }).HasDatabaseName("IX_BreakTheGlassElevations_ReviewQueue");
        builder.HasIndex(x => new { x.Status, x.ExpiresAtUtc }).HasDatabaseName("IX_BreakTheGlassElevations_Expiry");
    }
}

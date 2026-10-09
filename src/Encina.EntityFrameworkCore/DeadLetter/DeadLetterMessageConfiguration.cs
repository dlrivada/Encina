using Encina.Messaging.DeadLetter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.DeadLetter;

/// <summary>
/// Entity Framework Core configuration for <see cref="DeadLetterMessage"/>.
/// </summary>
/// <remarks>
/// <para>
/// Applications apply it like <c>ScheduledMessageConfiguration</c>:
/// <c>modelBuilder.ApplyConfiguration(new DeadLetterMessageConfiguration())</c>. The table, columns and
/// indexes match the <c>029_CreateDeadLetterMessagesTable.sql</c> scripts of the ADO.NET and Dapper
/// providers, so switching the provider family does not change the schema. Lengths come from
/// <see cref="DeadLetterStoreLimits"/>.
/// </para>
/// <para>
/// No index uses <c>HasFilter</c>: filters take dialect-specific SQL and this configuration serves SQL
/// Server, PostgreSQL and MySQL.
/// </para>
/// </remarks>
public sealed class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    /// <summary>
    /// The binary collation of the string filter columns on SQL Server.
    /// </summary>
    public const string SqlServerBinaryCollation = "Latin1_General_100_BIN2";

    /// <summary>
    /// The binary collation of the string filter columns on MySQL.
    /// </summary>
    public const string MySqlBinaryCollation = "utf8mb4_bin";

    private const int ExceptionTypeMaxLength = 512;

    private readonly string? _filterColumnCollation;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterMessageConfiguration"/> class.
    /// </summary>
    /// <param name="filterColumnCollation">
    /// The collation of <c>RequestType</c>, <c>ErrorCode</c>, <c>CorrelationId</c>, <c>SourcePattern</c>,
    /// <c>SourceMessageId</c> and <c>TenantId</c>. Pass <see cref="SqlServerBinaryCollation"/> on SQL Server and
    /// <see cref="MySqlBinaryCollation"/> on MySQL so that the unique source key and the filters compare
    /// case-sensitively, as the 029 scripts and the other providers do. PostgreSQL needs none (the default,
    /// <c>null</c>): its default comparison is case-sensitive.
    /// </param>
    public DeadLetterMessageConfiguration(string? filterColumnCollation = null)
    {
        _filterColumnCollation = filterColumnCollation;
    }

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("DeadLetterMessages");

        builder.HasKey(x => x.Id);

        ConfigureColumns(builder);
        ConfigureCollation(builder);
        ConfigureIndexes(builder);

        // Derived from ReplayedAtUtc; not a column.
        builder.Ignore(x => x.IsReplayed);
    }

    private void ConfigureCollation(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        if (string.IsNullOrEmpty(_filterColumnCollation))
        {
            return;
        }

        builder.Property(x => x.RequestType).UseCollation(_filterColumnCollation);
        builder.Property(x => x.ErrorCode).UseCollation(_filterColumnCollation);
        builder.Property(x => x.CorrelationId).UseCollation(_filterColumnCollation);
        builder.Property(x => x.SourcePattern).UseCollation(_filterColumnCollation);
        builder.Property(x => x.SourceMessageId).UseCollation(_filterColumnCollation);
        builder.Property(x => x.TenantId).UseCollation(_filterColumnCollation);
    }

    private static void ConfigureColumns(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        builder.Property(x => x.RequestType).IsRequired().HasMaxLength(DeadLetterStoreLimits.RequestTypeMaxLength);
        builder.Property(x => x.RequestContent).IsRequired();
        builder.Property(x => x.ErrorCode).IsRequired().HasMaxLength(DeadLetterStoreLimits.ErrorCodeMaxLength);
        builder.Property(x => x.ExceptionType).IsRequired(false).HasMaxLength(ExceptionTypeMaxLength);
        builder.Property(x => x.ExceptionStackTrace).IsRequired(false);
        builder.Property(x => x.CorrelationId).IsRequired(false).HasMaxLength(DeadLetterStoreLimits.CorrelationIdMaxLength);
        builder.Property(x => x.SourcePattern).IsRequired().HasMaxLength(DeadLetterStoreLimits.SourcePatternMaxLength);
        builder.Property(x => x.SourceMessageId).IsRequired().HasMaxLength(DeadLetterStoreLimits.SourceMessageIdMaxLength);
        builder.Property(x => x.TenantId).IsRequired(false).HasMaxLength(DeadLetterStoreLimits.TenantIdMaxLength);
        builder.Property(x => x.TotalRetryAttempts).IsRequired();
        builder.Property(x => x.FirstFailedAtUtc).IsRequired();
        builder.Property(x => x.DeadLetteredAtUtc).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired(false);
        builder.Property(x => x.ReplayClaimedAtUtc).IsRequired(false);
        builder.Property(x => x.ReplayedAtUtc).IsRequired(false);
        builder.Property(x => x.ReplayResult).IsRequired(false).HasMaxLength(DeadLetterStoreLimits.ReplayResultMaxLength);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        // Idempotent capture: one dead letter per source message
        builder.HasIndex(x => new { x.SourcePattern, x.SourceMessageId })
            .IsUnique()
            .HasDatabaseName("UX_DeadLetterMessages_Source");

        // Paging order
        builder.HasIndex(x => new { x.DeadLetteredAtUtc, x.Id })
            .HasDatabaseName("IX_DeadLetterMessages_DeadLetteredAt");

        // Health check, statistics and FromSource
        builder.HasIndex(x => new { x.ReplayedAtUtc, x.SourcePattern, x.DeadLetteredAtUtc })
            .HasDatabaseName("IX_DeadLetterMessages_Pending");

        // Cleanup
        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("IX_DeadLetterMessages_ExpiresAt");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_DeadLetterMessages_CorrelationId");

        builder.HasIndex(x => new { x.TenantId, x.DeadLetteredAtUtc })
            .HasDatabaseName("IX_DeadLetterMessages_Tenant");
    }
}

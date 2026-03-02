using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Retention;

/// <summary>
/// Extension methods for applying retention entity configurations to a <see cref="ModelBuilder"/>.
/// </summary>
public static class RetentionModelBuilderExtensions
{
    /// <summary>
    /// Applies all retention-related entity configurations (RetentionPolicies, RetentionRecords,
    /// LegalHolds, RetentionAuditEntries) to the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <remarks>
    /// Call this method in your <c>OnModelCreating</c> override when using the retention module:
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyRetentionConfiguration();
    /// }
    /// </code>
    /// </remarks>
    public static ModelBuilder ApplyRetentionConfiguration(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new RetentionPolicyEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RetentionRecordEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LegalHoldEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RetentionAuditEntryEntityConfiguration());

        return modelBuilder;
    }
}

using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Consent;

/// <summary>
/// Extension methods for applying consent entity configurations to a <see cref="ModelBuilder"/>.
/// </summary>
public static class ConsentModelBuilderExtensions
{
    /// <summary>
    /// Applies all consent-related entity configurations (ConsentRecords, ConsentAuditEntries,
    /// ConsentVersions) to the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <remarks>
    /// Call this method in your <c>OnModelCreating</c> override when using the consent module:
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyConsentConfiguration();
    /// }
    /// </code>
    /// </remarks>
    public static ModelBuilder ApplyConsentConfiguration(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new ConsentRecordEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ConsentAuditEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ConsentVersionEntityConfiguration());

        return modelBuilder;
    }
}

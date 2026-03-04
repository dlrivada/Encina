using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.BreachNotification;

/// <summary>
/// Extension methods for applying breach notification entity configurations to a <see cref="ModelBuilder"/>.
/// </summary>
public static class BreachNotificationModelBuilderExtensions
{
    /// <summary>
    /// Applies all breach notification-related entity configurations (BreachRecords,
    /// BreachPhasedReports, BreachAuditEntries) to the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <remarks>
    /// Call this method in your <c>OnModelCreating</c> override when using the breach
    /// notification module:
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyBreachNotificationConfiguration();
    /// }
    /// </code>
    /// </remarks>
    public static ModelBuilder ApplyBreachNotificationConfiguration(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new BreachRecordEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PhasedReportEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BreachAuditEntryEntityConfiguration());

        return modelBuilder;
    }
}

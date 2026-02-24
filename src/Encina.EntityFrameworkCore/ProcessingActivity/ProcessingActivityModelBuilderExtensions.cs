using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.ProcessingActivity;

/// <summary>
/// Extension methods for applying processing activity entity configurations to a <see cref="ModelBuilder"/>.
/// </summary>
public static class ProcessingActivityModelBuilderExtensions
{
    /// <summary>
    /// Applies the processing activity entity configuration (ProcessingActivities table)
    /// to the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <remarks>
    /// Call this method in your <c>OnModelCreating</c> override when using the processing activity module:
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyProcessingActivityConfiguration();
    /// }
    /// </code>
    /// </remarks>
    public static ModelBuilder ApplyProcessingActivityConfiguration(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new ProcessingActivityEntityConfiguration());

        return modelBuilder;
    }
}

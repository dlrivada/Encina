using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.DPIA;

/// <summary>
/// Extension methods for applying DPIA entity configurations to a <see cref="ModelBuilder"/>.
/// </summary>
public static class DPIAModelBuilderExtensions
{
    /// <summary>
    /// Applies all DPIA-related entity configurations (DPIAAssessments, DPIAAuditEntries)
    /// to the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The same <see cref="ModelBuilder"/> instance for chaining.</returns>
    public static ModelBuilder ApplyDPIAConfiguration(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new DPIAAssessmentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DPIAAuditEntryEntityConfiguration());

        return modelBuilder;
    }
}

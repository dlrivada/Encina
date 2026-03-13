using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.ProcessorAgreements;

/// <summary>
/// Extension methods for applying processor agreement entity configurations to a <see cref="ModelBuilder"/>.
/// </summary>
public static class ProcessorAgreementModelBuilderExtensions
{
    /// <summary>
    /// Applies all processor agreement-related entity configurations (Processors,
    /// DataProcessingAgreements, ProcessorAgreementAuditEntries) to the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The same <see cref="ModelBuilder"/> instance for chaining.</returns>
    public static ModelBuilder ApplyProcessorAgreementConfiguration(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new ProcessorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DataProcessingAgreementEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessorAgreementAuditEntryEntityConfiguration());

        return modelBuilder;
    }
}

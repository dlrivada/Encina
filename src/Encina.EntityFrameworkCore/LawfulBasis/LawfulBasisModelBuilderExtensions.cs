using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.LawfulBasis;

/// <summary>
/// Extension methods for applying lawful basis entity configurations to a <see cref="ModelBuilder"/>.
/// </summary>
public static class LawfulBasisModelBuilderExtensions
{
    /// <summary>
    /// Applies all lawful basis-related entity configurations (LawfulBasisRegistrations, LIARecords)
    /// to the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <remarks>
    /// Call this method in your <c>OnModelCreating</c> override when using the lawful basis module:
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyLawfulBasisConfiguration();
    /// }
    /// </code>
    /// </remarks>
    public static ModelBuilder ApplyLawfulBasisConfiguration(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new LawfulBasisRegistrationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LIARecordEntityConfiguration());

        return modelBuilder;
    }
}

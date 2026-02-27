using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.DataSubjectRights;

/// <summary>
/// Extension methods for applying DSR entity configurations to an EF Core model builder.
/// </summary>
public static class DSRModelBuilderExtensions
{
    /// <summary>
    /// Applies the Data Subject Rights entity configurations to the model builder.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplyDSRConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DSRRequestEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DSRAuditEntryEntityConfiguration());
        return modelBuilder;
    }
}

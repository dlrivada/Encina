using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.ABAC;

/// <summary>
/// Extension methods for applying ABAC (Attribute-Based Access Control) entity configurations
/// to a <see cref="ModelBuilder"/>.
/// </summary>
public static class ABACModelBuilderExtensions
{
    /// <summary>
    /// Applies all ABAC-related entity configurations (PolicySets, Policies) to the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <remarks>
    /// Call this method in your <c>OnModelCreating</c> override when using the ABAC
    /// persistent policy store:
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyABACConfiguration();
    /// }
    /// </code>
    /// </remarks>
    public static ModelBuilder ApplyABACConfiguration(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new PolicySetEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PolicyEntityConfiguration());

        return modelBuilder;
    }
}

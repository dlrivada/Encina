using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.BreakTheGlass;

/// <summary>
/// Extension methods for applying the break-the-glass entity configuration to a <see cref="ModelBuilder"/>.
/// </summary>
public static class BreakTheGlassModelBuilderExtensions
{
    /// <summary>
    /// Applies the break-the-glass entity configuration (BreakTheGlassElevations table, SQL Server) to the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <remarks>
    /// Call this method in your <c>OnModelCreating</c> override when using the break-the-glass store:
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyBreakTheGlassConfiguration();
    /// }
    /// </code>
    /// The store writes through <c>IDbContextFactory&lt;TContext&gt;</c>, so register the factory too
    /// (<c>AddDbContextFactory&lt;AppDbContext&gt;</c>).
    /// </remarks>
    public static ModelBuilder ApplyBreakTheGlassConfiguration(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new BreakTheGlassElevationEntityConfiguration());

        return modelBuilder;
    }
}

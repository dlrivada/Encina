using Microsoft.AspNetCore.Builder;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Extension methods for configuring Encina tenancy middleware in the ASP.NET Core pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the tenant resolution middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="app"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This middleware should be placed after authentication middleware and after
    /// <c>UseEncinaContext()</c> in the pipeline.
    /// </para>
    /// <para>
    /// The middleware resolves tenant identifiers using the configured resolver chain
    /// and updates the <see cref="Encina.IRequestContext"/> accordingly.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    ///
    /// app.UseAuthentication();
    /// app.UseEncinaContext();     // First, establish base context
    /// app.UseTenantResolution();  // Then, resolve tenant
    /// app.UseAuthorization();
    ///
    /// app.MapControllers();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<TenantResolutionMiddleware>();
    }
}

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
    /// Place this middleware after authentication middleware (claim-based resolution needs the
    /// user). <c>UseEncinaContext()</c> from the <c>Encina.AspNetCore</c> package is optional: when
    /// it is used, place it before this middleware so the resolved tenant is added to the context it
    /// establishes; without it, this middleware creates the ambient context itself.
    /// </para>
    /// <para>
    /// The middleware resolves tenant identifiers using the configured resolver chain and sets the
    /// tenant on the ambient <see cref="Encina.IRequestContext"/> held by
    /// <see cref="Encina.IRequestContextAccessor"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    ///
    /// app.UseAuthentication();
    /// app.UseEncinaContext();     // Optional: establish the base context (user, idempotency key)
    /// app.UseTenantResolution();  // Then, resolve the tenant into that context
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

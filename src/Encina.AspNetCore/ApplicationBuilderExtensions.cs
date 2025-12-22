using Microsoft.AspNetCore.Builder;

namespace Encina.AspNetCore;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> to configure Encina middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Encina context enrichment middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This middleware enriches <see cref="IRequestContext"/> with information from the HTTP request:
    /// <list type="bullet">
    /// <item><description><b>CorrelationId</b>: From X-Correlation-ID header or Activity.Current</description></item>
    /// <item><description><b>UserId</b>: From authenticated user claims</description></item>
    /// <item><description><b>TenantId</b>: From claims or X-Tenant-ID header</description></item>
    /// <item><description><b>IdempotencyKey</b>: From X-Idempotency-Key header</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Important</b>: This middleware should be placed after authentication middleware
    /// but before any middleware that uses Encina.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    ///
    /// app.UseAuthentication();
    /// app.UseAuthorization();
    /// app.UseEncinaContext(); // After auth, before endpoints
    ///
    /// app.MapControllers();
    /// app.Run();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseEncinaContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<EncinaContextMiddleware>();
    }
}

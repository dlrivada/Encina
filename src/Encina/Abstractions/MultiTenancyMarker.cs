namespace Encina;

/// <summary>
/// The container signal that the application runs with multi-tenancy enabled.
/// </summary>
/// <remarks>
/// <para>
/// A multi-tenancy package registers this type (for example <c>AddEncinaTenancy</c> in
/// <c>Encina.Tenancy</c>). Any package can then check it with
/// <c>IServiceProviderIsService.IsService(typeof(MultiTenancyMarker))</c> without referencing the
/// tenancy package, and fail closed when a tenant is required but the request carries none.
/// </para>
/// <para>
/// The marker carries no state: its presence in the container is the whole signal. An application
/// that resolves tenants in its own way, without a multi-tenancy package, registers it itself so
/// tenant-aware components gate their reads by tenant.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var isService = serviceProvider.GetRequiredService&lt;IServiceProviderIsService&gt;();
/// var multiTenant = isService.IsService(typeof(MultiTenancyMarker));
/// </code>
/// </example>
public sealed class MultiTenancyMarker;

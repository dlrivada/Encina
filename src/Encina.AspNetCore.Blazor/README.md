# Encina.AspNetCore.Blazor

[![NuGet](https://img.shields.io/nuget/v/Encina.AspNetCore.Blazor.svg)](https://www.nuget.org/packages/Encina.AspNetCore.Blazor)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

**Blazor Server integration for Encina's authorization pipeline.**

Resolves the current caller's principal from `AuthenticationStateProvider` when there is no ambient `HttpContext`, so `[Authorize]` and `[ResourceAuthorize]` on commands and queries keep working inside a Blazor Server interactive circuit.

## Why this package exists

`Encina.AspNetCore`'s `AuthorizationPipelineBehavior` asks an `IPrincipalResolver` for the caller's `ClaimsPrincipal` instead of reading `IHttpContextAccessor` directly. The default resolver, `HttpContextPrincipalResolver`, reads `IHttpContextAccessor.HttpContext?.User` and returns `null` — which the behavior treats as "not authenticated" — when there is no `HttpContext`.

A Blazor Server interactive circuit has no `HttpContext` for the whole lifetime of the circuit after the initial negotiate request; this is standard, documented ASP.NET Core Blazor Server behavior, not a bug in the framework. Any `[Authorize]` command or query sent from a component's event handler was therefore denied unconditionally, regardless of whether the user was signed in. `Encina.AspNetCore.Blazor` provides `AuthenticationStatePrincipalResolver`, which prefers `HttpContext.User` when a request is served over classic HTTP (a Blazor Server prerender, an API controller, or a non-Blazor page in the same app) and falls back to `AuthenticationStateProvider.GetAuthenticationStateAsync().User` — the mechanism Blazor components normally get their identity from via `CascadingAuthenticationState` — only when there is no `HttpContext`.

## Installation

```bash
dotnet add package Encina.AspNetCore.Blazor
```

## Quick start

Register Encina, `Encina.AspNetCore`, Blazor Server, then this package last, so `AddEncinaBlazorAuthorization()` replaces the default resolver:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEncina(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
}, typeof(Program).Assembly);

builder.Services.AddEncinaAspNetCore();

builder.Services.AddEncinaAuthorization(
    auth =>
    {
        auth.AutoApplyPolicies = true;
    },
    policies =>
    {
        policies.AddPolicy("CanEditOrders", p => p
            .RequireAuthenticatedUser()
            .RequireRole("Admin", "OrderManager"));
    });

// Blazor Server hosting
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Replaces the default HTTP-only IPrincipalResolver with one that
// also resolves the principal from AuthenticationStateProvider.
builder.Services.AddEncinaBlazorAuthorization();
```

The middleware pipeline still needs `app.UseAuthentication()` and `app.UseAuthorization()` in the usual order, as described in the [`Encina.AspNetCore` README](../Encina.AspNetCore/README.md#2-configure-middleware-pipeline).

A command or query authorized with `[Authorize]` behaves the same whether it is sent from an interactive Blazor Server component or from an HTTP endpoint in the same application:

```csharp
[Authorize(Roles = "Admin,OrderManager")]
public record UpdateOrderCommand(OrderId Id, string NewStatus) : ICommand<Order>;
```

```razor
@inject IEncina Encina

<button @onclick="UpdateStatus">Mark as shipped</button>

@code {
    private async Task UpdateStatus()
    {
        var result = await Encina.Send(new UpdateOrderCommand(OrderId, "Shipped"));
        // ... handle result.Match(...)
    }
}
```

Inside the circuit, `AuthenticationStatePrincipalResolver` has no `HttpContext` to read, so it awaits `AuthenticationStateProvider.GetAuthenticationStateAsync()` and hands `AuthorizationPipelineBehavior` the circuit's authenticated principal instead of `null`.

## Reference

### `AuthenticationStatePrincipalResolver`

Implements `Encina.AspNetCore.IPrincipalResolver`.

```csharp
public AuthenticationStatePrincipalResolver(
    AuthenticationStateProvider authenticationStateProvider,
    IHttpContextAccessor? httpContextAccessor = null)
```

`ResolvePrincipalAsync(CancellationToken)` returns `httpContextAccessor.HttpContext.User` when an `HttpContext` is present; otherwise it returns the `ClaimsPrincipal` from `authenticationStateProvider.GetAuthenticationStateAsync()`.

### `ServiceCollectionExtensions.AddEncinaBlazorAuthorization(IServiceCollection)`

- Calls `services.AddHttpContextAccessor()`, so requests served over classic HTTP in the same application still resolve the principal from `HttpContext.User`.
- Replaces the registered `IPrincipalResolver` with a **scoped** `AuthenticationStatePrincipalResolver` — scoped because `AuthenticationStateProvider` is itself scoped per Blazor circuit.
- Call it after registering Blazor Server (`AddServerSideBlazor()` or `AddRazorComponents().AddInteractiveServerComponents()`), which registers `AuthenticationStateProvider`.

## Dependencies

- `Encina.AspNetCore` (defines `IPrincipalResolver` and `AuthorizationPipelineBehavior`)
- `Microsoft.AspNetCore.App` (framework reference; brings in `Microsoft.AspNetCore.Components.Authorization` and `Microsoft.AspNetCore.Http`)

## See also

- [Encina.AspNetCore](../Encina.AspNetCore/README.md) — the base ASP.NET Core integration: request context enrichment, the authorization pipeline behavior, and Problem Details error mapping.
- [Authorization feature docs](../../docs/features/authorization.md)

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

# Encina.AspNetCore.Blazor

[![NuGet](https://img.shields.io/nuget/v/Encina.AspNetCore.Blazor.svg)](https://www.nuget.org/packages/Encina.AspNetCore.Blazor)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

**Blazor Server integration for Encina's authorization pipeline.**

Binds the request identity of a Blazor Server circuit from its `AuthenticationState`, so `[Authorize]` and `[ResourceAuthorize]` on commands and queries keep working inside an interactive circuit.

## Why this package exists

A Blazor Server circuit has no `HttpContext` after the initial negotiate request, and its connection (`/_blazor`) carries no request identity: `UseEncinaContext()` runs it anonymous. Without this package every `[Authorize]` command sent from a component's event handler would be denied. `AddEncinaBlazorAuthorization()` registers a circuit handler that fills that gap with the circuit's own `AuthenticationState`.

## Installation

```bash
dotnet add package Encina.AspNetCore.Blazor
```

## Quick start

A complete minimal `Program.cs` in the .NET 10 Blazor Web App shape. `App` is the application's root component (`App.razor`, as in the Blazor Web App template). Register Encina, `Encina.AspNetCore`, Blazor Server components, then this package after them:

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

builder.Services.AddAuthentication("Cookies").AddCookie();
builder.Services.AddCascadingAuthenticationState();

// Blazor Server hosting: opens the interactive circuits.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Runs each circuit activity in one inbound identity scope
// built from the circuit's current AuthenticationState.
builder.Services.AddEncinaBlazorAuthorization();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseEncinaContext();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
```

`AddInteractiveServerRenderMode()` maps the `/_blazor` hub that carries the circuits. The pipeline order is described in the [`Encina.AspNetCore` README](../Encina.AspNetCore/README.md#2-request-identity).

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

## Reference

### `ServiceCollectionExtensions.AddEncinaBlazorAuthorization(IServiceCollection)`

Registers the request identity model (`AddEncinaRequestIdentity()`) and a scoped circuit handler. Call it after registering Blazor Server (`AddServerSideBlazor()` or `AddRazorComponents().AddInteractiveServerComponents()`), which registers `AuthenticationStateProvider`.

| Situation | Identity the code sees |
|---|---|
| Inside a circuit activity (UI event, JavaScript interop call) | The user of the current `AuthenticationState`, in one inbound identity scope per activity |
| The authentication state changed | The new state, applied at the next activity |
| Outside an activity (a continuation that outlives it, a timer) | Anonymous |
| The scope is refused | The activity still runs, anonymous, under a masking scope |

## Dependencies

- `Encina.AspNetCore` (defines `AuthorizationPipelineBehavior` and `UseEncinaContext()`)
- `Microsoft.AspNetCore.App` (framework reference; brings in `Microsoft.AspNetCore.Components.Authorization` and `Microsoft.AspNetCore.Http`)

## See also

- [Encina.AspNetCore](../Encina.AspNetCore/README.md) — the base ASP.NET Core integration: request context enrichment, the authorization pipeline behavior, and Problem Details error mapping.
- [Authorization feature docs](../../docs/features/authorization.md)

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

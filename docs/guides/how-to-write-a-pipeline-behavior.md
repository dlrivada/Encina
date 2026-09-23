---
title: "How to write a pipeline behavior"
layout: default
parent: "Guides"
nav_order: 5
---

# How to write a pipeline behavior

This guide shows you how to write, register and order a custom `IPipelineBehavior<TRequest, TResponse>` for the Encina pipeline. It assumes you already sent your first command through `IEncina` (see the [quickstart](../tutorials/quickstart.md)) and understand what a pipeline behavior does (see [About the request pipeline](../architecture/request-pipeline.md)); it does not re-explain the pipeline, only how to add to it.

## 1. Implement `IPipelineBehavior<TRequest, TResponse>`

A behavior lives in the `Encina` namespace and takes the request, the ambient `IRequestContext`, a `RequestHandlerCallback<TResponse> nextStep` and a `CancellationToken`; it returns `ValueTask<Either<EncinaError, TResponse>>`. The built-in `ValidationPipelineBehavior<TRequest, TResponse>` (`src/Encina/Validation/ValidationPipelineBehavior.cs`) is a real example of the short-circuit pattern:

```csharp
public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ValidationOrchestrator _orchestrator;

    public ValidationPipelineBehavior(ValidationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        var validationResult = await _orchestrator.ValidateAsync<TRequest, TResponse>(
            request, context, cancellationToken).ConfigureAwait(false);

        return await validationResult.Match(
            Left: error => ValueTask.FromResult(Either<EncinaError, TResponse>.Left(error)),
            Right: _ => nextStep()).ConfigureAwait(false);
    }
}
```

Follow the same shape for your own behavior: guard the arguments, do your work, and either call `nextStep()` to continue the pipeline or return your own `Either<EncinaError, TResponse>.Left(...)` to stop it. Use `EncinaErrors.Create(code, message)` (or `EncinaErrors.FromException`) to build the `EncinaError` — the same factory the [quickstart](../tutorials/quickstart.md) uses for a handler failure.

If your behavior only makes sense for one side of CQRS, implement `ICommandPipelineBehavior<TCommand, TResponse>` or `IQueryPipelineBehavior<TQuery, TResponse>` instead of the generic interface; both extend `IPipelineBehavior<TRequest, TResponse>` with the same `Handle` signature.

## 2. Short-circuit with `Left`

To stop the pipeline before the handler runs — the way `ValidationPipelineBehavior` does when validation fails — return a `Left` without calling `nextStep()`. `nextStep` is simply never invoked, so the handler, the remaining behaviors and any behavior further down the chain never execute. Post-processors still run and see the `Left` you returned (see [About the request pipeline](../architecture/request-pipeline.md#the-dispatch-flow)).

```csharp
public sealed class RequireTenantBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        if (context.TenantId is null)
        {
            var error = EncinaErrors.Create("tenant.required", $"{typeof(TRequest).Name} requires a tenant.");
            return ValueTask.FromResult(Either<EncinaError, TResponse>.Left(error));
        }

        return nextStep();
    }
}
```

Only catch `OperationCanceledException` for a cancelled token inside a behavior; any other exception is a bug in the behavior, not a functional failure, and should be left to propagate (see [ADR-006](../architecture/adr/006-pure-rop-exception-handling.md)).

## 3. Register the behavior

Register an open generic behavior through `EncinaConfiguration.AddPipelineBehavior`, passed to `AddEncina`:

```csharp
services.AddEncina(cfg =>
{
    cfg.AddPipelineBehavior(typeof(RequireTenantBehavior<,>));
}, typeof(Program).Assembly);
```

`AddEncina` also scans every assembly you give it and registers any concrete (non-nested) type it finds that implements `IPipelineBehavior<,>`, `ICommandPipelineBehavior<,>` or `IQueryPipelineBehavior<,>` — you do not have to call `AddPipelineBehavior` for a behavior that already lives in a scanned assembly. Use the explicit call when you need to register a behavior from an assembly you are not scanning, or when you need to control its order relative to other explicitly-registered behaviors.

## 4. Control the order

Behaviors run in registration order, outermost first: the first one registered runs first and is the last to see the final `Either`; the last one registered sits closest to the handler. Registering `AddPipelineBehavior(typeof(A<,>))` before `AddPipelineBehavior(typeof(B<,>))` produces `A → B → Handler → B → A` for the parts of the pipeline each behavior wraps. `tests/Encina.UnitTests/Core/EncinaTests.cs` verifies this directly: registering `TrackingBehavior<,>` then `SecondTrackingBehavior<,>` and sending a request produces the event sequence `tracking:before, second:before, handler, second:after, tracking:after`.

When a behavior depends on another behavior's work (for example, a behavior that reads a value a tenant-resolution behavior wrote into `IRequestContext`), register the dependency first so it runs before the behavior that needs it.

## See also

- [About the request pipeline](../architecture/request-pipeline.md) — what behaviors, pre/post-processors and the dispatch flow are, and why `Either<EncinaError, TResponse>`.
- [Pipeline behaviors reference](../features/pipeline-behaviors.md) — every built-in behavior, what it does and how to register it.

---
title: "Pipeline behaviors"
layout: default
parent: "Features"
nav_order: 82
---

# Pipeline behaviors

Reference for the pipeline behaviors that ship in the core `Encina` package. For what a pipeline behavior is and how the dispatch flow assembles them, see [About the request pipeline](../architecture/request-pipeline.md); for writing your own, see [How to write a pipeline behavior](../guides/how-to-write-a-pipeline-behavior.md). Full member signatures are in the [API reference](https://dlrivada.github.io/Encina/api/).

Behaviors that belong to a specific provider or compliance module (transactions, outbox/inbox, read/write separation, GDPR, ABAC, and the rest) are documented on their own feature page, listed in the [features index](index.md); this page only covers the generic behaviors in `src/Encina/Pipeline/Behaviors/` and `src/Encina/Validation/`.

## Built-in behaviors

| Behavior | Package / namespace | What it does | How to register | Ordering notes |
|---|---|---|---|---|
| `CommandActivityPipelineBehavior<TCommand, TResponse>` | `Encina` (`Encina.Pipeline.Behaviors`) | Starts an `Activity` per command on the `Encina` `ActivitySource`, tags it with the request/response type, and marks it failed on a `Left` or on a functional failure detected by `IFunctionalFailureDetector`. | `cfg.AddPipelineBehavior(typeof(CommandActivityPipelineBehavior<,>))` inside `AddEncina`, or let assembly scanning find it if it lives in a scanned assembly. `AddEncina` registers `NullFunctionalFailureDetector` as the default `IFunctionalFailureDetector`; register your own before `AddEncina` to detect application-specific functional failures. | Implements `ICommandPipelineBehavior<TCommand, TResponse>`, so it only wraps commands. Register it as an outer behavior (before behaviors that can short-circuit) so the activity spans the whole remaining pipeline, including validation failures. |
| `CommandMetricsPipelineBehavior<TCommand, TResponse>` | `Encina` (`Encina.Pipeline.Behaviors`) | Records success/failure counters and a duration histogram for commands through `IEncinaMetrics`. | `cfg.AddPipelineBehavior(typeof(CommandMetricsPipelineBehavior<,>))`. `AddEncina` already registers the default `IEncinaMetrics` implementation (`EncinaMetrics`); replace it with your own implementation via `services.AddSingleton<IEncinaMetrics, YourImplementation>()` before calling `AddEncina` if you need a different backend. | Command-only (`ICommandPipelineBehavior<,>`). Commonly registered alongside `CommandActivityPipelineBehavior<,>` since both observe the full outcome of `nextStep()`. |
| `QueryActivityPipelineBehavior<TQuery, TResponse>` | `Encina` (`Encina.Pipeline.Behaviors`) | The query equivalent of `CommandActivityPipelineBehavior<,>`: starts an `Activity` tagged `Encina.Query.<TQuery>` and records functional failures. | `cfg.AddPipelineBehavior(typeof(QueryActivityPipelineBehavior<,>))`. Uses the same `IFunctionalFailureDetector` registration as `CommandActivityPipelineBehavior<,>`. | Query-only (`IQueryPipelineBehavior<,>`). Same outer-position guidance as `CommandActivityPipelineBehavior<,>`. |
| `QueryMetricsPipelineBehavior<TQuery, TResponse>` | `Encina` (`Encina.Pipeline.Behaviors`) | The query equivalent of `CommandMetricsPipelineBehavior<,>`: duration and outcome metrics through `IEncinaMetrics`. | `cfg.AddPipelineBehavior(typeof(QueryMetricsPipelineBehavior<,>))`. Uses the same `IEncinaMetrics` registration as `CommandMetricsPipelineBehavior<,>`. | Query-only (`IQueryPipelineBehavior<,>`). |
| `ValidationPipelineBehavior<TRequest, TResponse>` | `Encina` (`Encina.Validation`) | Runs the registered `IValidationProvider` (through `ValidationOrchestrator`) before the handler; on failure, returns `Left` without calling `nextStep()`. | Not registered by assembly scanning — `AddEncina` skips it explicitly because it needs a `ValidationOrchestrator`. It is registered for you by `services.AddEncinaFluentValidation(...)`, `services.AddDataAnnotationsValidation()` or `services.AddMiniValidation()`. | Applies to both commands and queries (`IPipelineBehavior<,>`). As with any behavior, the call that registers it first ends up outermost (see [ordering](../guides/how-to-write-a-pipeline-behavior.md#4-control-the-order)); calling the validation package's `AddEncina*Validation` method before behaviors added through `cfg.AddPipelineBehavior` makes validation the outermost check, so an invalid request never reaches those behaviors. |

## See also

- [About the request pipeline](../architecture/request-pipeline.md) — the dispatch flow, pre/post-processors, and why behaviors return `Either<EncinaError, TResponse>`.
- [How to write a pipeline behavior](../guides/how-to-write-a-pipeline-behavior.md) — write, register and order a custom behavior, including a short-circuit example.
- [Features index](index.md) — provider-specific and compliance behaviors (transactions, outbox/inbox, GDPR, ABAC, read/write separation, and more).

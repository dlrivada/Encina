# Scheduling Pattern

The Scheduling Pattern enables delayed and recurring execution of domain messages (commands, queries, notifications) through the Encina pipeline.

## Enabling

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseScheduling = true;

    // Optional: customize processor behavior
    config.SchedulingOptions.ProcessingInterval = TimeSpan.FromSeconds(15);
    config.SchedulingOptions.BatchSize = 50;
    config.SchedulingOptions.MaxRetries = 5;
    config.SchedulingOptions.BaseRetryDelay = TimeSpan.FromSeconds(10);
});
```

## Scheduling Messages

```csharp
// All scheduling methods return Either<EncinaError, Guid> (Railway Oriented Programming)

// Delayed execution (absolute time)
var result = await orchestrator.ScheduleAsync(
    new SendReminderCommand(orderId),
    executeAt: DateTime.UtcNow.AddHours(24));

result.Match(
    Right: messageId => logger.LogInformation("Scheduled: {Id}", messageId),
    Left: error => logger.LogError("Scheduling failed: {Code}", error.Message));

// Delayed execution (relative delay)
var cancelResult = await orchestrator.ScheduleAsync(
    new CancelOrderCommand(orderId),
    delay: TimeSpan.FromMinutes(30));

// Recurring (cron expression — requires ICronParser registration)
var recurringResult = await orchestrator.ScheduleRecurringAsync(
    new GenerateDailyReportCommand(),
    cronExpression: "0 8 * * *"); // Daily at 8 AM
```

## Background Processor

`ScheduledMessageProcessor` is a `BackgroundService` that automatically polls the store for due messages and dispatches them through `IEncina`.

**Auto-registered** when `UseScheduling = true` and `SchedulingOptions.EnableProcessor = true` (default). Disable with:

```csharp
config.SchedulingOptions.EnableProcessor = false;
```

### Processing Flow

1. Create a DI scope per cycle
2. Resolve `SchedulerOrchestrator` and `IScheduledMessageDispatcher` from the scope
3. Call `ProcessDueMessagesAsync` — retrieves due messages, deserializes each, invokes the dispatcher
4. Dispatcher determines if the request is `IRequest<TResponse>` (→ `IEncina.Send`) or `INotification` (→ `IEncina.Publish`)
5. On success: mark processed (one-time) or reschedule (recurring via `ICronParser`)
6. On failure: delegate to `IScheduledMessageRetryPolicy` for next-retry-time or dead-letter

### Retry Strategy

The default `ExponentialBackoffRetryPolicy` uses the formula:

```text
delay = BaseRetryDelay * 2^retryCount
```

With `BaseRetryDelay = 5s` and `MaxRetries = 3`:

| Attempt | Delay | Total Wait |
|---------|-------|------------|
| 1st failure | 5s | 5s |
| 2nd failure | 10s | 15s |
| 3rd failure | Dead-lettered | — |

**Custom retry policy**: register your own `IScheduledMessageRetryPolicy` before `AddEncina*()`:

```csharp
services.AddSingleton<IScheduledMessageRetryPolicy, MyCustomRetryPolicy>();
services.AddEncinaEntityFrameworkCore<AppDbContext>(config => ...);
```

### Custom Dispatcher

The default `CompiledExpressionScheduledMessageDispatcher` builds compiled delegates via `System.Linq.Expressions` (zero reflection on hot path). Override with:

```csharp
services.AddScoped<IScheduledMessageDispatcher, MyCustomDispatcher>();
```

## Configuration

| Property | Default | Description |
|----------|---------|-------------|
| `ProcessingInterval` | 30s | How often the processor polls for due messages |
| `BatchSize` | 100 | Max messages per cycle |
| `MaxRetries` | 3 | Max attempts before dead-lettering |
| `BaseRetryDelay` | 5s | Base delay for exponential backoff |
| `EnableProcessor` | true | Enable/disable the background processor |
| `EnableRecurringMessages` | true | Enable/disable cron-based recurring messages |

## Observability

### Structured Logging

EventIds 2320-2325 (`SchedulingProcessorLog`):

| EventId | Level | Message |
|---------|-------|---------|
| 2320 | Information | Processor starting (interval, batchSize, maxRetries) |
| 2321 | Information | Processor stopping |
| 2322 | Information | Processor disabled |
| 2323 | Debug | Batch completed (processedCount) |
| 2324 | Warning | Batch failed (errorCode, errorMessage) |
| 2325 | Error | Cycle threw unhandled exception |

### OpenTelemetry

**ActivitySource**: `Encina.Messaging.Scheduling`

- Activity `encina.scheduling.processor_cycle` per polling iteration
- Tags: `scheduling.batch_size`, `scheduling.processed_count`

**Meter**: `Encina` (shared)

- `encina.scheduling.processor.messages_total` (counter, tag: outcome=success|failure)
- `encina.scheduling.processor.cycle_duration_seconds` (histogram)

## Deferred Integrations

| Integration | Status | Issue |
|-------------|--------|-------|
| Distributed locks | Deferred | #716 |
| Idempotency | Deferred | #735 |
| Multi-tenancy | Deferred | #739 |
| Audit trail | Deferred | #749 |
| Health check | Deferred | TBD |

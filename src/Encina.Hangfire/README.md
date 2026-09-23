# Encina.Hangfire

[![NuGet](https://img.shields.io/nuget/v/Encina.Hangfire.svg)](https://www.nuget.org/packages/Encina.Hangfire)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**Hangfire integration for Encina** - Execute requests and notifications as reliable, persistent background jobs with automatic retries, scheduling, and monitoring.

Encina.Hangfire provides seamless integration between Encina's CQRS patterns and Hangfire's powerful background job processing, enabling fire-and-forget execution, delayed processing, and recurring jobs.

## Features

- **✅ Fire-and-Forget Jobs**: Enqueue requests and notifications for immediate background execution
- **✅ Delayed Execution**: Schedule jobs to run at specific times or after delays
- **✅ Recurring Jobs**: Set up CRON-based recurring request/notification execution
- **✅ Automatic Retries**: Leverage Hangfire's built-in retry mechanism for transient failures; permanent failures can skip retries (see [Failure Semantics and Retries](#failure-semantics-and-retries))
- **✅ Request Context Preservation**: Maintain CorrelationId, UserId, and TenantId across job boundaries
- **✅ Railway Oriented Programming**: Full support for `Either<EncinaError, T>` results
- **✅ Dashboard Monitoring**: View job status, history, and failures in Hangfire Dashboard
- **✅ Type-Safe API**: Strongly-typed extension methods for all job operations
- **✅ .NET 10 Native**: Built for modern .NET with nullable reference types

## Installation

```bash
dotnet add package Encina.Hangfire
dotnet add package Hangfire.AspNetCore  # or Hangfire.Core for non-web applications
```

## Quick Start

### 1. Configure Hangfire and Encina

```csharp
using Hangfire;
using Hangfire.SqlServer;
using Encina.Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Configure Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

// Add Hangfire server
builder.Services.AddHangfireServer();

// Register Encina with handlers
builder.Services.AddEncina(options =>
{
    options.RegisterServicesFromAssemblyContaining<Program>();
});

// Register Hangfire adapters
builder.Services.AddEncinaHangfire();

var app = builder.Build();

// Enable Hangfire Dashboard (optional, for monitoring)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new MyAuthorizationFilter() }
});

app.Run();
```

### 2. Fire-and-Forget Jobs

Execute requests or notifications in the background immediately:

```csharp
using Hangfire;
using Encina.Hangfire;

public class OrderController : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobs;

    public OrderController(IBackgroundJobClient backgroundJobs)
    {
        _backgroundJobs = backgroundJobs;
    }

    [HttpPost("orders")]
    public IActionResult CreateOrder([FromBody] CreateOrderCommand command)
    {
        // Enqueue command for background execution
        var jobId = _backgroundJobs.EnqueueRequest<CreateOrderCommand, Order>(
            command,
            context: RequestContext.Create());

        return Accepted(new { JobId = jobId });
    }
}
```

**With notifications:**

```csharp
public record OrderCreatedEvent(Guid OrderId, decimal Total) : INotification;

// Enqueue notification for background processing
var jobId = _backgroundJobs.EnqueueNotification(
    new OrderCreatedEvent(order.Id, order.Total),
    context: RequestContext.Create());
```

### 3. Delayed Jobs

Schedule jobs to execute after a delay or at a specific time:

```csharp
// Execute after 1 hour
var jobId = _backgroundJobs.ScheduleRequest<SendInvoiceCommand, Invoice>(
    new SendInvoiceCommand(orderId),
    delay: TimeSpan.FromHours(1));

// Execute at specific time
var jobId = _backgroundJobs.ScheduleRequest<ProcessPaymentCommand, Receipt>(
    new ProcessPaymentCommand(paymentId),
    enqueueAt: DateTimeOffset.UtcNow.AddDays(7));

// With notifications
var jobId = _backgroundJobs.ScheduleNotification(
    new PaymentReminderNotification(customerId),
    delay: TimeSpan.FromDays(3));
```

### 4. Recurring Jobs

Set up CRON-based recurring jobs:

```csharp
using Hangfire;
using Encina.Hangfire;

public class RecurringJobsSetup
{
    public static void Configure(IRecurringJobManager recurringJobs)
    {
        // Daily report generation at 2 AM
        recurringJobs.AddOrUpdateRecurringRequest<GenerateDailyReportCommand, Report>(
            recurringJobId: "daily-report",
            request: new GenerateDailyReportCommand(),
            cronExpression: Cron.Daily(2),  // 2 AM every day
            context: RequestContext.Create());

        // Hourly data cleanup
        recurringJobs.AddOrUpdateRecurringNotification(
            recurringJobId: "cleanup-expired-data",
            notification: new CleanupExpiredDataNotification(),
            cronExpression: Cron.Hourly(),
            context: RequestContext.Create());

        // Custom CRON expression (every Monday at 9 AM)
        recurringJobs.AddOrUpdateRecurringRequest<WeeklyEmailCommand, Unit>(
            recurringJobId: "weekly-email",
            request: new WeeklyEmailCommand(),
            cronExpression: "0 9 * * 1");  // CRON: minute hour day month weekday
    }
}

// In Startup/Program.cs
var app = builder.Build();
RecurringJobsSetup.Configure(app.Services.GetRequiredService<IRecurringJobManager>());
```

## Advanced Usage

### Request Context Preservation

Hangfire jobs automatically preserve request context (CorrelationId, UserId, TenantId):

```csharp
[HttpPost("process")]
public IActionResult ProcessData([FromBody] ProcessDataCommand command)
{
    // Create context with current user and tenant
    var context = RequestContext.Create() with
    {
        UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
        TenantId = User.FindFirstValue("tenant_id")
    };

    // Context is serialized and restored in background job
    var jobId = _backgroundJobs.EnqueueRequest<ProcessDataCommand, Result>(
        command,
        context);

    return Accepted(new { JobId = jobId, CorrelationId = context.CorrelationId });
}
```

### Railway Oriented Programming with Jobs

Jobs fully support ROP patterns:

```csharp
public record ProcessPaymentCommand(Guid PaymentId, decimal Amount)
    : IRequest<Receipt>;

public class ProcessPaymentHandler : ICommandHandler<ProcessPaymentCommand, Receipt>
{
    public async ValueTask<Either<EncinaError, Receipt>> Handle(
        ProcessPaymentCommand request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        // Validate payment
        if (request.Amount <= 0)
            return EncinaErrors.ValidationFailed("Amount must be positive");

        // Process payment logic
        var receipt = await _paymentService.ProcessAsync(request.PaymentId, request.Amount);

        return receipt;  // Implicit conversion to Right<EncinaError, Receipt>
    }
}

// Enqueue - handler returns Either<EncinaError, Receipt>
var jobId = _backgroundJobs.EnqueueRequest<ProcessPaymentCommand, Receipt>(
    new ProcessPaymentCommand(paymentId, 100m));

// EnqueueRequest calls HangfireRequestJobAdapter.ExecuteAsync, which returns plain Task:
// Hangfire's job storage never receives the Receipt. Only opt into persisting it with
// EnqueueRequestWithResult (HangfireRequestJobAdapter.ExecuteAndReturnResultAsync) when
// Receipt is known not to carry personal or health data (#1173).
//
// In Hangfire Dashboard:
// - Success: nothing is stored by default; with EnqueueRequestWithResult, the Receipt is
//   the job result
// - ValidationFailed: a permanent failure - the job throws EncinaJobPermanentFailureException
//   and, with EncinaAutomaticRetry, goes straight to Failed without retries
// - A timeout or an unavailable dependency: a transient failure - the job throws
//   EncinaJobFailedException and Hangfire retries it
```

### Failure Semantics and Retries

Hangfire marks a job as failed, and applies its retry policy, only when the job method throws. The adapters therefore turn every `Left` result into an exception, choosing the type from the failure:

| Handler outcome | Exception thrown | Hangfire default | With `UseEncinaAutomaticRetry` |
|-----------------|------------------|------------------|--------------------------------|
| `Right` | none (`ExecuteAsync` discards the response; use `ExecuteAndReturnResultAsync` / `EnqueueRequestWithResult` to persist it) | Succeeded | Succeeded |
| Cancellation (any Encina `*.cancelled` code, e.g. `encina.request.cancelled` or `encina.handler.cancelled`) while the job's token is cancelled, e.g. server shutdown | `OperationCanceledException` | Interrupted, not failed (after a server shutdown the job is processed again) | Same |
| Permanent failure (validation, not found, missing handler, missing or withdrawn consent, active processing restriction, authorization denied, ...) | `EncinaJobPermanentFailureException` | Retried (Hangfire retries every exception) | **Failed, no retries** |
| Transient or unclassified failure (timeout, unavailable dependency, rate limit, ...) | `EncinaJobFailedException` | Retried | Retried |

Permanent and transient are decided by `Encina.Messaging.Recoverability.IErrorClassifier`: the registered implementation, or `DefaultErrorClassifier` when none is registered. The default classifier looks at the error's exception, then its code, then its message; register your own `IErrorClassifier` to classify your domain error codes.

Only `EnqueueRequest` has an opt-in for keeping the response (`EnqueueRequestWithResult`, which enqueues `ExecuteAndReturnResultAsync`). `ScheduleRequestWithDelay`, `ScheduleRequestAt` and `AddOrUpdateRecurringRequest` always enqueue `ExecuteAsync`, so the response of a delayed, scheduled or recurring job is never stored in Hangfire.

To stop Hangfire from retrying permanent failures, replace its global retry filter once at startup:

```csharp
using Hangfire;
using Encina.Hangfire;

// Retries transient failures up to 5 times; permanent failures go straight to Failed.
GlobalJobFilters.Filters.UseEncinaAutomaticRetry(attempts: 5);
```

`UseEncinaAutomaticRetry` removes every `AutomaticRetryAttribute` from the collection and adds `EncinaAutomaticRetry.Create(attempts)`, an `AutomaticRetryAttribute` whose `ExceptOn` contains `EncinaJobPermanentFailureException` (Hangfire's attribute is sealed, so Encina configures it instead of subclassing it). You can also build the filter yourself, for example to retry only transient Encina failures:

```csharp
GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
{
    Attempts = 3,
    OnlyOn = [typeof(EncinaJobFailedException)]
});
```

Hangfire stores the type, message and stack trace of every failed attempt. The exception `Message` of both Encina exceptions therefore contains only the error code and a generic text, never `EncinaError.Message`, which may carry personal data such as a data-subject id. The error code is also available as `ErrorCode` and in `Exception.Data["Encina.ErrorCode"]`, and the exception that caused the error, if any, is the `InnerException`. The `RequestJobFailed`/`NotificationJobFailed` log lines carry only the error code too (#1173) — `EncinaError.Message` is not written anywhere by the adapter.

### Continuation Jobs

Chain jobs to execute sequentially:

```csharp
// Create parent job
var parentJobId = _backgroundJobs.EnqueueRequest<CreateOrderCommand, Order>(
    new CreateOrderCommand(customerId, items));

// Create continuation job that runs after parent succeeds
var childJobId = BackgroundJob.ContinueJobWith<HangfireNotificationJobAdapter<OrderCreatedEvent>>(
    parentJobId,
    adapter => adapter.PublishAsync(
        new OrderCreatedEvent(orderId),
        default));
```

### Batch Jobs

Execute multiple jobs in parallel:

```csharp
using Hangfire;

var batchId = BatchJob.StartNew(batch =>
{
    foreach (var orderId in orderIds)
    {
        batch.Enqueue<HangfireRequestJobAdapter<ProcessOrderCommand, Result>>(
            adapter => adapter.ExecuteAsync(
                new ProcessOrderCommand(orderId),
                default));
    }
});
```

## Configuration Options

### Hangfire Storage Options

```csharp
builder.Services.AddHangfire(config => config
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        // Job retry settings
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),

        // Performance tuning
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,

        // Job expiration
        JobExpirationCheckInterval = TimeSpan.FromHours(1),
        DeleteExpiredBatchesInBackground = true
    }));
```

### Hangfire Server Options

```csharp
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 5;  // Concurrent jobs
    options.Queues = new[] { "critical", "default", "low" };  // Queue priority
    options.ServerName = $"{Environment.MachineName}_{Guid.NewGuid():N}";
});
```

### Custom Job Filters

```csharp
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Server;

public class LogJobFilter : JobFilterAttribute, IServerFilter
{
    public void OnPerforming(PerformingContext context)
    {
        var logger = context.GetJobParameter<ILogger>("logger");
        logger?.LogInformation("Job {JobId} starting", context.BackgroundJob.Id);
    }

    public void OnPerformed(PerformedContext context)
    {
        var logger = context.GetJobParameter<ILogger>("logger");
        if (context.Exception != null)
            logger?.LogError(context.Exception, "Job {JobId} failed", context.BackgroundJob.Id);
        else
            logger?.LogInformation("Job {JobId} completed", context.BackgroundJob.Id);
    }
}

// Register globally
GlobalJobFilters.Filters.Add(new LogJobFilter());
```

## Monitoring and Diagnostics

### Hangfire Dashboard

Access the dashboard to monitor jobs:

```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new MyAuthorizationFilter() },
    StatsPollingInterval = 2000,  // Refresh every 2 seconds
    DisplayStorageConnectionString = false
});
```

Navigate to `/hangfire` to view:

- **Jobs**: Enqueued, scheduled, processing, succeeded, failed
- **Retries**: Failed jobs pending retry
- **Recurring**: All recurring jobs and their schedules
- **Servers**: Active Hangfire servers processing jobs

### Logging

All job adapters include structured logging:

```csharp
// Logs automatically include:
// - RequestType/NotificationType
// - CorrelationId (from RequestContext)
// - Job execution status
// - Error details (on failure)

// Example log output:
// [INF] Executing Hangfire job for request ProcessOrderCommand (CorrelationId: a1b2c3d4)
// [INF] Hangfire job completed successfully for request ProcessOrderCommand
```

## Comparison with Other Approaches

| Approach | Use Case | Pros | Cons |
|----------|----------|------|------|
| **Hangfire + Encina** | Background jobs with monitoring | Dashboard, persistence, retries, scheduling | Requires database storage |
| **Quartz.NET + Encina** | Complex scheduling needs | Powerful CRON, clustering | More complex setup |
| **Direct Encina Call** | Synchronous processing | Simple, immediate feedback | Blocks HTTP request thread |
| **IHostedService + Channel** | In-process async work | No external dependencies | No persistence, loses jobs on restart |

## Best Practices

### 1. Idempotency

Make your handlers idempotent since Hangfire retries failed jobs:

```csharp
public class ProcessPaymentHandler : ICommandHandler<ProcessPaymentCommand, Receipt>
{
    public async ValueTask<Either<EncinaError, Receipt>> Handle(...)
    {
        // Check if already processed
        var existing = await _repository.GetReceiptAsync(request.PaymentId);
        if (existing != null)
            return existing;  // Already processed, return existing result

        // Process payment
        var receipt = await _paymentService.ProcessAsync(request.PaymentId);
        await _repository.SaveReceiptAsync(receipt);

        return receipt;
    }
}
```

### 2. Small Job Payloads

Keep request objects small - Hangfire serializes them to storage:

```csharp
// Good: Small payload
public record ProcessOrderCommand(Guid OrderId) : IRequest<Result>;

// Bad: Large payload
public record ProcessOrderCommand(Guid OrderId, List<OrderItem> Items, Customer Customer)
    : IRequest<Result>;
```

### 3. Queue Organization

Use multiple queues for priority management:

```csharp
// Critical jobs (payments, orders)
BackgroundJob.Enqueue<HangfireRequestJobAdapter<ProcessPaymentCommand, Receipt>>(
    adapter => adapter.ExecuteAsync(command, default),
    queue: "critical");

// Low-priority jobs (reports, cleanup)
BackgroundJob.Enqueue<HangfireRequestJobAdapter<GenerateReportCommand, Report>>(
    adapter => adapter.ExecuteAsync(command, default),
    queue: "low");

// Configure server to prioritize critical queue
builder.Services.AddHangfireServer(options =>
{
    options.Queues = new[] { "critical", "default", "low" };
});
```

### 4. Error Handling

Let exceptions bubble up for automatic Hangfire retry:

```csharp
public class SendEmailHandler : ICommandHandler<SendEmailCommand, Unit>
{
    public async ValueTask<Either<EncinaError, Unit>> Handle(...)
    {
        // Don't catch transient errors - let Hangfire retry
        await _emailService.SendAsync(request.To, request.Subject, request.Body);

        return Unit.Default;
    }
}
```

## Troubleshooting

### Jobs Not Processing

**Issue**: Jobs remain in "Enqueued" state

**Solution**:

1. Ensure Hangfire Server is running: `builder.Services.AddHangfireServer()`
2. Check database connection string
3. Verify queue names match server configuration
4. Check server logs for exceptions

### Serialization Errors

**Issue**: `Could not load type 'MyRequest' from assembly`

**Solution**:

1. Ensure all assemblies containing requests/notifications are loaded
2. Use `UseRecommendedSerializerSettings()` in Hangfire configuration
3. Avoid generic types with complex type arguments

### Job Fails Immediately

**Issue**: Job fails with `InvalidOperationException: No service for type 'IEncina'`

**Solution**:

1. Ensure `AddEncinaHangfire()` is called after `AddEncina()`
2. Register all handler assemblies: `options.RegisterServicesFromAssemblyContaining<Program>()`
3. Verify DI container scope - adapters are transient

## Integration Examples

### ASP.NET Core Minimal API

```csharp
app.MapPost("/orders", async (CreateOrderCommand command, IBackgroundJobClient jobs) =>
{
    var context = RequestContext.Create();
    var jobId = jobs.EnqueueRequest<CreateOrderCommand, Order>(command, context);
    return Results.Accepted($"/jobs/{jobId}", new { JobId = jobId });
});
```

### With MediatR Migration

Already using MediatR? Encina.Hangfire works the same way:

```csharp
// MediatR
var jobId = BackgroundJob.Enqueue<IEncina>(m => m.Send(command, default));

// Encina.Hangfire
var jobId = _backgroundJobs.EnqueueRequest<CreateOrderCommand, Order>(command);
```

## Health Checks

Hangfire health checks are automatically registered when you configure the provider.

### Automatic Registration

```csharp
builder.Services.AddEncinaHangfire(config =>
{
    config.ProviderHealthCheck.Enabled = true; // Default
});

// Health check "encina-hangfire" is automatically registered
```

### Configuration Options

```csharp
builder.Services.AddEncinaHangfire(config =>
{
    config.ProviderHealthCheck.Enabled = true;
    config.ProviderHealthCheck.Name = "my-scheduler";
    config.ProviderHealthCheck.Timeout = TimeSpan.FromSeconds(5);
    config.ProviderHealthCheck.Tags = ["critical", "scheduling"];
});
```

### Health Check Details

| Property | Value |
|----------|-------|
| **Default Name** | `encina-hangfire` |
| **Default Tags** | `encina`, `scheduling`, `hangfire`, `ready` |
| **Check Type** | Verifies JobStorage is configured and accessible |
| **Data Returned** | `servers`, `queues`, `scheduled`, `enqueued` counts |

### ASP.NET Core Integration

```csharp
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/scheduling", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("scheduling")
});
```

## Roadmap

- ✅ Fire-and-forget jobs
- ✅ Delayed execution
- ✅ Recurring jobs
- ✅ Request context preservation
- ✅ Health checks
- ⏳ Batch job helpers
- ⏳ Job continuation builders
- ✅ Permanent vs transient failures (`EncinaAutomaticRetry`)

## Contributing

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](../../LICENSE) for details.

---

**Next Steps**: Check out [Encina.Quartz](../Encina.Quartz/README.md) for more advanced scheduling scenarios with clustering and complex CRON expressions.

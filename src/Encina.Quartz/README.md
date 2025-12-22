# Encina.Quartz

[![NuGet](https://img.shields.io/nuget/v/Encina.Quartz.svg)](https://www.nuget.org/packages/Encina.Quartz)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**Quartz.NET integration for Encina** - Execute requests and notifications as scheduled jobs with powerful CRON expressions, clustering support, and enterprise-grade job management.

Encina.Quartz provides seamless integration between Encina's CQRS patterns and Quartz.NET's robust scheduling engine, enabling complex scheduling scenarios, persistent job storage, and distributed job execution.

## Features

- **✅ Powerful Scheduling**: Full CRON expression support for complex schedules
- **✅ Clustering**: Distributed job execution across multiple servers
- **✅ Persistent Jobs**: Store jobs in SQL Server, PostgreSQL, MongoDB, or in-memory
- **✅ Misfire Handling**: Configurable behavior for missed job executions
- **✅ Job Chaining**: Execute jobs sequentially with dependencies
- **✅ Concurrent Execution Control**: Prevent or allow concurrent job runs
- **✅ Railway Oriented Programming**: Full support for `Either<MediatorError, T>` results
- **✅ Job Monitoring**: Built-in listeners and health checks
- **✅ .NET 10 Native**: Built for modern .NET with nullable reference types

## Installation

```bash
dotnet add package Encina.Quartz
dotnet add package Quartz.Extensions.Hosting
```

## Quick Start

### 1. Basic Configuration

```csharp
using Quartz;
using Encina.Quartz;

var builder = WebApplication.CreateBuilder(args);

// Register Encina with handlers
builder.Services.AddEncina(options =>
{
    options.RegisterServicesFromAssemblyContaining<Program>();
});

// Configure Quartz with Encina
builder.Services.AddEncinaQuartz(quartz =>
{
    // Use in-memory job store (for development)
    quartz.UseInMemoryStore();

    // Or use persistent storage (for production)
    quartz.UsePersistentStore(store =>
    {
        store.UseSqlServer(builder.Configuration.GetConnectionString("QuartzConnection"));
        store.UseJsonSerializer();
    });

    // Enable clustering (optional, for distributed scenarios)
    quartz.UseDefaultThreadPool(tp =>
    {
        tp.MaxConcurrency = 10;
    });
});

var app = builder.Build();
app.Run();
```

### 2. Simple Scheduled Job

Schedule a job using Quartz triggers:

```csharp
using Quartz;
using Encina.Quartz;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEncinaQuartz(quartz =>
        {
            // Schedule daily report at 2 AM
            quartz.AddRequestJob<GenerateDailyReportCommand, Report>(
                request: new GenerateDailyReportCommand(),
                configureTrigger: trigger => trigger
                    .WithIdentity("daily-report-trigger")
                    .WithCronSchedule("0 0 2 * * ?")  // 2 AM daily
                    .StartNow());
        });
    }
}
```

### 3. CRON Scheduling Examples

Quartz uses powerful CRON expressions:

```csharp
services.AddEncinaQuartz(quartz =>
{
    // Every 15 minutes
    quartz.AddNotificationJob<DataSyncNotification>(
        notification: new DataSyncNotification(),
        configureTrigger: trigger => trigger
            .WithCronSchedule("0 0/15 * * * ?"));

    // Every Monday at 9 AM
    quartz.AddRequestJob<WeeklyEmailCommand, Unit>(
        request: new WeeklyEmailCommand(),
        configureTrigger: trigger => trigger
            .WithCronSchedule("0 0 9 ? * MON"));

    // Last day of every month at 11:59 PM
    quartz.AddRequestJob<MonthlyReportCommand, Report>(
        request: new MonthlyReportCommand(),
        configureTrigger: trigger => trigger
            .WithCronSchedule("0 59 23 L * ?"));

    // Every weekday at 8 AM
    quartz.AddNotificationJob<MorningNotification>(
        notification: new MorningNotification(),
        configureTrigger: trigger => trigger
            .WithCronSchedule("0 0 8 ? * MON-FRI"));
});
```

**CRON Format**: `[second] [minute] [hour] [day-of-month] [month] [day-of-week] [year (optional)]`

### 4. Dynamic Job Scheduling

Schedule jobs at runtime using IScheduler:

```csharp
public class OrderController : ControllerBase
{
    private readonly IScheduler _scheduler;

    public OrderController(IScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    [HttpPost("orders/schedule-followup")]
    public async Task<IActionResult> ScheduleFollowUp([FromBody] ScheduleFollowUpRequest req)
    {
        // Create trigger for 7 days from now
        var trigger = TriggerBuilder.Create()
            .WithIdentity($"followup-{req.OrderId}")
            .StartAt(DateTimeOffset.UtcNow.AddDays(7))
            .Build();

        // Schedule the job
        var triggerKey = await _scheduler.ScheduleRequest<SendFollowUpCommand, Unit>(
            new SendFollowUpCommand(req.OrderId),
            trigger);

        return Ok(new { TriggerKey = triggerKey.ToString() });
    }
}
```

## Advanced Usage

### Clustering Configuration

Enable clustering for distributed job execution across multiple servers:

```csharp
builder.Services.AddEncinaQuartz(quartz =>
{
    quartz.UsePersistentStore(store =>
    {
        store.UseSqlServer(connectionString);
        store.UseJsonSerializer();
        store.UseClustering(cluster =>
        {
            cluster.CheckinInterval = TimeSpan.FromSeconds(20);
            cluster.CheckinMisfireThreshold = TimeSpan.FromSeconds(30);
        });
    });

    quartz.UseDefaultThreadPool(tp =>
    {
        tp.MaxConcurrency = 10;
    });
});
```

**How clustering works:**
- Jobs stored in shared database (SQL Server, PostgreSQL, etc.)
- Multiple servers run Quartz scheduler
- Only one server executes each job instance
- Automatic failover if server goes down

### Misfire Handling

Control what happens when a job misses its scheduled time:

```csharp
services.AddEncinaQuartz(quartz =>
{
    quartz.AddRequestJob<ProcessPaymentCommand, Receipt>(
        request: new ProcessPaymentCommand(paymentId),
        configureTrigger: trigger => trigger
            .WithCronSchedule("0 0 * * * ?")
            .WithMisfireHandling(MisfireInstruction.CronTrigger.FireOnceNow));  // Run immediately if missed
});
```

**Misfire policies:**
- `FireOnceNow`: Execute immediately, then continue normal schedule
- `DoNothing`: Skip missed execution, wait for next scheduled time
- `IgnoreMisfires`: Execute all missed runs

### Job Data and State

Pass complex data to jobs:

```csharp
public record ProcessBatchCommand(List<int> ItemIds, string BatchId) : IRequest<BatchResult>;

// Schedule with custom data
var trigger = TriggerBuilder.Create()
    .WithIdentity("batch-processor")
    .StartNow()
    .Build();

await _scheduler.ScheduleRequest<ProcessBatchCommand, BatchResult>(
    new ProcessBatchCommand(
        ItemIds: new List<int> { 1, 2, 3, 4, 5 },
        BatchId: Guid.NewGuid().ToString()),
    trigger);
```

### Preventing Concurrent Execution

Jobs are marked with `[DisallowConcurrentExecution]` by default:

```csharp
// QuartzRequestJob<TRequest, TResponse> automatically prevents concurrent runs
// If job is still running when next trigger fires, execution is skipped

services.AddEncinaQuartz(quartz =>
{
    // This job will never run concurrently
    quartz.AddRequestJob<LongRunningCommand, Result>(
        request: new LongRunningCommand(),
        configureTrigger: trigger => trigger
            .WithSimpleSchedule(schedule => schedule
                .WithIntervalInMinutes(5)
                .RepeatForever()));
});
```

### Job Monitoring and Listeners

Monitor job execution with custom listeners:

```csharp
using Quartz;

public class JobExecutionListener : IJobListener
{
    private readonly ILogger<JobExecutionListener> _logger;

    public JobExecutionListener(ILogger<JobExecutionListener> logger)
    {
        _logger = logger;
    }

    public string Name => "JobExecutionListener";

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job {JobKey} starting", context.JobDetail.Key);
        return Task.CompletedTask;
    }

    public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? exception, CancellationToken cancellationToken)
    {
        if (exception != null)
            _logger.LogError(exception, "Job {JobKey} failed", context.JobDetail.Key);
        else
            _logger.LogInformation("Job {JobKey} completed", context.JobDetail.Key);

        return Task.CompletedTask;
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Job {JobKey} vetoed", context.JobDetail.Key);
        return Task.CompletedTask;
    }
}

// Register listener
services.AddEncinaQuartz(quartz =>
{
    quartz.AddJobListener<JobExecutionListener>();
});
```

### Pausing and Resuming Jobs

Control job execution dynamically:

```csharp
public class JobManagementService
{
    private readonly IScheduler _scheduler;

    public JobManagementService(IScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public async Task PauseJob(string jobName)
    {
        var jobKey = new JobKey(jobName);
        await _scheduler.PauseJob(jobKey);
    }

    public async Task ResumeJob(string jobName)
    {
        var jobKey = new JobKey(jobName);
        await _scheduler.ResumeJob(jobKey);
    }

    public async Task DeleteJob(string jobName)
    {
        var jobKey = new JobKey(jobName);
        await _scheduler.DeleteJob(jobKey);
    }

    public async Task<bool> IsJobRunning(string jobName)
    {
        var jobKey = new JobKey(jobName);
        var executingJobs = await _scheduler.GetCurrentlyExecutingJobs();
        return executingJobs.Any(job => job.JobDetail.Key.Equals(jobKey));
    }
}
```

## Configuration Reference

### Quartz Storage Options

#### In-Memory (Development)

```csharp
services.AddEncinaQuartz(quartz =>
{
    quartz.UseInMemoryStore();
});
```

**Pros**: Fast, simple
**Cons**: Jobs lost on restart, no clustering

#### SQL Server (Production)

```csharp
services.AddEncinaQuartz(quartz =>
{
    quartz.UsePersistentStore(store =>
    {
        store.UseSqlServer(connectionString);
        store.UseJsonSerializer();
        store.UseClustering();
    });
});
```

**Pros**: Persistent, clustering support
**Cons**: Requires database setup

#### PostgreSQL (Production)

```csharp
services.AddEncinaQuartz(quartz =>
{
    quartz.UsePersistentStore(store =>
    {
        store.UsePostgres(connectionString);
        store.UseJsonSerializer();
        store.UseClustering();
    });
});
```

### Thread Pool Configuration

```csharp
services.AddEncinaQuartz(quartz =>
{
    quartz.UseDefaultThreadPool(tp =>
    {
        tp.MaxConcurrency = 20;  // Max parallel jobs
    });
});
```

### Job Execution Timeout

```csharp
services.AddEncinaQuartz(quartz =>
{
    quartz.AddRequestJob<LongRunningCommand, Result>(
        request: new LongRunningCommand(),
        configureTrigger: trigger => trigger
            .WithSimpleSchedule(schedule => schedule
                .WithIntervalInHours(1)
                .RepeatForever())
            .StartNow());

    // Configure job timeout via Quartz properties
    quartz.SchedulerId = "MyScheduler";
    quartz.SchedulerName = "MyScheduler";
    quartz.MaxBatchSize = 20;
    quartz.BatchTriggerAcquisitionFireAheadTimeWindow = TimeSpan.FromSeconds(0);
});
```

## Comparison with Hangfire

| Feature | **Quartz.NET** | **Hangfire** |
|---------|----------------|--------------|
| **Scheduling** | Advanced CRON, complex triggers | Simple CRON, basic scheduling |
| **Clustering** | ✅ Built-in, robust | ✅ Supported |
| **Storage** | SQL Server, PostgreSQL, MongoDB, RAM | SQL Server, Redis, RAM |
| **Dashboard** | ❌ No built-in UI | ✅ Web dashboard |
| **Job Chaining** | ✅ Native support | ✅ Continuation jobs |
| **Misfire Handling** | ✅ Fine-grained control | ⚠️ Basic |
| **Learning Curve** | Steeper | Easier |
| **Best For** | Complex scheduling, enterprise | Simple background jobs, quick setup |

**Use Quartz when:**
- You need complex CRON schedules (e.g., "last Friday of each quarter")
- Clustering across multiple servers is critical
- You need fine-grained control over misfires
- You already use Quartz.NET in your organization

**Use Hangfire when:**
- You want a simple dashboard UI
- Scheduling needs are straightforward
- Faster initial setup is preferred

## CRON Expression Reference

```
Format: [second] [minute] [hour] [day-of-month] [month] [day-of-week]

Examples:
0 0 12 * * ?         - Fire at 12pm (noon) every day
0 15 10 ? * *        - Fire at 10:15am every day
0 15 10 * * ?        - Fire at 10:15am every day
0 15 10 * * ? *      - Fire at 10:15am every day
0 15 10 * * ? 2025   - Fire at 10:15am every day during the year 2025
0 * 14 * * ?         - Fire every minute starting at 2pm and ending at 2:59pm, every day
0 0/5 14 * * ?       - Fire every 5 minutes starting at 2pm and ending at 2:55pm, every day
0 0/5 14,18 * * ?    - Fire every 5 minutes starting at 2pm and ending at 2:55pm, AND fire every 5 minutes starting at 6pm and ending at 6:55pm, every day
0 0-5 14 * * ?       - Fire every minute starting at 2pm and ending at 2:05pm, every day
0 10,44 14 ? 3 WED   - Fire at 2:10pm and at 2:44pm every Wednesday in the month of March
0 15 10 ? * MON-FRI  - Fire at 10:15am every Monday, Tuesday, Wednesday, Thursday and Friday
0 15 10 15 * ?       - Fire at 10:15am on the 15th day of every month
0 15 10 L * ?        - Fire at 10:15am on the last day of every month
0 15 10 ? * 6L       - Fire at 10:15am on the last Friday of every month
0 15 10 ? * 6#3      - Fire at 10:15am on the third Friday of every month
```

**Special characters:**
- `*` - All values
- `?` - No specific value (for day-of-month or day-of-week)
- `-` - Range (e.g., `MON-FRI`)
- `,` - List (e.g., `MON,WED,FRI`)
- `/` - Increment (e.g., `0/15` = every 15)
- `L` - Last (e.g., `L` = last day of month, `6L` = last Friday)
- `#` - Nth occurrence (e.g., `6#3` = third Friday)

## Database Schema Setup

For persistent storage, run Quartz schema scripts:

```sql
-- SQL Server
-- Download from: https://github.com/quartznet/quartznet/blob/main/database/tables/tables_sqlServer.sql

-- PostgreSQL
-- Download from: https://github.com/quartznet/quartznet/blob/main/database/tables/tables_postgres.sql
```

Or use migrations:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add QuartzTables
dotnet ef database update
```

## Troubleshooting

### Jobs Not Executing

**Issue**: Jobs scheduled but never execute

**Solution**:
1. Verify `AddQuartzHostedService()` is called
2. Check thread pool configuration: `MaxConcurrency > 0`
3. Ensure database connection string is correct (for persistent stores)
4. Verify CRON expression: Use https://www.freeformatter.com/cron-expression-generator-quartz.html

### Serialization Errors

**Issue**: `SerializationException` when using persistent store

**Solution**:
1. Use `UseJsonSerializer()` in persistent store configuration
2. Ensure request/notification types are serializable
3. Avoid complex nested generic types

### Clustering Issues

**Issue**: Jobs execute on multiple servers simultaneously

**Solution**:
1. Verify all servers use same database
2. Check `UseClustering()` is configured
3. Ensure clocks are synchronized (NTP)
4. Review `CheckinInterval` and `CheckinMisfireThreshold` settings

## Best Practices

### 1. Idempotency

Make handlers idempotent since Quartz may retry failed jobs:

```csharp
public class ProcessPaymentHandler : ICommandHandler<ProcessPaymentCommand, Receipt>
{
    public async ValueTask<Either<MediatorError, Receipt>> Handle(...)
    {
        // Check if already processed
        var existing = await _repository.GetReceiptAsync(request.PaymentId);
        if (existing != null)
            return existing;

        // Process payment
        var receipt = await _paymentService.ProcessAsync(request.PaymentId);
        return receipt;
    }
}
```

### 2. Use Job Keys for Management

```csharp
var jobKey = new JobKey($"payment-{paymentId}", "payments");

await _scheduler.ScheduleRequest<ProcessPaymentCommand, Receipt>(
    new ProcessPaymentCommand(paymentId),
    trigger,
    jobKey: jobKey);

// Later, cancel if needed
await _scheduler.DeleteJob(jobKey);
```

### 3. Monitor Job Health

```csharp
public class QuartzHealthCheck : IHealthCheck
{
    private readonly IScheduler _scheduler;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        if (!_scheduler.IsStarted)
            return HealthCheckResult.Unhealthy("Quartz scheduler not started");

        var runningJobs = await _scheduler.GetCurrentlyExecutingJobs(cancellationToken);
        return HealthCheckResult.Healthy($"{runningJobs.Count} jobs running");
    }
}

// Register in Startup
builder.Services.AddHealthChecks()
    .AddCheck<QuartzHealthCheck>("quartz");
```

## Roadmap

- ✅ CRON-based scheduling
- ✅ Clustering support
- ✅ Persistent job storage
- ✅ Misfire handling
- ⏳ Job history API
- ⏳ Custom dashboard
- ⏳ Job retry policies

## Contributing

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](../../LICENSE) for details.

---

**Related**: See [Encina.Hangfire](../Encina.Hangfire/README.md) for a simpler background job solution with built-in dashboard.

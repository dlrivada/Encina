# ABAC Observability Guide

## Overview

Encina.Security.ABAC ships with built-in OpenTelemetry-compatible observability through `System.Diagnostics.Activity` (distributed tracing) and `System.Diagnostics.Metrics` (metrics). Every policy evaluation, obligation execution, and advice invocation is instrumented automatically -- no additional configuration required beyond enabling the listeners.

All diagnostics are defined in the internal `ABACDiagnostics` class and exposed through the standard .NET observability APIs, making them compatible with any OpenTelemetry-compliant collector (Prometheus, Grafana, Jaeger, Azure Monitor, AWS X-Ray, etc.).

---

## Activity Source

| Property | Value |
|----------|-------|
| **Source Name** | `Encina.Security.ABAC` |
| **Source Version** | `1.0` |

The `ActivitySource` is created as a static singleton:

```csharp
internal static readonly ActivitySource ActivitySource = new("Encina.Security.ABAC", "1.0");
```

### StartEvaluation Activity

Each policy evaluation creates an `ABAC.Evaluate` activity of kind `Internal`:

```csharp
Activity? activity = ABACDiagnostics.StartEvaluation(requestTypeName);
// activity.OperationName == "ABAC.Evaluate"
// activity.Kind == ActivityKind.Internal
// Tag: abac.request_type = requestTypeName
```

The activity is only created when `ActivitySource.HasListeners()` returns `true`, ensuring zero overhead when tracing is not configured.

---

## Meter and Metrics

The meter shares the same identity as the activity source:

```csharp
internal static readonly Meter Meter = new("Encina.Security.ABAC", "1.0");
```

### Counters

| Metric Name | Type | Description |
|-------------|------|-------------|
| `abac.evaluation.total` | `Counter<long>` | Total number of ABAC policy evaluations |
| `abac.evaluation.permitted` | `Counter<long>` | Number of evaluations that resulted in Permit |
| `abac.evaluation.denied` | `Counter<long>` | Number of evaluations that resulted in Deny |
| `abac.evaluation.not_applicable` | `Counter<long>` | Number of evaluations that resulted in NotApplicable |
| `abac.evaluation.indeterminate` | `Counter<long>` | Number of evaluations that resulted in Indeterminate |
| `abac.obligation.executed` | `Counter<long>` | Total number of obligations executed |
| `abac.obligation.failed` | `Counter<long>` | Number of obligation executions that failed |
| `abac.obligation.no_handler` | `Counter<long>` | Number of obligations with no registered handler |
| `abac.advice.executed` | `Counter<long>` | Total number of advice expressions executed |

### Histograms

| Metric Name | Type | Unit | Description |
|-------------|------|------|-------------|
| `abac.evaluation.duration` | `Histogram<double>` | `ms` | Duration of ABAC policy evaluations in milliseconds |
| `abac.obligation.duration` | `Histogram<double>` | `ms` | Duration of individual obligation executions in milliseconds |

---

## Tag Constants

All tag keys used by activities and metrics are defined as internal constants:

| Constant | Tag Key | Used On | Description |
|----------|---------|---------|-------------|
| `TagRequestType` | `abac.request_type` | Activity | The MediatR request type name being evaluated |
| `TagEffect` | `abac.effect` | Activity | The evaluation result (`permit`, `deny`, `not_applicable`, `indeterminate`) |
| `TagPolicyId` | `abac.policy_id` | Activity | The identifier of the matching policy |
| `TagEnforcementMode` | `abac.enforcement_mode` | Activity | The current enforcement mode (`Block`, `Warn`, `Disabled`) |
| `TagObligationId` | `abac.obligation_id` | Activity | The identifier of the obligation being executed |
| `TagAdviceId` | `abac.advice_id` | Activity | The identifier of the advice being executed |

---

## Activity Recording Helpers

Four static helper methods set the appropriate tags and status codes on the current activity after the PDP produces a decision.

### RecordPermitted

```csharp
ABACDiagnostics.RecordPermitted(activity, policyId);
// Sets: abac.effect = "permit", abac.policy_id = policyId
// Status: ActivityStatusCode.Ok
```

### RecordDenied

```csharp
ABACDiagnostics.RecordDenied(activity, policyId, reason);
// Sets: abac.effect = "deny", abac.policy_id = policyId
// Status: ActivityStatusCode.Error with reason description
```

### RecordIndeterminate

```csharp
ABACDiagnostics.RecordIndeterminate(activity, reason);
// Sets: abac.effect = "indeterminate"
// Status: ActivityStatusCode.Error with reason description
```

### RecordNotApplicable

```csharp
ABACDiagnostics.RecordNotApplicable(activity);
// Sets: abac.effect = "not_applicable"
// Status: ActivityStatusCode.Ok
```

---

## Structured Logging

All log messages use compile-time source generation via `[LoggerMessage]` for zero-allocation logging when the log level is disabled. Event IDs occupy the `9000-9099` range reserved for ABAC diagnostics.

### Pipeline Messages (9000-9009)

| EventId | Level | Message Template | Parameters |
|---------|-------|------------------|------------|
| 9000 | `Debug` | `ABAC evaluation starting for {RequestType} ({PolicyCount} policy, {ConditionCount} condition attributes)` | `requestType`, `policyCount`, `conditionCount` |
| 9001 | `Debug` | `PDP decision for {RequestType}: {Effect} (policy: {PolicyId}, duration: {DurationMs:F2}ms)` | `requestType`, `effect`, `policyId`, `durationMs` |
| 9002 | `Debug` | `ABAC: Permit for {RequestType}` | `requestType` |
| 9003 | `Debug` | `ABAC enforcement: denied {RequestType}` | `requestType` |
| 9004 | `Warning` | `ABAC enforcement in Warn mode - would deny {RequestType}: {ErrorMessage}. Allowing request to proceed` | `requestType`, `errorMessage` |
| 9005 | `Warning` | `Permit obligations failed for {RequestType}. Overriding to Deny per XACML 7.18: {ErrorMessage}` | `requestType`, `errorMessage` |
| 9006 | `Debug` | `ABAC: NotApplicable for {RequestType} - allowing per DefaultNotApplicableEffect=Permit` | `requestType` |
| 9007 | `Debug` | `ABAC: NotApplicable for {RequestType} - denying per DefaultNotApplicableEffect=Deny` | `requestType` |
| 9008 | `Warning` | `ABAC: Indeterminate for {RequestType}: {Reason}` | `requestType`, `reason` |
| 9009 | `Error` | `ABAC evaluation failed for {RequestType} after {DurationMs:F2}ms` | `exception`, `requestType`, `durationMs` |

### Obligation Messages (9010-9019)

| EventId | Level | Message Template | Parameters |
|---------|-------|------------------|------------|
| 9010 | `Error` | `No handler registered for mandatory obligation {ObligationId}. Access denied per XACML 7.18` | `obligationId` |
| 9011 | `Error` | `Obligation handler for {ObligationId} failed: {ErrorMessage}. Access denied per XACML 7.18` | `obligationId`, `errorMessage` |
| 9012 | `Debug` | `Obligation {ObligationId} executed successfully` | `obligationId` |
| 9013 | `Debug` | `{Count} obligation(s) executed successfully` | `count` |
| 9014 | `Warning` | `OnDeny obligation failed for {RequestType}: {ErrorMessage}` | `requestType`, `errorMessage` |
| 9015 | `Debug` | `OnDeny obligations executed for {RequestType}` | `requestType` |

### Advice Messages (9020-9029)

| EventId | Level | Message Template | Parameters |
|---------|-------|------------------|------------|
| 9020 | `Debug` | `No handler registered for advice {AdviceId}. Skipping (advice is best-effort)` | `adviceId` |
| 9021 | `Warning` | `Advice handler for {AdviceId} failed: {ErrorMessage}. Continuing (advice is best-effort)` | `adviceId`, `errorMessage` |
| 9022 | `Debug` | `Advice {AdviceId} executed successfully` | `adviceId` |

---

## Health Check

The `ABACHealthCheck` verifies that the ABAC engine has policies loaded and can respond to authorization requests.

| Property | Value |
|----------|-------|
| **Default Name** | `encina-abac` |
| **Tags** | `encina`, `security`, `abac`, `ready` |
| **Registration** | `ABACOptions.AddHealthCheck = true` |

### Health Status Logic

| Condition | Status | Message |
|-----------|--------|---------|
| At least one PolicySet loaded | `Healthy` | ABAC engine has loaded policy sets |
| No PolicySets, but standalone Policies loaded | `Healthy` | ABAC engine has loaded standalone policies |
| No PolicySets and no Policies | `Degraded` | No policies or policy sets loaded. The ABAC engine will return NotApplicable for all requests |
| Exception querying PAP | `Unhealthy` | Failed to query the Policy Administration Point |

### Enabling the Health Check

```csharp
services.AddEncinaABAC(options =>
{
    options.AddHealthCheck = true;
});
```

This registers the health check with the default name and tags. Query it via the standard ASP.NET Core health check endpoint:

```
GET /health
```

---

## OpenTelemetry Integration

### Setting Up with Encina.OpenTelemetry

Configure the OpenTelemetry SDK to listen for ABAC traces and metrics:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Encina.Security.ABAC"))   // Subscribe to ABAC activities
    .WithMetrics(metrics => metrics
        .AddMeter("Encina.Security.ABAC"));   // Subscribe to ABAC metrics
```

### Exporting to Specific Backends

```csharp
// OTLP (Jaeger, Tempo, etc.)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Encina.Security.ABAC")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("Encina.Security.ABAC")
        .AddOtlpExporter());

// Prometheus (pull-based scraping)
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("Encina.Security.ABAC")
        .AddPrometheusExporter());
```

---

## Dashboard Examples

### Prometheus Query Patterns

**Evaluation rate by effect (per second):**

```promql
rate(abac_evaluation_total[5m])
```

**Deny rate over time:**

```promql
rate(abac_evaluation_denied[5m])
```

**Permit-to-deny ratio:**

```promql
rate(abac_evaluation_permitted[5m]) / rate(abac_evaluation_denied[5m])
```

**P95 evaluation duration:**

```promql
histogram_quantile(0.95, rate(abac_evaluation_duration_bucket[5m]))
```

**Obligation failure rate:**

```promql
rate(abac_obligation_failed[5m]) / rate(abac_obligation_executed[5m])
```

**Missing obligation handlers (should be zero in production):**

```promql
abac_obligation_no_handler
```

### Grafana Dashboard Panels (Suggested Layout)

| Panel | Type | Metric(s) | Purpose |
|-------|------|-----------|---------|
| **Evaluation Rate** | Time series | `abac.evaluation.total` | Traffic volume over time |
| **Decision Distribution** | Pie chart | `permitted`, `denied`, `not_applicable`, `indeterminate` | Decision breakdown |
| **Evaluation Latency** | Heatmap | `abac.evaluation.duration` | P50/P95/P99 latency |
| **Obligation Health** | Stat | `executed` vs `failed` vs `no_handler` | Obligation success rate |
| **Obligation Latency** | Time series | `abac.obligation.duration` | Per-obligation timing |
| **Advice Execution** | Counter | `abac.advice.executed` | Advice activity volume |
| **Health Check** | Status | `/health` endpoint | System readiness |

### Alerting Rules (Example)

```yaml
# Alert on high deny rate
- alert: HighABACDenyRate
  expr: rate(abac_evaluation_denied[5m]) > 10
  for: 5m
  labels:
    severity: warning
  annotations:
    summary: "High ABAC deny rate detected"

# Alert on obligation failures
- alert: ABACObligationFailure
  expr: rate(abac_obligation_failed[5m]) > 0
  for: 1m
  labels:
    severity: critical
  annotations:
    summary: "ABAC obligation handler is failing"

# Alert on missing obligation handlers
- alert: ABACMissingObligationHandler
  expr: abac_obligation_no_handler > 0
  labels:
    severity: critical
  annotations:
    summary: "Obligation has no registered handler"

# Alert on evaluation latency
- alert: ABACHighLatency
  expr: histogram_quantile(0.95, rate(abac_evaluation_duration_bucket[5m])) > 50
  for: 5m
  labels:
    severity: warning
  annotations:
    summary: "ABAC evaluation P95 latency exceeds 50ms"
```

---

## Source Files

| File | Purpose |
|------|---------|
| `src/Encina.Security.ABAC/Diagnostics/ABACDiagnostics.cs` | Activity source, meter, counters, histograms, tag constants, recording helpers |
| `src/Encina.Security.ABAC/Diagnostics/ABACLogMessages.cs` | `[LoggerMessage]` source-generated structured log methods (EventIds 9000-9022) |
| `src/Encina.Security.ABAC/Health/ABACHealthCheck.cs` | `IHealthCheck` implementation for PAP policy verification |

---
title: "ABAC Observability Guide"
layout: default
parent: "Features"
---

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
| `TagEffect` | `abac.effect` | Activity | The evaluation result (`permit`, `deny`, `indeterminate`) |
| `TagPolicyId` | `abac.policy_id` | Activity | The identifier of the matching policy |
| `TagEnforcementMode` | `abac.enforcement_mode` | Activity | The current enforcement mode (`Block`, `Warn`, `Disabled`) |
| `TagObligationId` | `abac.obligation_id` | Activity | The identifier of the obligation being executed |
| `TagAdviceId` | `abac.advice_id` | Activity | The identifier of the advice being executed |

---

## Activity Recording Helpers

Three static helper methods set the appropriate tags and status codes on the current activity once the requirements of the request have been decided. A required policy that is NotApplicable is recorded as a deny.

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
// When the PEP catches an exception, the reason is the exception type name, never its message.
```

---

## Decision Audit Telemetry

When `ABACOptions.DecisionAudit.Enabled` is `true`, the Policy Enforcement Point emits the instruments below on the same `Encina.Security.ABAC` activity source and meter, one set per audited decision. Nothing is emitted when the audit is disabled. None of them carries a subject, resource, tenant, attribute value or error or exception message ([SPEC-002](../../../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-062); the failure tag holds the store error code or the exception type name. For the trail itself see [Decision audit](decision-audit.md).

### Metrics

| Metric Name | Type | Unit | Tags | Description |
| --- | --- | --- | --- | --- |
| `abac.decision_audit.recorded` | `Counter<long>` | | `abac.outcome` (`Granted`, `Denied`, `DeniedNotEnforced`), `abac.enforcement_mode` (`Block`, `Warn`) | One increment per decision record written |
| `abac.decision_audit.failed` | `Counter<long>` | | `abac.failure_mode` (`FailClosed`, `BestEffort`), `error.type` (error code or exception type name) | One increment per record that could not be built or written |
| `abac.decision_audit.duration` | `Histogram<double>` | `ms` | none | Building plus writing one record, recorded once per audited decision |

`abac.decision_audit.failed` is the failure signal of the trail: alert on any increase, because under `FailClosed` each increment is a request that was denied for lack of evidence, and under `BestEffort` it is a decision missing from the trail. The same failures are logged (EventIds 9080 to 9083) and, when `ABACOptions.AddHealthCheck` is `true`, reported by the [health check](decision-audit.md#health).

### Spans and tags

| Item | Where | Description |
| --- | --- | --- |
| `ABAC.DecisionAudit.Record` | Activity, kind `Internal`, child of `ABAC.Evaluate` | Status `Ok` when the record was written; `Error` with the failure code as status description otherwise, plus tags `abac.failure_mode` and `error.type` |
| `abac.decision_id` | Tag on `ABAC.Evaluate` | The decision id (`Guid`, format `D`), set once the record is built |

### Prometheus examples

```promql
sum by (abac_failure_mode, error_type) (rate(abac_decision_audit_failed_total[5m]))
```

```promql
histogram_quantile(0.95, rate(abac_decision_audit_duration_bucket[5m]))
```

---

## Structured Logging

All log messages use compile-time source generation via `[LoggerMessage]` for zero-allocation logging when the log level is disabled. Event IDs occupy the `9000-9099` range reserved for ABAC diagnostics. The package's EventIds are allocated inside 9000-9099 (9098 and 9099 are both used); the unused ids to reuse first are 9006-9007, 9016-9019, 9023-9029, 9041-9049 and 9056-9057; 9079-9090 are the decision audit trail of #751, including the startup check (9084, 9086 and 9087). Ids 9094-9097 come from `PersistentPolicyAdministrationPoint` (9094, 9095, 9097) and `ABACPolicySeedingHostedService` (9096).

### Pipeline Messages (9000-9005, 9008-9009)

| EventId | Level | Message Template | Parameters |
|---------|-------|------------------|------------|
| 9000 | `Debug` | `ABAC evaluation starting for {RequestType} ({PolicyCount} policy, {ConditionCount} condition attributes)` | `requestType`, `policyCount`, `conditionCount` |
| 9001 | `Debug` | `PDP decision for {RequestType}: {Effect} (policy: {PolicyId}, duration: {DurationMs:F2}ms)` | `requestType`, `effect`, `policyId`, `durationMs` |
| 9002 | `Debug` | `ABAC: Permit for {RequestType}` | `requestType` |
| 9003 | `Debug` | `ABAC enforcement: denied {RequestType}` | `requestType` |
| 9004 | `Warning` | `ABAC enforcement in Warn mode - would deny {RequestType}: {ErrorCode}. Allowing request to proceed` | `requestType`, `errorCode` |
| 9005 | `Warning` | `Permit obligations failed for {RequestType}. Overriding to Deny per XACML 7.18: {ErrorCode}` | `requestType`, `errorCode` |
| 9008 | `Warning` | `ABAC: Indeterminate for {RequestType}: {Reason}` | `requestType`, `reason` |
| 9009 | `Error` | `ABAC evaluation failed for {RequestType} after {DurationMs:F2}ms` | `exception` (through `ForLogging()`), `requestType`, `durationMs` |

### Obligation Messages (9010-9019)

| EventId | Level | Message Template | Parameters |
|---------|-------|------------------|------------|
| 9010 | `Error` | `No handler registered for mandatory obligation {ObligationId}. Access denied per XACML 7.18` | `obligationId` |
| 9011 | `Error` | `Obligation handler for {ObligationId} failed: {ErrorCode}. Access denied per XACML 7.18` | `obligationId`, `errorCode` |
| 9012 | `Debug` | `Obligation {ObligationId} executed successfully` | `obligationId` |
| 9013 | `Debug` | `{Count} obligation(s) executed successfully` | `count` |
| 9014 | `Warning` | `OnDeny obligation failed for {RequestType}: {ErrorCode}` | `requestType`, `errorCode` |
| 9015 | `Debug` | `OnDeny obligations executed for {RequestType}` | `requestType` |

### Advice Messages (9020-9029)

| EventId | Level | Message Template | Parameters |
|---------|-------|------------------|------------|
| 9020 | `Debug` | `No handler registered for advice {AdviceId}. Skipping (advice is best-effort)` | `adviceId` |
| 9021 | `Warning` | `Advice handler for {AdviceId} failed: {ErrorCode}. Continuing (advice is best-effort)` | `adviceId`, `errorCode` |
| 9022 | `Debug` | `Advice {AdviceId} executed successfully` | `adviceId` |

### Required Policy, Condition and Handler Messages (9072-9078)

These messages carry error codes and exception types only, never an error or exception message.

| EventId | Level | Message Template | Parameters |
|---------|-------|------------------|------------|
| 9072 | `Warning` | `Lookup of required policy {PolicyId} failed: {ErrorCode}. The policy is Indeterminate` | `policyId`, `errorCode` |
| 9073 | `Error` | `Unexpected error while evaluating required policy {PolicyId}. The policy is Indeterminate` | `exception`, `policyId` |
| 9074 | `Warning` | `Required policy {PolicyId} for {RequestType} is not a top-level policy set or standalone policy in the policy store. The request is denied in Block mode and proceeds in Warn mode` | `policyId`, `requestType` (emitted in every enforcement mode that evaluates policies, not only `Warn`) |
| 9075 | `Debug` | `Condition {ConditionIndex} for {RequestType} evaluated to false. Access denied` | `conditionIndex`, `requestType` |
| 9076 | `Warning` | `Condition {ConditionIndex} for {RequestType} could not be compiled: {ErrorCode}. The condition is Indeterminate` | `conditionIndex`, `requestType`, `errorCode` |
| 9077 | `Warning` | `Condition {ConditionIndex} for {RequestType} failed during evaluation. The condition is Indeterminate` | `exception`, `conditionIndex`, `requestType` |
| 9078 | `Error` | `Handler for obligation or advice {ObligationId} threw an exception` | `exception`, `obligationId` |

### Fail-Closed Messages (9091-9093)

These messages carry error codes, source names and exception types only, never an error message, an exception message or a user identifier. The decision audit messages (9079-9090) are listed in the next section.

| EventId | Level | Message Template | Parameters |
|---------|-------|------------------|------------|
| 9091 | `Warning` | `ABAC denied {RequestType}: no authenticated security context with a user is available ({ErrorCode}). The request is denied in every enforcement mode` | `requestType`, `errorCode` |
| 9092 | `Warning` | `Retrieval of the {Source} from the policy administration point failed: {ErrorCode}. The decision is Indeterminate` | `source` (`policy sets` or `standalone policies`), `errorCode` |
| 9093 | `Error` | `Unexpected error while evaluating the policy store. The decision is Indeterminate` | `exception` (through `ForLogging()`: type and stack trace) |

### Decision Audit Messages (9079-9090)

Emitted by the Policy Enforcement Point, `AuditStoreABACDecisionRecorder` and the decision audit reader. They carry request type names, decision ids and error codes or exception types only, never an error or exception message. See [Decision audit](decision-audit.md). EventIds 9084, 9086 and 9087 come from the startup check, which runs only when the audit is enabled.

| EventId | Level | Message Template | Parameters |
|---------|-------|------------------|------------|
| 9079 | `Debug` | `ABAC decision for {RequestType} recorded: outcome {EnforcedOutcome}, reason {ReasonCode}` | `requestType`, `enforcedOutcome`, `reasonCode` |
| 9080 | `Error` | `The ABAC decision for {RequestType} could not be recorded ({FailureCode}). The request is denied (FailClosed)` | `requestType`, `failureCode` |
| 9081 | `Warning` | `The ABAC decision for {RequestType} could not be recorded ({FailureCode}). The request proceeds (BestEffort)` | `requestType`, `failureCode` |
| 9082 | `Error` | `The ABAC decision for {RequestType}, which was denied, could not be recorded ({FailureCode}). The denial stands` | `requestType`, `failureCode` |
| 9083 | `Error` | `The decision recorder threw while recording the ABAC decision for {RequestType}` | `exception` (through `ForLogging()`), `requestType` |
| 9084 | `Warning` | `The ABAC decision audit runs in BestEffort mode: a request proceeds even when its decision record cannot be written, so the trail may be incomplete` | none |
| 9086 | `Warning` | `The ABAC decision audit writes to InMemoryOperationAuditStore: the trail is lost when the process stops. Register a persistent IOperationAuditStore for production` | none |
| 9087 | `Critical` | `The ABAC decision audit is enabled but no IOperationAuditStore is registered. Register one (for example through a provider package) or disable DecisionAudit` | none; the host start then fails with `InvalidOperationException` |
| 9088 | `Debug` | `The evaluation trace of the ABAC decision for {RequestType} reached the limit of {MaxTraceEntries} entries and is truncated` | `requestType`, `maxTraceEntries` |
| 9089 | `Debug` | `The write of ABAC decision {DecisionId} reported a failure ({FailureCode}) but the entry is stored; the record counts as written` | `decisionId`, `failureCode` |
| 9090 | `Warning` | `A decision audit query ran without a tenant in a multi-tenant application (AllowCrossTenantQueries is set)` | none |
| 9098 | `Error` | `A stored ABAC decision audit entry could not be read; the query fails` | `exception` (through `ForLogging()`: type and stack trace) |
| 9099 | `Debug` | `The write of ABAC decision {DecisionId} failed ({FailureCode}) and the look-up that would confirm it failed too ({LookupFailure}); the write stays unconfirmed` | `decisionId`, `failureCode`, `lookupFailure` (an error code or an exception type) |

### Policy Administration Messages (9094-9097)

Emitted by `PersistentPolicyAdministrationPoint` (9094, 9095, 9097) and `ABACPolicySeedingHostedService` (9096). They carry error codes and exception types only, never an error or exception message. See [Persistent PAP](persistent-pap.md#policy-change-principal-and-audit-trail).

| EventId | Level | Message Template | Parameters |
|---------|-------|------------------|------------|
| 9094 | `Error` | `Audit write failed for policy change {Action} on {EntityType} '{EntityId}': {ErrorCode}` | `action`, `entityType`, `entityId`, `errorCode` |
| 9095 | `Error` | `Exception during the audit write for policy change {Action} on {EntityType} '{EntityId}'` | `action`, `entityType`, `entityId`, `exception` |
| 9096 | `Information` | `System actor scope opened for ABAC policy seeding; policy changes are recorded as made by the system actor` | none |
| 9097 | `Warning` | `ABAC policy changes are being applied without an audit record: {Condition}` | `condition` (logged once per PAP instance) |

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
| No PolicySets and no Policies | `Degraded` | No policies or policy sets loaded. Every policy named by `[RequirePolicy]` will be missing, so those requests are denied with `encina.authorization.abac_policy_not_found` |
| Exception querying PAP | `Unhealthy` | Failed to query the Policy Administration Point |

When `ABACOptions.DecisionAudit.Enabled` is `true`, the result also carries the decision audit state and the worse of the two statuses wins; see [Decision audit: Health](decision-audit.md#health).

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
| **Decision Distribution** | Pie chart | `permitted`, `denied`, `indeterminate` | Decision breakdown |
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
| `src/Encina.Security.ABAC/Diagnostics/ABACLogMessages.cs` | `[LoggerMessage]` source-generated structured log methods (EventIds allocated inside 9000-9099, unused ids 9006-9007, 9016-9019, 9023-9029, 9041-9049 and 9056-9057; 9079-9090, 9098 and 9099 are the ABAC decision audit trail (#751, 9084, 9086 and 9087 are its startup check); 9094-9097 come from `PersistentPolicyAdministrationPoint` (9094, 9095, 9097) and `ABACPolicySeedingHostedService` (9096); see [Structured Logging](#structured-logging)) |
| `src/Encina.Security.ABAC/Health/ABACHealthCheck.cs` | `IHealthCheck` implementation for PAP policy verification |

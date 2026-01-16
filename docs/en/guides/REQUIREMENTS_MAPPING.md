# Encina Testing Requirements Mapping

## Purpose

- Provide traceability between product requirements and automated verification layers.
- Highlight the suites and scenarios covering each Encina capability.

## Coverage Matrix

| Requirement ID | Description | Coverage |
|----------------|-------------|----------|
| `REQ-REQ-LIFECYCLE` | Command handler lifecycle, error and cancellation flows | Unit tests in `tests/Encina.UnitTests/EncinaTests.cs` |
| `REQ-REQ-QUERY` | Query determinism and pipeline ordering | Property tests in `tests/Encina.PropertyTests/ConfigurationProperties.cs` |
| `REQ-REQ-PIPELINE` | Pipeline behavior registration contracts | Contract tests in `tests/Encina.ContractTests/ServiceRegistrationContracts.cs` |
| `REQ-REQ-CONCURRENCY` | Concurrent publish guarantees for requests and notifications | Property tests in `tests/Encina.PropertyTests/NotificationProperties.cs` |
| `REQ-NOT-ORDER` | Notification ordering, cancellation, and error propagation | Property tests in `tests/Encina.PropertyTests/NotificationProperties.cs` |
| `REQ-NOT-MULTI` | Multi-handler notification registration | Contract tests in `tests/Encina.ContractTests/ServiceRegistrationContracts.cs` |
| `REQ-INF-METRICS` | Metrics emission and failure tagging | Unit tests in `tests/Encina.UnitTests/EncinaMetricsTests.cs` |
| `REQ-ERR-ZEROEXC` | Zero Exceptions policy – Encina operations surface expected failures as functional results | Unit tests in `tests/Encina.UnitTests/EncinaTests.cs` and `tests/Encina.UnitTests/PipelineBehaviorsTests.cs` |
| `REQ-CONF-LIFETIME` | Handler lifetime configuration | Contract tests in `tests/Encina.ContractTests/ServiceRegistrationContracts.cs` |
| `REQ-CONF-SCAN` | Assembly scanning fallbacks and caching | Unit tests in `tests/Encina.UnitTests/EncinaTests.cs` |
| `REQ-CONF-EDGE` | Configuration extension edge cases | Contract tests in `tests/Encina.ContractTests/ServiceRegistrationContracts.cs` |
| `REQ-QUAL-MUTATION` | Maintain ≥93.74% mutation score | CI step `scripts/run-stryker.cs` + summary `scripts/update-mutation-summary.cs`; documented in `MUTATION_TESTING.md` |
| `REQ-PERF-BASELINE` | Benchmark latencies and allocations within thresholds | Benchmarks project + CI gate `scripts/run-benchmarks.cs`/`scripts/check-benchmarks.cs`; documented in `PERFORMANCE_TESTING.md` |
| `REQ-LOAD-THROUGHPUT` | Sustained throughput under configurable load | Console harness `tests/Encina.LoadTests` plus NBomber scenarios `tests/Encina.NBomber` (profiles in `tests/Encina.LoadTests/profiles/`); both launched through `scripts/run-load-harness.cs`. CI enforces baseline guardrails via `ci/load-thresholds.json` (console harness) and `ci/nbomber-thresholds.json` (send-burst NBomber profile) through `scripts/check-load-metrics.cs` and `scripts/summarize-nbomber-run.cs`; documented in `LOAD_TESTING.md`. |

## Gaps & Planned Work

- Monitor NBomber guardrail results and extend coverage to the mixed-traffic profile once variance is understood; update thresholds in `ci/nbomber-thresholds.json` as needed.
- Link roadmap entries to requirement IDs so status updates map back to this matrix.
- Add traceability for future integration or end-to-end test suites as they come online.
- Feed NBomber mixed-traffic metrics into the history aggregation once the profile is automated, keeping `REQ-LOAD-THROUGHPUT` traceability updated with additional scenarios or failure-injection profiles.

## Maintenance

- Review matrix at every milestone; update entries after major features or refactors.
- Keep references stable by pointing to test class names or doc sections rather than line numbers.

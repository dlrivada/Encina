# SimpleMediator Testing Requirements Mapping

## Purpose

- Provide traceability between product requirements and automated verification layers.
- Highlight the suites and scenarios covering each mediator capability.

## Coverage Matrix

| Requirement ID | Description | Coverage |
|----------------|-------------|----------|
| `REQ-REQ-LIFECYCLE` | Command handler lifecycle, error and cancellation flows | Unit tests in `tests/SimpleMediator.Tests/SimpleMediatorTests.cs` |
| `REQ-REQ-QUERY` | Query determinism and pipeline ordering | Property tests in `tests/SimpleMediator.PropertyTests/ConfigurationProperties.cs` |
| `REQ-REQ-PIPELINE` | Pipeline behavior registration contracts | Contract tests in `tests/SimpleMediator.ContractTests/ServiceRegistrationContracts.cs` |
| `REQ-REQ-CONCURRENCY` | Concurrent publish guarantees for requests and notifications | Property tests in `tests/SimpleMediator.PropertyTests/NotificationProperties.cs` |
| `REQ-NOT-ORDER` | Notification ordering, cancellation, and error propagation | Property tests in `tests/SimpleMediator.PropertyTests/NotificationProperties.cs` |
| `REQ-NOT-MULTI` | Multi-handler notification registration | Contract tests in `tests/SimpleMediator.ContractTests/ServiceRegistrationContracts.cs` |
| `REQ-INF-METRICS` | Metrics emission and failure tagging | Unit tests in `tests/SimpleMediator.Tests/MediatorMetricsTests.cs` |
| `REQ-CONF-LIFETIME` | Handler lifetime configuration | Contract tests in `tests/SimpleMediator.ContractTests/ServiceRegistrationContracts.cs` |
| `REQ-CONF-SCAN` | Assembly scanning fallbacks and caching | Unit tests in `tests/SimpleMediator.Tests/SimpleMediatorTests.cs` |
| `REQ-CONF-EDGE` | Configuration extension edge cases | Contract tests in `tests/SimpleMediator.ContractTests/ServiceRegistrationContracts.cs` |
| `REQ-QUAL-MUTATION` | Maintain ≥92.37% mutation score | CI step `scripts/run-stryker.cs` + summary `scripts/update-mutation-summary.cs`; documented in `MUTATION_TESTING.md` |
| `REQ-PERF-BASELINE` | Benchmark latencies and allocations within thresholds | Benchmarks project + CI gate `scripts/run-benchmarks.cs`/`scripts/check-benchmarks.cs`; documented in `PERFORMANCE_TESTING.md` |
| `REQ-LOAD-THROUGHPUT` | Sustained throughput under configurable load | Harness `load/SimpleMediator.LoadTests` via `scripts/run-load-harness.cs`; documented in `LOAD_TESTING.md` |

## Gaps & Planned Work

- Record CPU/memory envelopes for the load harness and note tuning guidance in `LOAD_TESTING.md`.
- Link roadmap entries to requirement IDs so status updates map back to this matrix.
- Add traceability for future integration or end-to-end test suites as they come online.

## Maintenance

- Review matrix at every milestone; update entries after major features or refactors.
- Keep references stable by pointing to test class names or doc sections rather than line numbers.

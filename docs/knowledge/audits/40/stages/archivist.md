## Scope

Closing commit 750e82dd (timeline `closed` event, 2025-12-26; no PR). Issue created 2025-12-24T13:24:46Z, closed 2025-12-26T14:42:46Z. Paths verified to exist today:

- `src/Encina.Polly/Attributes/RateLimitAttribute.cs` (later commits: none)
- `src/Encina.Polly/Behaviors/RateLimitingPipelineBehavior.cs` (later: 1e6d4f5f cancellation checks, ace6533f EventId renumbering)
- `src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs` (later: ace6533f), `IRateLimiter.cs`, `RateLimitResult.cs`, `RateLimitState.cs`
- `src/Encina.Polly/ServiceCollectionExtensions.cs` (registers `IRateLimiter` and the behavior in `AddEncinaPolly`), `src/Encina/Errors/EncinaErrorCodes.cs` (`RateLimitExceeded`; also used by `src/Encina.Messaging/Recoverability/IErrorClassifier.cs`)
- Public API lines in `src/Encina.Polly/PublicAPI.Unshipped.txt` and `src/Encina/PublicAPI.Unshipped.txt`; manifest `.github/coverage-manifest/Encina.Polly.json` lists the six rate limiting files.

Tests moved by consolidation commit 65826302 (original `tests/Encina.Polly.*` and `benchmarks/Encina.Polly.Benchmarks` deleted):
- Present: `tests/Encina.UnitTests/Polly/{AdaptiveRateLimiterTests,RateLimitAttributeTests,RateLimitingPipelineBehaviorTests}.cs`, `tests/Encina.GuardTests/Infrastructure/Polly/{AdaptiveRateLimiterGuardsTests,RateLimitingPipelineBehaviorGuardsTests}.cs`, `tests/Encina.BenchmarkTests/Encina.Polly.Benchmarks/RateLimitingBenchmarks.cs`.
- Gone: `RateLimitingPropertyTests`, `ResilienceLoadTests` and `BehaviorContractTests` (no RateLimit reference in `tests/Encina.PropertyTests`, `LoadTests` or `ContractTests`).

Pre-draft corrections: `packages` took `Encina.Messaging.Enterprise` from the issue's "Affected Packages" (does not exist); `closed_at` was `12/26/2025`; `linked_prs: []` format; the invented spec-invariant destination was dropped. The commit message says 104 unit tests; counted from the commit, not re-run.

Code-stage hint: the limiter is independent of the circuit breaker (no reference in the rate limiter files) and there is no Meter or ActivitySource in Encina.Polly rate limiting.

## Destinations

- Adaptive limiter design: present in `src/Encina.Polly/README.md` (generic mention "rate limiting" only, no `[RateLimit]` usage); `docs/INVENTORY.md` (Spanish, lists `[RateLimit]`, `IRateLimiter`, `AdaptiveRateLimiter`), `CHANGELOG.md`, `docs/releases/v0.11.0`. Recorded as `docs` -> README (partial coverage: docs-reviewer should judge it).
- TimeProvider testability: present in `tests/Encina.UnitTests/Polly/AdaptiveRateLimiterTests.cs`.
- `AddEncinaRateLimiting` API, circuit breaker integration, metrics/alerting: absent in code; no ADR. Tracking, each state verified live with `gh issue view` on 2026-10-15 (the earlier title search `"rate limiting Polly in:title"` missed #728 and #197, whose titles do not have both words):
  - Metrics half of "Metrics and alerting": tracked by #728 (OPEN, "Add OpenTelemetry instrumentation to Polly resilience integration"; acceptance item "Bulkhead rejection and rate limiting events are metered"). Record destination: backlog -> #728.
  - Circuit breaker integration and alerting: untracked by any open issue.
  - `AddEncinaRateLimiting`: open #197 (OPEN, "Rate Limiting Pipeline Behavior") proposes `services.AddEncinaRateLimiting(config => ...)`, `EncinaErrors.RateLimitExceeded` and Retry-After headers, a different never-built design (PermitLimit, ASP.NET Core); partially related, not a tracker of this API.
  - #704 (OPEN, distributed cache for rate limiting state) and #740 (OPEN, tenant-scoped rate limiting): #40 asked for neither tenants nor distributed state, so they track nothing from #40.
- Load/property/contract tests: missing today (see Scope).

## Successor and duplicate issues

None. No successor or duplicate named in the issue; there are no comments. Issue state_reason is COMPLETED but delivery is partial.

## Lessons for the pipeline

- The close was by a commit whose title ("adaptive throttling") matched the theme while the issue's named API, package, circuit breaker and metrics were not delivered; compare each Feature bullet with `src/` symbols.
- A "nothing tracks X" claim must be searched by every feature name in it (metrics, AddEncinaRateLimiting, circuit breaker), not by the package plus a generic title phrase; and a related open issue is not a tracker unless the closed issue asked for what it covers.
- The consolidation commit deleted per-flag test projects without migrating all of them; check each test flag in the consolidated projects for the feature's symbols.

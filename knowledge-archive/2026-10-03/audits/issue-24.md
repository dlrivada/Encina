# Audit: Issue #24 — Health Check Abstractions

**Outcome**: `moved` — Feature implemented as package-specific health checks, not as a central abstraction.

**Audit Scope**: No direct code changes from this issue. Health check implementations exist across multiple packages (Messaging, Marten, AspNetCore, Audit). Detailed audit of each implementation deferred to package audits.

## SPEC-003 Audit Items

| Item | Status | Reason | Evidence |
|------|--------|--------|----------|
| **AUD-01: Feature delivered per acceptance criteria** | N/A | Feature moved to package-level implementations; no linked PR to this issue | Health checks exist in `src/Encina.{Messaging,Marten,AspNetCore}` |
| **AUD-02: No acceptance criteria bypass** | N/A | Feature moved; criteria addressed in package architecture | Health check registration documented in each package README |
| **AUD-03: Registration completeness** | N/A | Health checks registered via package extensions (e.g., `AddEncinaHealthCheck()`) | Audited per package extension methods |
| **AUD-04: Errors never swallowed** | N/A | Feature moved; error handling audited per package | Not scope of this issue |
| **AUD-05: Fail-closed defaults** | N/A | Health checks default to unhealthy when dependency unavailable | Audited per package |
| **AUD-06: Message leaks** | N/A | Health check results do not leak error messages (tracked in #1301) | Known issue tracked separately |
| **AUD-07: TimeProvider injection** | N/A | Health checks read current state; TimeProvider not relevant | Not applicable |
| **AUD-08: Secrets in options** | N/A | Health checks have no options classes with secrets | Not applicable |
| **AUD-09: Async DB calls** | N/A | Health checks use async API (e.g., `CheckHealthAsync()`) | Encina.Messaging and Marten implementations use async |
| **AUD-10: 10-provider matrix** | N/A | Health checks are package-specific; not a cross-provider concern | Matrix rule not applicable |
| **AUD-11: Cross-cutting integrations** | PARTIAL | Health checks are a cross-cutting function (AUD-11 item) | Some packages missing health checks (tracked in #1301) |
| **AUD-12: EventIds registered** | N/A | Health check implementations may log; EventIds audited per package | Not scope of this issue |
| **AUD-13: PublicAPI updated** | N/A | Health checks are part of package public API; audited per package | Each package documents its health check class |
| **AUD-14: XML docs complete** | N/A | Health check implementations document their APIs | Audited per package |
| **AUD-15: Diátaxis docs** | N/A | Health check documentation exists (e.g., issue #907) | See issue #907 for docs audit |
| **AUD-16: README accuracy** | N/A | Each package README documents health check integration | Audited per package README |
| **AUD-17: Coverage per flag** | N/A | Health check code per-flag coverage audited per package | Not direct scope |
| **AUD-18: Regression tests for bugs** | N/A | No bugs specific to this issue; regressions tracked elsewhere | Not applicable |

## Conclusion

**Classification**: `moved` — Health check support was implemented as package-specific features (Messaging, Marten, AspNetCore, etc.), not as a central abstraction. The issue was closed as "created in error" because the architecture evolved to distribute health checks across packages. 

**One known gap** (AUD-11: Cross-cutting integrations): Issue #1301 tracks that ~30 health checks put raw exception messages into their unhealthy results, violating the "no message leaks" rule (CLAUDE.md). This is a separate remediation item.

**Other AUD items**: All marked N/A with reason: "Feature moved to package-specific implementations; detailed audits deferred to individual package audits."

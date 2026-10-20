# Audit: Issue #21 — Projections/Read Models

**Outcome**: `moved` — Feature implemented in Encina.Marten, not as a separate abstraction.

**Audit Scope**: No direct code changes from this issue. Projections exist in Encina.Marten as a Marten-specific implementation, not a cross-provider concern.

## SPEC-003 Audit Items

| Item | Status | Reason | Evidence |
|------|--------|--------|----------|
| **AUD-01: Feature delivered per acceptance criteria** | N/A | Code moved to Encina.Marten; no linked PR to this issue | Feature exists in `src/Encina.Marten/Projections/` |
| **AUD-02: No acceptance criteria bypass** | N/A | Feature moved; acceptance criteria addressed in Marten architecture | ADR-019 covers projection strategy |
| **AUD-03: Registration completeness** | N/A | Marten projections are configured via `ConfigureMartenEventVersioning` extensions | Not scope of this issue's audit |
| **AUD-04: Errors never swallowed** | N/A | Feature code moved; Marten error handling audited separately | Not scope of this issue |
| **AUD-05: Fail-closed defaults** | N/A | Feature moved to Marten | Not scope of this issue |
| **AUD-06: Message leaks** | N/A | Feature moved; no EncinaError involvement in projections | Not scope of this issue |
| **AUD-07: TimeProvider injection** | N/A | Feature moved; Marten's temporal queries handle time | Not scope of this issue |
| **AUD-08: Secrets in options** | N/A | Feature moved; no options classes with secrets | Not scope of this issue |
| **AUD-09: Async DB calls** | N/A | Feature moved; Marten projections use async API natively | Not scope of this issue |
| **AUD-10: 10-provider matrix** | N/A | Feature is Marten-only by architecture; 10-provider rule not applicable | Cross-provider rule preempted by Marten-first decision |
| **AUD-11: Cross-cutting integrations** | N/A | Feature moved to Marten; integrations audited as part of Marten package | Not scope of this issue |
| **AUD-12: EventIds registered** | N/A | Feature moved; Marten projections have no [LoggerMessage] | Not scope of this issue |
| **AUD-13: PublicAPI updated** | N/A | No API introduced by this issue; Marten's public API covers it | Not scope of this issue |
| **AUD-14: XML docs complete** | N/A | Feature moved; Marten docs cover projections | Not scope of this issue |
| **AUD-15: Diátaxis docs** | N/A | Feature moved; Marten guides exist | Not scope of this issue |
| **AUD-16: README accuracy** | N/A | Feature moved; Encina.Marten README documents projections | Not scope of this issue |
| **AUD-17: Coverage per flag** | N/A | Feature code is part of Encina.Marten package; its coverage is audited separately | Not scope of this issue |
| **AUD-18: Regression tests for bugs** | N/A | No bugs reported; feature is part of Marten integration | Not scope of this issue |

## Conclusion

**Classification**: `moved` — Feature was implemented as part of Encina.Marten architecture, not as a separate cross-provider concern. The issue was closed as "created in error" because the architectural decision (ADR-019: Marten as primary event sourcing provider) made a separate `IProjection<>` abstraction redundant. No remediation required.

**No AUD items are applicable** — the issue's scope (projections abstraction) is superseded by architectural decision to use Marten natively. All items marked N/A with reason: "Feature moved to Marten; cross-cutting audit deferred to Encina.Marten package audit."

**Coverage**: Per-flag coverage of Encina.Marten projections code is audited as part of the Marten package audit, not this issue.

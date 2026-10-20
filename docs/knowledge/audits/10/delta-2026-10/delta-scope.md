# Delta scope of issue #10 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published audit result (docs/knowledge/audits/issue-10.md) only: the original audit has no published stage files.

## Knowledge record (docs/knowledge/issues/10.md)

```yaml
schema: 2
nav_exclude: true
issue: 10
title: "[DEBT] Fix DataAnnotations PropertyTests - ValidationContext issues"
closed: 2025-12-23
state_reason: completed
outcome: delivered
type: debt
area: testing-quality
packages: [Encina.DataAnnotations]
prs: []
linked_prs: []
knowledge:
  - kind: rule
    statement: "A test helper type used as the target of a CustomValidationAttribute (or any reflection-based DataAnnotations validator) must be public, not a private nested type, or Validator.TryValidateObject fails to resolve the validation method across the reflection boundary."
    current: unknown
    sources:
      - "https://github.com/dlrivada/Encina/issues/10, closed 2025-12-23; paraphrase: issue body root cause and closing comment citing commit 3f20fec"
      - "commit 3f20fec8f4ebbe98317be9ec02e8b9755ed19dc2, 2025-12-23; paraphrase: diff moves ContextAwareCommand and NoValidationCommand from private nested records to public top-level records with the comment 'Must be public because CustomValidationAttribute requires it'"
    destinations:
      - kind: none
        target: ""
        status: done
        reason: "The specific test scenario (CustomValidationAttribute-based context-aware validation) was removed entirely by the test-consolidation rewrite (commit d7b7b8ac) and no test in the repository exercises CustomValidationAttribute today; current: unknown because the underlying .NET reflection behavior still holds but nothing in Encina exercises it to confirm. Tracked as a test gap in the remediation issue instead of promoted to a rule, since it is a narrow fact about a rarely-used attribute rather than a project-wide pattern."
  - kind: direction-change
    statement: "The DataAnnotations property-test file was fully rewritten during test consolidation (tests/Encina.DataAnnotations.PropertyTests/DataAnnotationsValidationBehaviorPropertyTests.cs deleted in commit d7b7b8ac, replaced by tests/Encina.PropertyTests/Validation/DataAnnotations/ValidationInvariantProperties.cs), dropping the CustomValidationAttribute/context-enrichment scenario issue #10 fixed and replacing it with tests that assert only that validation still succeeds when context metadata is present, explicitly noting DataAnnotations cannot verify actual propagation."
    current: yes
    sources:
      - "git log --follow tests/Encina.DataAnnotations.PropertyTests/DataAnnotationsValidationBehaviorPropertyTests.cs, run 2026-09-25; paraphrase: file history ends at commit d7b7b8ac 'Add unit tests for domain modeling components'"
      - "tests/Encina.PropertyTests/Validation/DataAnnotations/ValidationInvariantProperties.cs:394-401, read 2026-09-25; quote: 'DataAnnotations validators do not have access to IRequestContext metadata... cannot verify actual propagation'"
    destinations:
      - kind: backlog
        target: "artifacts/issues/10-dataannotations-context-propagation-and-di-gaps.md (draft, not yet opened)"
        status: planned
        batch: "SPEC-003 audit of #10"
knowledge_unverified: []
audit:
  checklist: 1
  date: 2026-09-25
  verdict: findings-tracked
  record: docs/knowledge/audits/issue-10.md
remediation: []
review: verified
```

## Where the knowledge lives (record)

- The specific scenario (CustomValidationAttribute + context enrichment) no longer exists in the codebase: the file was replaced wholesale by test consolidation (commit `d7b7b8ac`) with `tests/Encina.PropertyTests/Validation/DataAnnotations/ValidationInvariantProperties.cs`, which tests a narrower claim (validation still succeeds with context present) and explicitly disclaims verifying propagation.
- No rule, ADR or checklist entry records the "reflection needs public types" lesson; it is narrow enough that it is tracked as a test gap rather than promoted (see Audit).

## Audit result (docs/knowledge/audits/issue-10.md)

---
issue: 10
checklist_version: 1
date: 2026-09-25
verdict: findings-tracked
---

# Audit of issue #10

Scope: issue #10 touched exactly one file, `tests/Encina.DataAnnotations.PropertyTests/DataAnnotationsValidationBehaviorPropertyTests.cs` (fixed by commit `3f20fec8f4ebbe98317be9ec02e8b9755ed19dc2`, "Fixes #7", which the closing comment on #10 also credits for this issue). That file was deleted in the test-consolidation commit `d7b7b8ac` and replaced by `tests/Encina.PropertyTests/Validation/DataAnnotations/ValidationInvariantProperties.cs` (confirmed with `git log --follow`). The successor file is the audit unit for AUD-01, AUD-04, AUD-05, AUD-06. Because the successor tests exercise `src/Encina.DataAnnotations/DataAnnotationsValidationProvider.cs` and `ServiceCollectionExtensions.cs`, and the package's own guard-test and DI-completeness gaps surfaced directly while auditing those tests, this audit also covers those two production files and the sibling `Encina.UnitTests/DataAnnotations/*.cs`, `.github/coverage-manifest/Encina.DataAnnotations.json`, and `src/Encina.DataAnnotations/README.md`. No `src/` file was changed by issue #10 itself — this is a test-only issue.

| ID | Outcome | Evidence |
|---|---|---|
| AUD-01 | **Finding (major, Class B)** | The decision issue #10 recorded — a type used by `CustomValidationAttribute` must be public for reflection to resolve it — has no test trace today. `Grep "CustomValidation"` across the worktree returns one hit, in `docs/engineering/PROJECT-HISTORY.md`; no test file uses the attribute. The successor tests (`ValidationInvariantProperties.cs:403-456`) only assert `result.IsValid`, not that `ValidationContext.Items` was actually read by a custom validator — the exact mechanism #10 exercised. No ADR, issue, or comment records this as a deliberate descope; it happened as a side effect of the test-consolidation rewrite (`d7b7b8ac`). Tracked in `10-dataannotations-context-propagation-and-di-gaps.md`. |
| AUD-02 | N/A | Issue #10 touched no entity, store, pipeline behavior, background service, or external integration; it fixed test-type visibility only. |
| AUD-03 | **Finding (major)** | `.github/coverage-manifest/Encina.DataAnnotations.json` lists exactly the 3 files that exist under `src/Encina.DataAnnotations/` (verified by glob: `DataAnnotationsValidationProvider.cs`, `GlobalSuppressions.cs`, `ServiceCollectionExtensions.cs`) — no misdeclared file. Measured coverage: unit 90.9%/100% (target 80%, pass); guard flag declares a 25% target but has zero source files under `tests/Encina.GuardTests/` for this package, so it is unmeasurable (finding, shared with FluentValidation and MiniValidator, tracked in `10-guard-tests-validation-providers.md`). Property flag is not declared in the manifest at all, though `tests/Encina.PropertyTests/Validation/DataAnnotations/ValidationInvariantProperties.cs` exists and reaches 100%/100% on both files when run in isolation (`dotnet test tests/Encina.PropertyTests --filter "FullyQualifiedName~Validation.DataAnnotations" --collect "XPlat Code Coverage"`, 2026-09-25: 20/20 passed, cobertura line-rate 1.0 for both classes) — a manifest gap, not a code defect; folded into the guard-tests remediation issue rather than a third issue since it is a one-line manifest addition, and the maintainer should decide the target percentage. |
| AUD-04 | Pass | `ValidationInvariantProperties.cs` instantiates `DataAnnotationsValidationProvider` through DI (`CreateProvider()`, lines 60-66) and calls `ValidateAsync` for every property; no reflection-only or type-only assertions. Confirmed by the coverage run above (100% line coverage from the property tests alone proves real code execution, not reflection). |
| AUD-05 | **Finding (major, shared with AUD-03)** | Guard Clause Tests required (CLAUDE.md: "All public methods... never accepted with justification"); none exist for this package's public methods and constructor. No `.md` justification file exists either (and would not be accepted). Unit, Property and Contract-adjacent categories are otherwise covered (Unit and Property tests exist and pass; this package is not provider-dependent so Integration is not required). |
| AUD-06 | N/A | Issue type is `debt`, not `bug`; the tests were flaky/broken, not a production defect, so AUD-06 (regression test for a fixed bug) does not apply. |
| AUD-07 | N/A | `Encina.DataAnnotations` is not a provider-dependent feature under the 10-database, caching, transport or lock categories; it is one of the 3 validation providers, and this issue touched only one of the three (no cross-provider claim was made). |
| AUD-08 | N/A | Neither production file logs; no `[LoggerMessage]` or `LoggerMessage.Define` in scope. |
| AUD-09 | Pass | `src/Encina.DataAnnotations/PublicAPI.Unshipped.txt` lists `DataAnnotationsValidationProvider`, its constructor, `ValidateAsync<TRequest>`, and `AddDataAnnotationsValidation`, matching the two production files exactly (verified by `adversarial-reviewer`, 2026-09-25). |
| AUD-10 | Pass | Both production files carry XML doc comments on every public member (`DataAnnotationsValidationProvider.cs:7-18`, `ServiceCollectionExtensions.cs:7-66`). |
| AUD-11 | Pass (once per package) | `src/Encina.DataAnnotations/README.md` exists, names real API (`AddDataAnnotationsValidation`, `DataAnnotationsValidationProvider`), states it has zero external dependencies, and its "Context-Aware Validation" example matches the provider's actual `ValidationContext.Items` population — read in full 2026-09-25 in place of `docs-reviewer` (not yet in the issue-worker spawn allowlist). One accuracy caveat: the README advertises context enrichment as tested ("🧪 Fully Tested") but no test proves the context-propagation half of it (see AUD-01 finding) — noted in the remediation issue rather than as a separate doc finding, since the fix is a test, not a doc edit. |
| AUD-12 | N/A | Not a security, compliance, audit or personal-data unit. |
| AUD-13 | Pass | Neither production file logs, traces or stores `EncinaError.Message` or an exception message; `ValidationError` messages come from `ValidationResult.ErrorMessage`, which is validation-attribute text (e.g. "Email is required"), not a leaked internal error. |
| AUD-14 | Pass | `Grep "DateTime.UtcNow\|DateTimeOffset.UtcNow"` across both production files: no match. |
| AUD-15 | N/A | No options class with a secret, password, connection string or key in scope. |
| AUD-16 | N/A | No database call in scope. |
| AUD-17 | **Finding (minor)** | `AddDataAnnotationsValidation` registers 3 services (`TryAddSingleton<IValidationProvider,...>`, `TryAddSingleton<ValidationOrchestrator>`, `TryAddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>))`) but no test builds the provider with `ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }`. `Grep "ValidateOnBuild"` across `tests/` returns 30 files, none under `DataAnnotations`. Low severity: the four existing `GetRequiredService` calls in `ServiceCollectionExtensionsTests.cs` already exercise the same resolution paths a `ValidateOnBuild` pass would check, and there are no constructor dependencies beyond what the collection itself provides. Tracked in `10-dataannotations-context-propagation-and-di-gaps.md`. |
| AUD-18 | Pass | `Grep "\[Obsolete\]"` across both production files: no match. |

## Test review (in place of a dedicated test-auditor)

- **Coverage per flag** (measured, `dotnet test ... --collect "XPlat Code Coverage"`, 2026-09-25, results under `artifacts/audit/coverage/{unit,property,guard}`): unit 90.9%/100% vs 80% target (pass); property 100%/100%, no manifest target declared (see AUD-03); guard 0% vs 25% target, unmeasurable — no source files (finding).
- **Missing test types**: Guard Clause Tests (finding, AUD-05). Integration and Contract are not required for this non-provider, non-public-API-surface package category; no justification file needed since neither is in the "always required" set for a non-database feature per CLAUDE.md's test-type table.
- **Regression test for the fixed defect**: not applicable per AUD-06 (type is `debt`, tests were broken not the production code).
- **Test quality**: `dotnet test tests/Encina.PropertyTests/Encina.PropertyTests.csproj --filter "FullyQualifiedName~Validation.DataAnnotations"` → 20/20 passed, 2026-09-25; no reflection-only assertions, no `Thread.Sleep`, AAA pattern followed, deterministic generators (`Gen.Elements`, `Gen.Choose`). One quality gap: the three context-metadata property tests (lines 403-456) assert a weaker claim (`IsValid` only) than their names imply ("ContextWithUserId... ValidationStillSucceeds" reads as if it tests propagation); this is the same finding as AUD-01, not a separate defect.

## Specialists run

- `adversarial-reviewer` (Sonnet), foreground, 2026-09-25: reviewed the successor test file, both production files, and `Encina.UnitTests/DataAnnotations/*.cs` against CLAUDE.md and the SPEC-003 checklist; findings above merge its report (AUD-01 major, AUD-03/05 major shared across 3 packages, AUD-17 minor, plus a minor note on the untested `CancellationToken` parameter, folded into the same remediation issue as AUD-17). Verdict: "merge after fixes" (none blocker-level).
- `docs-reviewer`: not run as a separate agent (not yet in the issue-worker spawn allowlist per the brief); performed the same check manually under AUD-11 above.
- Test review: performed by the coordinating worker as described above (SPEC-003 §15.4 allows this until a dedicated `test-auditor` agent exists).

## Remediation

- `artifacts/issues/10-guard-tests-validation-providers.md` — [TEST], groups the guard-test/manifest gap across Encina.DataAnnotations, Encina.FluentValidation and Encina.MiniValidator (AUD-03, AUD-05).
- `artifacts/issues/10-dataannotations-context-propagation-and-di-gaps.md` — [TEST], groups the context-propagation test gap (AUD-01), the DI ValidateOnBuild gap (AUD-17) and the untested cancellation-token behavior, all specific to Encina.DataAnnotations.
- No open issue found tracking either gap (`gh issue list --state open --search ...`, 2026-09-25); both are new drafts, not yet opened by the orchestrator.



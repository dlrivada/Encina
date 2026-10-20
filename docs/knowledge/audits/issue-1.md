---
issue: 1
unit: core (src/Encina/Core, src/Encina/Dispatchers)
checklist: 1
date: 2026-09-24
files_in_scope:
  - src/Encina/Core/ServiceCollectionExtensions.cs
  - src/Encina/Dispatchers/MediatorAssemblyScanner.cs
verdict: findings-tracked
---

# Audit — issue #1

Scope: the two files changed by the closing commit (c993620085f2077c81b6e1b176bf7a3b20396aaa), both still at their original paths (`git log --follow` shows no rename since #1). The third file the commit touched, `tests/Encina.Tests/StreamPipelineBehaviorTests.cs`, no longer exists as a standalone file after test consolidation (65826302); its scenarios are not reproduced under `tests/Encina.UnitTests/` today (see AUD-06).

## Coverage per flag (manifest: unit 70, guard 20 — `.github/coverage-manifest/Encina.json:200-207,305-312`)

Measured `dotnet test --filter` runs, this session, against the two files in scope (results: `artifacts/audit/coverage/unit/…/coverage.cobertura.xml`, `artifacts/audit/coverage/guard/…/coverage.cobertura.xml`):

| File | Unit line-rate | Guard line-rate | Target | Verdict |
|---|---|---|---|---|
| Core/ServiceCollectionExtensions.cs | 79.4% | 85.3% | 70 / 20 | pass |
| Dispatchers/MediatorAssemblyScanner.cs (EncinaAssemblyScanner) | 74.0% | 88.0% | 70 / 20 | pass, but see AUD-06 — line coverage passes without the stream-discovery branch ever firing |

Package-level unit/guard/contract coverage for the whole `Encina` core package was already measured by the SPEC-003 pilot 2 (2026-09-24): unit 90.8/70, guard 48.2/20, contract 85.9/15 — all above target; not re-measured here.

## Checklist

| AUD | Outcome | Evidence |
|---|---|---|
| AUD-01 | pass | Decision still implemented: `MediatorAssemblyScanner.cs:72-81` (`IStreamRequestHandler<,>`/`IStreamPipelineBehavior<,>` branches of `ProcessInterface`) and `ServiceCollectionExtensions.cs:82-83` (`RegisterStreamHandlers`/`RegisterStreamPipelineBehaviors` called from `AddEncina`, line 41-91). No later ADR/SPEC/issue reverses it. |
| AUD-02 | n/a | The unit's cross-cutting evaluation belongs to the whole `core` audit unit, already run by SPEC-003 pilot 2 (2026-09-24); not repeated per issue (§2.2). |
| AUD-03 | pass | Both files have manifest entries at `.github/coverage-manifest/Encina.json:200,305` matching their real paths; targets met per the table above. |
| AUD-04 | pass | `tests/Encina.GuardTests/Core/Dispatchers/MediatorAssemblyScannerGuardTests.cs` and `tests/Encina.UnitTests/Core/AmbientRequestContextTests.cs` instantiate and call real scanner/DI code (`EncinaAssemblyScanner.GetRegistrations`, `services.AddEncina()`); none are reflection-only or type-only assertions. |
| AUD-05 | pass | Manifest requires unit+guard only for these two files; both exist. Contract/property not required by the manifest for these files. |
| AUD-06 | **finding — major** | No test drives `EncinaAssemblyScanner.GetRegistrations`/`AddEncina` to *discover* a scanned `IStreamRequestHandler<,>` or `IStreamPipelineBehavior<,>` implementation — the exact scenario issue #1 was about. Verified two ways: (1) `MediatorAssemblyScanner.cs:74` (`result.StreamHandlers.Add(...)`) and `:80` (`AddWithOpenGenericFallback(result.StreamPipelines, ...)`) show **0 hits** in the Cobertura report from this session's unit-test run (`artifacts/audit/coverage/unit/…/coverage.cobertura.xml`); (2) the only test file using these interfaces, `tests/Encina.UnitTests/Core/AmbientRequestContextTests.cs:128,138-139`, registers the handler/behavior manually with `services.AddScoped(...)` after calling `services.AddEncina()` with no assembly argument, so `AddEncina`'s default scan (`ServiceCollectionExtensions.cs:50-53`) only scans the core `Encina` assembly, never the assembly holding the test handler. `tests/Encina.ContractTests/Core/HandlerRegistrationContracts.cs` has the equivalent discovery contracts for `IRequestHandler<,>`/`INotificationHandler<>` but none for stream types. Confirmed independently by `adversarial-reviewer` (2026-09-24), which also checked `NestedDispatchContextTests.cs` and `EncinaExplicitContextContractTests.cs` and found the same manual-registration pattern there. Tracked as an addition to open issue #1317 (see Remediation). |
| AUD-07 | n/a | Provider-agnostic core dispatch code; no database/cache/lock/validation/cloud provider involved. |
| AUD-08 | n/a | Neither file logs (`[LoggerMessage]`/`LoggerMessage.Define`); grep for `LoggerMessage`/`ILogger` in both files: no match. |
| AUD-09 | pass | `AddEncina`, `AddApplicationMessaging` declared in `src/Encina/PublicAPI.Unshipped.txt:151-154`; `EncinaAssemblyScanner` is `internal`, not part of the public surface. |
| AUD-10 | pass | Public `AddEncina`/`AddApplicationMessaging` members carry `///` XML doc comments (`ServiceCollectionExtensions.cs:13-40`); `EncinaAssemblyScanner` is internal, XML docs not required. |
| AUD-11 | n/a (package-level, not repeated per issue) | Already in pilot 2's scope; the only doc-accuracy defect found there for this file family ("stale doc wording") is tracked by #1318. |
| AUD-12 | n/a | Not a security/compliance/audit/personal-data unit. |
| AUD-13 | n/a | Neither file constructs an `EncinaError`, logs, or sets an activity/metric tag; grep for `EncinaError`/`.Message` in both files: no match. |
| AUD-14 | pass | Grep for `DateTime.UtcNow`/`DateTimeOffset.UtcNow` in both files: no match. |
| AUD-15 | n/a | No options classes with secrets in this scope. |
| AUD-16 | n/a | Not database-provider code. |
| AUD-17 | pass (with a known package-level gap) | `AddEncina` registers `IEncina`, `IRequestContextAccessor`, `IEncinaMetrics`, `IFunctionalFailureDetector`, `IModuleHandlerRegistry` and all scanned handlers/behaviors; this issue's own two lines add no new unresolved dependency. The absence of a `ValidateOnBuild`/`ValidateScopes` test for `AddEncina()` as a whole is a pre-existing, already-tracked gap: open issue #1317. |
| AUD-18 | **finding — minor, already tracked** | `AddApplicationMessaging` (`ServiceCollectionExtensions.cs:13-23`) is a "Legacy alias for AddEncina" — CLAUDE.md forbids legacy/compatibility code. Matches the scope of open issue #1318 ("Core pipeline: legacy compatibility alias, misdeclared coverage manifest entry, stale doc wording"); confirmed by `adversarial-reviewer`. No new issue drafted. |

## Specialist passes (brief §Method step 4)

- **adversarial-reviewer** (foreground, 2026-09-24): confirmed AUD-06 independently with additional evidence (`HandlerRegistrationContracts.cs` has the equivalent contract for regular handlers but none for stream types; `NestedDispatchContextTests.cs` and `EncinaExplicitContextContractTests.cs` show the same manual-registration pattern); confirmed AUD-18 matches #1318's existing scope; confirmed the CLAUDE.md #1309 rules (registration completeness beyond AUD-17, errors never swallowed, fail-closed gates, no `EncinaError.Message` leaks) are not applicable to this scope (pure DI/reflection wiring, no error path, no security gate). Verdict: "merge after fixes" — add a contract/unit test proving `AddEncina(assembly)` discovers a real `IStreamRequestHandler<,>`/`IStreamPipelineBehavior<,>` from the scanned assembly.
- **docs-reviewer**: skipped. This is a bug fix to internal DI-registration code with no dedicated documentation page or README section describing stream-handler assembly scanning specifically; the package README's registration guidance is unaffected by this fix, and the only doc-accuracy defect found for this file family (#1318) is already tracked.

## Remediation

- AUD-06 (major): add to open issue **#1317** ("[TEST] No ValidateOnBuild/ValidateScopes test proves Core.AddEncina() registers a resolvable service graph") as an additional checklist line: a contract test (in `tests/Encina.ContractTests/Core/HandlerRegistrationContracts.cs`, mirroring its existing `IRequestHandler<,>`/`INotificationHandler<>` cases) that defines a real `IStreamRequestHandler<,>` and `IStreamPipelineBehavior<,>` in the test assembly, calls `AddEncina(typeof(...).Assembly)`, and asserts both are resolvable — closing the 0-hit lines at `MediatorAssemblyScanner.cs:74,80`. Same audit unit (core) and template type (`[TEST]`) as #1317, so no new issue drafted (SPEC-003 §5.4 grouping); a worker cannot comment on issues, so this line is for the orchestrator to add.
- AUD-18 (minor): already fully covered by open issue **#1318**; no action needed here.

Deduplication check performed: `gh issue list --repo dlrivada/Encina --state open --search "stream handler registration"`, `"StreamRequestHandler assembly scan"`, `"IStreamPipelineBehavior"` — no existing issue covers the AUD-06 gap specifically.

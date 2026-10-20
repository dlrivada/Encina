---
title: "Review log: one request identity (#1705)"
nav_exclude: true
---

# Review log: one request identity (#1705)

Purpose: the review resolution tables and review logs of the #1705 implementation plan, kept apart from the plan so the plan follows the sections of the plan prompt. Back to the plan: [security-context-population-implementation-plan-1705.md](../security-context-population-implementation-plan-1705.md).

## Review resolution (2026-10-05)

Source: `artifacts/plan-review-1705/findings.json` (63 findings: 1 blocker, 37 major, 25 minor as listed there). Every finding was re-verified against the plan text and the code (Select-String over the rebased worktree, origin/main 10ac10b4) because part of the refutation was cut by the usage limit. Result: 60 applied, 3 applied in part (rows 30, 38, 56; rejected parts stated). None rejected whole. The PR #1775 review then reopened rows 3, 45, 53 and tightened row 14; see the "PR review (2026-10-05)" sub-table below. "Where" names the plan section changed.

| # | Sev | Finding | Result | Where |
|---|---|---|---|---|
| 1 | major | Deleting `PolicyChangeActorScope` breaks knowledge-records; record 1677 missing | Applied (verified `1677.md:46,75`) | Phase 4 task 4, Phase 7 task 5 |
| 2 | major | Seeding system-actor leftovers (9096, `IsSystem`, docs, fragment) | Applied | Design 3 PAP bullet, Phase 4 tasks 3, 5, 6, Documentation |
| 3 | major | Existing PAP and seeding unit suites missing; integration suites do not exist | Reopened by PR review M1: the first check was wrong (searched the class name); the PAP unit and integration suites are all listed now | Phase 4 task 9, Testing |
| 4 | major | Existing Security.Audit `ExcludeSystemAccess` tests and `ReadAuditOptions` docs missing | Applied | Phase 5 tasks 9, 11 |
| 5 | major | Phase 3 skips existing AspNetCore tests and the XML sample | Applied | Phase 3 tasks 2, 8 |
| 6 | major | `security-authorization.md` has 9 affected sections | Applied | Documentation (full revision) |
| 7 | major | EF interceptors keep a second channel | Applied (fallback removed, doc rewritten) | Summary, Phase 3 task 6, Consumer changes |
| 8 | minor | ABAC attributes derive from `SecurityAttribute`; reference cannot go | Applied (task 7 inverted: reference stays) | Design 5, Design 6, Phase 4 task 7 |
| 9 | minor | ABAC error message, XML docs and observability page still say "security context" | Applied | Design 6, Phase 4 tasks 1, 6, Documentation |
| 10 | minor | FsCheck property `WithUserIdCreatesNewContext` and wrong references | Applied | Phase 1 task 9 |
| 11 | minor | Other pages and the `IRequestPreProcessor` sample | Applied | Phase 1 task 5, Phase 7 task 7, Documentation |
| 12 | minor | PublicAPI removals are in Unshipped; ABAC ctor line | Applied | Phase 1 task 11, Phase 3 task 7, Phase 4 task 8, Phase 5 prompt |
| 13 | major | `UserId` independent interface member | Applied (extension property) | Design 1, Summary |
| 14 | major | Public unlogged identity creation bypasses declarations | Applied, tightened by PR review M3 (identity only through the scope factory; factory and `CreateAt` internal; explicit-context rule logs 165) | Design 1, Design 6 |
| 15 | major | `AuthorizationPipelineBehavior` keeps `IPrincipalResolver` | Applied (option a: deleted, Blazor handler pulled in) | Design 2, Phase 3 tasks 3-4 |
| 16 | major | F1/F4 conflict with SPEC-002 REQ-015/DEC-011/P-50 | Applied (cited; F1 narrowed; F4 dropped; persisted form and `BeginRestored`) | Header, Design 1, Design 3, Design 8 |
| 17 | major | #751 plan premise false; order decision ignored | Applied | Design 5, Design 7, Phase 7 task 4 |
| 18 | major | Remnants of system actor and "security context" wording | Applied; `abac.missing_context` renamed `abac.unauthenticated_caller` on the merits | Design 6, Phase 4 |
| 19 | minor | Documentation list misses pages | Applied | Documentation, Phase 7 task 8 |
| 20 | minor | Stale references; ABAC to Security reference; missing test files | Applied (rebased on #1720; refs refreshed) | Phase 4, Dependencies |
| 21 | blocker | Service identities turn the DSR restriction gate into an allow | Applied (fix pulled into #1705; F7 removed) | Design 6, Phase 5 task 12, Phase 6 task 2 |
| 22 | major | Public unlogged ways to mint or swap identity | Applied (four rules a-d) | Design 1, Design 3, Design 6 |
| 23 | major | Ambient identity outlives scope (plain AsyncLocal) | Applied (holder pattern, tests) | Design 2, Phase 2, Testing |
| 24 | major | Out-of-order disposal resurrects an identity | Applied | Design 2, Design 3 rules, Phase 2 |
| 25 | major | Any code can open the built-in seeding identity | Applied (`encina.` reserved, internal `BeginBuiltIn`, actor kind) | Design 3, Phase 4 |
| 26 | major | Authorization authorizes principals mapped to Anonymous | Applied | Design 2, Design 6, Phase 3 task 9 |
| 27 | major | Scopes open inside anonymous inbound requests | Applied (internal typed origin flag, refusal, opt-in) | Design 3 rules, Phase 2 |
| 28 | major | Tenant binding unspecified | Applied (`tenant_conflict`, never inherit) | Design 3 rules |
| 29 | major | Several `ClaimsIdentity` instances | Applied (authenticated only; conflict maps to Anonymous, EventId 164) | Design 4 |
| 30 | major | Security gate "deny anonymous" not true for empty lists / unknown attributes | Applied in part: anonymous pre-check and constructor guards applied; "deny unknown attribute types" rejected for authenticated callers because ABAC attributes derive from `SecurityAttribute` and are enforced by the PEP (the pre-check already closes the anonymous hole) | Design 6, Phase 5 task 2 |
| 31 | major | User id still reaches tags, logs, `ToString` | Applied | Design 4, Phase 1 task 6, Phase 5 task 2 |
| 32 | major | `UserId` not enforced as projection | Applied (same change as 13) | Design 1 |
| 33 | minor | Reserved `service:` check case and whitespace | Applied | Design 1, Design 4 |
| 34 | minor | `Disabled` mode not logged | Applied (EventId 9085, reusing #751's planned id; 9098 dropped by the PR review) | Design 6, Phase 4 task 6 |
| 35 | major | Security behavior keeps logging and tagging the user id | Applied | Phase 5 task 2 |
| 36 | major | Dispatch-activity identity tag has no task or test | Applied | Phase 1 task 7, Testing, Matrix #2 |
| 37 | major | Tenant and metadata behavior of scopes undefined | Applied | Design 3 rules, Testing |
| 38 | major | Non-HTTP entry-point coverage incomplete (gRPC, GraphQL claim, dispatchers) | Applied in part: entry-point table, corrected GraphQL/gRPC classification and dispatcher list added; dedicated gRPC/GraphQL TestServer tests replaced by the stated justification (both call `IEncina.Send` inside the HTTP pipeline) | Summary entry-point table, Design 2 |
| 39 | major | `IPrincipalResolver` remains a third channel; Blazor drift | Applied (same change as 15) | Design 2, Phase 3 |
| 40 | major | `AddEncinaAuthorization` does not register the behavior | Applied (verified `ServiceCollectionExtensions.cs:164-196`) | Summary, Design 5, Phase 3 task 3 |
| 41 | minor | PAP `actor` metadata and test | Applied | Phase 4 tasks 3, 9 |
| 42 | minor | EventId 9096 stale | Applied (repurposed) | Phase 4 task 5, EventId table |
| 43 | minor | Matrix #10 claims one tenant claim map | Applied | Matrix #10, Design 4, F5/F3 |
| 44 | minor | DI tests do not cover seeding chain; uniqueness validator | Applied | Design 3, Phase 4 task 9 |
| 45 | minor | Integration suites do not exist; Audit not in targets | Audit targets applied; the "no suite" premise was wrong (PR review M1): `PersistentPapScopeScenario` and its three SQL Server users exist and are rewritten | Summary, Phase 4 task 9, Testing |
| 46 | minor | More `security-authorization.md` sections | Applied (same as 6) | Documentation |
| 47 | minor | `RequestContext.Create(string)` reads `UtcNow` | Applied (deleted, `TenantResolutionMiddleware` on `TimeProvider`) | Summary, Phase 1 task 6, Phase 3 task 5 |
| 48 | major | Consumer tests stubbing `UserId` missing | Applied (checkable `UserId.Returns` acceptance) | Phase 1 task 10, Phase 4 task 9, Phase 5 task 11, Testing |
| 49 | major | Per-flag coverage skips packages; command gives no per-flag data | Applied | Testing, Verification commands |
| 50 | major | Encina.Security contract flag has no planned test | Applied | Testing (Contract row) |
| 51 | major | PAP `actor` metadata replacement unspecified | Applied | Design 3, Phase 4 task 9 |
| 52 | major | No test proves ids and claims stay out of logs/tags | Applied (sentinel-value tests) | Testing rules |
| 53 | major | Integration row relies on suites that do not exist | Corrected by PR review M1: the existing shared-fixture scenario is extended instead of a new class; justification file kept | Testing (Integration row) |
| 54 | major | CRAP gate has no runnable command | Applied | Verification commands |
| 55 | minor | Coverage-manifest upkeep | Applied | Phase 6 task 5 |
| 56 | minor | PublicAPI steps point at wrong files | Applied in part: Unshipped targets and ABAC line applied; the claim that `Encina.Testing.FsCheck` is untracked is not fully accurate (it has `PublicAPI.Shipped.txt`), so the step is "verify the csproj, edit only if tracked" | Phase 1 task 11 |
| 57 | minor | `.security` changelog fragment | Applied | Documentation |
| 58 | minor | ADR-035 reservation row | Applied (row committed; branch rebased) | Header, Phase 7 task 2 |
| 59 | minor | "Every implementation" contract test unreachable | Applied | Testing (Contract row) |
| 60 | minor | Property domain vacuous; Arb cannot produce services | Applied | Testing (Property row) |
| 61 | minor | Load justification ignores the static AsyncLocal | Applied | Testing (Load row), Phase 3 task 9 |
| 62 | minor | No mutation coverage for `Identity/` | Applied as an orchestrator step (hot spot) | Design 8 |
| 63 | minor | TimeProvider and production `CreateForTest` acceptances not checkable | Applied | Verification commands sweeps, Phase 1 task 6 |

### PR review (2026-10-05)

Source: `artifacts/pr-review/1775.md` (verdict merge after fixes; 5 major, 6 minor, 5 nit). Inventory greps were re-run after rebasing on 1deeaada: `UserId.Returns` 67 sites in 44 files, `ISecurityContextAccessor` in 11 test files, `Substitute.For<IRequestContext>` 397 calls in 176 files, `CreateForTest(... userId ...)` 55 of 344, `IRequestContext? requestContext = null` 31 in 30 files.

| Id | Sev | Finding | Result | Where |
|---|---|---|---|---|
| M1 | major | Three PAP integration suites and `PersistentPolicyAdministrationPointScopeTests` exist and are missing | Applied; rows 3, 45, 53 corrected; the new parallel integration class is dropped, the existing scenario is extended | Summary, Phase 4 task 9, Testing (Integration row) |
| M2 | major | Repositories take `IRequestContext` by constructor (second channel) | Applied (31 parameters removed, ambient accessor, `ExcludeSystemAccess` on ambient identity) | Phase 5 task 14, Consumer changes |
| M3 | major | Public unlogged mint path (factory `Create` + `CreateAt` + explicit `Send`) | Applied (identity only through the scope factory; `IRequestIdentityFactory` internal; `CreateAt` internal; `BeginInbound`; logged explicit-context rule; architecture test) | Summary amendments, Design 1, 2, 4, Phases 1-3 |
| M4 | major | `BeginRestored` trusts persisted roles and permissions | Applied (`PersistedRequestIdentity` = SPEC-002 REQ-015 fields with causation id; restored User has no roles or permissions; note for #1164) | Design 1 |
| M5 | major | User id still in log 1702 and `SecurityErrors` details | Applied (tasks and sentinel tests) | Phase 5 task 15 |
| m1 | minor | 9098 duplicates #751's 9085 | Applied (9085, #1705 lands first, #751 reuses) | Design 6, Phase 4, EventId table |
| m2 | minor | Unrunnable `Select-String -Recurse`; `UserIdClaimType` sweep and nonexistent `samples` | Applied | Phase 4 task 9, Phase 5 task 13, Phase 7 task 8, Verification commands |
| m3 | minor | Stale `abac.missing_context` text; Phase 3 prompt "restore in finally" | Applied | Testing, Phase 3 prompt |
| m4 | minor | `InternalsVisibleTo` for `Encina.Testing.FsCheck`; unnecessary ContractTests reference | Applied | Summary amendments, Testing (Contract row) |
| m5 | minor | Origin marker is a spoofable metadata string | Applied (internal typed `RequestOrigin`; SignalR note kept for F2 and the how-to) | Summary amendments, Design 3, Phase 1 task 6 |
| m6 | minor | EventIds 164-167 left empty after Phase 1 | Applied (162-165 Phase 1, 166-171 Phase 2) | Phase 1 task 7, Phase 2 task 8, EventId table |
| n1 | nit | "61 applied, 2 in part" | Applied (60 / 3) | Review resolution intro |
| n2 | nit | Counts drifted | Applied (67 in 44; 11 files; 397 in 176) | Design 1, Phase 1 task 10 |
| n3 | nit | Phase 1 breaks middleware callers before Phase 3 | Applied (minimal compile fix in Phase 1) | Phase 1 task 6 |
| n4 | nit | `Resolve` signature change | Applied (`Either<EncinaError, IRequestContext>`, callers map the `Left`) | Design 1 |
| n5 | nit | Plan file name still says "security-context" | Applied by decision: kept; ADR-035 cites it as its plan | Phase 7 task 2 |

---

## Review log (Phase 2 scope shape)

Sources, all from PR [#1849](https://github.com/dlrivada/Encina/pull/1849) (the design record; body not rewritten, one header note added 2026-10-09): section 7 of [`docs/plans/request-identity-phase2-scope-shape-1705.md`](../request-identity-phase2-scope-shape-1705.md) (25 plan edits, "S7-n"), the pr-reviewer review `artifacts/pr-review/1849.md` (F1-F11) and the adversarial review `artifacts/pr-review/1849-adversarial.md` (majors A-M1 to A-M4, minors A-m1 to A-m12), plus the maintainer decisions on #1705 of 2026-10-05. Every `src/` citation was re-checked on `origin/main` 61d5dc3d; the plan and the Phase 1 files are unchanged since `c3626ed`, so the section 7 line numbers held. The historical review tables above keep the `Begin*` names they were written with; M6 supersedes them.

### Section 7 edits

| Item | Edit | Where |
|---|---|---|
| S7-1 | M6 (scope shape) amendment row | Summary amendments |
| S7-2 | M3: `RunInboundAsync`, issuer mechanism, no-issuer refusal, seam builds only | Summary amendments (M3) |
| S7-3 | m5: connection flows start with no context (superseded in part by the decision on A-m6, confirmed by the maintainer as MQ-2 on 2026-10-05: the marker requires `AllowOverInbound`) | Summary amendments (m5) |
| S7-4 | #1705 item (3) names the delegate API, "invalidate when the work completes" | Summary |
| S7-5 | Entry-point rows: connection flows, `RunRestoredAsync` | Entry-point table |
| S7-6 | Design 1: `RunRestoredAsync` signature, issuer, per-token claims, `tenant_conflict`, setter, builders | Design 1 |
| S7-7 | Middleware flow: skip, `RunInboundAsync`, no `finally`, `Left` → 500, ordering | Design 2 |
| S7-8 | Accessor lifetime: frame restore, LIFO by construction, 170, connection flows | Design 2 |
| S7-9 | Blazor: per-activity `RunInboundAsync`, `Left`, refresh, outside-activity reads | Design 2 |
| S7-10 | SignalR: mechanism cited, streaming hub methods in F2 | Design 2, Design 8 (F2) |
| S7-11 | Option B row reworded | Design 3 |
| S7-12 | API block replaced; `RequestContextScope` deleted; `IdentityScopeOptions` | Design 3 |
| S7-13 | Rules renamed; ending = invalidate + 168; implementation constraint; `unsupported_accessor` | Design 3 |
| S7-14 | Opt-outs name `RunAsServiceAsync` | Design 6 |
| S7-15 | Task 5 files | Phase 2 task 5 |
| S7-16 | Task 6 error codes | Phase 2 task 6 |
| S7-17 | Task 8: 170 renamed, 168 from `finally` | Phase 2 task 8 |
| S7-18 | Task 9: `Pop` → `End`, issuer, comparison, setter, validator, second architecture test | Phase 2 tasks 4, 9, 10 |
| S7-19 | Task 10 tests | Phase 2 task 13, Testing (Unit row) |
| S7-20 | Task 11 resolved; `TestIdentity.Service`/`Principal` builders | Phase 2 task 11 and closing note |
| S7-21 | Phase 2 prompt rewritten | Phase 2 prompt |
| S7-22 | Phase 3 tasks 1 and 4, prompt | Phase 3 tasks 0, 1, 4, prompt |
| S7-23 | WebSocket TestServer tests | Phase 3 task 9 |
| S7-24 | Unit and contract rows | Testing |
| S7-25 | ADR-035 content; combined prompt | Phase 7 task 2, Combined prompt |

### Review findings

| Id | Sev | Finding | Result | Where |
|---|---|---|---|---|
| A-M1 | major | One-line downgrade (`RequestContext = null`, or an anonymous explicit context) bypasses the "over a User / over inbound" refusals | Applied (Q1 in Phase 2): immutable holder origin and kind surviving `Invalidate`; refusals walk the chain including ended holders; `Push`/`SetUnchecked` record the walked facts before dropping an ended parent; setter identity- and origin-preserving, no clear; #1855 absorbed | Design 1 (setter), Design 3 rule 4, Phase 2 task 9, M6 |
| A-M2 | major | Blazor `Left` branch runs under the connection identity; misordered routing silently fail-open | Applied: `next(activity)` inside the internal anonymous masking scope; endpoint null before `next` and non-null after → Critical 202, once | Design 2, Phase 3 tasks 0, 1, 4, 9 |
| A-M3 | major | `HubMetadata` skip misses GraphQL-over-WebSocket and raw WebSocket endpoints | Applied: every WebSocket upgrade request is skipped (`context.WebSockets.IsWebSocketRequest`, works before routing) | Entry-point table, Design 2, Phase 3 task 1 |
| A-M4 | major | Issuer liveness only checked in `Resolve`; an accepted explicit context outlives its issuer | Applied: `ContextHolder.ReadContext()` returns null once the identity's issuer is not live | Design 1 (Issuer), Phase 2 task 9 |
| A-m1 | minor | Event 170 must test `!IsValid()`, not `IsDisposed` | Applied | Design 3 (Ending), Phase 2 task 8 |
| A-m2 | minor | Issuer under-specified; one identity per scope; rule order | Applied (rule order 1-7) | Design 1, Design 3 |
| A-m3 | minor | Per-token list: keep `auth_time`, add `nonce`, `at_hash`, `c_hash`; options passed in; `HasClaim` comparison; validator | Applied (Q2) | Design 1, Design 4, Phase 2 task 4 |
| A-m4 | minor | Tenant setter bypass until F5 | Applied: tenant change only while `!IsDispatchInFlight`; F5 acceptance criterion | Design 1 (setter), Design 8 (F5) |
| A-m5 | minor | `TenantResolutionMiddleware` installs a connection-lifetime tenant | Applied: same skip | Design 2, Phase 3 task 5, Consumer changes |
| A-m6 | minor | The skip removes the inbound guard from hub methods | Applied in Phase 3 (maintainer-confirmed, MQ-2, 2026-10-05): anonymous connection-origin marker; service/principal scopes need `AllowOverInbound`; `RunInboundAsync` permitted | M6, m5, Design 2, Design 3 rule 4, Phase 3 task 0 |
| A-m7 | minor | Stream claim wrong: `Resolve` runs at the first `MoveNextAsync` | Applied (verified `Encina.Stream.cs:37-39`) | Design 3 (Streams), Testing (Unit row) |
| A-m8 | minor | Section 7 misses `IEncina` XML docs, remarks, plan lines, extra test migrations | Applied for `IEncina.cs:53-64,86-90,137-141` (the remark is at `:61-64` on main), `TestIdentity`/`TestRequestContext` remarks, plan lines 145, 348, 505-508, 518, 552, 802 and every `JobContext()` use in `EncinaExplicitContextContractTests.cs` (the review listed `:27,35,57,196`; `:79,103,106,109` were added). **Not applied** for `EncryptionPipelineIntegrationTests.cs:72,373`: verified that they pass the context to `IEncryptionOrchestrator.EncryptAsync`, not to a dispatch, so they are builder use and stay | Phase 2 tasks 11, 12 |
| A-m9 | minor | The seam-drop reason also fits public `RunAsPrincipalAsync` | Applied: ADR-035 statement (public, logged, refused over User/inbound chains, not a security boundary) | Phase 7 task 2 |
| A-m10 | minor | `IsSameAs` compares frozen claims but gates evaluate the live mutable principal | Applied: clone at construction and per read | Design 1, Phase 2 task 4 |
| A-m11 | minor | Encina-owned long-lived loops started inside a scope capture it | Applied: `SuppressFlow` sweep in core; how-to | Design 3, Phase 2 task 9, Documentation |
| A-m12 | nit | Deferred-dispatch sample persists more than the code | Applied: error code only | Design 1 (Persisted form), Design 8, Documentation |
| F1 | major | Identity-preserving setter breaks far more tests than budgeted | Applied: 86 setter lines in 11 files plus issuer-less explicit dispatches listed and budgeted in Phase 2, with what the rewritten setter tests assert | Phase 2 task 12 |
| F2 | major | Routing-order fallback still fails open | Applied, as A-M2 and A-M3 plus MQ-1 (maintainer decision 2026-10-05): WebSockets are skipped before routing, circuits are masked, misordering logs Critical 202; the SSE hole is closed by skipping every `Accept: text/event-stream` request (MQ-1 (c)) and a detected misordering fails closed with 500 on every later request (MQ-1 (b)) | Design 2 steps 0, 1, 5 |
| F3 | major | Blazor sample runs the activity under the connection identity | Applied as A-M2 | Design 2 |
| F4 | minor | SSE mis-described: SSE keeps its GET open like WebSockets | Applied (`HttpConnectionDispatcher.cs:156-158` is the SSE branch; only long polling ends at the first poll) | Design 2 step 1 |
| F5 | minor | Section 7 misses plan lines 76, 167, 192, 265-266, 382, 389, 399, 508, 765, 786, 809, 865, 890, 971, 982, 1000 | Applied: every `Begin*` name outside the historical review tables replaced; 192, 508, 765, 786 and 809 rewritten for meaning | M4, Design 1-3, Design 8, Phase 2 prompt, Consumer changes, Testing, Documentation, Research, Matrix, Dependencies, Decisions |
| F6 | minor | `RequestIdentity.Issuer` construction cycle; identity per open | Applied: internal `IdentityIssuer` created first, passed to the identity, bound once to the holder | Design 1, Phase 2 task 4 |
| F7 | minor | `RequestIdentity.ForService` presented as existing | Applied: "Phase 2 internal" (only `ForUser` exists, `RequestIdentity.cs:166`) | Design 1 model |
| F8 | minor | Samples log the whole outcome; `GetCode()` is `Option<string>` | Applied: `error.GetCode().IfNone("unknown")` (`EncinaErrors.cs:100`); the factory logs 167 itself, so the Blazor handler logs nothing extra | Design 1, Design 2, Design 3 |
| F9 | minor | Cancellation and exception semantics unspecified | Applied: a cancelled token returns `Left(EncinaErrorCodes.RequestCancelled)` before `Push`; exceptions from `work`, `OperationCanceledException` included, propagate after invalidation | Design 3 (rules 2, Results), Testing |
| F10 | minor | Tenant interplay on hub endpoints; no opt-out for cross-tenant operator flows | Applied: tenant middleware skip (A-m5); `tenant_conflict` has no opt-out on purpose (rule 5) | Design 1, Design 2 |
| F11 | minor | PR #1849 body stale | Not applied here: PR #1849 is the design record and is not edited (maintainer process decision); orchestrator item | — |
| N1-N3 | nit | Source path prefixes, Mermaid diagram, second review line | N1 not applicable (the plan cites `aspnetcore` paths by file name); N2 and N3 left to the design record | — |
| Plan | correction | `ExplicitContextConflictTests` row said "logs 169"; the explicit-context conflict is 165 | Applied | Testing (Unit row) |

### Maintainer decisions on MQ-1 and MQ-2 (2026-10-05)

| Id | Question | Decision | Where applied |
|---|---|---|---|
| MQ-1 | A pipeline with `UseEncinaContext` before `UseRouting` cannot see `HubMetadata`, so a SignalR connection over SSE (or the first long poll) keeps the connect-time identity. Log only, block after detection, or skip SSE requests? | **Decided: (b) and (c) together.** (c) `EncinaContextMiddleware` and `TenantResolutionMiddleware` also skip requests that send `Accept: text/event-stream`, under the connection-origin marker, so the SSE connect-time identity hole is closed whatever the pipeline order (an ordinary SSE endpoint also reads Anonymous until it opts in per invocation); (b) after the first detection (Critical 202) the middleware answers 500 to every later request without calling `next` | Entry-point table, M6 connection-flows row, Design 2 steps 0, 1, 5, Phase 3 tasks 1, 5, 9 and prompt, ADR-035 items, Combined prompt, Maintainer decisions item 11 |
| MQ-2 | Does the connection-origin marker make `RunAsServiceAsync` inside a hub method need `AllowOverInbound`? | **Decided: confirmed.** Connection endpoints run under the connection-origin anonymous marker; `RunAsServiceAsync` and `RunAsPrincipalAsync` inside a hub method require `AllowOverInbound` (logged), `RunInboundAsync` per invocation and per activity is permitted (m5 consequence kept) | m5, Design 2, Design 3 rule 4, Phase 3 task 0, Maintainer decisions item 11 |

---

## Review log (PR #1862 adversarial review)

Source: `artifacts/pr-review/1862-adversarial.md` (verdict merge after fixes; 5 major, 10 minor, 4 maintainer decisions), reviewed at plan commit 54a89b73 against `origin/main` ab4ec134, and the maintainer decisions D1-D4 in the last #1705 comment of 2026-10-05. Every `src/` and `tests/` citation added or changed in this round was re-checked with `git show origin/main:<path>` on ab4ec134. The `dotnet/aspnetcore` `release/10.0` citations (`DefaultWebSocketManager.cs`, `WebSocketMiddleware.cs`, `ConnectionEndpointRouteBuilderExtensions.cs`, `WebSocketClient.cs`, `RewriteMiddleware.cs`, `StatusCodePagesExtensions.cs`) are taken from the review and were not re-read here.

| Id | Sev | Finding | Result | Where |
|---|---|---|---|---|
| 1 | major | `context.WebSockets.IsWebSocketRequest` is false until `WebSocketMiddleware` runs; SignalR adds `UseWebSockets` inside its endpoint, so raw and GraphQL WebSockets after `UseEncinaContext` keep the connect-time user; TestServer hides it | Applied: the predicate also reads `IHttpUpgradeFeature.IsUpgradableRequest` + `Upgrade: websocket` and `IHttpExtendedConnectFeature.IsExtendedConnect` + `Protocol` `websocket`; `ConnectionRequestPredicateTests` without `IHttpWebSocketFeature` plus a Kestrel loopback test; the same predicate (private copy, same test table) in `TenantResolutionMiddleware` | Entry-point table, M6 connection-flows row, Design 2 step 1 and tenant bullet, Phase 3 tasks 1, 5, 9, prompt, Testing |
| 2 | major | A benign re-route (`UseRewriter`, `UseStatusCodePagesWithReExecute`) or a 404 trips the 500 latch; a static latch breaks other hosts in the process | Applied as D1: trip only when the endpoint after `next` carries `HubMetadata`; instance field with `Interlocked.Exchange`; test that re-routing and 404 set no latch and that a second host is unaffected | Design 2 step 5, M6 connection-flows row, Phase 3 tasks 1, 9, prompt, ADR-035 item |
| 3 | major | Nested dispatch from a dead flow loses the chain facts because `SetUnchecked` drops non-scope holders through `NearestScope` (`RequestContextAccessor.cs:94,128-129`) | Applied: every new holder inherits the facts of the holder current when it is created, before `LiveOrNull` or `NearestScope` drop anything; tests for nested dispatch and two successive sets | M6 holder-origin row, Design 3 rule 4, Phase 2 tasks 9, 13, prompt, Testing (`HolderFactInheritanceTests`) |
| 4 | major | User ids still logged by `AuthorizationPipelineBehavior` 200/201 and the EF interceptors 3050/3000 | Applied: kind instead of user id (EventIds kept), `["userId"]` error details of the behavior become `["identityKind"]`; added to Phase 3 tasks 3 and 6 and the sentinel list | Design 2 (Authorization bullet), Design 4, Phase 3 tasks 3, 6, 9, prompt, Testing rules, Consumer changes, EventId table, changelog `.security` |
| 5 | major | Ordinary SSE endpoints have no opt-in | Applied as D2: public `HttpContext.CreateInboundRequestInfo()`, `RunInboundAsync` per event or per dispatch, never the whole stream; `InboundRequestInfo` shape and visibility defined (public record, Phase 2) | Entry-point table, Design 2, Design 3 API block, Phase 2 task 5, Phase 3 tasks 9, 11, Documentation |
| 6 | minor | CS0051: public `InvokeAsync`/constructor with an internal parameter type | Applied: `EncinaContextMiddleware` and `RequestIdentityCircuitHandler` become `internal sealed` (`UseMiddleware` activates internal types; resolving from `RequestServices` rejected: hidden from `ValidateOnBuild`, per-request service locator); PublicAPI lines `:24-26` removed; `InternalsVisibleTo` for `Encina.GuardTests` | Design 2, Phase 3 tasks 1, 4, 7, prompt, Consumer changes, changelog `.removed` |
| 7 | minor | Test budget misses 38 + 2 `Push`/`Pop` calls and the issuer-less `WithIdentity` dispatch of `RequestContextPropagationTests.cs:232-242` | Applied: 40 calls budgeted (`AccessorLifetimeTests` 38, `RequestIdentityGuardTests.cs:65-66` 2); the re-list pattern covers `TestRequestContext.WithIdentity(` and names the propagation test | Phase 2 task 12, prompt |
| 8 | minor | Two Phase 3 tests contradict the latch (negotiate trips it first) | Applied: fresh host per misordered case with the connection as first request, or `SkipNegotiation`; the WebSocket case asserts the latch on close (D1) | Phase 3 task 9 |
| 9 | minor | Setter "same origin" undefined with no readable context; setter stores a foreign context without a snapshot | Applied: no readable context → reference Anonymous/`Unspecified`; the setter checks and stores `CopyOf(value)` | Design 1 (setter), Phase 2 tasks 9, 13, prompt |
| 10 | minor | Principal cloning keeps unauthenticated identities; `WindowsIdentity` clone duplicates the handle | Applied as D4 | Design 1 (cloning), Phase 2 tasks 4, 13, prompt, ADR-035 item |
| 11 | minor | Rule 5 skipped on rule 4's accept path; rule 3 reads the ambient, not the chain | Applied: the tenant rule runs on every accepted context; rule 3 reads the holder facts ("chain has a User"); identity rules renumbered 1-6 with a note for older references | Design 1 (rule order), Phase 2 tasks 9, 13, prompt |
| 12 | minor | Missing EventIds; `RequestCancelled` in the middleware; `RunInboundAsync` must not return `Left` for client input | Applied: 172 `InboundScopeOpened`, 173 `ScopeTenantChanged`, 174 `ScopeOpenedOverInbound` (free in `EventIdRanges.Core` on ab4ec134); `RequestCancelled` → no response, no log; client-controlled `InboundRequestInfo` members normalized, never refused | m6 row, Design 2 steps 2 and 4, Design 3 (opening logs, inbound input), Phase 2 tasks 5, 8, 13, Phase 3 tasks 1, 9, EventId table |
| 13 | minor | Per-token exclusion set has no path into identities built outside the factory | Applied: `perTokenClaimTypes` parameter on `ForUser`/`ForService` (null = `DefaultPerTokenClaimTypes`), `IsSameAs` removes the union of both sets | Design 1 model and comparison, Phase 2 tasks 4, 13 |
| 14 | minor | `TenantResolutionMiddleware` skip location ambiguous; a skip at the start bypasses `RequireTenant` and validation | Applied: only the context write (`:117-121`) is skipped; test that the 400 still happens on a connection request | Design 2 (tenant bullet), Phase 3 tasks 5, 9, Consumer changes |
| 15 | nit | Research rows; `:117-121` writes only when a tenant resolves; PR body lists MQ-1/MQ-2 open; `Accept` matching unspecified | Applied: Research rows rewritten (`Resolve :72`, `Enter :176`, `AsyncLocal :47`, no "restore pattern for scopes"); "whenever a tenant resolves"; `Accept` matching specified (parsed media types, wildcards excluded, raw fallback). **Not applied here:** the PR #1862 body is the orchestrator's (this worker does not edit PRs) | Research, Design 2 step 1 and tenant bullet, Phase 3 task 5 |
| Plan | correction | Design 4 said `ConflictingAuthenticatedIdentities` is EventId 168; it is 164 (m6, `RequestIdentityLog.cs`) | Applied | Design 4 |
| DR-1 | minor | docs-reviewer: the `IsSameAs` bullet still pointed at "rule 5" for the tenant | Applied ("the tenant rule") | Design 1 |
| DR-2 | minor | docs-reviewer: wrong `WithIdentity` file list | Applied (verified with `git grep -l`: the five files and which ones dispatch) | Phase 2 task 12 |
| DR-3 | minor | docs-reviewer: the benchmark cannot reach the now-internal middleware | Applied: `InternalsVisibleTo` for `Encina.AspNetCore.Benchmarks` | Design 2 (Visibility), Phase 3 task 4 |
| DR-4 | nit | docs-reviewer: bound constants missing from the `InboundRequestInfo` sketch; `:164` passes `null`, not `userId` | Applied (`MaxIdLength`, `MaxUserAgentLength`, `MaxDataRegionLength`; call-site list corrected) | Design 3, Phase 3 task 3 |

Maintainer decisions D1-D4 are recorded in "Maintainer decisions", item 12.

---

## Review log (PR #1862 final review)

Source: the final review of PR #1862 on plan commit de79c0b7 (minors 3 and 5-9, two optional suggestions) and the maintainer decisions E1-E3 in the last #1705 comment of 2026-10-05, recorded in "Maintainer decisions", item 13. Every `src/` citation added or changed in this round was re-checked with `git show origin/main:<path>` and `git grep` on `origin/main` ab4ec134 (unchanged since the previous round): `InboxMessageConfiguration.cs:20` (`HasMaxLength(255)` on `MessageId`), `HttpAuditContextExtensions.cs:54,101` and `HttpDataResidencyContextExtensions.cs:41` (the only three `cref="EncinaContextMiddleware"` outside the class itself), `ApplicationBuilderExtensions.cs:44`, `EventIdRanges.cs` (`Core` 100-199, `AspNetCore` 200-249), `RequestIdentityLog.cs` (162-165 only), and `EncinaEventIdAllocationTests.cs:46` (`Encina.AspNetCore` maps to `AspNetCore`). The `dotnet/aspnetcore` behaviour of the extended-CONNECT and rewrite paths is taken from the review and was not re-read here.

| Id | Sev | Finding or decision | Result | Where |
|---|---|---|---|---|
| E1 | decision | The D1 latch is reachable through a rewrite that lands on a hub | Applied: the latch needs endpoint null before `next`, `HubMetadata` after, **and an unchanged `Request.Path`**; a changed path logs Warning and latches nothing. Documented order `UseRouting` → rewriter and error handlers → `UseEncinaContext`. Design 2 step 5 and Phase 3 task 9 rewritten; new test for a rewrite whose target is a hub. **Deviation from the brief: the Warning is EventId 203 in the `AspNetCore` range, not 175 in the core range.** The middleware logs from `Encina.AspNetCore`, and `EncinaEventIdAllocationTests` maps that assembly to `AspNetCore` 200-249 (AGENTS.md section 7: never an id outside the package's range); 175 stays in the core range and is used by optional suggestion 2 | Entry-point table, M6 connection-flows row, m6 row, Design 2 step 5, Phase 3 tasks 1 and 9, prompt, Consumer changes, Research EventId table, ADR-035 item, docs, Maintainer decisions 13 |
| E2 | decision | A persisted tenant on an External restore is untrusted | Applied: for `External` the persisted tenant is ignored; the only accepted tenant is the new optional `trustedTenantId` argument that the dispatcher fills from trusted per-inbox or per-endpoint configuration, otherwise no tenant; an invalid configured value returns `Left`; `Internal` keeps the persisted tenant. API signature of `RunRestoredAsync` gains `string? trustedTenantId = null` (before the cancellation token); P-50 wires the mapping | Design 1 ("Persisted form", signature), Design 3 (signature, rule 3, tenant binding), Phase 2 tasks 5, 9, 13, prompt, Dependencies, ADR-035 item |
| E3 | decision | `Accept: text/event-stream` also matches MCP Streamable HTTP POSTs | Applied: the connection skip applies only to **GET** requests whose `Accept` lists `text/event-stream`, in both middlewares; a POST that streams its response keeps its request identity; predicate table gains POST, PUT and HEAD rows; a POST-streaming test; docs note on MCP Streamable HTTP | Entry-point table (new row), m5 and M6 rows, Design 2 step 1, tenant bullet, SSE bullet, Phase 3 tasks 1, 5, 9, prompt, Consumer changes, Documentation |
| 3 | minor | Only `Protocol == "websocket"` extended CONNECT counts as a connection | Applied: every `IsExtendedConnect` request is a connection request whatever its `Protocol`; the HTTP/1.1 detection stays on the `websocket` `Upgrade` token; predicate table gains `webtransport` and `null` rows and an `IsExtendedConnect = false` row | Design 2 step 1, M6 row, Phase 3 tasks 1, 5, 9, prompt |
| 5 | minor | The idempotency key shares the 128 bound and the `Activity` id fallback is unbounded | Applied: `MaxIdempotencyKeyLength = 255` (the inbox `MessageId` column width, `InboxMessageConfiguration.cs:20`) separate from `MaxIdLength = 128`; the `Activity.Current?.Id` fallback is bounded (over-long or control characters → new GUID); tests keep a 200-character key and drop a 256-character one | Design 3 sketch and "Inbound input", Phase 2 tasks 5, 13, prompt, Phase 3 task 9 |
| 6 | minor | The internal-visibility rationale is wrong: method-injected `InvokeAsync` parameters are invisible to `ValidateOnBuild` too | Applied: the rationale is the per-request service-locator lookup only; `AspNetCoreIdentityRegistrationTests` resolves `IRequestContextScopeFactory` and `IInternalRequestContextScopeFactory` explicitly under `ValidateOnBuild`+`ValidateScopes`; the three dangling crefs are added to Phase 3 task 7; `RequestIdentityCircuitHandler` keeps a public constructor (effective accessibility is internal, so no CS0051) | Design 2 (Visibility), Phase 3 tasks 4, 7, 9, prompt |
| 7 | minor | The D2 SSE opt-in loses route and subdomain tenants | Applied: documented in the entry-point table, Design 2, Phase 3 task 11 and the how-to, and tied to F5 as a second acceptance criterion | Entry-point table, Design 2 (SSE bullet), F5 row, Phase 3 task 11, Documentation |
| 8 | minor | D4 copy of an authenticated identity with an empty `AuthenticationType` becomes unauthenticated; Windows group-name roles stop matching | Applied: non-empty fallback type `encina-authenticated`; the Windows consequence documented (reference page, ADR-035); tests pin both | Design 1 (cloning), Phase 2 tasks 4, 13, prompt, ADR-035 item, Documentation |
| 9 | minor | `UserAgent` keeps control characters; the how-to omits where loops start; pooled `HttpContext` hazard | Applied: control characters stripped before truncation (512); the how-to says to start loops at host start or under `SuppressFlow()`; the D2 builder docs state that `HttpContext` is pooled and must not be captured | Design 3 ("Inbound input", loops bullet), Design 2 (SSE bullet), Phase 3 task 11, Documentation |
| O1 | optional | Log 174 carries the declared service name | Applied for `RunAsServiceAsync` (a declared name is not a secret) | Design 3 (opening logs), Phase 2 tasks 8, 13, prompt |
| O2 | optional | Information when application code calls the public `RunInboundAsync` | Applied: the public member logs new core EventId **175** `InboundScopeOpenedByApplication` (Information); the middleware and the Blazor handler call the internal twin `IInternalRequestContextScopeFactory.RunHostInboundAsync`, which logs 172 at Debug as the accepted choice (3) of the previous round requires. The middleware's `InvokeAsync` therefore injects only the internal factory | M3 row, m6 row, Design 2 steps 2 and 4 and Visibility, Design 3 (API block, opening logs), Phase 2 tasks 5, 8, 13, Phase 3 tasks 0, 1, 4, 10, prompts, Consumer changes, Research EventId table |

docs-reviewer pass on this round (verdict publish after fixes, no blocker; all findings verified and applied):

| Id | Sev | Finding | Result | Where |
|---|---|---|---|---|
| DR-A | major | Rule 4 named only `RunInboundAsync` as permitted over a `Connection` holder, so read literally it refused the Blazor handler's `RunHostInboundAsync` | Applied: `RunHostInboundAsync` is covered wherever `RunInboundAsync` is permitted or has no opt-out; the ConnectionFlowIdentityTests circuit case asserts it | m5 row, Design 3 rule 4, Phase 3 tasks 0 and 9 |
| DR-B | major | Several sentences still said the middleware or the Blazor handler uses the public `RunInboundAsync`, or left the new member out of the trusted-path lists | Applied: entry-point row, M6 row, Design 1 trusted-path sentence, API-block comment, opt-out list, ADR-035 item | Entry-point table, M6 row, Design 1, Design 3, Design 6, Phase 7 task 2 |
| DR-C | minor | Stale EventId ranges 166-174 | Applied (166-175) | Phase 1 task 7, Testing |
| DR-D | minor | E2 returned `Left` with an undefined code | Applied: `encina.identity.invalid_persisted_identity` defined once and reused | Design 1, Design 3 rule 3, Phase 2 tasks 6 and prompt |
| DR-E | minor | Phase 2 prompt omitted the invalid `trustedTenantId` check; extension overloads omitted the parameter | Applied | Phase 2 prompt, Design 3 comment |
| DR-F | minor | Maintainer decisions 11 and 12 read as the pre-E3 and pre-E1 rules | Applied: forward pointers to item 13 | Maintainer decisions 11 and 12 (D1) |
| DR-G | minor | M3 row sentence garbled by the new parenthetical | Applied | M3 row |
| DR-H | unverified | `UseStatusCodePagesWithReExecute` and `UseExceptionHandler` probably restore path and endpoint, so Warning 203 arises only from `UseRewriter` or custom middleware | Not applied as a rule change (the maintainer decision names all three): the caveat is stated in Design 2 step 5 and the Phase 3 worker verifies it before writing the test | Design 2 step 5 |
| DR-I | not run | `lychee`/`markdownlint` on the page | See the report: run by the orchestrator in CI | n/a |

Maintainer decisions E1-E3 are recorded in "Maintainer decisions", item 13.

# Phase 0 — Encina 1.0 Baseline (diagnostic audit)

**Evidence date:** 2026-09-21
**Status:** historical, pre-remediation snapshot of `main` at commit `3e87b114`. It is kept as the baseline the 1.0 work is measured against and is not updated to the current state; #1088 (build) was fixed the same day in PR #1093 and the findings marked resolved in §9 record what has changed since. The build and CI conclusions in §1 and §2 describe that commit, not today's repository.
**Scope:** the diagnostic pass defined in `ENCINA-1.0-RECONCILIATION.md` §13 and `AI-DEVELOPMENT-MODEL.md` §22 (Passes 1, 2, 4, 6 and 7). No production code was modified. Pass 3 (historical archaeology of GitHub discussions) and per-issue classification of the 579 open issues are **not** done here; they are the next step (§10).
**Method:** every figure below was read from the repository, the GitHub API (`gh`) or the published dashboard data files on this date. Where a claim could not be verified it is marked as such.

---

## 1. Headline findings

1. **The solution does not build.** `dotnet build Encina.slnx -c Release` stops at restore with 283 NuGet-audit errors (NU1902/03/04 promoted by `TreatWarningsAsErrors`), dominated by Marten 8.30.0 (critical). Every CI workflow has been red for months for the same reason. → #1088, **P0**.
2. **All quality dashboards are stale**, because the workflows that feed them fail at the build step. Coverage data ends 2026-04-04; the mutation `latest.json` is a single scoped shard from 2026-04-12; CodeQL last succeeded 2026-05-04. Any quality statement made today is based on data at least five months old.
3. **The package inventory has four different answers** (README 39, ROADMAP 53, `src/` 110, `Encina.slnx` 99). Eleven projects, including two compliance packages the CHANGELOG lists as implemented and a duplicate Secrets package family, are outside the solution. → #1089, **P0** for the 1.0 package list.
4. **Public metric claims are unbacked.** "92.3% coverage", "0 SonarCloud issues", "0 build warnings", "6,500+ tests" in `ROADMAP.md` do not match the evidence (51.29% per-flag, Sonar never run, build broken, ~31,000 test attributes). → #1090.
5. **Release engineering is mostly absent.** Policy files exist, but there is no package signing, provenance, SourceLink/deterministic-build metadata in `Directory.Build.props`, nor a NuGet publish path; the SBOM workflow last ran in March.
6. **The backlog is 77% features.** 446 of 579 open issues are `[FEATURE]`, spread over 21 open milestones from v0.13.4 to v0.23.0. The 1.0 release gate (v0.23.0) has only 16 open items.

None of these is surprising for a pre-1.0 solo project. All of them are fixable. But the first three make every downstream statement unverifiable until they are resolved, which is why they are ordered first in §9.

---

## 2. A — Current product

| Item | Value | Source |
|---|---|---|
| Version | `0.13.0-dev` (`VersionPrefix` + `VersionSuffix`) | `Directory.Build.props` |
| Last release tag | `v0.12.0` (2026-02-16); 773 commits since (2026-02-17 → 2026-09-21) | `git tag`, `git rev-list` |
| Projects under `src/` | 110 `.csproj` | filesystem |
| Projects in `Encina.slnx` | 99 | `Encina.slnx` |
| Orphan projects (in `src/`, not in solution) | 11: `Compliance.AIAct` (49 files), `Compliance.Consent` (30), `Secrets` + 4 providers (64), `Testing.Architecture`, `Testing.FsCheck`, `Testing.Testcontainers`, `Testing.Verify` | #1089 |
| Duplicate families | `Encina.Secrets.*` (orphan) vs `Encina.Security.Secrets.*` (in solution) | #1089 |
| Coverage manifests | 113 packages | `.github/coverage-manifest/` |

**Provider matrix actually present in `src/`** (compare with `CLAUDE.md`, which is the documented intent):

| Category | Present | Notes |
|---|---|---|
| Database (10 required) | ADO ×3, Dapper ×3, EF Core (1 package, 3 DBs), MongoDB | Matches the rule. SQLite packages removed from `src/` (ADR-024). |
| Caching (8 documented) | Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB | **7 present; Memcached absent** ("planned" in CLAUDE.md). |
| Transports (10 documented) | RabbitMQ, AzureServiceBus, AmazonSQS, Kafka, NATS, Redis.PubSub, MQTT, InMemory, gRPC, GraphQL | Matches. Plus SignalR, Refit. |
| Distributed lock (4 documented) | InMemory, Redis, SqlServer | **3 present**; PostgreSQL/MySQL "planned". |
| Validation (3) | FluentValidation, DataAnnotations, MiniValidator | Matches. |
| Scheduling | Hangfire, Quartz | Matches. |
| Event sourcing | Marten, Marten.GDPR, Audit.Marten | Matches. |
| Cloud | AzureFunctions, AwsLambda, Aspire.Testing | GCP Functions absent ("planned"). |
| Compliance | 15 packages (GDPR, Consent*, LawfulBasis, DSR, DataResidency, Retention, Anonymization, BreachNotification, DPIA, PrivacyByDesign, CrossBorderTransfer, ProcessorAgreements, NIS2, AIAct*, Attestation) | * = orphan. |
| Security | Security, ABAC (+Analyzers), AntiTampering, Audit, Encryption, PII, Sanitization, Secrets ×5 | |
| Messaging encryption | DataProtection, AwsKms, AzureKeyVault | |
| Secrets (standalone) | Secrets ×5 | Orphan duplicate of Security.Secrets. |
| Testing | 12 packages (4 orphan) | |
| Tooling | Cli, IdGeneration, GuardClauses, DomainModeling, Tenancy (+AspNetCore), OpenTelemetry, Polly, Extensions.Resilience, CDC ×6 | |

---

## 3. B — Roadmap

`ROADMAP.md` (line 12) says 53 active packages and lists every category as "✅ Production". Its Quality Metrics table (lines 32–38) is hand-typed and not generated.

Milestones (GitHub):

| Milestone | Open / closed | Comment |
|---|---|---|
| v0.02 – v0.12.4 (16 milestones) | 0 open | Closed. |
| **v0.13.0** Security & Compliance | 4 / 57 | 2 bugs (#856, #858), 1 debt (#860), the EPIC. Effectively done. |
| v0.13.4 Critical Bugs & Quality Debt | 25 / 10 | Hardening. |
| v0.13.5 Data Integrity & Cross-Cutting | 22 / 1 | Mostly features (lock/idempotency integrations). |
| v0.13.6 Compliance Completion & Coverage | 21 / 1 | Mixed; contains legacy coverage targets (#66, #67). |
| v0.14.0 – v0.18.2 (12 milestones) | 20–24 open each, ~0 closed | Feature expansion (cloud-native, multi-tenancy, EIP, AI/LLM, new transports…). |
| v0.19.0 Observability Exporters | 28 / 0 | Features + #1050, #1051. |
| v0.20.0 / v0.20.1 Web & DX | 24 / 0, 23 / 0 | Features. |
| v0.21.0 Testing & Performance | 37 / 0 | Benchmarks/load tests for every package. |
| v0.22.0 Quality & Documentation | 46 / 9 | Docs, Sonar, coverage, DocFX, performance claims. |
| **v0.23.0 Release Preparation** | 16 / 1 | The release gate. |

Observation: the milestone sequence encodes an implicit plan of **ten more feature releases before 1.0**. That plan predates the decision, recorded in `ENCINA-1.0-RECONCILIATION.md`, that 1.0 is a consolidation of the existing product. The two are incompatible and the human must choose (→ SPEC-000, DEC-002).

---

## 4. C — GitHub backlog

| Metric | Value |
|---|---|
| Open issues | 579 (2 without milestone) |
| By type | FEATURE 446 · TEST 52 · DEBT 32 · EPIC 26 · INFRA 13 · BUG 6 · SPIKE 3 · REFACTOR 1 |
| Open EPICs | 26 (one per milestone v0.13.0 → v0.23.0, plus #712 cache integration and #1037 DDD ergonomics) |
| Open bugs | #73 (flaky tests), #801 (Marten Version), #856 (HTTP body leak), #858 (PII HMAC), #898 (Coverlet), #922 (SanitizeForSql), + #1088 (build) |

**Provisional P0–P3 at milestone level** (per-issue classification is the next step):

| Priority | Content |
|---|---|
| **P0** | #1088 build; #1089 orphans/duplicates; the 6 open bugs (each to be confirmed); v0.23.0 release-gate INFRA items #92, #93, #98, #100, #101, #104; #1050 EventId registration; security debt #852 (SSRF), #858; #1032 DocFX errors that break docs (subset). |
| **P1** | Rest of v0.13.4 (hardening, #853 IValidateOptions, #855 mutable collections, #857 orphans); #1032 remainder; v0.22.0 documentation set (#80–#89, #903–#908); performance-claim debt #927–#929 (+ #1090); #1026/#1087 mutation; #1051 CPM pinning; #75/#76 Sonar. |
| **P2** | All `[FEATURE]` in v0.13.5, v0.14.0 – v0.20.1 (new providers, EIP, AI/LLM, Aspire, multi-tenancy core, source generators, hot reload…); v0.21.0 exhaustive benchmarks/load tests per package (#924–#946, #552–#567); #620 Bugsnag. Unless SPEC-000 declares a given feature part of the 1.0 contract. |
| **P3** | Legacy coverage-target issues #19, #65, #66, #67 (targets no longer used); duplicates to be detected in the per-issue pass; issues already implemented but open (to be detected by checking CHANGELOG/code). |

---

## 5. D — Quality

### CI state

| Workflow | Last success | Latest run | Failing job |
|---|---|---|---|
| `ci.yml` | 2026-04-06 | 2026-07-27 ✗ | `build` (NU19xx) |
| `ci-full.yml` | 2026-05-04 | 2026-09-21 ✗ | `build` (NU19xx) |
| `codeql.yml` | 2026-05-04 | 2026-09-21 ✗ | `Analyze` (build) |
| `mutation-tests.yml` | 2026-04-17 | 2026-09-18 ✗ | `test-baseline` + all 17 shards |
| `benchmarks.yml` | 2026-07-20 | 2026-09-20 ✗ | every benchmark job |
| `docs.yml` | 2026-04-27 | 2026-07-20 ✗ | `Build Documentation` (NU19xx) |
| `sonarcloud.yml` | — | skipped on PRs | `SONAR_TOKEN` not configured (#75) |
| `publish-coverage.yml`, `publish-mutations.yml` | — | skipped | upstream failed |
| `sbom.yml` | 2026-03-25 | 2026-03-25 | not run since |

Root cause for all build failures: #1088.

### Coverage (per-flag obligations model, `docs/coverage/data/latest.json`, 2026-03-29)

| Metric | Value |
|---|---|
| Overall obligations met | **51.29 %** (68,564 / 133,684) |
| Packages measured | 100 (38 meet all targets, 50 do not, 12 excluded) |
| Lowest | Marten 8.75 %, Aspire.Testing 16 %, DistributedLock.Redis 26.5 %, Audit.Marten 27 %, MongoDB 27 %, all ADO/Dapper providers 28–33 % (target 50) |
| History | 25 entries, 2026-03-?? → 2026-04-04, overall rising 38.5 → 39.4 % in that window |

The `latest.json` still carries a legacy `categories` block (Provider 50 %, Full 85 %, Logic 80 %…) alongside per-package `perFlag` data, i.e. the file format itself reflects the transition from the category model to the manifest model. `CLAUDE.md` and `TESTING.md` were corrected on 2026-09-21 to describe only the manifest model.

### Mutation testing

`docs/mutations/data/latest.json` is a **scoped run** (`**/Sharding/Migrations/Strategies/*.cs`, 59 mutants, score 5.08 %, 15 compile errors, 25 ignored) from 2026-04-12, with a single history entry. The 17-shard weekly matrix has never completed successfully since it was introduced (all runs fail at `test-baseline`). Upstream status and the next experiment are in `Stryker-xUnit-v3.md` / #1087.

### Tests

| Project | `[Fact]`/`[Theory]` |
|---|---|
| Encina.UnitTests | 19,007 |
| Encina.GuardTests | 7,253 |
| Encina.IntegrationTests | 2,908 |
| Encina.ContractTests | 1,507 |
| Encina.PropertyTests | 249 |
| **Total** | **~30,900** (attribute count; theories expand further) |

Test execution could not be verified locally because the solution does not restore. Known test-health issues: #73 (flaky), #531 (DistributedLock tests hanging, excluded from CI), #898 (Coverlet not instrumenting two assemblies).

### Static analysis

`TreatWarningsAsErrors=true` is on. CodeQL has not produced a result since May. SonarCloud has never run (#75). "0 warnings" cannot be asserted while the build fails at restore.

---

## 6. E — Documentation

| Item | State |
|---|---|
| `CHANGELOG.md` | `[Unreleased] - v0.13.0` section is **2,621 lines** (lines 1–2621) covering seven months and ~40 packages; the header no longer matches the content (it also documents v0.13.x hardening and later work). |
| DocFX | `docs/docfx.json` exists; `docs.yml` fails at build (#1088). The 254-warning estimate in #1032 dates from before the failure and was not re-verified. |
| ADRs | 25 (`docs/architecture/adr/`), index present. Two versions of ADR-007 coexist (`007-extensibility-strategy.md` and `-v2.md`). |
| Plans | `docs/plans/` active: test consolidation, performance infrastructure, OTLP exporter (#1043, rescued 2026-09-21). |
| Language | `docs/INVENTORY.md` is in Spanish (`CLAUDE.md` mandates English for documentation). |
| Package READMEs | Not audited in this pass. |
| Performance claims in docs | "50–100x faster than reflection" in `docs/architecture/component-diagram.md:355` and `docs/architecture/patterns-guide.md:739`; ADR-003's own table shows ~2.3x. #927 covers ADR-003 only (→ #1090). |

---

## 7. F — Release

| Item | State |
|---|---|
| `SECURITY.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `LICENSE`, `CODEOWNERS`, PR template, issue templates, `dependabot.yml`, `nuget.config` | Present. (#95, #96, #97 ask to "finalize" them; content not audited here.) |
| `global.json` | Absent (SDK version not pinned). |
| `Directory.Build.props` package metadata | `Authors`, `RepositoryUrl` only. No `PackageLicenseExpression`, `PackageReadmeFile`, `PackageIcon`, `PublishRepositoryUrl`/SourceLink, `Deterministic`/`ContinuousIntegrationBuild`, `EnablePackageValidation` at the root (per-project overrides not audited). |
| `release-on-milestone.yml` | Exists; contains **no** `dotnet pack`, `nuget push`, signing, attestation or provenance steps. |
| Signing / provenance | None (#92 SLSA, #93 Sigstore open). |
| NuGet | No API key / publish workflow (#101); package IDs not reserved (#100). |
| SBOM | Workflow exists; last run 2026-03-25. |
| Branch protection | Unknown from the repository (#98 open). Note: the working copy was on a detached HEAD at `main`'s tip, and direct commits to `main` appear in history. |

---

## 8. G — Performance

- Infrastructure exists and is documented (ADR-025, performance dashboard, DocRef citation system).
- `benchmarks.yml` has failed every week since 2026-07-20 (build), so the performance dashboard is also stale.
- Unbacked multipliers remain in three documents (§6). #927–#929 track the ADR-003/ADR-012/README instances.
- v0.21.0 asks for benchmarks and load tests for essentially every package (37 issues). Per `ENCINA-1.0-RECONCILIATION.md` §6.2 this is P2 except for critical paths.

---

## 9. Findings register

Class per `AI-DEVELOPMENT-MODEL.md` §14 (A deterministic · B conflicts with an existing decision/contract · C needs a human decision).

| # | Finding | Class | Priority | Tracking |
|---|---|---|---|---|
| F-01 | Solution fails to restore: Marten 8.30.0 (critical) + 8 transitive advisories; all CI red since May | A | P0 | #1088 |
| F-02 | 11 `src/` projects outside `Encina.slnx`; `Encina.Secrets.*` duplicates `Encina.Security.Secrets.*`. **Corrected 2026-09-21:** six of them (`Encina.Compliance.AIAct`, `Encina.Compliance.Consent`, `Encina.Testing.Architecture`, `Encina.Testing.FsCheck`, `Encina.Testing.Testcontainers`, `Encina.Testing.Verify`) were built and tested all along through ProjectReferences from the test projects and were simply never listed in the solution; only the five `Encina.Secrets.*` projects (superseded by #400 three days after #603 delivered them) were dead code. **Resolved by DEC-001**: six added to the solution (99 → 105 projects), `Encina.Secrets.*` deleted, #452 closed as superseded. | C → decided | P0 | #1089, DEC-001 |
| F-03 | ROADMAP/README metrics and package counts unbacked | A (generate) | P1 | #1090 |
| F-04 | Coverage dashboard frozen at 2026-04-04, `latest.json` mixes legacy categories with per-flag data | A (after F-01) | P1 | #1088, #912 |
| F-05 | Mutation matrix has never completed; `latest.json` is one shard | A (after F-01) + external | P1 | #1026, #1087 |
| F-06 | CodeQL and SonarCloud produce no results | A | P1 | #1088, #75 |
| F-07 | No signing/provenance/publish path; package metadata minimal; no `global.json` | A | P0 (release gate) | #92, #93, #100, #101, #102 |
| F-08 | Milestone plan implies 10 feature releases before 1.0; contradicts the consolidation decision. **Resolved by DEC-002 (2026-09-21):** 1.0 = existing modules hardened + compliance EPICs #881 and #880 (minus the five new regulation packages); no target date; feature milestones v0.14–v0.20 outside the contract but not blocked. | C → decided | P0 (scope) | SPEC-000 DEC-002 |
| F-09 | `CHANGELOG.md` Unreleased section is 2,621 lines with a stale header | A | P1 | to open after SPEC-000 decides versioning (DEC-005) |
| F-10 | Legacy coverage-target issues (#19, #65, #66, #67) reference a model no longer used | A | P3 | close or reword, via #1090 |
| F-11 | Two ADR-007 files coexist | A | P2 | to open |
| F-12 | `docs/INVENTORY.md` in Spanish, contrary to `CLAUDE.md` | A | P2 | to open |
| F-13 | 6 open bugs, 2 of them security (#856 body leak, #858 SHA256 vs HMAC) | A (fix) | P0 | existing issues |
| F-14 | Provider matrix in `CLAUDE.md` lists 8 caches / 4 locks; `src/` has 7 / 3 | B (rule vs implementation) | P1 | SPEC-000 DEC-003 |
| F-15 | Performance multipliers without benchmark in two architecture docs | A | P1 | #1090 (extends #927) |
| F-16 | Detached-HEAD working copy and direct pushes to `main`; branch protection status unknown | C (policy) | P1 | #98 |

---

## 10. Knowledge debt observed

- The per-flag coverage model lived in `coverage-report.cs` and the dashboard while `CLAUDE.md`, `TESTING.md` and `ROADMAP.md` still described category/global targets. Corrected in the docs on 2026-09-21; ROADMAP pending (#1090).
- The decision that 1.0 is a consolidation release exists only in `docs/engineering/` (unmerged until today); the milestone structure on GitHub still encodes the opposite.
- Why `Encina.Secrets.*` and `Encina.Security.Secrets.*` both exist is not recorded anywhere found in this pass (candidate for Pass 3 archaeology: #452, #694, #743).
- The 6 `Migrate *Service to Encina.Scheduling` debt items (#766–#772) imply an architectural rule ("background services use Encina.Scheduling") that is not written as a rule.

---

## 11. Automation opportunities (Pass 7)

| Recurring problem | Executable check |
|---|---|
| Projects outside the solution | Script comparing `src/**/*.csproj` with `Encina.slnx`; fail CI on drift. |
| Hand-typed package counts / metrics | Generate the ROADMAP/README tables from `src/` and dashboard JSON. |
| Stale dashboards going unnoticed | Publish workflows should fail loudly (not skip) when the upstream run fails, or a scheduled check should flag `latest.json` older than N days. |
| NuGet audit regressions | Already enforced by `TreatWarningsAsErrors`; the gap is Dependabot coverage (#1042) and transitive pinning (#1051). |
| Unbacked performance claims | Extend the DocRef renderer to fail on `\d+x` patterns outside a `<!-- docref -->` marker. |
| Worktree leftovers | `git worktree list` in the topology pass (rule added to `AI-DEVELOPMENT-MODEL.md` §4). |

---

## 12. Smallest practical next steps (in order)

1. **Fix #1088** (restore the build). Nothing else can be verified until then. Class A; routable to the local AI under supervision, with Claude verifying the audit output.
2. **Re-run `ci-full.yml` and `mutation-tests.yml`** to refresh the dashboards; re-read §5 with fresh data.
3. **Human decisions on SPEC-000** (DEC-001 … DEC-006), which unblock the per-issue classification.
4. **Per-issue P0–P3 pass** over the 579 open issues, starting with v0.13.x, v0.22.0 and v0.23.0 (Historian/Auditor work; local AI candidate with provenance, per `ai-task-routing.md`).
5. **Resolve #1089** according to DEC-001 (canonical package list).

---

*Related documents: `AI-DEVELOPMENT-MODEL.md`, `ENCINA-1.0-RECONCILIATION.md`, `ai-task-routing.md`, `Stryker-xUnit-v3.md`, `../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md`.*

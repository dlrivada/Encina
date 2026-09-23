# SPEC-000 — Encina 1.0 Baseline and Release Scope

| | |
|---|---|
| **Status** | 🟢 APPROVED (amended 2026-09-24) — DEC-001 … DEC-006 decided by the maintainer on 2026-09-21; DEC-007 (the scope widened by SPEC-002) decided on 2026-09-24 |
| **Author** | Specifier (Claude), from `docs/engineering/PHASE0-BASELINE.md`; amendment of 2026-09-24 from [SPEC-002](SPEC-002-eu-regulatory-readiness.md) §11.2 |
| **Date** | 2026-09-21; amended 2026-09-24 |
| **Evidence** | `PHASE0-BASELINE.md` (2026-09-21); [SPEC-002 — EU Regulatory Readiness](SPEC-002-eu-regulatory-readiness.md) (2026-09-24) |
| **Supersedes** | — |

> This specification implements nothing. Its first six human decisions were recorded on 2026-09-21 and it is **APPROVED**: it is the boundary of the Encina 1.0 project, and work outside it is P2 by default. On 2026-09-24 it was amended (DEC-007, §11) to take in the pre-1.0 scope that SPEC-002 decided: EU regulatory readiness, the Spanish national provisions SPEC-002 turns into requirements, multi-tenancy, and the integration and platform capabilities of SPEC-002 §5.11. Requirements state *what* must be true for 1.0; they do not prescribe *how*. Design choices that arise while satisfying a requirement go through the ADR process (`AI-DEVELOPMENT-MODEL.md` §7).
>
> **Identifiers.** Unprefixed REQ, AC, DEC and INV identifiers are this specification's. Identifiers of SPEC-002 are always written with the prefix (SPEC-002 REQ-005, SPEC-002 DEC-011).

---

## 1. Problem

Encina has 110 projects, ~31,000 tests, 25 ADRs and 579 open issues, but no single statement of what "1.0" means. Different sources describe different products (README, ROADMAP, CHANGELOG, `src/`, the solution file), the quality dashboards are frozen because the build is broken, and the milestone plan on GitHub implies ten further feature releases before 1.0. Without an approved scope, every open issue is implicitly a 1.0 blocker and the release date is unbounded.

## 2. Definition of 1.0

Encina 1.0 is **the first stable, supported release of the product that exists today**: the same packages, the same public API surface, hardened, verified with reproducible evidence, documented well enough for a third party to adopt it, and released through a professional pipeline; **plus the capabilities that SPEC-002 places in 1.0** (REQ-029, DEC-007). It is not the release that contains every idea in the backlog, and it has **no target date**: the sequence of work matters, the calendar does not (DEC-002).

Compliance with the EU regulatory framework Encina targets (GDPR and its Digital Omnibus amendments, ePrivacy, NIS2, AI Act) is **part of the 1.0 contract**, not an optional module set: the applications Encina is built for are EU applications, for which these obligations are law (DEC-002).

Since the amendment of 2026-09-24 (DEC-007), that contract is **EU regulatory readiness as [SPEC-002](SPEC-002-eu-regulatory-readiness.md) defines it**: Encina provides the mechanisms, safe defaults, extension points and honest documentation that an application needs to apply the law, and the application, through its controller or producer, applies it and takes every legal decision (SPEC-002 §2). Encina facilitates and never blocks. In particular, 1.0 includes:

- **The Spanish national provisions that SPEC-002 turns into 1.0 requirements.** LOPDGDD art. 32 blocking (*bloqueo*: retained data excluded from processing and released only through an audited disclosure path, SPEC-002 REQ-005), art. 9.2 and DA 17ª as the national legal basis next to the Art. 9(2) condition (SPEC-002 REQ-010) and art. 7, consent of minors (SPEC-002 REQ-059, only if SPEC-002 §12 question 1 is answered yes); Ley 41/2002 art. 17.1 clinical-record retention of at least five years from each discharge (SPEC-002 REQ-001), art. 18.3 access limits for third parties' data and professionals' subjective annotations (SPEC-002 REQ-009) and art. 18.4 representation and deceased patients, in shape only (SPEC-002 REQ-011); LSSI arts. 20–22 on commercial communications, with consent per purpose and channel and the art. 21.2 existing-client basis (SPEC-002 REQ-014); and the national rows of the per-package article tables (SPEC-002 REQ-022). The designs stay jurisdiction-neutral: Spanish law is the acceptance case, not hard-coded behaviour.
- **Sensitive health data.** An application built on Encina can keep clinical records (special-category data under GDPR Art. 9) within the law (SPEC-002 DEC-001 (b), Tier B). The acceptance case is a small Spanish psychology practice (SPEC-002 §4), exercised by the generic PracticeManagement reference scenario that gates 1.0 (SPEC-002 REQ-038).
- **Multi-tenancy.** Every capability SPEC-002 adds or changes is tenant-aware on every store and provider it ships on, and a single-tenant application needs no tenant configuration (SPEC-002 DEC-009 (b), SPEC-002 REQ-061). Retrofitting a tenant key into persisted shapes after 1.0 would break the API and the schema.
- **Observability.** Every such capability is instrumented with OpenTelemetry tracing and metrics, structured logging with registered EventIds, and health checks; Encina never sends telemetry to its maintainer or project (SPEC-002 DEC-010, SPEC-002 REQ-062).
- **The integration and platform capabilities of SPEC-002 §5.11** (SPEC-002 REQ-039 – SPEC-002 REQ-058), with the channel satellites, webhook presets and storage providers of SPEC-002 DEC-016 (§6 lists what stays out).

Consequences:

- Breaking changes stop being free after 1.0. Anything that must break should break before.
- Providers, transports and integrations not shipped in 1.0 are additive later; their absence is not a defect.
- A quality claim that cannot be regenerated from CI is not made.
- A compliance module that ships in 1.0 states which articles of which regulation it covers, and that statement is backed by a specification and tests; a module that covers a regulation partially says so.
- 1.0 arrives materially later than under the scope of 2026-09-21. The maintainer accepted that later date (DEC-007); there is still no target date.

## 3. Requirements

Identifiers are stable. Each requirement has at least one acceptance criterion in §4 and a verification method in §8.

### Scope and inventory

- **REQ-001** There is exactly one authoritative list of the packages that ship in 1.0, and every other source (README, ROADMAP, INVENTORY, solution file, coverage manifests, CI) is derived from or checked against it.
- **REQ-002** Every project under `src/` is either in the 1.0 package list, explicitly deferred (kept out of the solution with a recorded reason), or removed.
- **REQ-003** No two packages provide the same capability under different names.
- **REQ-004** Every provider-dependent feature in the 1.0 list declares which providers it supports and which are deferred, and the shipped set is coherent across providers (same interfaces, same options, same semantics).

### Build and quality gates

- **REQ-005** The solution builds from a clean clone in Release with zero warnings and zero NuGet audit findings of **moderate or higher** severity (DEC-004); the audit stays promoted to an error by `TreatWarningsAsErrors`.
- **REQ-006** All test projects in the 1.0 list run green in CI, with no test excluded from CI without an open issue naming it.
- **REQ-007** Every package in the 1.0 list reaches its per-flag coverage targets as declared in its manifest, or its manifest is revised with a recorded justification before release.
- **REQ-008** Mutation testing runs reproducibly, publishes per-file results with the exact tool versions, and documents its known limitations; no mutation-score threshold is a release blocker while upstream results are unreliable.
- **REQ-009** Static analysis (CodeQL, and SonarCloud if DEC-006 keeps it) produces a current result with no open findings of **high or higher** severity; medium findings are listed with a disposition in the evidence report (DEC-004).
- **REQ-010** Architecture tests, PublicAPI analyzers and EventId-range checks pass for every package in the 1.0 list.

### Defects and security

- **REQ-011** Every open `[BUG]` at approval time is either fixed or explicitly deferred with a recorded reason. Security-classified bugs cannot be deferred.
- **REQ-012** Options that accept URLs, endpoints or connection strings validate scheme and host (SSRF class, #852) for every package in the 1.0 list.

### Public API and documentation

- **REQ-013** Every public type and member in the 1.0 list has XML documentation and appears in `PublicAPI.Shipped.txt` at release (the Unshipped files are empty after the release commit).
- **REQ-014** The documentation site builds without errors; the warnings that remain are classified and either fixed or listed with justification.
- **REQ-015** A third party can go from zero to a working request/handler with a database provider and one messaging pattern using only the published documentation (quickstart, fundamentals, providers overview, one complete example).
- **REQ-016** Every quantitative performance claim in README, docs or ADRs cites a reproducible benchmark (DocRef) or is removed.
- **REQ-017** `CHANGELOG.md` has one dated section per released version from 0.13.0 onward (DEC-005): the current Unreleased content becomes `[0.13.0]`, every later 1.0 block ships as its own minor version with its own section, and `[1.0.0]` is the top section at release.

### Release engineering

- **REQ-018** Packages are produced by CI from a tagged commit, with deterministic build settings, SourceLink, license, readme and icon metadata, carry a GitHub Actions artifact attestation and are Sigstore-signed (DEC-004; #92, #93).
- **REQ-019** Package identifiers are reserved on NuGet.org and the publish workflow is exercised end to end against a pre-release version before 1.0.
- **REQ-020** The SDK version is pinned (`global.json`) and `main` is protected with `enforce_admins`, linear history, required conversation resolution, stale-review dismissal, no required approving review (a solo maintainer cannot approve their own pull requests; review comes from the bots and the adversarial reviewer of REQ-023) and exactly the required checks `build`, `ci-result` and CodeQL `Analyze` (DEC-006). `ci-result` is the always-run summary job of `ci.yml`: it fails when any of `build` (which includes `Check formatting`) or the `test-*` jobs failed or was cancelled, so matrix job names never appear in the protection settings.
- **REQ-021** `SECURITY.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md` and the issue/PR templates describe the actual 1.0 process.

### Evidence

- **REQ-022** The release candidate is accompanied by an evidence report generated from CI artifacts (build, tests per flag, coverage per flag, mutation, static analysis, API diff, SBOM, benchmark snapshot) rather than hand-typed numbers.
- **REQ-023** An independent adversarial review of the release candidate is performed and its findings are resolved or deferred with reasons before the final human release decision.

### Regulatory compliance (DEC-002)

- **REQ-024** Every compliance package in the 1.0 list (GDPR, Consent, LawfulBasis, DataSubjectRights, DataResidency, Retention, Anonymization, BreachNotification, DPIA, PrivacyByDesign, CrossBorderTransfer, ProcessorAgreements, NIS2, AIAct, Attestation) has a specification that lists the regulation articles it covers, the articles it explicitly does not cover, and the evidence (tests, audit records) for each covered article. The ePrivacy Directive (2002/58/EC, Art. 5(3) consent for storing or accessing information on terminal equipment, and the EDPB guidance on cookie consent) is covered by `Encina.Compliance.Consent`, whose specification carries an ePrivacy article table next to its GDPR table; no other package claims ePrivacy coverage. SPEC-002 refines this requirement: its §3 is the law map (which acts bind Encina itself and which bind the applications built on it, with verification flags and a re-verification cadence), SPEC-002 REQ-022 fixes the shape of each per-package article table and adds the national articles each package helps apply (LOPDGDD art. 32 and Ley 41/2002 arts. 17 and 18.3 for Retention and DataSubjectRights; LSSI arts. 20–22 for Consent), and its §6 coverage matrix records, for each SPEC-002 requirement, the Encina package or type concerned, its status (Covered, Partial, Missing or Conflict), the evidence and the tracking issue.
- **REQ-025** The AI Act module implements the obligations planned in EPIC #881 (Arts. 9, 10, 10.2f, 11, 12, 13/50, 14, 43, 51–56, plus multi-tenancy, module isolation and the Marten migration) before 1.0, in the dependency order the EPIC defines.
- **REQ-026** The NIS2 lifecycle and the Digital Omnibus adaptations planned in EPIC #880 (#822–#827, #810–#816) are implemented before 1.0; the five new regulation packages in that EPIC (DORA, eIDAS2, Data Act, ENS, EHDS) are post-1.0, except the generic extension points SPEC-002 places in 1.0 (EHDS logging and export, SPEC-002 REQ-052; trusted time-stamping and e-signature integration points, SPEC-002 REQ-048; SPEC-002 DEC-015). The Digital Omnibus adaptations implement a proposal that is not adopted (COM(2025) 837): all seven (#810–#816) are built before 1.0, each behind an option that is **off by default** and named after its draft article, and current law stays the default behaviour until the proposal is adopted (SPEC-002 DEC-012, SPEC-002 REQ-024).

### Provider completeness (DEC-003)

- **REQ-027** The caching and distributed-lock provider sets ship complete as `CLAUDE.md` documents them: eight caching providers (adding `Encina.Caching.Memcached`, #277) and five lock providers: the three that exist (`InMemory`, which is the single-process testing provider, `Redis`, `SqlServer`) plus `Encina.DistributedLock.PostgreSQL` with `pg_advisory_lock` (#207) and `Encina.DistributedLock.MySQL` with `GET_LOCK` (#208), i.e. four production-grade backends and one in-memory. The remaining lock backends in the `CLAUDE.md` table (Azure Blob, DynamoDB, Consul, etcd, ZooKeeper) are post-1.0. Each provider ships with the same interfaces, options, health check, OpenTelemetry instrumentation and test types as the existing providers of its category.
- **REQ-028** The cloud provider set for 1.0 is AWS Lambda and Azure Functions; Google Cloud Functions (#205) is post-1.0 and `CLAUDE.md` states the deferral explicitly instead of a three-way triangle.

### EU regulatory readiness (DEC-007, SPEC-002)

- **REQ-029** The pre-1.0 scope decided in SPEC-002 is part of the 1.0 contract. Every SPEC-002 requirement placed before 1.0 is implemented and verified before `1.0.0-rc.1`: the items of SPEC-002 §11.2 table 1 (SPEC-002 DEC-011 (a) + (d) + (e), SPEC-002 DEC-009 (b), SPEC-002 DEC-005 (a)) and table 2 (the integration and platform capabilities of SPEC-002 §5.11, SPEC-002 REQ-039 – SPEC-002 REQ-058), together with the SPEC-002 requirements that refine or extend requirements of this document (SPEC-002 REQ-022 refines REQ-024, SPEC-002 REQ-024 refines REQ-026, SPEC-002 REQ-026 – SPEC-002 REQ-030 extend REQ-018 – REQ-022, and the SPEC-002 defect requirements fall under REQ-011 as well). One part stays outside the contract, as SPEC-002 states: the rules part of SPEC-002 REQ-011 (post-1.0; its persisted shape is in 1.0). SPEC-002 REQ-059 (consent for minors) is included: the reference practice treats minors (SPEC-002 §12 question 1, answered 2026-09-24). An item SPEC-002 promotes into the P0 backlog cites this requirement and the SPEC-002 requirement it implements (INV-004).

## 4. Acceptance criteria

| AC | Requirement | Criterion |
|---|---|---|
| AC-001 | REQ-001 | A committed manifest (one entry per project under `src/`, with `packageId`, disposition `ship` / `deferred` / `removed`, and a reason plus ADR or issue reference for every non-`ship` entry) is the authoritative list. A script compares it by name and disposition against `src/`, `Encina.slnx`, README, ROADMAP, `docs/INVENTORY.md` and the CI configuration (the package lists in `ci.yml` / `ci-full.yml` / `release-on-milestone.yml` and the set of `.github/coverage-manifest/*.json` files), and CI fails on any project missing from the manifest, any manifest entry without a project, any `ship` project absent from any of those sources, any non-`ship` project present in any of them, or any disposition mismatch. |
| AC-002 | REQ-002 | Every `src/**/*.csproj` has exactly one manifest entry; every `ship` project is in `Encina.slnx` and in the coverage manifests; no `deferred` or `removed` project is in `Encina.slnx`; every `deferred` entry cites the ADR or issue that records the reason. Count equality alone is not sufficient. |
| AC-003 | REQ-003 | Only one of `Encina.Secrets.*` / `Encina.Security.Secrets.*` exists (DEC-001). |
| AC-004 | REQ-004 | Each feature in the list has a `Supported providers / Deferred providers` block in its spec or README, and contract tests cover the supported set. |
| AC-005 | REQ-005 | `ci.yml` and `ci-full.yml` green on `main`; local `dotnet build -c Release` exits 0. |
| AC-006 | REQ-006 | Zero `Skip=` without issue reference; #531 and #73 closed or re-scoped. |
| AC-007 | REQ-007 | Coverage dashboard shows every 1.0 package green on all applicable flags; `latest.json` timestamp within the release week. |
| AC-008 | REQ-008 | Mutation dashboard shows a completed 17-shard run within the release month; `Stryker-xUnit-v3.md` updated with the outcome of #1087. |
| AC-009 | REQ-009 | CodeQL run within the release week with zero alerts ≥ threshold; Sonar per DEC-006. |
| AC-010 | REQ-010 | `Encina.Testing.Architecture` rules and `EventIdUniquenessRule` pass in CI. |
| AC-011 | REQ-011 | At tag time the union of open issues with the `bug` label and open issues whose title starts with `[BUG]` contains only issues labelled `deferred-1.0`, each with a reason comment; and no issue in that union carries a security classification (`security` label or `[BUG]` title tagged security) whatever its other labels — one such issue fails the gate even if it is labelled `deferred-1.0`. |
| AC-012 | REQ-012 | #852 closed; a guard/contract test exists per options class. |
| AC-013 | REQ-013 | RS0016/RS0017 clean; all `PublicAPI.Unshipped.txt` empty after release commit. |
| AC-014 | REQ-014 | `docs.yml` green; #1032 closed or its remaining warnings listed in the evidence report. |
| AC-015 | REQ-015 | Quickstart (#81), fundamentals (#82), providers overview (#83) and example 01 (#87) published; a fresh-environment walkthrough recorded in the evidence report. |
| AC-016 | REQ-016 | #927, #928, #929, #1090 closed; DocRef lint passes. |
| AC-017 | REQ-017 | Tag `v0.13.0` exists and its CHANGELOG section is dated; each subsequent minor tag has a dated section; at release the top section is `## [1.0.0] - <date>`. |
| AC-018 | REQ-018 | `release-on-milestone.yml` (or successor) packs, signs, attests and publishes; `Directory.Build.props` carries the metadata; #92, #93, #102 closed. |
| AC-019 | REQ-019 | A `1.0.0-rc.1` is published to NuGet.org through the workflow; #100, #101 closed. |
| AC-020 | REQ-020 | `global.json` present; `GET /repos/dlrivada/Encina/branches/main/protection` output in the evidence report shows `enforce_admins: true`, `required_approving_review_count: 0`, `required_conversation_resolution: true`, `required_linear_history: true` and exactly the contexts `build`, `ci-result`, `Analyze`; `ci.yml` shows `ci-result` needing `build` and every `test-*` job with `if: always()`; #98 closed. |
| AC-021 | REQ-021 | #95, #96, #97 closed. |
| AC-022 | REQ-022 | `docs/releases/v1.0.0/evidence.md` generated by a script from CI artifacts. |
| AC-023 | REQ-023 | Adversarial review report attached to the release PR with every finding dispositioned. |
| AC-024 | REQ-024 | One `SPEC-NNN` per compliance package under `docs/specifications/`, with an article coverage table; the package README links to it. The `Encina.Compliance.Consent` specification has two tables, GDPR and ePrivacy (Art. 5(3) at minimum), each row pointing at its test or audit evidence. Every table has the shape of SPEC-002 REQ-022, including the national rows it names (the Consent specification adds an LSSI arts. 20–22 table), and SPEC-002 AC-022 holds. |
| AC-025 | REQ-025 | EPIC #881 closed with its twelve compliance child issues (#836–#847); #71, #72, #74 moved out of it (done 2026-09-21). |
| AC-026 | REQ-026 | #822–#827 and #810–#816 closed; #804–#808 labelled post-1.0 and left open; SPEC-002 AC-024 holds (each Omnibus behaviour off by default, with a test asserting that the default follows current law). |
| AC-027 | REQ-027 | #207, #208 closed and the Memcached scope of #277 delivered; contract tests cover the 8 caches and the 5 locks (InMemory, Redis, SqlServer, PostgreSQL, MySQL); the `CLAUDE.md` lock table marks exactly those five as shipped and the other five as post-1.0, and both provider tables match `src/`. |
| AC-028 | REQ-028 | #205 labelled post-1.0; `CLAUDE.md` cloud section lists AWS + Azure with GCP deferred. |
| AC-029 | REQ-029 | On the release-candidate commit, SPEC-002 AC-001 – SPEC-002 AC-044 hold; the PracticeManagement reference scenario is green in CI Full with tenancy on (SPEC-002 AC-038); the SPEC-002 EPIC is closed. |

## 5. Constraints

- .NET 10 only; C# 14; nullable enabled (`CLAUDE.md`).
- The per-flag obligations coverage model is normative (`AI-DEVELOPMENT-MODEL.md` §3).
- Provider coherence rules for database (10), caching, transports, locks and validation apply to whatever is in the 1.0 list (`CLAUDE.md`), with the actual set fixed by DEC-003.
- Cross-cutting integration rule (ADR-018) applies to any new code written for 1.0.
- EventId allocation via `EventIdRanges.cs` (ADR-021).
- Existing quality infrastructure is orchestrated, not replaced (`ENCINA-1.0-RECONCILIATION.md` §11).
- Scripting: PowerShell or C# file-based apps only (`CLAUDE.md`).
- Every capability SPEC-002 adds or changes is tenant-aware and instrumented, and security- and compliance-relevant behaviours fail closed unless an explicit, logged opt-out says otherwise (SPEC-002 §8, SPEC-002 DEC-006, SPEC-002 DEC-009, SPEC-002 DEC-010).

## 6. Non-goals

- New providers, transports, cloud integrations, AI/LLM features, source generators, hot reload, dashboards, EIP part 2, Aspire integration, modular-monolith features (milestones v0.14.0 – v0.20.1). They are not blocked, they are simply not part of the 1.0 contract (DEC-002). **Exceptions (DEC-007):** what SPEC-002 places in 1.0 is in the contract even where it is a new package, integration or provider:
  - the integration and platform capabilities of SPEC-002 §5.11: `Encina.Http` (SPEC-002 REQ-039), `Encina.Security.OAuth` (SPEC-002 REQ-040), `Encina.Notifications` (SPEC-002 REQ-041), ordered outbox partitions with pacing (SPEC-002 REQ-042, #469), domain events written to the outbox in the same commit (SPEC-002 REQ-043), saga correlation by external key and wait-for-event (SPEC-002 REQ-044), `ISequenceGenerator` (SPEC-002 REQ-045), `Encina.Storage` (SPEC-002 REQ-046), the persistent hash-chained log (SPEC-002 REQ-047), trusted time-stamping and e-signature integration points (SPEC-002 REQ-048), breach notifier channels (SPEC-002 REQ-049), DPIA screening against the AEPD lists (SPEC-002 REQ-050), break-the-glass access (SPEC-002 REQ-051), EHDS logging and export extension points (SPEC-002 REQ-052), X.509 certificate retrieval (SPEC-002 REQ-053), the relational `IPersonalDataLocator` (SPEC-002 REQ-054), column-level encryption at rest (SPEC-002 REQ-055), row claiming in the outbox and scheduler processors (SPEC-002 REQ-056), ISO/IEC 27001 and 27701 control mappings (SPEC-002 REQ-057) and VEX statements (SPEC-002 REQ-058);
  - the channel satellites of `Encina.Notifications`, the webhook presets of the ingestion core (SPEC-002 REQ-036) and the storage providers of `Encina.Storage` listed in SPEC-002 DEC-016: per category, the minimum the reference scenario exercises plus one alternative;
  - the multi-tenancy of SPEC-002 REQ-061, including the tenancy issues SPEC-002 DEC-009 moves into 1.0 (#737, #738, #739, #760, #798, #596); the rest of "Post-1.0: Multi-Tenancy Core" (#876) stays post-1.0.
- What SPEC-002 keeps out of 1.0 stays a non-goal: Verifactu-specific code in public Encina, since only regulation-neutral primitives ship (SPEC-002 DEC-002); a payments abstraction (SPEC-002 DEC-003); further channel satellites, webhook presets and storage providers beyond SPEC-002 DEC-016; persistent dead-letter stores (#583, #149; the dead-letter state, its count and its requeue in the outbox table are in 1.0, SPEC-002 REQ-017); relational stores for the Marten-only compliance modules, which require PostgreSQL in 1.0 (SPEC-002 DEC-008); the rules part of SPEC-002 REQ-011.
- New regulation packages beyond the ones that exist today: DORA, eIDAS2, Data Act, ENS, EHDS (#804–#808) are the first post-1.0 features (DEC-002, SPEC-002 DEC-015). Only the generic extension points the reference application needs are in 1.0: EHDS logging and export (SPEC-002 REQ-052) and trusted time-stamping and e-signature integration points (SPEC-002 REQ-048).
- Benchmarks and load tests for every package (v0.21.0); only critical paths (mediator pipeline dispatch #560, messaging orchestrators #561).
- Reaching a specific mutation score.
- Closing every open issue.
- Backward compatibility with any pre-1.0 version.

## 7. Invariants

- **INV-001** Nothing is claimed in public documentation that CI cannot regenerate.
- **INV-002** No production change lands without a green `ci.yml`.
- **INV-003** Every architectural choice made while satisfying a requirement is recorded as an ADR with alternatives and the human decision.
- **INV-004** The P0 backlog only shrinks; adding to it requires a recorded reason referencing a requirement in this document. An item that SPEC-002 promotes into 1.0 meets this through REQ-029: its recorded reason cites REQ-029 and the SPEC-002 requirement it implements (for example "REQ-029 / SPEC-002 REQ-005").
- **INV-005** Agents do not change this document; they propose changes as a PR to it with the human as approver.
- **INV-006** No agent commits to `main` directly, with or without administrator rights; every change reaches `main` through a pull request whose required checks are green (DEC-006).

## 8. Verification

| Requirement group | Method |
|---|---|
| Inventory (REQ-001–004) | Inventory script + CI drift check; contract tests per provider set. |
| Build/quality gates (REQ-005–010) | `ci.yml`, `ci-full.yml`, `mutation-tests.yml`, `codeql.yml`, architecture tests; dashboards regenerated. |
| Defects/security (REQ-011–012) | Issue tracker state at tag time; guard/contract tests. |
| API/docs (REQ-013–017) | PublicAPI analyzers; `docs.yml`; DocRef lint; walkthrough in evidence report. |
| Release (REQ-018–021) | Dry run against `1.0.0-rc.1`; workflow logs and NuGet.org listing in evidence report. |
| Evidence (REQ-022–023) | Generated evidence report; adversarial review report. |
| Regulatory compliance and readiness (REQ-024–026, REQ-029) | Per-package article specifications; the verification methods of SPEC-002 §10; the PracticeManagement reference scenario in CI Full; SPEC-002 acceptance criteria listed in the evidence report. |

## 9. Human decisions required

These are Class C. Agents have presented the options; the maintainer decides. Once decided, each becomes a short ADR (or a line in this table with the decision and reason) and this document moves to APPROVED.

| ID | Decision | Options presented | Agent recommendation | **Human decision** |
|---|---|---|---|---|
| **DEC-001** | Canonical package list and disposition of the 11 projects outside the solution (#1089), incl. `Encina.Secrets.*` vs `Encina.Security.Secrets.*` | (a) `Security.Secrets` canonical, delete `Secrets.*`; (b) the reverse; (c) keep both (rejected: REQ-003). AIAct/Consent: (a) add to solution and 1.0; (b) defer. Testing.*: add to solution. | (a) for Secrets; Consent and AIAct in 1.0 (EU regulation is mandatory for the target applications); Testing.* in. | **Decided 2026-09-21.** (a): `Encina.Security.Secrets.*` is canonical (#400 superseded #603 three days after it landed; #452 closed as superseded). The five `Encina.Secrets.*` projects deleted from `src/`. The other six projects (AIAct, Consent, Testing.Architecture, Testing.FsCheck, Testing.Testcontainers, Testing.Verify) were never in `Encina.slnx` but are built and tested through ProjectReferences; they are now listed in the solution (99 → 105 projects). AIAct and Consent are part of 1.0; AIAct gets a short article-coverage audit before any "AI Act compliant" claim (REQ-024). |
| **DEC-002** | Release scope: which milestones/EPICs are part of 1.0, and whether 1.0 has a target date | (a) consolidation only, no EPIC; (b) existing modules hardened + the compliance EPICs #881 (AI Act) and #880 (NIS2 + Digital Omnibus), new regulation packages post-1.0; (c) whole feature milestones v0.14–v0.20. Date: fixed vs none. | (b); no date; benchmarks only for #560/#561. | **Decided 2026-09-21.** (b). No target date: sequencing matters, the calendar does not. EPIC #881 complete (twelve compliance issues), EPIC #880 without DORA/eIDAS2/Data Act/ENS/EHDS (#804–#808 post-1.0). #71, #72, #74 moved from #881 to v0.21.0 because they are testing/CI work, not AI Act. Feature milestones v0.14–v0.20 are not blocked, just outside the 1.0 contract. |
| **DEC-003** | Provider set for 1.0 where `CLAUDE.md` and `src/` disagree: caching (Memcached absent), locks (PostgreSQL/MySQL absent), cloud (GCP absent) | (a) ship what exists and amend `CLAUDE.md`; (b) implement the missing ones before 1.0. | (a), or (a) plus the two database locks. | **Decided 2026-09-21.** Caching and distributed locks are implemented **as documented** before 1.0: Memcached (#277, the Memcached part only), PostgreSQL `pg_advisory_lock` (#207) and MySQL `GET_LOCK` (#208), so the 8-cache rule in `CLAUDE.md` becomes true and the 1.0 lock set is five providers (InMemory, Redis, SqlServer, PostgreSQL, MySQL); `CLAUDE.md`'s lock heading, which said "4 existing + 8 planned" over a table of 3 + 7, is corrected to match. Cloud stays as it is: Google Cloud Functions (#205) is post-1.0 and `CLAUDE.md`'s cloud triangle is amended to say GCP is deferred. |
| **DEC-004** | Severity thresholds: NuGet audit level that blocks the build, CodeQL/Sonar level that blocks release, SLSA level for provenance | Audit: block on ≥ moderate (current behaviour) vs ≥ high. Static: block on ≥ high. Provenance: SLSA L2 (#92) vs GitHub artifact attestations only. | Audit ≥ moderate (keep), static ≥ high, GitHub attestations + Sigstore signing (#93). | **Decided 2026-09-21.** NuGet audit keeps breaking the build at **moderate or higher** (the five red months were a process failure, not a threshold failure). Static analysis blocks the release at **high or higher**; medium findings are listed with a disposition in the evidence report. Provenance = **GitHub Actions artifact attestations + Sigstore-signed packages** (#92, #93), which satisfies SLSA L2 in practice without separate infrastructure. |
| **DEC-005** | Versioning and CHANGELOG: go from 0.13.0-dev straight to 1.0.0-rc.1, or cut 0.13.0 first; how to consolidate the 2,621-line Unreleased section | (a) tag 0.13.0 now as a checkpoint, then one minor version per 1.0 block, then rc; (b) skip to 1.0.0-rc.1 and fold Unreleased into a "0.13 → 1.0" section. | (a). | **Decided 2026-09-21.** (a). Once the build-repair and DEC-001 branches are on `main`, the `[Unreleased] - v0.13.0` section becomes `[0.13.0] - <date>` as is, the maintainer's agent creates tag `v0.13.0` (which triggers `ci-full` and the dashboard publishes), and `VersionPrefix` moves to `0.14.0`. Each remaining 1.0 block then closes its own minor version (AI Act, NIS2 + Digital Omnibus, providers + release engineering, …) before `1.0.0-rc.1`. Milestones v0.14 – v0.23 are renumbered **after** the per-issue P0–P3 classification, not before. |
| **DEC-006** | Process policy: keep SonarCloud (needs #75) or drop it in favour of CodeQL + analyzers; branch protection on `main` with required `ci.yml`; agents commit only via PR | (a) keep Sonar; (b) drop Sonar and remove the claim. Protection: on/off. | Keep Sonar (it turned out to be configured already), protection on with `enforce_admins`, PR-only for all agents. | **Decided 2026-09-21.** SonarCloud stays (token and project already exist; quality gate currently *failed* on stale data; blocks at high per DEC-004; #75 closed as done). Branch protection on `main` stays and is corrected: required checks aligned with the real `ci.yml` job names (the list required a non-existent `Build` and the retired SQLite EF shard), `Check formatting` and CodeQL `Analyze` required, `enforce_admins` on so nobody bypasses it, and the SQLite entry removed from the `test-ef-providers` matrix (ADR-024). Agents, including the maintainer's own, deliver **only through pull requests**: no direct commits to `main` (INV-006). |
| **DEC-007** | Widen the 1.0 scope to the pre-1.0 scope decided in SPEC-002 (SPEC-002 §11.2 and SPEC-002 AC-040), and accept the later 1.0 date that follows | (a) full readiness: every item of SPEC-002 §11.2 tables 1 and 2, with the relational `IPersonalDataLocator` and column-level encryption (SPEC-002 DEC-011 (a) + (d) + (e)) and multi-tenancy (SPEC-002 DEC-009 (b)); (b) lifecycle only: the defects, retention floor, erasure arbitration, blocking, persisted request context and the AI Act catalogue, everything else post-1.0; (c) this document as approved on 2026-09-21 | (a) (SPEC-002 DEC-011) | **Decided 2026-09-24.** (a). The maintainer took the scope decisions in SPEC-002 on 2026-09-23 (SPEC-002 DEC-011 (a) + (d) + (e); every capability of SPEC-002 §5.11 in 1.0) and 2026-09-24 (SPEC-002 DEC-001 (b), DEC-009 (b), DEC-012, DEC-014, DEC-015 and DEC-016, all SPEC-002) and approved the widened 1.0 scope on 2026-09-24; this amendment records it and takes effect when its pull request merges (INV-005). Recorded here: §2 extends the contract to EU regulatory readiness, the Spanish national provisions SPEC-002 turns into requirements, sensitive health data and multi-tenancy; REQ-029 incorporates the SPEC-002 pre-1.0 requirements by reference; §6 no longer excludes the SPEC-002 §5.11 capabilities, the SPEC-002 DEC-016 satellites and providers or the SPEC-002 REQ-061 multi-tenancy; REQ-024 and REQ-026 point to SPEC-002 (law map, article tables, coverage matrix; Omnibus adaptations built before 1.0, off by default). **1.0 date:** the maintainer accepted that 1.0 arrives materially later (up to 62 P0 and 6 P1 new issues, 25 moved issues, six 10-provider schema families and four new stores, SPEC-002 §11.2); DEC-002's "no target date" stands. **Milestones (SPEC-002 DEC-014):** "v0.14.0 — Hardening" holds defects only; three new pre-1.0 milestones, "Compliance Lifecycle (SPEC-002)", "Integration & Platform Capabilities (SPEC-002)" and "Reference Scenario (SPEC-002)", join the DEC-005 sequence and each closes its own minor version before `1.0.0-rc.1`; v0.15.0 and v0.16.0 keep their numbers. **Milestone numbers assigned (maintainer, 2026-09-24):** the three new milestones are `v0.17.0 — Compliance Lifecycle`, `v0.18.0 — Integration & Platform Capabilities` and `v0.20.0 — Reference Scenario`; the existing `v0.17.0 — Providers & Testing`, `v0.18.0 — Documentation` and `v0.19.0 — Release Engineering` are renumbered to `v0.19.0 — Providers & Testing`, `v0.21.0 — Documentation` and `v0.22.0 — Release Engineering` respectively; `v0.15.0`, `v0.16.0` and `v1.0.0-rc.1` are unchanged. |

## 10. Traceability

| Source | Requirements |
|---|---|
| `PHASE0-BASELINE.md` F-01 | REQ-005 |
| F-02, F-03 | REQ-001, REQ-002, REQ-003 |
| F-04, F-05, F-06 | REQ-007, REQ-008, REQ-009 |
| F-07 | REQ-018, REQ-019, REQ-020 |
| F-08 | §2, §6, DEC-002, REQ-024–026 |
| F-09 | REQ-017, DEC-005 |
| F-13 | REQ-011, REQ-012 |
| F-14 | REQ-004, REQ-027, REQ-028, DEC-003 |
| F-15 | REQ-016 |
| F-16 | REQ-020, DEC-006 |
| `ENCINA-1.0-RECONCILIATION.md` §6.1, `Stryker-xUnit-v3.md` §5 | REQ-008 |
| `ENCINA-1.0-RECONCILIATION.md` §8 (EPIC #893) | REQ-018 – REQ-021 |
| [SPEC-002](SPEC-002-eu-regulatory-readiness.md) §2, §11.2 (SPEC-002 DEC-001, DEC-009, DEC-011, DEC-012, DEC-014 – DEC-016) and SPEC-002 AC-040 | §2, §5, §6, REQ-024, REQ-026, REQ-029, INV-004, DEC-007 |

## 11. Change log

| Date | Change |
|---|---|
| 2026-09-21 | DEC-001 … DEC-006 decided by the maintainer; status APPROVED. |
| 2026-09-24 | Amendment required by SPEC-002 AC-040 (DEC-007). §2 extends the 1.0 contract to EU regulatory readiness as SPEC-002 defines it, the Spanish national provisions SPEC-002 turns into requirements (LOPDGDD arts. 7, 9.2 and DA 17ª, 32; Ley 41/2002 arts. 17.1, 18.3, 18.4; LSSI arts. 20–22), sensitive health data (SPEC-002 DEC-001 (b)), multi-tenancy (SPEC-002 DEC-009 (b)) and observability, and records the later 1.0 date. New REQ-029 and AC-029 incorporate the SPEC-002 pre-1.0 requirements by reference. REQ-024 and AC-024 point to SPEC-002 for the law map, the article-table shape with national rows and the coverage matrix. REQ-026 and AC-026 record SPEC-002 DEC-012 (all seven Omnibus adaptations built before 1.0, off by default) and the SPEC-002 DEC-015 extension points. §5 gains the SPEC-002 tenancy, telemetry and fail-closed constraint. §6 no longer excludes the SPEC-002 §5.11 capabilities, the SPEC-002 DEC-016 satellites and providers or the SPEC-002 REQ-061 multi-tenancy, and lists what SPEC-002 keeps out. INV-004 names REQ-029 as the reference for SPEC-002 promotions. §8 and §10 gain rows; this change log is added. Status stays APPROVED (amended). |
| 2026-09-24 | Milestone numbers for SPEC-002 DEC-014 assigned; v0.17.0–v0.19.0 renumbered to v0.19.0, v0.21.0, v0.22.0. |
| 2026-09-24 | SPEC-002 REQ-059 (consent for minors) included: the reference practice treats minors. |

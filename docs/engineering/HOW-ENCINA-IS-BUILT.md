# How Encina is built

> A public account of the working method behind Encina: a specification-driven process adapted for one maintainer, a paid frontier model, a free local model and cost-tiered agents, on top of a quality system that measures itself. Written so that an engineer outside the project can understand it and replicate it. Tracking: #1104 (EPIC #1102). Internal counterpart: [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md); task routing: [`ai-task-routing.md`](ai-task-routing.md).
>
> Every figure in this document is cited to an issue, a pull request, a dashboard or a ledger file in the repository. Nothing is typed from memory (policy from #1090). Sections 2 to 4 were first drafted by the free local model from the internal documents and then rewritten; section 6 records the cost of that experiment.

## How to read this

- If you want to contribute, start with the contributor guide (#1103) and come back here for the "why".
- If you want to copy the setup for your own project, read sections 3, 4 and 7.
- If you want the honest numbers, read section 6.

## 1. The problem

Encina is a large pre-1.0 .NET library: 105 projects in the solution, ten database providers with strict coherence rules, twelve cross-cutting concerns that every feature must address, and a compliance surface (GDPR, ePrivacy, NIS2, AI Act) that is part of the 1.0 contract because the target applications are European (SPEC-000 §2). It is maintained by one person.

In spring 2026 the project stalled for several months. When work resumed in September 2026, a diagnostic pass found the state that a stalled solo project tends to reach: the solution did not build (283 NuGet audit errors promoted to build errors by `TreatWarningsAsErrors`), every CI workflow had been red since May, the quality dashboards were frozen on data from April, the package inventory had four different answers depending on which document you read, and the public metrics in the roadmap were not backed by anything measurable ([`PHASE0-BASELINE.md`](PHASE0-BASELINE.md) §1, #1088, #1089, #1090).

Two constraints shape everything that follows. The paid AI budget is small and fixed. And there is no release date: the order of the work matters, the calendar does not (SPEC-000 DEC-002). The method described here is the answer to "how does one person, with a limited budget and no deadline, get a project of this size to a credible 1.0 without lying to themselves about its state".

## 2. The adapted spec-driven process

### 2.1 Specifications first, decisions explicit

The root problem the process attacks is knowledge debt: engineering decisions that exist only in GitHub comment threads or in the maintainer's memory, so that every new session (human or agent) has to reconstruct them from hundreds of comments before it can act ([`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md) §2, §17). The remedy is that important knowledge lives in the repository, specifications precede implementation, and architectural decisions stay explicit and traceable (§1).

Three kinds of artifact carry that knowledge (§19):

| Artifact | Lives in | Answers |
|---|---|---|
| Specification `SPEC-NNN` | `docs/specifications/` | What must be true, and how we know (requirements `REQ-`, acceptance criteria `AC-`, invariants `INV-`, decisions `DEC-`) |
| Architecture decision record | `docs/architecture/adr/` | How we chose to make it true, and what we rejected |
| Plan | `docs/plans/` | In what order, and where we are |

A specification is not documentation written after the fact. It is the reference against which implementation and verification are judged, and it separates "what the system should do" from "how we choose to make it true" (§5, §6). The first specification of the new phase, [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md), defines the 1.0 boundary itself: 28 requirements, 28 acceptance criteria, 6 invariants and 6 human decisions, each decision recorded with the options presented, the agent's recommendation and what the maintainer actually chose.

### 2.2 The roles

The process is organised as a pipeline of roles rather than a single "assistant". Each role has an input, an output and one question it answers (§9 to §13):

- **Historian.** Mines issues, comments, code and docs into durable knowledge with mandatory provenance (link, date, quote). Answers: which past decisions and failures matter for the current state?
- **Auditor.** Evaluates the repository against that knowledge: architecture, provider coherence, tests, coverage, public API, observability, documentation, cross-cutting integration. Answers: does the repository conform to its own rules?
- **Specifier.** Turns a problem statement plus context into a `SPEC-NNN`. Answers: what exactly must the system do, and how will we know it is done?
- **Architect.** Produces the decision analysis: alternatives, trade-offs, consequences, reversibility, a recommendation. Answers: how should this be designed within the existing constraints?
- **Human decision gate.** The maintainer resolves genuine trade-offs and Class B/C findings. Answers: which trade-offs are acceptable for this product?
- **Implementer.** Builds what an approved specification says, or fixes deterministic findings. Answers: how do we build what was specified?
- **Verifier.** Produces evidence, not opinions: tests, coverage per flag, architecture rules, API rules. Answers: can we prove the implementation wrong?
- **Adversarial Reviewer.** Assumes the specification may be incomplete and the tests may give false confidence, and looks for counterexamples. Answers: what did every previous stage miss?

In practice a single model plays several roles in sequence; the roles matter because they force the outputs (a report with provenance, a specification, a decision analysis, evidence) rather than a conversation.

### 2.3 Findings, classes and priorities

Anything the Auditor or the reviewers find is classified before anyone acts on it (§14):

- **Class A** is mechanical or deterministic: the repository already contains enough information to know the right fix (a missing XML comment, a missing `PublicAPI` entry, a missing test registration). Agents may fix Class A on their own.
- **Class B** is a conflict with an explicit decision or contract: either the implementation drifted from a recorded decision, or a change would alter a public contract. Class B is always an escalation, never a silent fix: the agent must say whether the code or the decision is wrong, and let the human choose.
- **Class C** is a genuine open decision with more than one valid option. The agent presents the alternatives and a recommendation; the human decides.

Backlog priority uses a separate P0 to P3 scale ([`ENCINA-1.0-RECONCILIATION.md`](ENCINA-1.0-RECONCILIATION.md) §4) so that "how serious is this finding" and "when do we do this work" never share a letter.

### 2.4 The human decision gate

Human attention is the scarcest resource, so the process concentrates it where it changes the outcome (§15). The maintainer decides product semantics, architectural trade-offs, acceptable complexity, provider support policy, security trade-offs and anything irreversible. The maintainer is not asked to inspect null checks, assertions, XML comments, repetitive provider implementations or formatting.

Decisions are recorded where the next agent will find them: in the specification's decision table or in an ADR, with the options, the recommendation and the reason the human chose otherwise when they did (§7). On 2026-09-21 the six decisions of SPEC-000 were taken this way, one by one, in a conversation in the maintainer's language, and then written into the specification in English; the conversation is not the record, the specification is.

### 2.5 What is different from textbook spec-driven development

- **Provenance is mandatory.** A Historian report without links, dates and quotes is a draft, not knowledge, and cannot be promoted to a rule or an ADR (§9). Verification then means following the links, not re-reading history.
- **Artifacts, not conversation.** Anything important must survive the end of a session. The next agent receives files (reports, specifications, pull requests, issue comments), never hidden conversational context (§4). The two AI tiers described in section 3 do not talk to each other at all; they exchange artifacts ([`ai-task-routing.md`](ai-task-routing.md) §6).
- **No prompt bureaucracy.** The process started with the smallest set of roles that could demonstrate the model, and adds an agent only when it shows measurable value; every extra agent costs context, maintenance and coordination (§20).
- **Agents work through pull requests only.** One task, one worktree, one branch; two agents never share a branch; an agent never commits to `main`; every change reaches `main` through a pull request whose required checks are green (§4, SPEC-000 INV-006). This is enforced by branch protection, not by good intentions (section 4.5).
- **The human gate is narrow on purpose.** Textbook processes tend to widen human review as the stakes rise. Here the gate is kept to genuine trade-offs, and the rest is pushed into evidence that machines can check.

### 2.6 The closed-issue audit pipeline

A closed issue carries design and code that already exist, but not the rest of the specialist pipeline that normally checks work before it becomes a permanent part of Encina: no independent reviewer, no tester, no QA gate. The SPEC-003 audit (#1345, [`docs/specifications/SPEC-003-closed-issue-knowledge-migration-and-quality-audit.md`](../specifications/SPEC-003-closed-issue-knowledge-migration-and-quality-audit.md)) runs that missing half, issue by issue, and the [`issue-audit` skill](../../.claude/skills/issue-audit/SKILL.md) is the orchestrator's procedure for it.

One audit runs at a time, in its own worktree, through six fixed stages, each with exactly one owner and exactly one artifact: an archivist builds and verifies the knowledge record and scope, a code auditor adversarially reviews today's code in that scope (including the siblings the issue's own fix did not reach), a test auditor measures per-flag coverage and test quality against the manifest, a docs reviewer checks what the issue delivered in documentation, a remediation stage drafts one follow-up issue per finding with the free local model, and an independent verifier re-checks every prior claim against its source and returns a `Verdict: PASS` or `Verdict: FAIL` — never fixing anything itself, only naming the stage to re-run. Lessons the stages surface flow back into the pipeline: either applied now, recorded as not applicable, or written into a stage agent's own memory file so the next audit starts smarter.

What holds this together is hooks, not prose: `audit-stage-guard.ps1` refuses to spawn a stage agent out of order, for the wrong issue, or below the pipeline's minimum model, and `enforce-path-ownership.ps1` refuses to let anyone but the stage's assigned agent write that stage's artifact — the fabrication gap that let an earlier, unversioned coordinator claim a stage was done when it was not.

Credit where it is due: the single-owner-role, mandatory-handoff and independent-QA discipline this pipeline enforces adapts the ideas of [unclebob/swarm-forge](https://github.com/unclebob/swarm-forge) to Claude Code, PowerShell and C# (see [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md) §4 for the principles and what Encina adopted from each).

## 3. The AI layer

### 3.1 Two models, one budget

Two models share the work ([`ai-task-routing.md`](ai-task-routing.md) §1 to §3):

| | Paid frontier model | Free local model |
|---|---|---|
| What | Claude (the maintainer's subscription) | Qwen 3.8 27B served by `llama-server` (llama.cpp, CUDA) on the maintainer's workstation |
| Cost of a token | Paid, budgeted | Zero |
| Used for | Specifying, deciding, verifying, adversarial review, anything where an error is expensive | Bulk reading, classification, summaries, mechanical fixes, first drafts, coverage tests from a precise brief |
| Selection rule | Low volume, high cost of error | High volume, low cost of error |

The split is a routing hypothesis that is revised as the local model's behaviour is observed, not a permanent norm (§1). The economic goal is to keep paid tokens for the decisions that matter and move everything mechanical to the free tier.

### 3.2 What the local model does well, and where it fails

The local model's limits were measured by the maintainer over many trials before any work was delegated ([`ai-task-routing.md`](ai-task-routing.md) §2.1):

- It does not hallucinate verifiable data when it has tools; checked against the GitHub API.
- It reads and summarises the project's non-standard documents correctly (the mutation methodology, the coverage obligations model).
- It silently drops parts of long multi-point instructions. Section 6.4 has a measured example.
- On open-ended research it can loop, re-exploring the same files until the context is exhausted; one such session burned close to a million tokens and delivered nothing.
- Its speed (60 to 100 tokens per second) is never the bottleneck; reliability over long sessions is.

Briefs are written around those limits: one bounded task per brief, numbered points with "without omitting any", the exact output path, the sources it may read and nothing else, the production facts it needs stated in the brief (so it never has to explore `src/`), and "stop when the file is written". Open-ended work is split into an investigation prompt that writes findings to a file and a second prompt that forbids further searching. Format rules that must not be dropped go in the system prompt and in the first line of the brief, not in point 4 (section 6.4).

### 3.3 Roles and routing

Each role of section 2.2 has a default tier ([`ai-task-routing.md`](ai-task-routing.md) §4):

| Role | Tier | Why |
|---|---|---|
| Historian | Local, sampled by Claude | High volume, verifiable through provenance |
| Auditor, passes 1 and 2 | Local | Mechanical checks |
| Auditor, passes 4 and 6 | Local draft, Claude consolidates | Consistency analysis needs judgement at the end |
| Specifier | Claude | An omission in the requirements is the most expensive error there is |
| Architect | Claude | Trade-off reasoning |
| Human decision gate | Human | Not delegable |
| Implementer, mechanical | Local | Bounded, verifiable |
| Implementer, feature from an approved SPEC | Claude | Interpreting a specification is not a bounded task |
| Verifier, first pass | Local | Tests, drift checks |
| Verifier, final gate | Claude | The last word must not come from a model that skips instructions |
| Adversarial Reviewer | Claude | The hardest reasoning in the pipeline |

Routing is decided consciously before a task starts, with two GitHub labels: `ai:local-candidate` and `ai:claude-required` (§6).

### 3.4 Agents by cost tier

The paid tier is itself layered. The main session runs on the most capable model available and keeps only the roles above marked Claude. Everything it spawns runs on the cheapest model and effort that does the job, declared once per agent type in [`.claude/agents/`](../../.claude/agents/README.md) (#1099):

| Agent | Model / effort | Role | Writes? |
|---|---|---|---|
| `pr-watcher` | Haiku, low | Reports each failed check, bot review and the merge of a pull request as they happen | No |
| `ci-diagnoser` | Sonnet, medium | Root-causes one failed job or test and proposes the minimal fix | No |
| `mechanical-fixer` | Sonnet, low | Executes an already-decided change in its own worktree, verifies, commits | Yes, own worktree only |
| `adversarial-reviewer` | Opus, high | The Adversarial Reviewer of section 2.2 | No |

Below all of them sits a watcher that costs no model tokens at all: a script that polls GitHub and emits one line per event (a failed check, a bot reply, a merge), which the main session reacts to (section 6.2).

The counterpart for the free tier lives in `.opencode/agents/` and `.opencode/skills/`: opencode is the orchestrator that points the local model at the repository through an OpenAI-compatible provider ([`ai-task-routing.md`](ai-task-routing.md) §2). For work that only needs "read these documents, produce this file", a small C# script calls the local server directly, without an agent loop, and records the exact token usage per task (section 6.3).

### 3.5 Verification is never delegated

Whatever the local model produces is verified by Claude or by the maintainer before it counts: a sample of a report's provenance links is followed, delegated tests are rebuilt and re-run, delegated documents are spot-checked against their sources ([`ai-task-routing.md`](ai-task-routing.md) §2.1). The Verifier produces evidence, not "looks good": which automated tests prove which acceptance criteria, which negative cases exist, whether provider variants behave coherently, whether the architecture, public API and coverage rules pass ([`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md) §12). The Adversarial Reviewer then assumes the implementation is wrong and looks for the counterexample (§13). Only then does the human gate see it, and only if there is a real decision to take (§15).

The mechanical guarantee behind all of this is that every change, from either tier, enters `main` through a pull request with green required checks (§4). Section 6.5 shows what happened the first time that rule was tested against a bot's stale review.

## 4. The quality system

### 4.1 Coverage as obligations, not a percentage

Encina does not have a project-wide coverage percentage. Each test type is a flag: `unit`, `guard`, `contract`, `property`, `integration`. Coverage is measured independently per flag: a line covered by a unit test contributes nothing to the guard or contract flag ([`AGENTS.md`](../../AGENTS.md) §9, "Testing obligations"). A manifest per package (`.github/coverage-manifest/{Package}.json`) declares which flags apply to each source file and the target percentage per flag. The number of coverable lines differs per flag, because each test type exercises different code paths, so a file's obligations are the sum of its per-flag coverable lines, not one figure repeated. The full CI run executes each test project separately, collects one Cobertura report per flag, and `coverage-report.cs` computes the obligations; a package is green only when every applicable flag reaches its own target. The [coverage dashboard](https://dlrivada.github.io/Encina/coverage/) shows exactly that, per package and per flag; its overall percentage is informational ([`TESTING.md`](../en/guides/TESTING.md), "Coverage").

One rule follows from the model and catches a whole class of fake tests: a test must execute real package code. Reflection-only tests (`typeof(T).GetMethod(...)`, `IsInterface.ShouldBeTrue()`) load metadata and cover zero lines, so contract and property tests must instantiate real implementations.

### 4.2 Test types and when each applies

Seven test types, each with a place and a rule ([`AGENTS.md`](../../AGENTS.md) §9, "Testing obligations"): unit (always), guard (every public method's argument validation), contract (public interfaces and API shape), property (FsCheck invariants, required for database features), integration (real databases in Docker through shared collection fixtures, mandatory for database features and never waived), load (only for concurrent behaviour such as unit of work, tenancy, read/write separation) and benchmarks (hot paths only). When a test type is legitimately not implemented for a feature, a justification document with a fixed structure sits where the tests would be; a folder with neither tests nor a justification means the question was never asked. Justifications are never accepted for unit, guard, contract, or for integration tests of database features.

### 4.3 Mutation testing that survives its own tooling

Mutation testing (Stryker.NET) runs weekly across the 17 folders of the core package as a parallel GitHub Actions matrix ([`AGENTS.md`](../../AGENTS.md) §9, "Testing obligations"; [methodology](../testing/mutation-measurement-methodology.md)). Two upstream bugs shape the design: xUnit v3 breaks Stryker's per-test coverage analysis, so every mutant would run the whole suite; and Stryker only honours a test filter from its config file. The workaround pairs each folder with a test-namespace filter patched into the config before the run, so each mutant runs tens to hundreds of tests instead of tens of thousands. Shards fail independently (`fail-fast: false`, `continue-on-error`), an aggregate job merges the per-shard reports, and the publish step carries forward per-file results for folders that did not run, so one flaky shard never erases a week of data. The [mutations dashboard](https://dlrivada.github.io/Encina/mutations/) accumulates per file; there is no project-wide mutation target.

### 4.4 Citations instead of hand-typed numbers

Any number in documentation that comes from a measurement is cited, not typed. Benchmarks, load tests and mutation results each publish a DocRef index with the dashboard; documents contain markers (`docref-table`, `mutref-table`, inline `docref:`/`mutref:`) that a renderer expands after every publish, a reverse index records which document cites which measurement, and the dashboards show a "Cited In" column ([`AGENTS.md`](../../AGENTS.md) §9, "Testing obligations"). Markers inside fenced code blocks are ignored so that documents can show the syntax. Coverage is the one dimension still missing a citation system; [SPEC-001](../specifications/SPEC-001-coverage-docref-citations.md) specifies it and #1092 tracks it. The reason the rule exists is #1090: the roadmap claimed "92.3% coverage", "0 build warnings" and "6,500+ tests" while the build was broken and the dashboards were five months stale.

### 4.5 Guard rails in the build and on the branch

- Zero warnings: every analyzer warning is an error, including nullability, code style and NuGet audit advisories at moderate severity or higher (SPEC-000 DEC-004). The five red months of 2026 were caused by that rule doing its job on a vulnerable Marten version while nobody was there to react; the rule was kept.
- Public API tracking: every public member is declared in `PublicAPI.Unshipped.txt`, so an API change is visible in the diff.
- Structured logging: every `[LoggerMessage]` event id belongs to a range registered in one file, and architecture tests assert global uniqueness, range membership and non-overlap ([`AGENTS.md`](../../AGENTS.md) §7, "Structured logging and EventIds").
- Branch protection on `main` (SPEC-000 REQ-020, DEC-006): `enforce_admins` on, linear history, conversation resolution, no self-approval requirement (one maintainer cannot approve their own pull request), and exactly three required checks: `build`, `ci-result` and CodeQL `Analyze`. `ci-result` is an always-run job that fails if any upstream job failed or was cancelled, so the protection never lists matrix job names and never goes stale when shards are renamed; the previous configuration had done exactly that and blocked merges on a shard that no longer existed.
- Bots on the pull request: CodeRabbit (manual trigger, one review per hour on the free tier), Codecov patch coverage, SonarCloud, CodeQL, link check, docs build, conventional commit titles. None of them is a required check; the required three are enough to keep `main` buildable and tested, and the bots' findings are answered thread by thread (section 6.5).

## 5. GitHub as the system of record

GitHub holds the history; the repository holds the knowledge ([`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md) §18). The rules that make that workable:

- **Every problem becomes an issue, immediately.** Bugs found while doing something else, technical debt that would derail the current task, missing tests, investigations: each gets an issue with a typed prefix (`[BUG]`, `[DEBT]`, `[TEST]`, `[SPIKE]`, `[INFRA]`, `[FEATURE]`, `[EPIC]`, `[REFACTOR]`) and its template, and the current task continues. On 2026-09-22 alone this produced #1096 (a real defect found while refuting a bot's finding), #1097, #1100, #1102 to #1104.
- **Issues carry routing and scope.** Labels `ai:local-candidate` / `ai:claude-required` decide who works on it; milestones are renumbered only after the backlog is classified, never before (SPEC-000 DEC-005).
- **Pull requests carry the evidence.** The description says which issue it fixes, what was verified and how, and what each of the twelve cross-cutting functions got (integrated, deferred with an issue, or not applicable with a reason). Bot threads are answered with the commit that fixes them, or with the evidence that refutes them.
- **Workflows are the referee.** CI on pull requests, the full suite with coverage on tags and weekly, mutation weekly, dashboards published to GitHub Pages from CI artifacts, never from a developer machine.
- **Decisions are not in comments.** They are written into specifications and ADRs and then linked from the issue; the issue thread is where they were discussed, not where they live.
- **No AI attribution in the history.** Commits and pull requests are authored by the maintainer; the working method is documented here, not in every commit message.
- **Closed issues migrate into knowledge, with the code re-audited, not just archived.** [SPEC-003](../specifications/SPEC-003-closed-issue-knowledge-migration-and-quality-audit.md) turns each closed issue into a knowledge record under `docs/knowledge/issues/*.md` plus an audit of the code it touched against today's standards, processed one issue at a time. `tools/ai/fetch-closed-issue-data.ps1` fetches the ground truth per issue (body, human comments, labels, milestone, close reason, and every linked pull request through GraphQL and the timeline, because search alone misses early issues); `.github/scripts/knowledge-records.cs` validates a record's front matter and provenance with `--check` and generates the grouped `index.md` and `PROJECT-HISTORY.md` with `--generate`. A `knowledge-records` job in `ci.yml` runs `--check` on every pull request, docs-only ones included, and gates `ci-result`. `docs/knowledge/**` belongs to the issue-worker that closes the issue (SPEC-003 DEC-005): it writes its own record and audit result in the same pull request, because it holds the facts and the audit outcome of its own diff; `docs-writer` keeps access too, so it can cite a record from the destination page it writes.

## 6. Lessons with data

Everything in this section happened on 2026-09-21 and 2026-09-22, the first two days of the resumed project, and is cited to the pull requests, issues and ledger files involved.

### 6.1 The bottleneck is waiting, not producing

The first day repaired a build broken for five months and restored every workflow (#1088, PR #1093). The second day merged three more pull requests. Their timing:

| Pull request | Opened (UTC) | Merged (UTC) | Wall time | Size |
|---|---|---|---|---|
| #1093 build repair + Phase 0 docs | 08:44 | 11:07 | 2 h 23 min | 14 commits, +4,488 / −186 |
| #1098 SPEC-000 decisions, solution cleanup | 11:08 | 12:04 | 56 min | 13 commits, +202 / −6,189 |
| #1099 agent definitions (no code) | 11:48 | 12:43 | 55 min | 1 commit, +128 / −1 |
| #1101 0.13.0 checkpoint | 12:46 | see PR | | 2 commits, +7 / −3 |

Almost none of that wall time was spent writing code. It was CI (about 40 minutes for a full pull-request run at the time), bot reviews, review rounds, re-runs of flaky external checks, and the manual steps around branch protection. The conclusion drawn in the same conversation, and recorded in #1097: latency per change will not drop much, but throughput can, by never working in series. Work is pipelined (the next specification starts while the previous pull request is in CI), pushes are batched (one per round of corrections, because every push restarts CI and cancels the bot review), and pull requests auto-merge on green so that nobody waits watching them. The two-tier CI of #1097 (a fast required tier on pull requests, the slow suites on `main` and nightly) is the infrastructure side of the same lesson.

### 6.2 Watch by events, never by reports

The first pull-request watcher was an Opus agent asked to "watch and report at the end". It cost 125,000 tokens for 35 minutes of polling and reported a failing check and a bot review after the maintainer had already seen both on the screen (agent usage recorded in the session; the resulting rule is stated in [`.claude/agents/README.md`](../../.claude/agents/README.md)). The replacement is now three versioned scripts under `tools/ai/`, none costing a model token: `watch-pr-events.ps1` polls one named pull request and emits one line per event (`CHECK-FAIL`, `REVIEW-COMMENT`, `ISSUE-COMMENT`, `REVIEW`, `CHECKS-DONE`, `PR-MERGED`/`PR-CLOSED`); `watch-open-prs.ps1` polls every open pull request of the repository and emits `PR #n CHECK-FAIL`, `PR #n NEW-THREAD` and `PR #n MERGED`/`CLOSED`; `watch-worktrees.ps1` polls the worktrees under `.claude/worktrees/` and emits `STALLED`, `RESUMED` and `REMOVED` when one stops changing. The main session reacts to each event within minutes; model-backed agents are used only when a log needs a diagnosis. A second Opus watcher used for the rest of the day consumed 168,000 tokens for two hours; the same job now goes to the Haiku `pr-watcher` (#1099).

### 6.3 What the free model actually saves

Token usage of the local model is read from the server's own `usage` field and appended to `artifacts/local-ai/ledger.csv` per task; the paid-side cost of writing the brief and checking the result is estimated in `artifacts/local-ai/ledger-notes.md` next to a verdict. The first measured day:

| Task | Local tokens (in / out) | Time | Paid tokens (brief + check, est.) | Verdict |
|---|---|---|---|---|
| 14 unit tests for a serializer (via opencode) | not recorded | 13 min | about 15,000 including one fix | 14 written; 5 failed because the brief omitted one collaborator to mock; fixed in one edit |
| 3 replay unit tests (via opencode) | not recorded | 6 min | about 5,000 | Complete, build and tests green, summary written |
| Draft §3 of this document | 9,490 / 2,317 | 32 s | about 4,500 | Faithful, 39 citations all verified on a sample of 5; choppy prose, rewritten |
| Draft §2 of this document | 7,014 / 2,113 | 26 s | about 3,500 | Good; light editing |
| Draft §4, first attempt | 9,400 / 2,175 | 29 s | about 1,500 | Rejected: zero citations, the rule was dropped |
| Draft §4, second attempt | 9,484 / 1,545 | 23 s | about 1,200 | Partial: 7 valid citations for 14 paragraphs; completed by hand |

Writing those three sections from the sources without the local model would have cost the paid tier on the order of 15,000 to 20,000 tokens per section (an estimate, not a measurement). The saving on drafting work is therefore roughly three quarters, with the caveat that the rewrite is paid. Where the local model earns its place is volume: the next planned use is the P0 to P3 classification of 579 open issues, in batches of 25 to 40 with a closed brief, which no budget would allow the paid tier to do.

### 6.4 How briefs fail, precisely

- **A brief that says "read these five files" overflows the context** (49k tokens on the tested setup); the orchestrator retries instead of compacting, and nothing is delivered. Put the production facts in the brief and forbid reading `src/`.
- **Every collaborator on the code path must be named.** The serializer tests failed because the brief did not say that encryption also calls the subject-info provider; unmocked, it returned nothing and the encryption was silently skipped.
- **Instructions get dropped from the middle of a list.** The §4 draft ignored point 4 ("every paragraph ends with a citation") entirely. Moving the same rule to the system prompt and to the first sentence of the brief recovered citations on half the paragraphs; expect partial compliance on long outputs and check with a script, not by reading.
- **The message goes before the file argument** on the opencode command line, or the message is swallowed; the launcher is `opencode.cmd` through `cmd /c`, because a PowerShell shim cannot be started detached.

### 6.5 Bots have quirks, and rules have edge cases

- CodeRabbit on a free plan reviews only on an explicit `@coderabbitai review`, one review per hour; every push cancels the review in progress. Its "changes requested" review is not dismissed when its own threads are resolved, so under required conversation resolution it blocks auto-merge until the maintainer dismisses it by hand; that happened on #1093, #1098 and #1101. Its findings were mostly right: 12 on #1093 (11 accepted, one refuted with a real-database concurrency test), 9 on #1098, all verified and resolved by the bot itself after the fixes. One refutation surfaced a real defect elsewhere (#1096).
- The link checker failed three pull requests in one day on third-party sites (a timeout, a 500, three connection resets) with zero broken links; each re-run was green. Checking external availability on every pull request is a category error; #1097 moves it to the nightly tier.
- Codecov evaluates patch coverage as reports arrive; with test shards still running it flagged 36% on a change that finished at 100%. Treat it as informational until all shards are done.
- The old automatic release workflow pushed directly to `main`; the new branch protection rejects that, so it can no longer cut a release (#1100). Protection changes have to be replayed against every workflow that writes to the branch.
- A permission classifier that refuses "merge without review" also refuses dismissing a bot's stale review, and once refused a read-only `gh pr view`. The workaround is to hand the maintainer a one-line command to run; the deeper fix is to stop the bot from requesting changes at all.

### 6.6 What was decided about time

There is no date for 1.0. The first two days show why an estimate would be dishonest: the work that dominated them (infrastructure repair, protection, bots) will not repeat, and the throughput of the steady state is unknown until a few specifications have gone through the whole pipeline. When that data exists, "pull requests merged per day" is the number to estimate from. Until then, the sequence is published (SPEC-000 §9) and the calendar is not.

## 7. How to replicate it

This section is the conceptual checklist, in the order that worked here. It is deliberately short and it is not enough on its own: replicating the setup from a clean machine means installing and building a specific set of tools (the .NET SDK, Docker with the compose profiles, `gh`, llama.cpp with CUDA and a model, opencode, Claude Code) and copying or re-creating a set of home-made scripts (the coverage obligations report, the mutation sharding and history, the three DocRef renderers, the CI gate, the PR watcher, the local-AI ledger). The step-by-step guide with the exact commands, the files to copy from this repository and indicative times is [`REPLICATION-GUIDE.md`](REPLICATION-GUIDE.md) (#1107); the [contributor guide](../contributing/README.md) (#1103) covers the subset needed to work on Encina itself.

1. **Write down what you will not lie about.** A rule that every metric in documentation must be cited from a machine-produced source, and the tooling to expand citations (section 4.4). Without it, every other measurement decays.
2. **Define coverage as obligations.** One manifest per package declaring flags and targets per file; one script that computes per-flag coverage from per-flag reports; one dashboard that shows it. Ban reflection-only tests explicitly.
3. **Write SPEC-000 before anything else.** Requirements, acceptance criteria, invariants, and a decision table with options, recommendation and the human's choice. Take the decisions one by one with the human, in their language, and write them in the specification in the project's language.
4. **Protect the branch with one summary check.** An always-run `ci-result` job that fails on any failed or cancelled upstream job, plus build and static analysis; `enforce_admins` on; no required approvals if there is one maintainer; conversation resolution on. Then audit every workflow that pushes to the branch (section 6.5).
5. **Split CI in two tiers** (#1097): fast and required on pull requests; everything slow, external or expensive on merge and nightly, opening issues on failure instead of blocking pull requests.
6. **Set up the free model and measure it before trusting it.** Verify strengths and limits with real tasks; write the brief template around the limits; record every task's tokens and a verdict in a ledger from the first task.
7. **Pin every agent to a model and an effort.** One file per agent type with its role, its tools, whether it may write, and its report format. Watchers on the cheapest model or on a script; judgement on the expensive one; the main session on the most capable model for specifying and deciding only.
8. **Watch by events.** A script that emits one line per pull-request event; react within minutes; batch corrections into one push.
9. **Pipeline the work.** The next specification starts while the previous pull request is in CI; auto-merge on green; worktrees per task; agents never on the same branch.
10. **Make every problem an issue the moment it appears**, with a typed prefix and a template, and keep going. Review the lessons section of this document every few weeks and append what changed, with its evidence.
11. **Enforce the process with hooks, not prose.** A rule stated only in an agent's prompt gets skipped under pressure; a `PreToolUse`/`Stop` hook that reads the tool call and denies the wrong one enforces it every time. The SPEC-003 audit pipeline (§2.6) is the clearest example: stage order, single-owner artifacts and foreground-only spawns are hook-checked, not merely asked for.

## Evidence index

- Specifications: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md), [SPEC-001](../specifications/SPEC-001-coverage-docref-citations.md).
- Internal method documents: [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md), [`ai-task-routing.md`](ai-task-routing.md), [`ENCINA-1.0-RECONCILIATION.md`](ENCINA-1.0-RECONCILIATION.md), [`PHASE0-BASELINE.md`](PHASE0-BASELINE.md).
- Agent definitions: [`.claude/agents/`](../../.claude/agents/README.md).
- Issues cited: #1088, #1089, #1090, #1092, #1096, #1097, #1099, #1100, #1102, #1103, #1104.
- Pull requests cited: #1093, #1098, #1099, #1101.
- Dashboards: [coverage](https://dlrivada.github.io/Encina/coverage/), [mutations](https://dlrivada.github.io/Encina/mutations/).
- Ledgers (not versioned, kept under `artifacts/local-ai/` on the maintainer's machine): `ledger.csv`, `ledger-notes.md`.

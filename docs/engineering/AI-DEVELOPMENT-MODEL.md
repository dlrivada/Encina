# AI Development Model for Encina

## 1. Purpose

This document defines the long-term engineering model we want to establish for the Encina repository.

> Public counterpart: [`HOW-ENCINA-IS-BUILT.md`](HOW-ENCINA-IS-BUILT.md) explains the same method for readers outside the project, with the lessons and measurements collected while applying it (#1104).

The objective is not merely to use AI to write code faster.

The objective is to evolve Encina toward a **Specification-Driven, evidence-based, multi-agent development process** in which:

- important engineering knowledge is preserved in the repository;
- historical knowledge from GitHub is progressively extracted and formalized;
- architectural decisions are explicit and traceable;
- specifications precede implementation;
- agents have clearly separated responsibilities;
- automated verification provides evidence of correctness;
- human intervention is concentrated on genuine ambiguity and architectural trade-offs;
- existing code is progressively audited and brought into conformance;
- the process itself becomes increasingly self-maintaining.

The desired final state is not "AI writes code without humans".

The desired final state is:

> **Humans decide what the system should mean and what trade-offs are acceptable. Agents implement and attempt to falsify those decisions. Automated verification provides evidence that the implementation conforms to the specification.**

Human review should remain available, but it should cease to be the primary mechanism by which ordinary implementation correctness is established.

---

## 2. Current Encina situation

Encina is a large .NET 10 repository with a substantial amount of accumulated engineering knowledge.

That knowledge currently exists in several places:

- source code;
- tests;
- architecture tests;
- benchmarks;
- coverage tooling and reports;
- PublicAPI tracking;
- CI configuration;
- documentation;
- ADRs;
- plans;
- `AGENTS.md` and `CLAUDE.md`;
- `.opencode` agent definitions;
- issue templates;
- pull request templates;
- GitHub issues;
- GitHub issue comments;
- pull requests;
- CodeRabbit reviews;
- Claude/AI-generated analyses;
- historical implementation decisions;
- decisions made and subsequently modified by the maintainer.

This history is valuable.

However, GitHub is currently both:

1. the historical record of how Encina evolved; and
2. an implicit knowledge base containing decisions that may not exist anywhere else.

This creates **knowledge debt**.

A future agent should not have to reconstruct an important architectural decision by searching hundreds of historical GitHub comments if that decision is still relevant to the current system.

The long-term principle is:

> **GitHub preserves history. The repository preserves current engineering knowledge.**

This does NOT mean copying every GitHub issue into the repository.

Instead, historical material should be mined for durable knowledge and converted into appropriate repository artifacts:

- specifications;
- ADRs;
- architectural rules;
- provider matrices;
- testing rules;
- invariants;
- acceptance criteria;
- documentation;
- executable checks;
- agent instructions.

Historical discussion that is no longer relevant should remain only in GitHub.

---

## 3. Important constraint: do not assume conventional coverage rules

Encina does NOT necessarily use conventional code-coverage methodology.

The repository contains its own coverage formulas, tools and interpretation.

Therefore:

**Do not assume that a generic rule such as "line coverage must be >= 85%" is the authoritative Encina quality criterion.**

Before making any judgement about coverage:

1. discover how Encina calculates coverage;
2. identify the tools and scripts involved;
3. understand its formulas;
4. understand which code is included/excluded;
5. understand how the project interprets the resulting metrics;
6. identify the actual quality gates;
7. treat the repository's established methodology as normative unless there is evidence that it is obsolete or intentionally being changed.

If the existing methodology appears inconsistent, incomplete or technically questionable, do not silently replace it.

Document the finding and present alternatives.

### Authoritative sources for coverage (as of 2026-09-21)

Read these before forming any opinion about Encina coverage, in this order:

1. `.github/coverage-manifest/{Package}.json` — per file, which test types (flags) apply, and per package, the target percentage of each flag.
2. `.github/scripts/coverage-report.cs` — the computation. It implements the **obligations model**: each (flag × coverable line) is one obligation, and every flag is measured independently. A line covered only by unit tests does not count toward the guard, contract, property or integration flag.
3. The coverage dashboard at <https://dlrivada.github.io/Encina/coverage/> — the published result; a package is green only when every applicable flag reaches its own target.
4. `AGENTS.md` §9 "Testing obligations" — the human-readable summary.

There is **no single project-wide percentage**. Statements such as "≥85% line coverage" that survived in the old `CLAUDE.md` (now the frozen [engineering handbook](ENGINEERING-HANDBOOK.md)) and `docs/en/guides/TESTING.md` were documentation drift from an earlier category-based model; they were corrected on 2026-09-21. If you find another one, treat it as drift, not as a rule.

---

## 4. Inspiration: Uncle Bob's SwarmForge

The proposed model is influenced by the ideas demonstrated in Robert C. Martin's **SwarmForge**.

The important concept is not the exact implementation of SwarmForge.

SwarmForge demonstrates a development process in which multiple specialized agents work on isolated worktrees with explicit responsibilities and durable handoffs.

Examples include roles such as:

- specifier;
- coder;
- refactorer;
- architect;
- hardener;
- QA/reviewer.

The important principles are:

### Separation of responsibility

An agent should have a clear purpose.

A specification agent should not silently implement the feature.

An implementation agent should not redefine acceptance criteria because they are inconvenient.

A verification agent should actively try to demonstrate that the implementation is wrong.

### Durable handoffs

Important information should survive an agent session.

The next agent should receive artifacts rather than relying on hidden conversational context.

### Observable behavior over prompt wording

Do not create tests that merely verify that an agent prompt contains particular words.

The important thing is what the resulting engineering process actually does.

### Isolated changes

Agents should be able to work independently and safely, preferably using isolated branches/worktrees where appropriate.

Isolation has a cost that must be paid back: a worktree that outlives its task is a stale copy of the repository that later agents can mistake for the current state. Rules:

- one task, one worktree, one branch; never two agents on the same branch at once;
- an agent never commits to `main`, with or without administrator rights: every change reaches `main` through a pull request whose required checks are green (SPEC-000 INV-006, DEC-006);
- when the task ends (merged, abandoned or superseded), the worktree is removed and, if the branch is not merged, its disposition (keep / delete / convert to issue) is recorded;
- `git worktree list` should be part of the repository-topology pass of any audit (§22, Pass 1), and orphaned worktrees are a finding.

### Human intervention at decision boundaries

Humans should intervene when the system encounters a real decision that cannot be resolved mechanically.

### What Encina adopted (2026-09-25, #1345)

The SPEC-003 closed-issue audit pipeline ([`docs/specifications/SPEC-003-closed-issue-knowledge-migration-and-quality-audit.md`](../specifications/SPEC-003-closed-issue-knowledge-migration-and-quality-audit.md), [`HOW-ENCINA-IS-BUILT.md`](HOW-ENCINA-IS-BUILT.md) §2.6) is where the SwarmForge principles above became concrete Encina mechanisms:

- **Separation of responsibility** → the four audit-stage agents, each with an explicit `Owns` / `Does not own` section in its own definition: `issue-archivist` (knowledge record and scope, never judges code or tests), `issue-auditor` (adversarial code review, never tests or docs), `test-auditor` (coverage and test quality, never production code or docs), `audit-verifier` (re-verification only, never fixes anything).
- **Durable handoffs** → one artifact per stage under `artifacts/knowledge/stages/` (`archivist.md`, `code.md`, `tests.md`, `docs.md`, `remediation.md`, `verification.md`), committed on the audit branch by `tools/ai/audit/audit-commit-stage.ps1` before the next stage may start.
- **Observable behavior over prompt wording** → `.claude/hooks/audit-stage-guard.ps1` and `.claude/hooks/enforce-path-ownership.ps1` check the tool calls themselves (spawn order, model, which file a Write/Edit targets), not whether an agent's prompt happens to contain the right words; the hooks' own regression suite (`.claude/hooks/tests/Test-Hooks.ps1`) is what proves the pipeline behaves as designed.
- **Isolated changes** → one `wia-<n>` worktree per audit, on its own `audit/<n>` branch, created and removed by `tools/ai/audit/audit-next.ps1` and `audit-done.ps1`; only one audit is open at a time.
- **Human intervention at decision boundaries** → the verifier's `Verdict: FAIL` is the gate that sends a stage back for correction instead of letting a wrong claim through, and every remediation draft the pipeline produces becomes a real issue only after a separate `tools/ai/audit/open-remediation.ps1` run the orchestrator makes once the audit is closed and verified, never automatically.

---

## 5. Specification-Driven Development

The target process is fundamentally SDD-oriented.

A feature should eventually have an explicit specification before implementation.

A specification should answer at least:

### Problem

What problem are we solving?

### Requirements

What must the system do?

Requirements should have stable identifiers, for example:

- `REQ-001`
- `REQ-002`

### Acceptance criteria

How do we know that each requirement is satisfied?

Acceptance criteria should similarly be identifiable:

- `AC-001`
- `AC-002`

### Constraints

What existing architectural, provider, performance, API, security or compatibility rules constrain the solution?

### Non-goals

What is explicitly outside the scope?

### Invariants

What must remain true before and after the change?

### Verification

How can each requirement and acceptance criterion be verified?

The specification is not merely documentation.

It should become the source against which implementation and verification are evaluated.

---

## 6. The critical distinction: specification versus design decision

A requirement says what must be true.

A design decision explains how we choose to make it true.

These must not be conflated.

For example:

> The system must support cross-shard operations.

is a requirement.

Possible designs could include:

- two-phase commit;
- Saga;
- deferred execution;
- another transaction model.

The architectural decision must record the alternatives and trade-offs.

This is particularly important for Encina because previous development discussions have demonstrated that AI agents such as Claude and CodeRabbit can generate useful architectural alternatives and recommendations.

The desired future behavior is NOT:

> "Let Claude decide."

Nor is it:

> "Force the human to design everything."

Instead:

> **Agents explore the design space. The human makes the final decision when a genuine trade-off exists. The decision is then recorded so future agents do not repeat the same debate.**

---

## 7. Architectural Decision process

Whenever a meaningful architectural/design choice exists, agents should produce a decision analysis containing:

1. Problem;
2. Context;
3. Constraints;
4. Alternatives considered;
5. Advantages of each;
6. Disadvantages of each;
7. Consequences;
8. Recommendation;
9. Risks;
10. Reversibility;
11. Impact on existing Encina architecture;
12. Impact on providers;
13. Impact on testing;
14. Impact on performance;
15. Impact on public API;
16. Impact on documentation/operations;
17. Human decision.

The human maintainer must be able to modify the agent recommendation.

For example:

```text
Agent recommendation:
Option B

Human decision:
Option C

Reason:
Prefer operational simplicity over theoretical scalability.
```

The final human decision becomes authoritative.

The system must remember that decision.

A future agent must not repeatedly recommend Option B simply because it independently reaches the same conclusion.

---

## 8. Proposed agent model

The exact number of agents is intentionally not fixed.

The architecture should evolve based on observed needs.

The initial conceptual pipeline is:

```text
HISTORIAN
    ↓
AUDITOR
    ↓
SPECIFIER
    ↓
ARCHITECT
    ↓
HUMAN DECISION GATE
    ↓
IMPLEMENTER
    ↓
VERIFIER
    ↓
ADVERSARIAL REVIEWER
    ↓
ACCEPT / REMEDIATE
```

Not every task requires every stage.

---

## 9. HISTORIAN

The Historian investigates existing knowledge.

Sources may include:

- source code;
- tests;
- docs;
- ADRs;
- plans;
- `AGENTS.md` and `CLAUDE.md`;
- `.opencode`;
- CI;
- GitHub issues;
- issue comments;
- pull requests;
- review comments;
- existing automated tooling.

The Historian's responsibility is to identify durable engineering knowledge.

It should answer questions such as:

- Why does this code exist?
- Was this behavior deliberately chosen?
- Was an alternative considered?
- Was a previous implementation rejected?
- Does an issue contain an architectural decision that is not recorded elsewhere?
- Does current code still match the historical decision?
- Has a previous agent/contributor discovered a failure mode that is not encoded in tests or documentation?

The Historian does NOT automatically modify production code.

### Provenance is mandatory

Historian output becomes durable repository knowledge (ADRs, rules, invariants). It is also the role most likely to be delegated to a cheaper model (see `ai-task-routing.md`). Therefore every extracted claim must carry its source:

- a link to the issue, issue comment, PR, review comment or commit it was taken from;
- the date of that source;
- a one-line quote or paraphrase, clearly marked as which.

A Historian report without provenance is a draft, not knowledge, and must not be promoted to an ADR or rule. The reviewer (human or Claude) verifies by following the links, not by re-reading the whole history. Claims whose source cannot be found are recorded as "unverified" and listed separately.

On 2026-09-24 the maintainer approved splitting this work into two programs, recorded in docs/specifications/SPEC-003-closed-issue-knowledge-migration-and-quality-audit.md (PR #1310, not yet merged at the time of writing). Knowledge migration runs per closed issue: this is the Historian's job, extracting durable engineering knowledge from a closed issue's history as described above. Code quality auditing runs per package instead, as a separate "audit unit" that applies the SPEC-003 checklist to one package at a time; that is the Auditor's job (§10), not the Historian's.

---

## 10. AUDITOR

The Auditor evaluates the current repository against the accumulated knowledge.

The audit should investigate at least:

### Architecture

- Does implementation match documented architecture?
- Are architectural boundaries respected?
- Are there duplicated patterns?
- Are there obsolete patterns?
- Are there accidental dependencies?

### Provider coherence

Encina has strong provider-coherence requirements.

The Auditor must discover the CURRENT provider matrix from the repository rather than assuming it.

For every provider-dependent feature, determine:

- which providers should support it;
- which actually support it;
- which have tests;
- which have integration tests;
- which have documentation;
- whether behavior is coherent.

### Testing

Determine the testing strategy actually used by Encina.

Potential categories include:

- unit tests;
- guard tests;
- property tests;
- contract tests;
- integration tests;
- load tests;
- benchmark tests;
- architecture tests;
- mutation testing;
- API compatibility tests;
- other project-specific verification.

Do not assume every feature requires every category.

Determine the applicable requirements from the repository.

### Coverage

Use Encina's own coverage methodology.

### Public API

Check:

- PublicAPI tracking;
- API compatibility;
- public XML documentation;
- analyzer requirements.

### Observability

Check:

- logging;
- event IDs;
- OpenTelemetry;
- metrics;
- health checks;
- diagnostics.

Event ID allocation must follow the existing Encina mechanism and must not introduce collisions.

### Documentation

Check applicable:

- XML documentation;
- package README;
- feature documentation;
- inventory;
- changelog.d/ fragment (never CHANGELOG.md's Unreleased section directly — see changelog.d/README.md);
- ROADMAP;
- ADRs;
- plans.

### Cross-cutting integration

Explicitly investigate whether the feature integrates correctly with Encina's transversal capabilities.

This is a known architectural concern in Encina history.

---

## 11. REMEDIATOR / IMPLEMENTER

Implementation agents may fix findings that are sufficiently deterministic.

Examples:

- missing XML documentation;
- missing PublicAPI entry;
- obvious test gaps;
- mechanical provider registration;
- missing documentation inventory entry;
- straightforward observability integration;
- repetitive implementation across already-defined provider patterns.

However:

**An implementation agent must never silently redefine an architectural decision or weaken a requirement to make the tests pass.**

If implementation reveals that the specification is wrong or incomplete, the agent must escalate.

---

## 12. VERIFIER

The Verifier's purpose is not to confirm that the implementation looks reasonable.

Its purpose is to try to prove that the implementation is wrong.

For each acceptance criterion it should determine:

- what evidence exists;
- what automated test proves it;
- whether the test is meaningful;
- whether negative cases exist;
- whether edge cases are covered;
- whether provider variants behave coherently;
- whether architecture rules still pass;
- whether public API rules pass;
- whether Encina's coverage methodology passes;
- whether performance requirements are satisfied where applicable.

The Verifier should produce evidence rather than statements such as:

> "Looks good."

---

## 13. ADVERSARIAL REVIEWER

For important changes, an independent agent may perform adversarial review.

It should assume that:

- the implementation may contain a subtle bug;
- the specification may be incomplete;
- tests may give false confidence;
- provider implementations may diverge;
- documentation may describe behavior that does not exist;
- an architectural invariant may have been violated.

Its job is to find counterexamples.

It should not simply repeat the Implementer's reasoning.

---

## 14. Classification of findings

Findings should be classified.

## Class A — Mechanical / deterministic

The repository provides enough information to determine the correct solution.

Examples:

- missing generated entry;
- missing XML documentation;
- missing test registration;
- missing provider implementation where an existing pattern is unambiguous.

These may normally be fixed autonomously.

## Class B — Conflict with an existing explicit decision or contract

The repository already contains an explicit decision (ADR, rule, public API, provider contract, documented semantics), and the finding cannot be resolved without either changing that decision or changing code to conform to it.

This covers two situations:

- implementation differs from the recorded decision (drift): the agent must determine whether the implementation is wrong or the decision is obsolete, and must not silently choose between them;
- the proposed change would alter a public contract, a central abstraction or provider semantics: the agent must escalate rather than proceed.

Class B is therefore always an escalation, never an autonomous fix.

> These three classes are the single definition used across `docs/engineering/`. `ENCINA-1.0-RECONCILIATION.md` refers to them and uses a separate P0–P3 scale for backlog priority, so the letters are not overloaded.

## Class C — Genuine unresolved decision

There are multiple technically valid options with meaningful trade-offs.

This requires human decision.

The agent must provide the alternatives and recommendation.

---

## 15. Human Decision Gate

Human review should be concentrated here.

The human should primarily decide:

- product semantics;
- architectural trade-offs;
- acceptable complexity;
- compatibility policy;
- performance versus maintainability;
- provider support policy;
- security trade-offs;
- irreversible decisions;
- intentional deviations from established architecture.

The human should NOT normally need to inspect every:

- null check;
- test assertion;
- XML comment;
- repetitive provider implementation;
- formatting change;
- compiler warning;
- mechanical refactoring.

Those should be handled through automation and verification.

---

## 16. Existing code audit

An important part of the project is a progressive audit of the EXISTING Encina codebase.

This is not just a feature-development workflow.

The goal is eventually to establish confidence that existing code conforms to:

- current architecture;
- current specifications;
- current provider requirements;
- current testing strategy;
- current coverage methodology;
- current public API rules;
- current observability rules;
- current documentation requirements;
- current performance expectations;
- current security requirements.

This should happen incrementally.

The first audit must be diagnostic.

**Do not modify the repository merely because a potential improvement has been discovered.**

First establish:

1. what exists;
2. what is known;
3. what is missing;
4. what conflicts;
5. what is obsolete;
6. what is ambiguous;
7. what should become an automated rule.

Only then should remediation begin.

---

## 17. Knowledge debt

We should explicitly track situations where important knowledge exists only in transient or historical locations.

Examples:

- decision exists only in GitHub comments;
- provider rule exists only in maintainer memory;
- testing requirement exists only in an old issue;
- architectural rationale exists only in a CodeRabbit review;
- an AI agent repeatedly makes the same mistake because the reason for an existing design is undocumented.

These are forms of **knowledge debt**.

The purpose of the new system is to progressively eliminate this debt.

---

## 18. GitHub versus repository

Do not attempt to move the entire GitHub history into the repository.

Instead:

```text
GitHub
    ↓
historical evidence
    ↓
historian
    ↓
durable knowledge
    ↓
repository artifacts
```

Repository artifacts should be concise and current.

Possible artifacts include:

- specifications;
- ADRs;
- architecture rules;
- testing rules;
- provider matrices;
- invariants;
- executable verification;
- agent instructions.

GitHub remains the authoritative historical record.

The repository becomes the authoritative source for current engineering rules.

---

## 19. Agent instructions should be scoped

The current `CLAUDE.md` contains a large amount of valuable knowledge.

Do not delete or rewrite it blindly.

However, investigate whether its responsibilities should eventually be separated.

### What already exists (as of 2026-09-21)

- `CLAUDE.md` — the large, Claude-specific instruction file.
- `.opencode/agents/` — `encina-review.md`, `encina-test.md`, `encina-docs.md` (subagent definitions for opencode, i.e. the local AI).
- `.opencode/skills/` — `provider-coherence`, `eventid-allocation`, `cross-cutting-check`, `test-workflow`, `release-checklist`. These are already the "rules" layer; they must not be duplicated under another directory.
- `docs/architecture/adr/` — ADR-001 to ADR-025.
- `docs/plans/` — active plans.
- `.claude/` — only `launch.json`, `settings.local.json` and transient worktrees. There is no `.claude/agents/` and no `AGENTS.md`.

**Update (2026-09-23):** `.claude/agents/` now exists and holds the Claude Code subagent definitions (`pr-watcher`, `ci-diagnoser`, `mechanical-fixer`, `adversarial-reviewer`, `issue-worker`; see `.claude/agents/README.md`). This **supersedes**, for Claude Code subagents, the "decided target structure" below of putting new agent roles only in `.opencode/agents/` for both tools to read: Claude Code has no mechanism to load role prompts from `.opencode/agents/`, so a role that must run as a Claude Code subagent needs its own definition in `.claude/agents/`. Claude Code sessions spawn subagents through `.claude/agents/`, the mechanism Claude Code itself reads; opencode keeps its own equivalent role prompts in `.opencode/agents/`. A role that must run under both tools needs a definition in each directory — the two are the same layer (role prompts) read by two different tools, not a duplicated hierarchy, but they are no longer a single shared source file.

### Decided target structure

The decision (2026-09-21) is to use a **tool-agnostic root file plus the existing opencode layout**, rather than a parallel `.claude/` hierarchy. As the Update note above records, this was superseded on 2026-09-23 for Claude Code subagents specifically: `.claude/agents/` is the parallel hierarchy Claude Code requires, because Claude Code cannot read `.opencode/agents/`. The rest of the layout below (AGENTS.md, `docs/specifications/`, the label routing) still stands:

```text
AGENTS.md                      # tool-agnostic engineering rules; read natively by opencode, Codex, Cursor
CLAUDE.md                      # Claude-specific; imports AGENTS.md with @AGENTS.md and adds only Claude-specific guidance

.opencode/
    agents/                    # role prompts (existing three + historian/auditor/specifier/... as they earn their place)
    skills/                    # executable/rule-like knowledge (existing five; add, do not duplicate)

docs/
    specifications/            # SPEC-000 onward (created with SPEC-000; numbering independent from ADRs)
    architecture/adr/          # decisions (existing)
    engineering/               # process documents (this folder)
    plans/                     # active plans (existing)
```

Consequences:

- `AGENTS.md` is created during Phase 0 by extracting the tool-agnostic parts of `CLAUDE.md`, not by writing new content; `CLAUDE.md` keeps only what is specific to Claude Code (scripting policy exceptions, memory, etc.).
- **Superseded 2026-09-23 for Claude Code subagents** (see the Update note above): new agent roles do not go in `.opencode/agents/` alone. Claude Code loads subagent definitions only from `.claude/agents/`; opencode loads them only from `.opencode/agents/`. A role that must run under both tools needs a definition in each directory — there is no single file both tools read.
- Specifications live in `docs/specifications/` with the `SPEC-NNN` prefix. ADRs keep their own sequence in `docs/architecture/adr/`.
- The GitHub labels `ai:local-candidate` and `ai:claude-required` exist (created 2026-09-21) and are the routing mechanism described in `ai-task-routing.md`.

This is a target, NOT an instruction to create all of these files immediately.

First audit what already exists.

Avoid duplicating information that is already correctly represented.

**Update 2026-09-25 (#1353): implemented.** The root `AGENTS.md` now holds the operative, tool-agnostic engineering rules; it is read natively by opencode, Codex and Cursor. The root `CLAUDE.md` imports it with `@AGENTS.md` and adds only what is specific to Claude Code (Active Plans, orchestration and delegation, model routing, the closed-issue audit, CodeRabbit). The maintainer chose to keep the pre-2026-09-25 `CLAUDE.md` verbatim as a frozen human reference rather than deleting it: it was moved with `git mv` to [`docs/engineering/ENGINEERING-HANDBOOK.md`](ENGINEERING-HANDBOOK.md) with a one-line snapshot banner, its relative links were not fixed, and it is not maintained going forward. Every heading and rule of that frozen snapshot is traced to its destination in [`docs/engineering/agents-md-traceability.md`](agents-md-traceability.md).

### 2026-09-23 review of the agent system ([#1181](https://github.com/dlrivada/Encina/issues/1181))

A review of the 2026-09-22 and 2026-09-23 sessions (about 20 PRs and 40 issues run by an orchestrator with one `issue-worker` per issue) changed the agent system as follows. Details live in `.claude/agents/README.md` and the agent definitions.

1. **Model choice.** Workers run on Sonnet. The orchestrator switches a worker to Opus only when its brief states why: an unknown root cause or a design-heavy task. Most briefs are closed, and Sonnet executes them at a fraction of the cost.
2. **Self-review before hand-off.** A worker whose change touches production code runs `adversarial-reviewer` on its own diff and fixes blockers and majors before it reports, so findings are fixed before the PR opens rather than through a review, fix, re-push and CI loop. The orchestrator still runs the PR-level review when CodeRabbit is rate limited.
3. **Main-checkout guard.** Workers wrote into the main checkout three times, through relative paths in `[IO.File]` calls that .NET resolves against the process directory. The `block-main-checkout-writes` hook now denies, for the writing agents, the file-tool writes, the shell writes it recognises and the working-tree git commands whose target it can resolve to the main checkout rather than a worktree. A target held in a variable it cannot resolve is allowed, with a warning when the command runs from the main checkout; writes by other programs are not seen. The list of recognised writes is in `.claude/agents/README.md`.
4. **Edit tool only for source files.** A PowerShell `-replace` corrupted six files in #1159. Workers now edit repo source files (the extension list in `issue-worker.md`, including `.txt` PublicAPI files, `.sql`, `.ps1`, `.xml` and `.editorconfig`) only with the Edit and Write tools. The same hook blocks `Set-Content`, `Add-Content`, `Out-File`, `Tee-Object`, `[IO.File]` and redirection writes to such files when their target resolves inside the project, and a `-replace` or `.Replace(` rewrite into a variable target when the command names a repository source path.
5. **Turn limits.** `mechanical-fixer` goes from 30 to 50 turns and `issue-worker` from 80 to 120. Both limits were hit, and every resume re-reads the context it had already paid for.
6. **Issue bodies from workers.** Workers write each follow-up as a complete issue file in the template format under `artifacts/issues/` and list the paths; the orchestrator creates the issue from the file through the `open-issue` skill, so long issue texts stay out of its context.
7. **`worker-brief` skill.** The fixed part of every brief (worktree and absolute paths, no publishing, changelog fragments, verification, self-review, delegation, report format, model choice) lives in `.claude/skills/worker-brief/SKILL.md`; the orchestrator writes only the goal, scope, decisions and acceptance.
8. **Prohibited commands.** Workers used some of the Unix commands `CLAUDE.md` prohibits without anyone noticing. The `block-prohibited-commands` hook, wired into every agent, now rejects them (and Bash loops, conditionals, `case`, `test`/`[[ ]]`, `$( ... )`, backtick and process substitution) and names the PowerShell or tool equivalent.
9. **Delegation stays mandatory.** The review proposed making delegation proportional (inline edits for small steps, because a specialist start carries a fixed context cost). The maintainer rejected it on 2026-09-23: every step that belongs to a specialist goes to that specialist, at every level and however small, because the cost of a spawn is trivial next to the quality the specialist brings. The ownership table in `.claude/agents/README.md` is the single statement of who does what; drafting follow-up issue files belongs to the local model, and the worker checks the draft. The actual per-spawn cost is in the usage ledger at `artifacts/agent-usage/ledger.csv`, not typed here by hand.
10. **Delegation enforced by hooks (part 2).** Every agent that delegates can spawn, within an allowlist that `block-worker-spawn` enforces per agent (`issue-worker`: `ci-diagnoser`, `mechanical-fixer`, `Explore`, `adversarial-reviewer`, `docs-writer`; `docs-writer`: `mechanical-fixer`, `docs-reviewer`, `Explore`; `mechanical-fixer`: `ci-diagnoser`, `Explore`). `enforce-path-ownership` denies an `issue-worker`'s file-tool edits on documentation (the `docs-writer`'s; `docs/plans/**` stays with the worker) and on changelog fragments, PublicAPI files and coverage manifests (the `mechanical-fixer`'s), and a `docs-writer`'s edits on `src/`, `tests/` and `.github/`. `require-specialists`, a `Stop` hook, reads the agent's own transcript and its worktree's diff and sends it back once when the diff needs a specialist it has not spawned (`adversarial-reviewer` for production code, `docs-writer` or `docs-reviewer` for documentation, `mechanical-fixer` for changelog/PublicAPI/manifests). `guard-orchestrator-writes`, a project hook, denies the main session's file-tool edits and the shell writes it can resolve under `src/` and `tests/` of any checkout; the three writing agents (`issue-worker`, `mechanical-fixer`, `docs-writer`, recognised by the hook input's `agent_type`) are left to their own hooks, and every other subagent is treated like the main session.
11. **Review of part 2.** An adversarial review of the hooks closed these gaps: the orchestrator guard exempts only the three writing agents, and a project `Agent` hook limits which subagent types the orchestrator spawns; commands hidden in `pwsh -Command`/`bash -c` wrappers are analysed in their own shell and `pwsh -EncodedCommand` is blocked for the agents; the documentation category is prose only (`docs/**/*.md` and the images the pages show), so the site's code and data under `docs/` are code, owned by the `issue-worker` and self-reviewed; the stop gate counts only spawns that succeeded and finds the worktree from the brief and the orchestrator's later messages; the orchestrator guard also sees `git checkout`/`git restore` pathspecs, `git apply`/`git am` patches, `-OutFile`, `Start-Process` redirections and `Expand-Archive`; `mechanical-fixer` never publishes; the `docs-writer` edits an allowlist of paths; Bash ANSI-C strings are decoded. A rebase conflict in `src/` or `tests/` goes to an `issue-worker` through the `worker-brief` skill. The guard still does not see every write (unresolved targets, other programs, `git merge`/`rebase`/`reset`); the list is under "Limits" in `.claude/agents/README.md`.
12. **Review against the hooks/sub-agents reference.** `require-specialists` treated a plain `Stop` input (no `agent_id`/`agent_type`, which per the [hooks reference](https://code.claude.com/docs/en/hooks.md) means the main session) the same as its own `SubagentStop` when `-Agent` was given, instead of skipping it; fixed so an empty `agent_type` is a mismatch like a different one. The `dotnet run <file>.cs` / `pwsh`/`powershell -File <file>.ps1` bypass noted in "Limits" is now partially closed: `guard-orchestrator-writes` and `block-main-checkout-writes` read the named script's own text and act on it when it both references a guarded path and contains a file-write API; it is still a text heuristic, not an execution of the script.
13. **Stalled workers (2026-09-24).** A worker that spawns a specialist in the background and then ends its turn waiting for it stalls permanently: the child's completion notification reaches the orchestrator, not the worker that spawned it, so nothing ever wakes the worker up. The rule is that a worker runs every specialist spawn in the foreground (`run_in_background: false`) and never ends a turn waiting on one. On the orchestrator's side, when a child-completion notice arrives for a specialist whose parent worker is still an open task, the orchestrator resumes that parent worker (for example through `SendMessage`) instead of treating the notice as its own.

#### Cost control (2026-09-23)

The maintainer approved these defaults to keep the cost of the agent system low and measurable:

- Workers (`issue-worker`, `docs-writer`) run on Sonnet; Opus only when the brief carries a one-line justification (an unknown root cause, a design-heavy task). `adversarial-reviewer` runs on Sonnet too, and the orchestrator passes Opus only for security, personal-data or design-changing PRs. `mechanical-fixer` and `pr-watcher` run on Haiku.
- The local model (llama-server, which the orchestrator starts at the beginning of a session) takes drafts, classification and summaries through the `local-ai-task` skill.
- Briefs stay small: the fixed part of the `worker-brief` skill plus a closed variable part, so a worker finishes within its turn limit; a resume re-reads the whole context it had already paid for.
- Every worker run appends a line to `artifacts/agent-usage/ledger.csv` in its worktree (`timestampUtc,agent,task,model,subagentTokens,notes`), so the effect of these choices shows in the numbers.

---

## 20. Do not create a prompt bureaucracy

The objective is not to create dozens of prompts and agents merely because the architecture looks elegant.

Every additional agent introduces:

- context overhead;
- maintenance;
- coordination cost;
- potential contradictions.

Start with the smallest architecture capable of demonstrating the model.

Possible first experiment:

```text
AUDITOR
   ↓
SPECIFIER
   ↓
IMPLEMENTER
   ↓
VERIFIER
```

Introduce Historian, Architect and Adversarial Reviewer where they provide measurable value.

---

## 21. Important rule: preserve current Encina conventions

Before proposing changes, agents must discover the existing repository conventions.

Do not replace an existing mechanism with a generic industry-standard mechanism merely because it is more familiar.

This is especially important for:

- coverage;
- provider support;
- testing;
- PublicAPI;
- event IDs;
- scripts;
- CI;
- architecture;
- documentation;
- performance infrastructure.

The existing repository is the primary source of truth.

If the existing convention is problematic, identify the problem and propose alternatives.

---

## 22. Audit methodology

The initial audit should proceed in passes.

## Pass 1 — Repository topology

Understand:

- projects;
- packages;
- tests;
- tooling;
- documentation;
- CI;
- agent configuration;
- scripts;
- benchmarks;
- architecture tests.

## Pass 2 — Engineering rules

Extract the actual rules from:

- `AGENTS.md` and `CLAUDE.md`;
- other agent instructions;
- ADRs;
- plans;
- CI;
- test infrastructure;
- tooling.

## Pass 3 — Historical archaeology

Inspect representative GitHub issues, pull requests and discussions.

Do not attempt to read every historical issue indiscriminately.

Prioritize:

- architectural changes;
- large features;
- provider work;
- testing infrastructure;
- performance;
- security;
- changes with extensive design discussion;
- issues where AI agents produced design alternatives.

## Pass 4 — Consistency analysis

Compare:

```text
documented rule
      ↕
historical decision
      ↕
current implementation
      ↕
tests
      ↕
CI/tooling
```

Identify mismatches.

## Pass 5 — Knowledge extraction

Determine which historical knowledge should become durable repository knowledge.

## Pass 6 — Technical gap analysis

Identify real defects, missing tests, architectural inconsistencies and quality gaps.

## Pass 7 — Automation opportunities

For every recurring problem ask:

> Can this be detected automatically?

If yes, prefer an executable check over another paragraph in an agent prompt.

## Pass 8 — Remediation planning

Only after the diagnostic model is established should implementation/remediation begin.

---

## 23. What success looks like

The long-term system should make a typical change look approximately like this:

```text
Issue / request
      ↓
Specification
      ↓
Historical/context analysis
      ↓
Architecture analysis
      ↓
Design alternatives
      ↓
Human decision when necessary
      ↓
Implementation
      ↓
Automated verification
      ↓
Adversarial verification
      ↓
Evidence
      ↓
Pull Request
```

The PR should therefore contain more than:

> "Implemented feature X."

It should be possible to answer:

- What was requested?
- What requirements were defined?
- Which design was chosen?
- What alternatives were rejected?
- Why?
- What code changed?
- What tests prove correctness?
- What providers were affected?
- What coverage methodology was applied?
- What architectural checks passed?
- What documentation changed?
- What remains uncertain?

---

## 24. The ultimate objective

The system should progressively move Encina from:

```text
Knowledge distributed across:

human memory
GitHub discussions
AI conversations
source code
tests
documentation
```

toward:

```text
Explicit specification
        +
Explicit architecture
        +
Explicit decisions
        +
Executable rules
        +
Automated verification
        +
Historical traceability
```

The result should be a repository that is increasingly understandable not only to humans, but also to independent AI agents.

A new capable agent should be able to enter the repository and reconstruct:

- what Encina is;
- what rules govern it;
- why major decisions were made;
- what must not be changed;
- what may be changed;
- how correctness is demonstrated.

That is the core objective of this initiative.

---

## 25. Instructions for the initial investigation

When first working on this initiative:

1. **Do not modify production code.**
2. **Do not rewrite `CLAUDE.md` yet.**
3. **Do not create a large swarm of agents yet.**
4. **Do not assume conventional coverage metrics.**
5. **Do not assume historical GitHub discussions are authoritative today.**
6. **Do not assume current documentation is correct merely because it exists.**
7. **Do not assume implementation is correct merely because tests pass.**
8. **Do not replace existing Encina mechanisms with generic alternatives without analysis.**
9. **Do not silently make architectural decisions.**
10. **Record uncertainty explicitly.**

The first deliverable should be an **audit report**, not code.

The audit should identify:

- what already works well;
- what knowledge is already encoded correctly;
- what knowledge is fragmented;
- what knowledge exists only in GitHub;
- what documented decisions conflict with implementation;
- what important decisions are implicit;
- what technical gaps exist;
- what testing gaps exist;
- how Encina's coverage methodology actually works;
- what automation already exists;
- what should become executable rules;
- what should become repository documentation;
- what requires human architectural decisions;
- what the smallest practical next step is.

Only after this report should the repository begin to be transformed.

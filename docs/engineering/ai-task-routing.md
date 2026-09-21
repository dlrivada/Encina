# AI Task Routing for Encina

## 1. Purpose

This document complements `AI-DEVELOPMENT-MODEL.md` and `ENCINA-1.0-RECONCILIATION.md`. Those two define *what* engineering process we want (SDD, SwarmForge-style roles, backlog reconciliation). This one answers a narrower, operational question:

> Of the roles defined in `AI-DEVELOPMENT-MODEL.md` §8, which can a local AI (free, but with known limitations) take on, and which must stay with Claude (paid, but with more reliable reasoning)?

The economic goal is concrete: reduce paid-token consumption on high-volume, low-risk work (Dependabot triage, mechanical fixes requested by automated reviewers, bulk reading of history) without compromising reliability on the decisions that matter.

This document is **not permanently normative**. It is a routing hypothesis, to be revised as we observe how the local AI actually behaves on Encina.

---

## 2. Local AI setup (reference)

- Models: Qwen 3.6 (fast, ~65–100 tok/s with MTP speculative decoding) and Qwen 3.8 27B (slower, ~60–70 tok/s, but with observably deeper investigation).
- Engine: `llama-server` (llama.cpp built with CUDA) on a single local consumer GPU, with MTP speculative decoding enabled.
- Orchestration: opencode, pointing at both Ollama and the local `llama-server` through OpenAI-compatible providers. Role prompts live in `.opencode/agents/`, rule-like knowledge in `.opencode/skills/`.
- Known limitations, verified empirically (not theoretical):
  - It may skip parts of a multi-point instruction unless the points are enumerated explicitly.
  - On very open-ended research tasks it can enter a re-exploration loop after context compaction. Partially mitigated by opencode ≥ 1.18.31, which recognises the `llama-server` error pattern as a context overflow instead of retrying indefinitely.
  - It did not hallucinate verifiable data when it had real tools available (confirmed with `gh api` while investigating an issue comment), but this has not been tested exhaustively and must not be assumed.

---

## 3. Routing criterion

Each role in the `AI-DEVELOPMENT-MODEL.md` §8 pipeline (`HISTORIAN → AUDITOR → SPECIFIER → ARCHITECT → HUMAN DECISION GATE → IMPLEMENTER → VERIFIER → ADVERSARIAL REVIEWER`) is evaluated on two axes:

1. **Cost of an error**: is an omission or a subtle reasoning failure easy to detect afterwards, or does it propagate silently into later decisions?
2. **Nature of the task**: is it mostly volume (reading, summarising, mechanical writing), or does it require deep reasoning and trade-off judgement?

High volume + low error cost → local AI candidate. Low volume + high error cost → Claude.

---

## 4. Proposed routing by role

| Role / task | Routing | Reason |
|---|---|---|
| **HISTORIAN** — mine historical issues/PRs into ADRs and durable artifacts | 🟢 Local, with Claude sampling | Bulk reading and summarising. Because the output feeds durable repository knowledge, every extracted claim must carry provenance (link + date, see `AI-DEVELOPMENT-MODEL.md` §9); Claude verifies a sample by following the links before anything is promoted to an ADR or rule. |
| **AUDITOR** — Pass 1/2 (repository topology, rule extraction from `CLAUDE.md`/ADRs/CI) | 🟢 Local | Mechanical, high volume, low risk; an error is caught when the report is read. |
| **AUDITOR** — Pass 4/6 (consistency analysis, technical gaps) | 🟡 Local drafts, Claude consolidates | The local model can detect real drift (verified: it found the mismatch between `TESTING.md` and the coverage methodology), but the report feeds `SPEC-000`, so Claude gives the final sign-off. |
| **SPECIFIER** — formal REQ-*/AC-* | 🔴 Claude | Requirements are the source of truth everything downstream is verified against; a subtle omission here is too expensive. |
| **ARCHITECT** — alternatives and trade-offs | 🔴 Claude | Deep reasoning; exactly where Claude's cost is justified. |
| **HUMAN DECISION GATE** | 👤 Maintainer | Not delegable, by definition (`AI-DEVELOPMENT-MODEL.md` §7). |
| **IMPLEMENTER** — mechanical/bounded changes (a dependency bump, a one-line fix literally suggested by CodeRabbit/Copilot at a specific location, a workflow tweak already specified) | 🟢 Local | Low risk, high volume. |
| **IMPLEMENTER** — new feature following an approved SPEC | 🔴 Claude | Interpreting the nuances of a specification is not a bounded task. |
| **VERIFIER** — first mechanical pass (tests, documentation drift, gates such as `--check-dangling`) | 🟢 Local | Cheap, and verified reliable for this kind of cross-check. |
| **VERIFIER** — final gate before merging anything non-trivial | 🔴 Claude | `ENCINA-1.0-RECONCILIATION.md` §19 names "false confidence from metrics" as a risk; the last word on correctness must not come from a model tier that sometimes skips parts of an instruction. |
| **ADVERSARIAL REVIEWER** | 🔴 Claude | The most demanding reasoning task in the pipeline. |

---

## 5. Concrete use cases already identified

- **Dependabot**: the local AI's job is *triage*, not implementation (Dependabot already produces the PR). Scope each task to **one PR at a time** ("read the diff of this Dependabot PR and summarise the risk in one paragraph"), never "review all of Dependabot" in a single open-ended session, which triggers the re-exploration loop described in §2.
- **CodeRabbit/Copilot requesting point changes**: if the suggestion is mechanical and localised (e.g. "add a null check here"), local AI + human gate before merge. If the reviewer questions a design decision, escalate to Claude (the boundary between IMPLEMENTER and ARCHITECT).

---

## 6. Coordinating two AIs

Principles already set in `AI-DEVELOPMENT-MODEL.md` §4, applied explicitly here:

1. **Durable handoffs, never shared conversational context**: the local AI does not "talk" to Claude or vice versa. It delivers an artifact (a `.md` report, a PR, an issue comment) that Claude or the maintainer reads afterwards.
2. **Branch/worktree isolation**: never both AIs on the same branch at the same time, and worktrees are removed when the task ends (`AI-DEVELOPMENT-MODEL.md` §4).
3. **Routing labels**: the GitHub labels `ai:local-candidate` and `ai:claude-required` (created 2026-09-21) are applied to issues so that routing is decided consciously before a task is launched.
4. **Supervision while the opencode loop is not confirmed fixed**: do not leave local tasks unattended overnight yet. Warning sign: the same `n_tokens` value repeating without progress in the `llama-server` log. After a period of confirmed stability with opencode ≥ 1.18.31, unattended night batches for routine work (Dependabot triage) can be reconsidered.

---

## 7. Reviewing this document

Revise this routing when:

- the local AI fails on a task marked 🟢 (demote that task to 🟡 or 🔴);
- the opencode loop bug is confirmed fixed and stable (allows relaxing §6.4);
- the local model changes (a more capable version could absorb roles currently marked 🟡 or 🔴).

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

### 2.1 Verified setup and limits (maintainer's trials, consolidated 2026-09-22)

- **Engine and model for delegated work:** `llama-server` (llama.cpp, CUDA) serving Qwen 3.8 27B `UD-Q4_K_XL`, OpenAI-compatible on `127.0.0.1:8080`, one request at a time (`-np 1`). Ollama is the fallback for this specific setup: the maintainer's Ollama build failed on the MTP `nextn` layer of the `UD-Q4_K_XL` GGUF, and Ollama's own MTP-enabled Qwen tags were not part of the trials, so this is a statement about the tested configuration, not a general limitation of Ollama. Qwen 3.6 is faster (~80–100 tok/s vs ~60–70) but shallower: in a head-to-head on the same prompt only 3.8 found the `TESTING.md` coverage drift.
- **Tuned flags:** `--spec-type draft-mtp --spec-draft-n-max 4` (empirical optimum: n=2 ~64, n=4 ~68, n=8 ~62 tok/s), `-c 49152` (safe VRAM margin; 65536 runs with <2 GB free), `-fa on -ctk q8_0 -ctv q8_0`, `--no-mmproj`, `--temp 1.0` for Qwen 3.8.
- **Thinking must be disabled per request, via the API** (`chat_template_kwargs.enable_thinking=false` on llama-server, `reasoning_effort: none` on Ollama); template tricks do not work.
- **Strengths with evidence:** no hallucination on verifiable data when tools are available (checked against `gh api`); correct reading of Encina's non-standard docs (mutation methodology, obligations coverage model).
- **Limits with evidence:** (1) silently drops parts of multi-point instructions — enumerate every point and say "without omitting any"; (2) re-exploration loop on open-ended research (one session: ~968k tokens, 75 % in tool calls, no deliverable) — split into "investigate and write findings to a file" and "now write the deliverable, no more searching"; (3) speed is not the bottleneck, reliability over long sessions is — no unattended overnight batches until the opencode ≥ 1.18.31 loop fix is confirmed stable.
- **Brief template:** one bounded task, numbered points, the exact output file path, the sources it may read, and the sentence "stop when the file is written". Claude (or the maintainer) verifies a sample by following the provenance links the brief requires.

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

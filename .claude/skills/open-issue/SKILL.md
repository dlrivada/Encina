---
name: open-issue
description: Open a GitHub issue in dlrivada/Encina in the house format - the right template prefix, the template's headers verbatim and in order, checkboxes ticked, labels and milestone. Use for every bug, debt item, test gap, spike or follow-up found while working, including findings from reviews.
---

# Open an issue

Every identified problem is either fixed now or recorded as an issue before moving on (`AGENTS.md` §11, Issues, plans and changelog). The `check-issue-template` hook blocks `gh issue create` calls that break the format below.

Main session (orchestrator) only. An `issue-worker` or `docs-writer` does not run this skill; it writes each follow-up as an issue file and lists the paths in its report (§5).

## 1. Pick the template

| Situation | Template | Prefix |
|---|---|---|
| Wrong behaviour in Encina code, or tests failing because of a code bug | `bug_report.md` | `[BUG]` |
| Messy, duplicated or incomplete code that works | `technical_debt.md` | `[DEBT]` |
| Missing tests, coverage flag below its manifest target, load tests, benchmarks | `test_implementation.md` | `[TEST]` |
| New capability | `feature_request.md` | `[FEATURE]` |
| Investigation or architecture decision | `architecture_spike.md` | `[SPIKE]` |
| CI/CD, Docker, build, developer tooling | `infrastructure.md` | `[INFRA]` |
| Restructuring without behaviour change | `refactoring.md` | `[REFACTOR]` |
| Multi-issue initiative | `epic.md` | `[EPIC]` |

Never use `[TECH-DEBT]`, `[TESTING]`, `[ARCHITECTURE]`, `[DECISION]`, `[REVIEW]` or `[Phase N]`.

## 2. Write the body

1. Read the template file in `.github/ISSUE_TEMPLATE/` now; do not work from memory.
2. Keep every `##` header verbatim and in order. Fill every section; write "None" or "Not applicable" with a reason rather than dropping one.
3. Tick the checkboxes that apply (`[x]`) and replace the template's placeholder text.
4. Be concrete: file paths with line numbers, the failing command or log excerpt, the rule or requirement broken (`AGENTS.md` section, SPEC/ADR id).
5. For a `[BUG]` whose cause is known, add a Root Cause paragraph under Additional Context.

House-style references: #1050 (`[DEBT]`) and #949 (`[BUG]`).

## 3. Create it

Write the body to a scratchpad file and pass it by path, so the hook can read it and quoting cannot break it:

```powershell
gh issue create --repo dlrivada/Encina --title "[DEBT] <specific title>" --body-file <file> --label technical-debt --label p1-recommended --milestone "<milestone>"
```

**An issue is opened complete or not at all** (maintainer rule of 2026-10-06, #1926). The checklist, in the order it is done:

1. Template and body (§1-§2), body drafted per the local-draft rule below.
2. Labels: the template's default label for its prefix, the area labels that apply (`gh label list`), and exactly one priority label: `p0-mandatory`, `p1-recommended` or `p2-post-1.0` (an `[EPIC]` needs none). `needs-decision` only when the body holds at least one Dilemma (see "Decisions: Trivial, Settled, Dilemma" below); never because an issue lists options.
3. Milestone: `--milestone` with the literal title of an existing milestone (`gh api repos/dlrivada/Encina/milestones --paginate --jq '.[].title'`; titles use an em dash, copy them exactly). Never create the issue without one.
4. After the create, add it to user project 1 with the keyring token, because the session token (`GITHUB_TOKEN`/`GH_TOKEN`) cannot write projects: `pwsh -NoProfile -Command "Remove-Item Env:GITHUB_TOKEN,Env:GH_TOKEN -ErrorAction SilentlyContinue; gh project item-add 1 --owner dlrivada --url <issue url>"`.
5. When the body states a parent, link it as a sub-issue: `gh api repos/dlrivada/Encina/issues/<parent>/sub_issues -F sub_issue_id=<id>`, where `<id>` is the new issue's numeric id (`gh api repos/dlrivada/Encina/issues/<n> --jq .id`).
6. When the body states a blocker, record it with the issue dependencies API: `gh api repos/dlrivada/Encina/issues/<n>/dependencies/blocked_by -F issue_id=<blocking issue's numeric id>`.

The `check-issue-template` hook enforces steps 2 and 3 (a missing item blocks the call and the message lists every one); it does not check the project (the active token cannot write projects), so step 4 (adding the issue to user project 1 with the keyring credentials, clearing `GITHUB_TOKEN`/`GH_TOKEN` for that call) is always yours. No workflow writes to the project: the `issue-hygiene` workflow only labels `needs-triage`, comments once and posts the weekly summary using the default token, and does not touch the project. `open-remediation.ps1` does steps 2-4 itself for the remediation issues it opens (a failed project add is a warning that names the retry command). Issues created outside these paths (for example on the web) are found with `pwsh tools/ai/issues/check-project-membership.ps1` (read-only, exit 1 when an open issue is not on project 1; `-Add` adds them), run locally with the keyring credentials. For the maintainer: after the PR that drops it merges, revoke the classic personal access token that was created for the old `PROJECT_TOKEN` and delete the repository secret `PROJECT_TOKEN`.

**Decisions: Trivial, Settled, Dilemma (#2470).** Every implicit choice in an issue (defaults, error semantics, storage, public names, behaviour on missing context, dependencies, ordering, retention) is listed in a decision register and classified:

- **Trivial**: follows a cited convention.
- **Settled**: the only option that complies, with a verifiable citation (an `AGENTS.md` section or rule, a SPEC id, an ADR number, a law article, a link to an earlier maintainer decision, or named comparable products). Write it as a settled choice with its citation, never as an open option, and never with `needs-decision`.
- **Dilemma**: two or more valid options, each better in a concrete named scenario. Only a Dilemma is written with its options, pros and cons, your recommendation and the scenario in which each option wins, and leaves the decision open: never write "Rejected" or "Chosen" before the maintainer has decided.

The `decision-extractor` agent produces the register (the orchestrator spawns it; `adversarial-reviewer` then tries to break every Settled and Trivial entry, and a credible scenario where another option wins turns it into a Dilemma). An issue without a plan carries the register as an issue comment headed "Decision register", posted by the orchestrator, and it is copied into the closing PR's knowledge record. Zero Dilemmas is valid: write "No open design choices" with the settled table.

**`needs-decision` only with a Dilemma.** The label means "the maintainer has a real choice to make", so add it only when the body holds a Dilemma entry; the `check-issue-template` hook denies `gh issue create` and `gh issue edit --add-label` with `needs-decision` when the body does not contain the word "Dilemma", and it no longer demands the label on `[FEATURE]` and `[SPIKE]`. A `[FEATURE]` "Alternatives Considered" or a `[SPIKE]` "Options to Evaluate" section lists the options of the Dilemmas, or states that the choice is settled and cites why. Only the maintainer decides a Dilemma. Do not ask the maintainer about a Dilemma when opening the issue: the questions come when work on the issue starts (`worker-brief` skill). The answer is recorded in the repository: in the plan's `## Maintainer Decisions` for a feature, and in the closing PR's knowledge record for any other issue. The orchestrator does not write a worker brief for an issue with an open Dilemma until the maintainer decides and the label is removed (`worker-brief` skill). The Settled entries are listed one line each, with their citation, in the daily report so the maintainer can veto any of them (a vetoed entry becomes a Dilemma); work continues meanwhile.

- The `check-issue-template` hook also refuses a `--body-file` with no evidence it was drafted by the free local model, or by `local-ai-standin` when the local model is switched off (#1410, #1593; both leave a ledger row, `ledger.csv` or `standin-ledger.csv`): draft the body with `local-ai-task` first and keep its first line `<!-- local-draft: <path to the local-ai output> -->` in the scratchpad copy, or, when the local model genuinely cannot do the task, use a first line `<!-- local-draft: none, reason: <text> -->` (a non-empty reason; it is logged to `artifacts/local-ai/opt-outs.log`). Neither line is stripped before the call: it is published as the issue's first line of raw markdown too. GitHub's default rendered view hides an HTML comment, but it is still visible in Edit mode, the API and the diff, so keep the reason short and free of anything sensitive.
- Pick the milestone from SPEC-000's release scope. Post-1.0 work goes to a post-1.0 milestone, never to the current one by default.
- A `[FEATURE]` gets an implementation plan before work starts (see the `implementation-plan` skill).

## 4. Afterwards

- Link the issue from the PR or document that produced it.
- Mention every issue opened in the final summary to the maintainer.
- Score the new issue against the #1552 priority list: `pwsh -NoProfile -File tools/ai/priority/score-issues.ps1 -Issue <n>` (needs a prior `-All` run's `artifacts/priority/scores.json` to rank against; an `[EPIC]` title or a `Post-1.0:` milestone reports its class instead of a rank). Report "rank R of N, total T" with its top two weighted criteria in the summary to the maintainer. The ranking (`scores.json` `generatedAtUtc`) must be less than 24 h old when a front is picked; if it is older, run `score-issues.ps1 -All -Resume` first (only new or changed issues reach the model), so age and unblocking stay current across the whole list. Label an issue `urgent` when it meets SPEC-004 REQ-010 (broken CI on `main`, audit blocker, defect blocking other fronts): its total gets +100 and it ranks above every non-urgent issue. `priority-critical` is a product star rating, not urgency.

## 5. From a worker's issue file

Workers write follow-ups to `<worktree>/artifacts/issues/<slug>.md` (git-ignored). The file is the body from §2 preceded by a header block:

```text
<!-- issue
title: [DEBT] Specific title with the template prefix
labels: technical-debt, area-x
milestone: <milestone, or empty>
-->
## Type
...
```

Read only the header, strip it into a scratchpad body file, and create the issue with the title and labels typed literally, so the `check-issue-template` hook can validate them (it skips titles and body paths held in variables):

```powershell
Get-Content '<worktree>\artifacts\issues\<slug>.md' -TotalCount 5
$lines = Get-Content '<worktree>\artifacts\issues\<slug>.md'
$lines | Select-Object -Skip ([array]::IndexOf($lines, '-->') + 1) | Set-Content '<scratchpad>\<slug>.md'
gh issue create --repo dlrivada/Encina --title "<title>" --body-file '<scratchpad>\<slug>.md' --label "<labels>" --milestone "<milestone>"
```

When the header leaves the milestone or the priority label empty, §3 decides them (the hook refuses the call without both); then run §3's project, parent and blocked-by steps. If the hook blocks the call, the worker's body is wrong: fix the file or send it back, never loosen the check.

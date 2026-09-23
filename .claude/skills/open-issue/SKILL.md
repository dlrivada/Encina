---
name: open-issue
description: Open a GitHub issue in dlrivada/Encina in the house format - the right template prefix, the template's headers verbatim and in order, checkboxes ticked, labels and milestone. Use for every bug, debt item, test gap, spike or follow-up found while working, including findings from reviews.
---

# Open an issue

Every identified problem is either fixed now or recorded as an issue before moving on (CLAUDE.md, When to Create Issues). The `check-issue-template` hook blocks `gh issue create` calls that break the format below.

Main session (orchestrator) only; an `issue-worker` reports instead of running this skill.

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
4. Be concrete: file paths with line numbers, the failing command or log excerpt, the rule or requirement broken (CLAUDE.md section, SPEC/ADR id).
5. For a `[BUG]` whose cause is known, add a Root Cause paragraph under Additional Context.

House-style references: #1050 (`[DEBT]`) and #949 (`[BUG]`).

## 3. Create it

Write the body to a scratchpad file and pass it by path, so the hook can read it and quoting cannot break it:

```powershell
gh issue create --repo dlrivada/Encina --title "[DEBT] <specific title>" --body-file <file> --label technical-debt --milestone "<milestone>"
```

- Use the template's default label plus the area labels that apply (`gh label list` when unsure).
- Pick the milestone from SPEC-000's release scope. Post-1.0 work goes to a post-1.0 milestone, never to the current one by default.
- A `[FEATURE]` gets an implementation plan before work starts (see the `implementation-plan` skill).

## 4. Afterwards

- Link the issue from the PR or document that produced it.
- Mention every issue opened in the final summary to the maintainer.

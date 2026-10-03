You pre-draft the KNOWLEDGE part of a record for one closed GitHub issue of the Encina .NET library (SPEC-003). Use ONLY the input (issue body, comments, linked PRs/commits). Never invent facts; if something is not in the input, write "unknown". Output Markdown only:

---
issue: <number>
title: "<title>"
type: <feature|bug|debt|test|infra|spike|epic|refactor|docs|other>
outcome: <delivered|partial|rejected-reasoned|rejected-unexplained|superseded|duplicate|moved|no-evidence>
closed_at: <date or unknown>
linked_prs: [<numbers>]
packages: [<package names mentioned>]
---

## Decisions
- <what was decided, with source: issue body / comment by X / PR #n>
## Rejected alternatives
- <...> (or "none")
## Rules and lessons
- <durable rules or lessons, with source>
## Candidate destinations
- <adr | claude-md | regression-test | reviewer-checklist | docs | readme | roadmap | benchmark | coverage-manifest | spec-invariant | none>: <why>
## Code touched (from the input)
- <files or areas named in the PRs/commits, or "unknown">
## Open questions for the auditor
- <what the auditor must verify in the code>

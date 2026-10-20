- A known-issues page that names projects goes stale when test projects are consolidated; check every named project against `tests/` directory names, and check the technology claim (here NBomber) against the project's own files, not only its name.
Applied: role:docs-reviewer Check every project a page names against tests/ and src/ directory names, and every technology claim against that project's own files.
- Cobertura `filename` attributes differ by flag: unit and guard report `src\Encina\...`, contract reports `Encina\...` (inside package `Encina`); a file match on `src/Encina/...` returned "0/0" for the contract flag and looked like "not executed" until the package-scoped match was used. Match on the package node and a `*Encina/<path>` suffix for every flag.
Applied: role:test-auditor Match Cobertura files per package node plus a path suffix: unit/guard report src\Pkg\..., contract reports Pkg\...; a 0/0 on one flag is a matching error until proven otherwise.
- For rule (b) on a unit that touched only tests, the scope's source files come from the original audit's "code under test" row; say in the delta brief whether sibling files of the same feature belong to the scope, otherwise the stage has to decide and the sibling gaps end up under Informational.
Applied: role:test-auditor In delta rule (b) the scope is the original audit's files; sibling files of the same feature go under Informational with their measurements, not as findings.
- tests 2: recorded as duplicate of #1821 by manual override
Applied: not applied: record of the -DuplicateOf override; the corrected contract proposal was added to #1821 as a comment.
- The Glob tool returned nothing for existing worktree paths (docs/testing/*.md); Grep found them. Absent-path claims need a Grep as a second check.
Applied: not applied: already a remediation-drafter role lesson since the #2 delta audit.
- A sibling-page convention is evidence about drift, not about the rule: docs finding 3 (missing just-the-docs front matter) stays a finding although the other docs/testing pages lack it too, because `encina-docs` SKILL section 2 requires front matter on every docs/ page. Check the convention source before dropping a finding.
Applied: role:remediation-drafter Never drop a finding because sibling pages share the gap; the rule source (e.g. encina-docs SKILL section 2) decides, and directory-wide drift is stated in the draft.
- The drafts of the earlier remediation run were missing from the remediation folder when this stage re-ran, so all five were rewritten from the inputs.
Applied: not applied: by design, a full -Prepare removes the audit's previous drafts and inputs before writing the new manifest (issue-audit skill section 2).
- When `-Finalize` strips issue references that are neither in the finding nor a manifest candidate, the verifier cannot demand them in drafts; verification should treat "orchestrator adds a comment" as the agreed channel and check only that the draft text does not contradict the relation (here tests-1's reworded #1327 line).
Applied: role:audit-verifier A relation -Finalize strips from drafts (not in the finding, not a manifest candidate) is requested as an orchestrator comment on the opened issue, not as a correction.
- After a remediation regeneration that rewrote all drafts (the earlier drafts were missing), re-compare headers against the templates with a command, not by eye; it takes one call and covers every draft.
Applied: role:audit-verifier Compare draft headers to the templates with a command after any regeneration.

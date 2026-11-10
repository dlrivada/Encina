## Pages reviewed
Issue #66 delivered no page (closed not-planned, no closing commit; archivist and code stages confirm an empty diff). Pages reviewed are the destinations the knowledge record names, plus the page that states the replacement model.

| Page | Physical lines | Mermaid blocks | Emoji lines | Notes |
| --- | --- | --- | --- | --- |
| `docs/testing/mutation-measurement-methodology.md` | 362 | 0 | 0 | destination of the decision "Mutation score is tracked per file with no project-wide target"; explanation + reference |
| `docs/testing/coverage-measurement-methodology.md` | not counted | 1 | n/a | read only for the statements `:23` and `:213` on the single project-wide threshold |
| `AGENTS.md` | n/a | n/a | n/a | section 9 `:125` (per-flag coverage) and `:132` (mutation per file); both state the replacement model |
| `docs/en/guides/MUTATION_TESTING.md` | n/a | n/a | n/a | searched for the statement; `:18`, `:47`, `:232` are the only hits for "per-file"/"project-wide" |

Counts by command: `(Get-Content f).Count` (physical lines), `Select-String '^\s*```'` (14 fence lines on the mutation page, 0 `mermaid`), emoji by Unicode range (0 hits).

## Findings
1. **Major** — `docs/testing/mutation-measurement-methodology.md` (whole page; headings "Scope" `:14`, "The mutation score formula" `:36`, "Relationship with coverage and performance" `:348`) never states the decision the record routes to it as `status: done`: that the mutation score has no project-wide target. A search of the page for `project-wide`, `no project`, `threshold` and `target` returns only unrelated hits (`:57` per-run, `:235` target project, `:110` a job name, `:228-229` merge checks); `docs/en/guides/MUTATION_TESTING.md` has no such statement either. The only place that states it is `AGENTS.md:132`, which links to this page as the methodology, so the link leads to a page that does not say it. The statement that exists is about coverage (`docs/testing/coverage-measurement-methodology.md:23`, `:213`). The check that fails is rule (a) point 5 (adequate docs for the delivered knowledge) and the accuracy of the record's destination. Add one sentence and a pointer to the per-file score in "The mutation score formula", and say that the Stryker `break` threshold is 0 (`.github/stryker-config.json:14-18`: high 80, low 60, break 0), so no mutation-score gate fails a run.
2. **Minor** — `docs/testing/mutation-measurement-methodology.md:29` ("~6,181 mutants") carries a count literal with no date and no command, and it has no citation (the "about 35.6 s per mutant" on `:32` does cite the #1087 spike and is not a finding). `:172` ("Passes, about 30 % slower (125 s against 74-102 s)") is a performance figure dated by the paragraph at `:167` (2026-10-03, Windows, Debug, `-maxthreads 4`) but gives no command that reproduces it. Rule (a) point 3 needs a citation or a sentence with the date and the command. Related page-level issues exist (#1711, #2059, #1900); none names these lines.

## Informational (not findings)
- Duplicates, not re-reported: the weekly-schedule claim and the missing job-graph diagram on the same page are tracked (#2059, #1900); the stale 17-shard and VsTest wording is #1711; the README badge claims are #2072 and #2073. The page has 0 Mermaid blocks; that gap is #1900.
- Placement: `docs/testing/` is excluded from Jekyll in `docs/_config.yml` (see audit #31 `docs.md` informational), so missing front matter on the methodology pages is not a finding. The mutation page is linked from `AGENTS.md:132`, `README.md:25`, `docs/en/guides/TESTING.md:90` and the coverage and performance methodology pages.
- `docs/index.md:73-75` ("Quality & Coverage") links only the coverage dashboard and not the mutation page; covered by the placement note above, not raised.
- The replacement model for the coverage decision is stated correctly: `AGENTS.md:125`, `coverage-measurement-methodology.md:23`, `:213`. No Spanish, no emoji on the reviewed pages.
- Not run: `lychee`, `markdownlint-cli2` and the `cov-docs-render.cs` dangling check were not executed (no page was changed by the issue; this is a read-only stage). The `mutref` table at `:70-90` is a marker-expanded table and was not recomputed.

## Lessons for the pipeline
- When a record marks a docs destination `done` for a decision phrased as a negation ("no project-wide target"), search the destination page for that negation itself, not for the topic; the page can be on-topic and still never state it, and a rule file that links to it makes the gap invisible.

Remediation for #67:
- code 1 (Major): draft 67-code-1-about-63-of-src-encina-is-in-no.md
- code 2 (Major): draft 67-code-2-the-reusable-template-encina-full-ci-yml-offers.md
- code 3 (Minor): draft 67-code-3-github-scripts-list-survivors-cs-computes-the-per.md
- code 4 (Minor): draft 67-code-4-the-mutation-dashboard-and-its-inputs-apply-a.md
- code 5 (Minor): draft 67-code-5-github-scripts-show-mutant-cs-can-never-run.md
- tests 1 (Major): draft 67-tests-1-github-workflows-mutation-tests-yml-300-334-select.md
- tests 2 (Minor): draft 67-tests-2-github-scripts-list-survivors-cs-85-case-timedout.md
- tests 3 (Minor): draft 67-tests-3-github-workflows-templates-encina-full-ci-yml-561.md
- docs 1 (Blocker): draft 67-docs-1-docs-en-guides-requirements-md-26-35-53.md
- docs 2 (Major): draft 67-docs-2-docs-en-guides-requirements-md-34-and-52.md
- docs 3 (Major): draft 67-docs-3-docs-ci-cd-templates-md-211-documents-the.md
- docs 4 (Major): draft 67-docs-4-docs-testing-mutation-measurement-methodology-md-does-not.md
- docs 5 (Minor): draft 67-docs-5-hand-typed-counts-in-prose-with-no-date.md
- docs 6 (Minor): duplicate of #1900 (manual override)
- docs 7 (Minor): draft 67-docs-7-rule-a-point-5-mutation-testing-has-a.md

## Lessons for the pipeline
- docs 6: recorded as duplicate of #1900 by manual override
- code 1: recorded as not a duplicate and kept out of location merges by manual override
- tests 1: recorded as not a duplicate and kept out of location merges by manual override
- (tests 1) The manifest's partially related line for tests 1 is #2059, which the verifier called unrelated (a docs issue about the paused schedule); the draft cites the manifest line verbatim. The verifier asked for #2064 (static checks of the mutation, load and benchmark workflows) instead; the rule does not let this draft cite it, so the orchestrator should replace the line or add #2064 as a comment when opening.
- (code 1) The manifest has no partially related lines for code 1; the draft cites #2829, #2897, #2830 and #2914 because the finding names them, each as a package-level issue.
- (docs 6) Two additions to #1900 for the orchestrator's comment: a flowchart of the accumulation model (`## The accumulation model`) and a diagram in `docs/en/guides/MUTATION_TESTING.md`. The methodology table lists four jobs; publish is the separate workflow `publish-mutations.yml`.
- (docs 5) The README "17-shard" comment (#1711) and the "~6,181 mutants" item (#3324) are excluded; only the six 76-shard occurrences are drafted.
- (docs 7) The draft is restricted to the tutorial gap; the docs-index navigation entry belongs to #1901 (named in Description as the finding states it, not as a Related Issues line, since the manifest has no partially related line for it).
- (code 2, code 4, docs 3) These three drafts hold a Dilemma; the header carries only the route's labels, so the orchestrator adds `needs-decision` when opening them (maintainer decides the option).
- (code 2, tests 3, docs 3) The step text follows the re-run stage: bash `-eo pipefail`, the loop without `globstar` (other steps of the file set `shopt -s globstar nullglob`, for example `:247`), both branches derived and not run.

Remediation for #66:
- docs 1 (Major): draft 66-docs-1-docs-testing-mutation-measurement-methodology-md-whole-page.md
- docs 2 (Minor): draft 66-docs-2-docs-testing-mutation-measurement-methodology-md-29-6.md

## Lessons for the pipeline
- (docs 1) A sibling page can hold two lines about the same word: `coverage-measurement-methodology.md:23` argues that a single project-wide threshold is wrong, while `:213` states "There is no project-wide target". Cite the line whose meaning matches the rule (`:213`).
- (docs 1) The first drafts of this run repeated an earlier docs stage claim that the Stryker configuration had no break threshold; `.github/stryker-config.json:14-18` sets high 80, low 60, break 0, and the draft now states that.

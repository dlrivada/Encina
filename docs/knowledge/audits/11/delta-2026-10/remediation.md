Remediation for #11:
- tests 1 (Minor): duplicate of #1850 (manual override)
- tests 2 (Minor): duplicate of #1850 (manual override)
- docs 1 (Minor): draft 11-delta-2026-10-docs-1-src-encina-fluentvalidation-readme-md-58-61-2.md
- docs 2 (Minor): draft 11-delta-2026-10-docs-2-src-encina-fluentvalidation-readme-md-94-239-and.md
- docs 3 (Minor): duplicate of #1330 (manual override)
- docs 4 (Minor): draft 11-delta-2026-10-docs-4-src-encina-fluentvalidation-readme-md-15-fully-tested.md
- docs 5 (Minor): duplicate of #1850 (manual override)

## Lessons for the pipeline
- docs 3: recorded as duplicate of #1330 by manual override
- docs 5: recorded as duplicate of #1850 by manual override
- tests 1: recorded as duplicate of #1850 by manual override
- tests 2: recorded as duplicate of #1850 by manual override
- docs 4: recorded as not a duplicate and kept out of location merges by manual override
- (docs 4) Tests for the package exist (unit tests under `tests/Encina.UnitTests/FluentValidation/`, property tests in `tests/Encina.PropertyTests/Validation/FluentValidation/ValidationInvariantProperties.cs`), so the "Fully Tested" sentence is uncited and unmeasurable (assembly absent from the Cobertura output), not false; the draft says so. The same phrase is in the READMEs of `Encina.DataAnnotations`, `Encina.MiniValidator` and `Encina.GuardClauses` (count-mode search of `src/**/README.md`: 4 files), which the finding did not report. (State the verified state of an uncited claim: not measurable is not the same as untrue; search sibling READMEs for the same phrase.)
- (docs 1, docs 2, docs 4) The findings name #898 (docs 4) without a title; the manifest has none, so the drafts do not cite it and docs 4 says "tracked in a separate open issue". The orchestrator can add #898 as a comment when opening docs 4. (Issues named by a finding without a manifest title go to the orchestrator as a comment.)
- (docs 1, docs 2, docs 4) Pass 2 after a verifier FAIL rewrote all three drafts (not on disk when this run started). The Related Issues line for the consolidated issue carries no second issue number in its title text (the manifest title of #1850 names another issue, and -Finalize would strip it and leave dangling text). Docs 2 Current Behavior now places `using FluentValidation;` at `README.md:28` (Step 1 block) and says the Step 3 (`Right<...>`), Step 4 and ASP.NET (`ValidationException`) blocks show no `using` line for `LanguageExt.Prelude` or `FluentValidation`. A scope search of `src/**/README.md` for "automatically registered" finds the validation-behavior claim only in the FluentValidation README. (Never copy a manifest issue title that contains another issue number into a Related Issues line; write a number-free description.)
- (docs 1, docs 2) The `error.Exception` use in the README samples is the subject of #1330 and is left out of the docs 2 draft; the verified facts were re-read in the audit worktree: `MediatorAssemblyScanner.cs:93-98`, `ServiceCollectionExtensions.cs:62-64, :104-106`, `IEncina.cs:46`, and the README lines `:28, :53, :58-61, :86, :94, :105, :239-246`. (Keep each defect with the issue that owns it.)

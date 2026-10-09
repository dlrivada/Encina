Remediation for #14:
- tests 1 (Major): draft 14-delta-2026-10-tests-1-src-encina-minivalidator-minivalidationprovider-cs-and-src-e.md
- tests 2 (Minor): draft 14-delta-2026-10-tests-2-src-encina-validation-validationpipelinebehavior-cs-and-src.md
- docs 1 (Major): draft 14-delta-2026-10-docs-1-src-encina-minivalidator-readme-md-384-396-mix.md
- docs 2 (Major): draft 14-delta-2026-10-docs-2-src-encina-minivalidator-readme-md-105-sample-use.md
- docs 3 (Minor): draft 14-delta-2026-10-docs-3-src-encina-minivalidator-readme-md-268-comparison-table.md
- docs 4 (Minor): draft 14-delta-2026-10-docs-4-src-encina-minivalidator-readme-md-96-241-305.md
- docs 5 (Major): duplicate of #1850 (manual override)
- docs 6 (Major): duplicate of #1850 (manual override)
- docs 7 (Major): duplicate of #1850 (manual override)
- docs 8 (Minor): duplicate of #1330 (manual override)

## Lessons for the pipeline
- docs 5: recorded as duplicate of #1850 by manual override
- docs 6: recorded as duplicate of #1850 by manual override
- docs 7: recorded as duplicate of #1850 by manual override
- docs 8: recorded as duplicate of #1330 by manual override
- docs 4: recorded as not a duplicate and kept out of location merges by manual override
- (tests 1, 2) The targets and ceilings (guard 22 and 20, package guard 21, guard 33 and 5, and the unit, property and contract 100 values) come from the tests stage's measured line counts plus reading the code; the stage wrote no tests and ran no manifest check after proposing them. The drafts say "proposals derived from those measurements", and the ceil arithmetic of every guard value was recomputed from the covered and coverable counts and matches the stage. The existence claim "no guard test references the MiniValidator package" was re-checked with a Grep of `tests` for `MiniValidat` (only the guard project file matches, no `.cs` file). Keep derived values labelled until a measured run confirms them.
- (tests 1, docs 1) Issues the findings name without a manifest title (the guard-test tracker for the three validation packages in tests 1, the DataAnnotations delta re-audit issue in docs 1) appear in Related Issues with a description of what the finding says about them, because this agent cannot read the live issue bodies. The title of the consolidated delta re-audit issue that holds the ValidationOrchestrator targets (tests 2) contains another issue number, so that draft describes it without the title. The partially related lines of docs 1 and docs 4 are verbatim. Issues the findings only mention in the informational part of the tests stage were left out.
- (docs 1-4) The Glob and brace-glob tools are unreliable for existence checks in the audit worktree; the zero-hit claims (no `AddFluentValidation(` in any Markdown file or in `src/` other than the README line, the phrases "Mix and Match" and "Works great with FluentValidation" only in the MiniValidator README) were checked with Grep over the folder, with the README line itself as the positive control. Namespaces were read from the source (`IEncina.cs:3`, `EncinaError.cs:4`, both `namespace Encina;`) before the docs 4 draft said `using Encina;` is needed.
- (docs 1) The finding says the other package's validators "never run"; the draft states the verified mechanism: every registration in both packages is a `TryAdd*` (`ServiceCollectionExtensions.cs:65-67` in MiniValidator, `:62-64` and `:104-106` in FluentValidation), so the first call wins for the provider, the orchestrator and the behavior. The README sample calls `AddMiniValidation()` first, so the FluentValidation validators are never invoked in that sample.
- (docs 2) The README says `error.Message.Split(", ").Skip(1)` extracts the errors; the draft quotes `ValidationResult.cs:88` for the message format and the orchestrator line `ValidationOrchestrator.cs:70-71` that puts it in the `Left`, so the first-error loss is shown from the code rather than from the finding.
- (docs 1) The "#2014 partially related" line of docs 1 is re-added by -Finalize from the manifest although the verifier found it false; no override exists to drop it (gap recorded on #1863). The orchestrator removes that line from the opened issue body.

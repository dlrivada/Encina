Remediation for #57:
- code 1 (Major): draft 57-code-1-src-encina-modules-moduleservicecollectionextensions-cs-75-r.md
- code 2 (Major): draft 57-code-2-src-encina-modules-moduleservicecollectionextensions-cs-105.md
- code 3 (Minor): draft 57-code-3-src-encina-modules-moduleservicecollectionextensions-cs-70-7.md
- code 4 (Minor): draft 57-code-4-module-name-and-shared-assembly-validation-is-missing.md
- code 5 (Minor): draft 57-code-5-module-lifecycle-failure-handling-in-src-encina-modules.md
- code 6 (Minor): draft 57-code-6-module-telemetry-is-dead-code-and-the-lifecycle.md
- code 7 (Minor): draft 57-code-7-imodulelifecycle-hooks-cannot-reach-any-service-modules-are.md
- tests 1 (Major): draft 57-tests-1-github-coverage-manifest-encina-json-entries-modules-moduler.md
- tests 2 (Major): draft 57-tests-2-no-test-calls-addencinamodules-to-resolve-and-use.md
- tests 3 (Major): draft 57-tests-3-tests-encina-unittests-core-modules-modulelifecyclehostedser.md
- tests 4 (Minor): draft 57-tests-4-tests-encina-unittests-core-modules-moduleregistrytests-cs-1.md
- docs 1 (Major): draft 57-docs-1-rule-a-point-5-no-concept-or-guide.md
- docs 2 (Major): draft 57-docs-2-rule-a-point-5-the-feature-appears-in.md
- docs 3 (Major): draft 57-docs-3-knowledge-not-stated-on-any-reader-facing-page.md
- docs 4 (Major): draft 57-docs-4-docs-guides-health-checks-md-110-146-modular.md
- docs 5 (Minor): draft 57-docs-5-rule-a-point-1-and-point-4-on.md
- docs 6 (Minor): duplicate of #1299 (manual override)

## Lessons for the pipeline
- docs 6: recorded as duplicate of #1299 by manual override
- (docs 3) The record and docs stage say a duplicate module name is rejected "with ArgumentException"; `ModuleConfiguration.AddModule` throws `InvalidOperationException` (`ModuleConfiguration.cs:75, :106`) and only the `ModuleRegistry` constructor throws `ArgumentException` (`ModuleRegistry.cs:82`). The draft states both. (Check the exception type of a delivered contract in the code before repeating a record's wording.)
- (docs 3, correction) The record has no duplicate-name decision; only the docs stage said it.
- (code 4) A null module name passes `ModuleConfiguration.AddModule` (the null is only an argument of `Equals`) and fails at `ModuleRegistry.cs:41`, not at registration; the draft states that path. (Trace a null input through the whole registration path before saying where it fails.)
- (code 4, correction) The null name throws from the `AddEncinaModules` call itself, because the registry is built eagerly at `ModuleServiceCollectionExtensions.cs:70`; it is not thrown in `AddModule` and not at provider construction.
- (tests 1, 2, 3) Targets and measured figures are the tests stage's Release-build figures; the proposed targets are provisional and were not run. The deletion of the old module tests is git history that this agent could not re-read. (Keep provisional labels on unrun targets.)
- (all drafts) Related Issues lists only #57 and the issues the finding itself names with a manifest title (#1955 in docs 1); `possiblyRelated` hits such as #360 were left out of drafts that do not name them. The orchestrator may add #360 as a comment on the code 7 draft's issue; it is already cited there because the finding names it.

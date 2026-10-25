## Pages reviewed

Issue #58 delivered module-scoped pipeline behaviors (`IModulePipelineBehavior`, `AddEncinaModuleBehavior`, `RequestContextModuleExtensions`). It shipped no docs page and no README section. Reviewed, from the worktree root:

- `src/Encina/Modules/IModulePipelineBehavior.cs` (the XML doc is the only user-facing description of the feature).
- `docs/features/module-isolation.md` (1059 lines, the nearest module page) and `docs/features/index.md`.
- `docs/tutorials/index.md`, `docs/index.md`, `README.md` (placement and learning paths).
- Search: `Get-ChildItem docs,README.md,src\Encina -Recurse -Include *.md` excluding `knowledge`, `releases` and `plans`, pattern `AddEncinaModuleBehavior|IModulePipelineBehavior|ModuleBehaviorAdapter|module-scoped|module-specific behavior|module behavior`: 2 hits, `docs/engineering/PROJECT-HISTORY.md:471` (one history sentence) and `docs/INVENTORY.md:6136` (an unrelated purpose-registry bullet). `docs/features/module-isolation.md` has 0 hits for `IModulePipelineBehavior|AddEncinaModuleBehavior|GetModuleName`.

## Findings

1. **Major** — No concept or guide page and no reference exists for module-scoped behaviors (rule (a) point 5). Evidence: the search above finds the feature in no page of `docs/` except one history sentence (`docs/engineering/PROJECT-HISTORY.md:471`); `docs/features/module-isolation.md` covers database isolation only (headings `Isolation Strategy Comparison`, `Request Flow`, `Provider-Specific Setup`) and never mentions module behaviors; `docs/features/index.md:82` links only Module Isolation; `docs/tutorials/` contains only `index.md` and `quickstart.md`, and `docs/tutorials/index.md` has 0 hits for "odul"; `README.md` mentions modules only in a roadmap table row (`README.md:551`). The only description a reader finds is the XML doc of `IModulePipelineBehavior`. Check that fails: encina-docs SKILL placement (a feature needs a guide in `docs/features/`, linked from the index) and issue-audit rule (a) point 5 (a feature in scope appears in a guide, a reference and the tutorials and learning paths). Nearest open work: the code stage's findings 1 and 2 (#58 audit) change what a page could truthfully say, so the page should be written after those are decided.

2. **Major** — The only usage sample for the feature does not compile and describes a registration shape that does not exist. `src/Encina/Modules/IModulePipelineBehavior.cs:49` shows `services.AddEncinaModuleBehavior<OrderModule, OrderAuditBehavior<,>>();`; the only overloads take four closed type arguments `TModule, TRequest, TResponse, TBehavior` (`src/Encina/Modules/ModuleBehaviorServiceCollectionExtensions.cs:43-47`, `:86-91`), so the two-argument open-generic call matches none. Check that fails: SKILL §6 real API (wrong parameter list is a blocker for a page; for a doc comment on the public contract it is Major because it is the sole documentation). Same defect as the code stage's finding 2; this finding is the documentation side and is fixed by correcting the sample once the registration shape is decided.

3. **Minor** — The XML documentation of the delivered types is stale. `IModulePipelineBehavior.cs:18` says the stage contract "is declared but not yet applied; until the order resolver lands, behaviors still run in registration order", but the resolver is registered and applied (code stage finding 7a); `IModulePipelineBehavior.cs:67` names the metadata key `"ModuleName"` while the real key is `"Encina.ModuleName"` (`src/Encina/Modules/RequestContextModuleExtensions.cs:11`). Check that fails: accuracy against today's code (SKILL §6). Overlaps code finding 7; one remediation covers both.

4. **Minor** — Visuals (rule (a) point 1). The feature has no diagram showing which handlers a module behavior wraps (module, handler assembly, adapter, global behaviors); the nearest page, `module-isolation.md`, has a request-flow section but none for behaviors. A flowchart of "request, handler resolved, owning module, behavior runs or is skipped" would show the filtering rule that the code stage found undiagnosable (code findings 4 and 6). Minor because no page exists yet; it becomes part of the page of finding 1.

## Informational (not findings)

- No figure to check: no page quotes coverage, mutation or performance numbers for this feature. `CHANGELOG.md` and the pre-v0.10.0 release page mention "100 module-related unit tests" for #58; they are dated history pages, not guides, so no `covref` is expected.
- No ADR is named by any page for the filtering design (assembly association instead of direct registration); `docs/engineering/PROJECT-HISTORY.md:471` states it as history. Whether it needs an ADR is a maintainer decision, not a docs defect.
- `docs/knowledge/audits/57/` (the sibling audit of #57, the module registry) records the same absence of a module guide; one page can cover both once written.
- Nothing was run: lychee and markdownlint were not needed because no page was changed or added; relative links in the pages named above were not part of the issue.

## Lessons for the pipeline

- For an issue that shipped no page, the XML doc of the delivered public contract is the de facto documentation; compare its example to the registration overloads' type-parameter count, because a symbol search passes (`AddEncinaModuleBehavior` exists) while the call shape fails.
- A feature whose nearest page covers a different concern (here `module-isolation.md`, database isolation) is not the feature's page; check the page for the feature's type names before counting it.

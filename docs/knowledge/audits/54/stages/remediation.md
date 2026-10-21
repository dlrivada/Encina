Remediation for #54:
- docs 1 (Minor): draft 54-docs-1-docs-engineering-project-history-md-724-adr-candidate.md
- docs 2 (Major): draft 54-docs-2-docs-engineering-project-history-md-524-and-724.md
- docs 3 (Minor): draft 54-docs-3-docs-inventory-md-4279-api-versioning-389-version.md
- docs 4 (Minor): draft 54-docs-4-docs-releases-v0-11-0-changelog-details-md.md
- docs 5 (Minor): draft 54-docs-5-rule-a-point-5-adequate-docs-the-decision.md

## Lessons for the pipeline
- (docs 3, docs 4) The docs stage listed the contradicting rows as `INVENTORY.md:4279, :4303` plus `:2834, :5381, :6837` and the release page entries; a full search for the four issue numbers found more places: `INVENTORY.md` rows and detail sections for #348 (`:4778`, `:4850-4858`, `:4955`, `:5157`) and #458 (`:7024`, `:7454`), and the same planned entries in `CHANGELOG.md` (`:6927-6932`, `:6992-6995`, `:7493-7494`, `:8253`) and `docs/releases/pre-v0.10.0/README.md` (`:6522`, `:6590-6592`). The drafts list them. (For a rejected idea, search every page for the issue numbers of its backlog twins, not only the pages the first search returned.)
- (docs 5) The docs stage proposes "a short how-to under the web guides" but no folder of that name exists; the draft names `docs/guides/` and its `index.md` (five pages listed), the folder that exists. `Asp.Versioning` appears in 4 `docs/` files (Grep, files-with-matches), none a guide. (Name only folders that exist as the home of a new page.)
- The Glob tool returned no files for `artifacts/knowledge/stages/*.md` in the audit worktree although the files exist; existence was confirmed by reading them. (Confirm existence with Read or count-mode Grep; Glob misses existing worktree paths.)
- Issue #889 is named by docs 2 ("#889 lists #192") but the manifest carries no title for it, so no draft cites it; the orchestrator can add it as a comment on the docs 2 issue when opening. (Issues named without a manifest title go to the orchestrator as a comment.)
- This agent has no shell, so the issue states (#389 and #458 closed as duplicates of #348 on 2026-10-06, #192 and #348 open, their Post-1.0 milestones) are stated as the docs stage recorded them from `gh api` on 2026-10-21. (State issue facts that need `gh` as the stage recorded them.)

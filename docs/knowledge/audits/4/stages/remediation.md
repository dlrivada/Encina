Remediation for #4:
- docs 1 (Major): duplicate of #85 (manual override)
- docs 2 (Major): duplicate of #85 (manual override)
- docs 3 (Major): duplicate of #85 (manual override)
- docs 4 (Minor): duplicate of #86 (manual override)
- docs 5 (Minor): duplicate of #90 (manual override)
- docs 6 (Minor): duplicate of #85 (manual override)

## Lessons for the pipeline
- docs 1: recorded as duplicate of #85 by manual override
- docs 2: recorded as duplicate of #85 by manual override
- docs 3: recorded as duplicate of #85 by manual override
- docs 4: recorded as duplicate of #86 by manual override
- docs 5: recorded as duplicate of #90 by manual override
- docs 6: recorded as duplicate of #85 by manual override
- (docs 1-6) Every finding here is an edit to the body of an open issue (#85, #86, #90), not a change to a repository file. The six drafts are written as debt issues because the manifest routes them so, but the orchestrator may prefer one comment per target issue (docs 1, 2, 3, 6 on #85; docs 1 and 4 on #86; docs 5 on #90) over six new issues. This agent has no `gh` and could not read the live issue bodies, so every statement about #4, #85, #86 and #90 is the docs stage's account of them.
- (docs 5) The draft keeps "re-scope #90" and "close #90" as alternatives for the maintainer; the orchestrator should add the `needs-decision` label when opening it. The live-site check was not done by the stage or by this agent.
- (docs 3) The stage says the `encina-docs` skill decides the placement; the skill (section 1) puts how-to guides in `docs/guides/` with a title that starts with "How to" and lists no `docs/migration/` location, so the draft proposes `docs/guides/` and leaves a separate folder as the maintainer's alternative. The existence of `docs/migration/` was checked with a Grep on the folder ("Path does not exist"), because Glob is unreliable for existence checks here.
- (docs 2) The stage cites `IEncina.cs:46`, `:77` and `:89`; `:46` is `Send` without a context, `:77` is `Send` with a context and `:89` is `Publish`. The draft also names `Stream` (`IEncina.cs:144`), which the stage did not. All other identifiers and lines (`IRequestHandler.cs:57`, `IStreamRequest.cs:35`, `IStreamRequestHandler.cs:61`, `CqrsContracts.cs:29, :60, :79, :97`) were re-read in the audit worktree.
- (docs 1) The stage asks for the README link and the `docs/index.md` and `docs/tutorials/index.md` entries; the `encina-docs` skill indexes a how-to from `docs/guides/index.md`, so the draft adds that index. The README does link the Pages site's dashboards (`README.md:19-26`), so the draft says only that it has no link to a migration guide or comparison.
- (docs 5) The stage says "no root `docfx.json` exists"; that is verified, but `docs/docfx.json` does exist and is what `docs.yml:206, :209` runs, so the draft states both. `peaceiris` has 0 matches under `.github`.
- (docs 6) The finding records a present state rather than a defect; the draft turns it into the one actionable item it supports (link the guide and the existing comparison section both ways when #85 lands) and makes no change to `docs/introduction.md` now, since a link to a missing page would fail the link check. The stage mentions a learning path; none was confirmed to exist, so the drafts name only the tutorials index.
- (all drafts) Related Issues carry only #4, the issues the findings name (#85, #86, #90, #1381) and the manifest's partially related line for docs 2 (#2014, verbatim; its body was not read). The search hits in the manifest are not cited. The mapping-table claim that #4 is "closed" and #85, #86, #90 are "open" is as the stage recorded it on 2026-10-10.
- (2026-10-10, #4) (correction, docs 1-6) All six findings are duplicates of #85 (docs 1, 2, 3, 6), #86 (docs 4) and #90 (docs 5) by manual override after the verifier FAIL, so the manifest has no draft file and none is written. The earlier lessons above that describe drafts (the issue-comment suggestion, the `needs-decision` label for docs 5, the docs 3 placement, the docs 2 line citations, the docs 1 index, the docs 5 `docfx.json`, the docs 6 reading and the Related Issues line) describe the earlier version and no longer apply to a draft; the orchestrator can use their verified facts (for example `IEncina.cs:46, :77, :89, :144`, `docs/docfx.json` run by `docs.yml:206, :209`, the `encina-docs` placement in `docs/guides/`) as a comment on #85, #86 and #90 when it records the duplicates. (kept lessons are history: never reword them; add corrections as new lessons after them)

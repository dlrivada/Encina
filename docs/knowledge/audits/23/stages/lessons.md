- The pre-draft outcome `rejected-unexplained` was wrong: its open question ("why created in error") is answered by searching same-titled issues (`gh issue list --state all --search "<title> in:title"`), which found #44. GitHub's state_reason COMPLETED is not evidence of delivery.
Applied: not applied: already a role lesson of issue-archivist (audits #21 and #22).
- A fresh wia worktree has no `artifacts/knowledge/issues` or predraft; read the pre-draft from the main checkout and create the issues directory before writing.
Applied: not applied: corrected in PR #1679 (the Write tool creates the folder; never New-Item it).
- For a closed-in-error issue the successor's files carry unit/guard gaps that look like findings; the "Findings" section stays at "- none" and they go to "Informational (not findings)" for the successor's audit.
Applied: not applied: already a role lesson of test-auditor (audit #22).
- A filter `FullyQualifiedName~EncinaFixtureTests` does not match `EncinaTestFixtureTests`; a class-name filter must be checked against the class list, and the Base\ duplicate folder makes every such filter run both copies.
Applied: role:test-auditor Check a class-name test filter against the actual class list before running it; tests/Encina.UnitTests/Testing/Base duplicates Testing/ (issue to open), so filters there run both copies.
- The docs stage cites `EncinaFixture.cs:37`; the class declaration is at `src/Encina.Testing/EncinaFixture.cs:42` (docs stage wrong by five lines; the draft uses :42).
Applied: role:docs-reviewer Read every cited line (and the file's line count) before writing a file:line citation (audit #23 FAIL: ROADMAP.md:3102 in an 841-line file, EncinaFixture.cs:37 for :42).
- Open issue #1203 covers READMEs for Anonymization, PrivacyByDesign, CrossBorderTransfer and Security.Audit only, not Encina.Testing, so it shares no file anchor with this finding and is not cited.
Applied: not applied: informational; the verifier confirmed no duplicate.

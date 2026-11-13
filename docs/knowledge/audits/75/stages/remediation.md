Remediation for #75:
- code 1 (Minor): draft 75-code-1-src-encina-testing-fakes-16-cs-files-a.md
- code 2 (Minor): draft 75-code-2-sonarcloud-yml-51-installs-the-scanner-unpinned-dotnet.md
- code 3 (Minor): draft 75-code-3-five-of-the-46-multicriteria-ignore-entries-are.md
- tests 1 (Minor): draft 75-tests-1-github-workflows-sonarcloud-yml-whole-file-scan-completeness.md
- tests 2 (Minor): draft 75-tests-2-github-workflows-sonarcloud-yml-60-164-the-46.md
- docs 1 (Major): draft 75-docs-1-roadmap-md-42-the-sonarcloud-issues-row-of.md
- docs 2 (Minor): draft 75-docs-2-roadmap-md-159-milestone-completed-58-issues-closed.md

## Lessons for the pipeline
- (2026-11-12, #75) This agent has no shell, so the SonarCloud and `gh` facts (run 43217793385 log lines, file counts 3041/2961, `SONAR_TOKEN` date, run times, closed state of the issue) are stated as the stages recorded them; only repository facts were re-read in wia-75 (`sonarcloud.yml:3-8, 25-28, 51, 60-68, 107-128, 161-162, 176`, `.config/dotnet-tools.json`, `Directory.Packages.props:44`, `Directory.Build.props:34, 70-73`, `Encina.Testing.Fakes.csproj:5`, `ROADMAP.md:42, 159`, `PHASE0-BASELINE.md:15, 148`). Unverified by this agent: the live SonarCloud state.
- (2026-11-12, #75) docs 2 cites PHASE0-BASELINE as `:15` (item 4); the same claim is also at `:148`, which the draft adds. The line is in the v0.12.0 section, which the docs stage did not name.
- tests 2: recorded as not a duplicate and kept out of location merges by manual override

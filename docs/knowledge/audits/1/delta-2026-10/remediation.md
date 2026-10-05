Remediation for #1:
- tests 1 (Minor): draft 1-delta-2026-10-tests-1-github-coverage-manifest-encina-json-200-207-core.md
- tests 2 (Minor): draft 1-delta-2026-10-tests-2-github-coverage-manifest-encina-json-313-320-dispatchers.md
- tests 3 (Minor): merged into tests 1 (same location)
- docs 1 (Major): draft 1-delta-2026-10-docs-1-readme-md-365-streaming-lines-357-383-the.md
- docs 2 (Minor): draft 1-delta-2026-10-docs-2-readme-md-streaming-357-383-rule-a-point.md
- docs 3 (Minor): draft 1-delta-2026-10-docs-3-readme-md-streaming-rule-a-point-1-one.md

## Lessons for the pipeline
- The tests stage called the type in `src/Encina/Dispatchers/MediatorAssemblyScanner.cs` `MediatorAssemblyScanner`; the file declares `internal static class EncinaAssemblyScanner` (line 9). A finding about a type should name the declared type, not only the file name.
- The delta rule (b) prompt cannot tell the tests stage whether to compare against the package aggregate or a per-file number; since the manifest has only aggregates, the delta brief should state that findings propose per-file values and cite the measured per-file coverage to support them.
- The docs finding on `README.md:365` was found by comparing the handler return type with the interface; checking every name in a sample for existence is not enough, so a docs stage should compare the signatures of any implemented interface too.

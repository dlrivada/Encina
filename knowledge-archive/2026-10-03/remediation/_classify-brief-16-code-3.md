Classify ONE finding from the SPEC-003 audit of closed GitHub issue #16 of the Encina .NET library. Reply with
EXACTLY one line and nothing else:

kind: bug|test|debt|docs; duplicate-of: #m|none; keywords: k1, k2, k3

- kind: "bug" for a code defect, "test" for missing tests or a coverage gap, "debt" for messy, duplicated,
  incomplete or slow code that is not itself a defect, "docs" for documentation drift.
- duplicate-of: the number of one of the candidate open issues below ONLY if it covers the exact same
  problem as this finding; otherwise "none".
- keywords: up to 3 short keywords for the finding.

Candidate open issues (from gh issue list --search, title -- first 400 characters of body):
#1343: [DEBT] Error-message leak static scan misses multi-line calls and ex.Message; leaks in SagaRunner and CDC cache invalidation -- ## Type - [ ] Failing tests - [x] Missing tests - [ ] Code quality (warnings, analyzers) - [ ] Performance optimization - [ ] Refactoring needed - [ ] Documentation gap - [x] Incorrect implementation - [ ] Other ## Description The fix for #1319 extended `ErrorMessageLeakStaticScanTests` to `src/Encina`. While doing so, the adversarial review found two gaps in the scan: 1. It misses statements th

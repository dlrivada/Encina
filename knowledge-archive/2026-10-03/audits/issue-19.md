# Audit: Issue #19 - Coverage Threshold 85%

| ID | Check | Outcome | Evidence |
|---|---|---|---|
| AUD-01 | Code implements the decision or change is recorded | n/a | Issue was rejected-reasoned; the old 85% single-percentage model was superseded by per-flag, per-package model in .github/coverage-manifest/ and CLAUDE.md |
| AUD-02 | Cross-cutting functions integrated or deferred | n/a | No feature implementation; technical debt (coverage model) issue |
| AUD-03 | Source files in coverage manifest and reach targets | n/a | This issue is ABOUT the coverage manifest system, not about specific package code |
| AUD-04 | No reflection-only or type-assert-only tests | n/a | No code changes; coverage methodology only |
| AUD-05 | Required test types exist or justified | n/a | No production code; technical debt issue |
| AUD-06 | Fixed bugs have regression tests | n/a | No bug fix; technical debt issue |
| AUD-07 | Provider-dependent features tested on all providers | n/a | No production code; technical debt issue |
| AUD-08 | EventIds within registered ranges | n/a | No logging added; technical debt issue |
| AUD-09 | Public types in PublicAPI files | n/a | No public types added; technical debt issue |
| AUD-10 | Public types have XML documentation | n/a | No public types added; technical debt issue |
| AUD-11 | Package README accuracy and Diátaxis | n/a | No package changes; coverage model only |
| AUD-12 | Security/compliance behaviors fail closed | n/a | No security or compliance code; technical debt issue |
| AUD-13 | No EncinaError message leaks | n/a | No error handling added; technical debt issue |
| AUD-14 | No direct DateTime.UtcNow reads | n/a | No business logic; coverage model only |
| AUD-15 | Options classes protect secrets | n/a | No options classes; technical debt issue |
| AUD-16 | Database calls async with CancellationToken | n/a | No database code; technical debt issue |
| AUD-17 | AddEncina* extensions register all services | n/a | No new extensions; technical debt issue |
| AUD-18 | No Obsolete members | n/a | No public types; technical debt issue |

## Coverage

No production code affected; verdict: **not-audited** (rejected-reasoned technical debt issue).

## Decision Record

Issue #19 requested increasing the global code coverage threshold to 85%, but this request was closed with a reasoned rejection: the coverage model had already been superseded by a more sophisticated per-flag, per-package system in `.github/coverage-manifest/`. 

The per-flag model measures each test type (unit, guard, contract, property, integration) independently against package-specific targets, which is more accurate and actionable than a single global percentage. This is now the standard in CLAUDE.md and enforced by `.github/scripts/coverage-report.cs`.

## Remediation

None found. The coverage model transition is complete and documented in CLAUDE.md and the coverage dashboard.

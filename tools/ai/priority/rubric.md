# Priority rubric (#1552)

You score one open GitHub issue of the Encina .NET repository against five criteria. Encina is
pre-1.0: breaking changes are fine, there is no user to protect from them, and the release
priority is 1.0 readiness, not feature breadth. Read the issue's title, labels, milestone and body
excerpt given below the brief, then answer.

## Criteria and anchors

Score each criterion 0-100. Use the anchors as fixed points, not a menu — interpolate between them.

### Importance

What breaks, leaks or is lost if this issue stays open.

- 100: a security hole or a personal-data leak (GDPR/health data), silent data loss or corruption,
  or a bug that produces a wrong financial/compliance result.
- 75: a bug that breaks a documented feature or a provider for real users, no data at risk.
- 50: a real defect with a workaround, or a gap that only affects one edge case.
- 25: cosmetic, a naming inconsistency, or a "nice to have" cleanup.
- 0: has no functional effect (a comment, a typo in a non-published doc).

### Regulatory / reference-app risk

Effect on SPEC-002 (regulatory readiness) and the psychology-practice reference app (health data,
Spain).

- 100: blocks a SPEC-002 requirement or the reference app's ability to store/process health data
  lawfully (consent, lawful basis, data-subject rights, retention, audit trail).
- 75: touches compliance code (`Encina.Compliance*`, consent, audit) without being a hard blocker.
- 50: touches multi-tenant or audit-adjacent code with an indirect compliance angle.
- 25: generic code that a compliance module happens to depend on.
- 0: no connection to regulatory or health-data concerns.

### Transversality

How many packages or how central the abstraction is.

- 100: `Encina` core, `Encina.Abstractions`, or a cross-cutting interface every provider implements
  (`IOutboxStore`, `ICacheProvider`, `IValidationProvider`, ROP `Either`).
- 75: a whole category (all 10 database providers, all 8 caches, all 10 transports) but not core.
- 50: one provider package or one satellite package used by several others.
- 25: a single package, no fan-out.
- 0: documentation-only or a single test file.

### Method acceleration

Effect on the agents' own tooling: hooks, the SPEC-003 audit pipeline, CI scripts, worker briefs.

- 100: a hook or pipeline defect that blocks or corrupts agent work today (e.g. a hook false
  positive stopping every worker, a broken hook test).
- 75: a missing or slow step in the audit/CI pipeline that agents work around by hand each time.
- 50: a tooling nicety that saves noticeable time but has a manual fallback.
- 25: a tooling change with a narrow, rare payoff.
- 0: unrelated to agent or CI tooling.

### Effort (only when the issue's own Effort Estimate checkbox is not ticked)

Inverse effort: higher score means less work.

- 100: a small, mechanical, single-file change (`Small`, under 1 hour).
- 60: a medium change touching a few files or one provider (`Medium`, 1-4 hours).
- 25: a large change spanning many files, providers, or requiring new tests across the matrix
  (`Large`, over 4 hours).
- 0: an open-ended investigation or a change with unclear scope.

## Output format

Reply with exactly one JSON object and nothing else — no prose, no Markdown code fence, no
trailing comment. Every "why" is one sentence, at most 140 characters. Always include all five
keys, even when the issue's own Effort Estimate box is already ticked (the caller decides whether
to use your effort score or the checkbox's).

```json
{
  "importance": { "score": 0, "why": "..." },
  "regulatory": { "score": 0, "why": "..." },
  "transversality": { "score": 0, "why": "..." },
  "method": { "score": 0, "why": "..." },
  "effort": { "score": 0, "why": "..." }
}
```

Output only that JSON object. Stop immediately after the closing brace.

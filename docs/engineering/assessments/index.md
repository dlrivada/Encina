---
title: "Assessments"
layout: default
nav_exclude: true
---

# Assessments

This page is for a maintainer or contributor who wants to know how Encina evaluates a whole concern (not one change) and where the results are kept. An assessment is a dated, evidence-backed review of one cross-cutting concern across the whole repository; this page lists them and links each one.

## What an assessment is

An assessment answers "how well does the repository follow its own rules for this concern, and why does it not?". It differs from an ADR (a decision) and from a closed-issue audit (one issue, see [SPEC-003](../../specifications/SPEC-003-closed-issue-knowledge-migration-and-quality-audit.md)): it looks at a concern across every package, at a point in time.

Every assessment page contains at least these sections, in this order (a page may add sections of its own, such as the library comparison in the observability assessment):

| Section | Content |
|---|---|
| Verdict | One paragraph, honest about what is good and what is not |
| What is good | Findings that hold, each with its evidence |
| Findings | What fails, ranked by severity, each with `file:line` evidence and the issue that tracks it |
| Root causes | Why the gap exists, from the start of the project and not only recent weeks |
| Proposal | Smallest effective step first; machine-enforced rules before design work |
| Not verified | What the assessor could not confirm, stated as such |

Rules for the figures in an assessment:

- Counts are measured with `Select-String` over `src/` and `tests/` on the date of the assessment. They are not coverage figures (coverage is cited, never typed; see [`AGENTS.md`](../../../AGENTS.md) section 9) and are approximate where the page says so.
- Each `file:line` was re-checked against the repository when the page was written; a reference that moved is marked "(moved)" with its current location.
- Every finding names the issue that tracks it, or says that none was opened.

## Assessments

| Date | Scope | Verdict | Page |
|---|---|---|---|
| 2026-10-05 | Observability: traces, metrics, logs, health checks, propagation | Good base (core dispatcher, structured logs, health checks); gaps at every asynchronous and external boundary | [Observability](2026-10-05-observability.md) |
| 2026-10-05 | Railway Oriented Programming and LanguageExt discipline | ROP is real and widely used (345 of 378 abstraction methods return `Either`); nothing mechanical stops regression, and concrete debt remains | [Railway Oriented Programming](2026-10-05-railway-oriented-programming.md) |

The decisions behind the rules these assessments measure against: [ADR-001](../../architecture/adr/001-railway-oriented-programming.md) and [ADR-006](../../architecture/adr/006-pure-rop-exception-handling.md) (ROP), [ADR-018](../../architecture/adr/018-cross-cutting-integration-principle.md) (cross-cutting integration) and [ADR-021](../../architecture/adr/021-eventid-uniqueness-enforcement.md) (EventIds).

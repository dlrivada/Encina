# Specifications

Specification-Driven Development artifacts for Encina (see `docs/engineering/AI-DEVELOPMENT-MODEL.md` §5–§6).

- Files are named `SPEC-NNN-<slug>.md`; the sequence is independent from ADRs (`docs/architecture/adr/`).
- A specification states **what** must be true (`REQ-*`), how we know (`AC-*`), constraints, non-goals, invariants and verification. It never prescribes the design; design choices become ADRs.
- Status values: `DRAFT` (agent-authored, decisions pending) → `APPROVED` (human decisions recorded) → `IMPLEMENTED` → `SUPERSEDED`.
- Agents do not edit an `APPROVED` specification directly; they open a PR against it.

| Spec | Title | Status |
|---|---|---|
| [SPEC-000](SPEC-000-encina-1.0-baseline-and-release-scope.md) | Encina 1.0 Baseline and Release Scope | 🟢 APPROVED (2026-09-21) |
| [SPEC-001](SPEC-001-coverage-docref-citations.md) | DocRef citations for coverage (#1092) | 🟢 APPROVED (2026-09-22) |
| [SPEC-002](SPEC-002-eu-regulatory-readiness.md) | EU regulatory readiness (refines SPEC-000 REQ-024) | APPROVED — pending SPEC-000 amendment (AC-040) |

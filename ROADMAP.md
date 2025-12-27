# Encina Roadmap

> **Build resilient .NET applications with Railway Oriented Programming.**

This document outlines the vision, current status, and future direction of Encina. For detailed task tracking, see [GitHub Issues](https://github.com/dlrivada/Encina/issues) and [Milestones](https://github.com/dlrivada/Encina/milestones).

---

## Current Status

**Version**: Pre-1.0 (breaking changes allowed)
**Packages**: 53 active (including CLI tool)
**Target**: .NET 10

| Category | Packages | Status |
|----------|----------|--------|
| Core & Validation | 5 | ✅ Production |
| Web Integration | 2 | ✅ Production |
| Serverless | 2 | ✅ Production |
| Database Providers | 12 | ✅ Production |
| Messaging Transports | 10 | ✅ Production |
| Caching | 8 | ✅ Production |
| Scheduling | 2 | ✅ Production |
| Resilience | 3 | ✅ Production |
| Event Sourcing | 1 | ✅ Production |
| Observability | 1 | ✅ Production |
| Developer Tooling | 1 | ✅ Production |

### Quality Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Test Count | 3,800+ | 5,000+ |
| Line Coverage | 67% | ≥85% |
| Mutation Score | 79.75% | ≥80% |
| Build Warnings | 0 | 0 |

---

## Design Principles

- **Functional First** — Pure ROP with `Either<EncinaError, T>` as first-class citizen
- **Explicit over Implicit** — Code should be clear and predictable
- **Performance Conscious** — Zero-allocation hot paths, Expression tree compilation
- **Composable** — Behaviors are small, composable units
- **Pay-for-What-You-Use** — All features are opt-in via satellite packages

---

## Development Phases

Progress is tracked via [GitHub Milestones](https://github.com/dlrivada/Encina/milestones).

### Phase 1: Stability
*Ensure green CI and all tests passing*

Focus: Fix failing tests, re-enable excluded test projects, verify all workflows.

→ [View Phase 1 Issues](https://github.com/dlrivada/Encina/milestone/1)

### Phase 2: Functionality
*Expand capabilities with new features*

Key areas:
- **Saga Enhancements** — Timeouts, low-ceremony syntax, not-found handlers
- **Developer Tooling** — `Encina.Testing` package with fluent assertions, ✅ `Encina.Cli` scaffolding tool (Issue #47)
- **Performance** — ✅ Delegate cache optimization (Issue #49), Source generators for NativeAOT (Issue #50), Switch-based dispatch (Issue #51)
- **Enterprise Patterns** — ✅ Recoverability pipeline (Issue #39), ✅ Rate limiting (Issue #40), ✅ Dead Letter Queue (Issue #42), ✅ Bulkhead Isolation (Issue #53), ✅ Routing Slip (Issue #62), ✅ Scatter-Gather (Issue #63), ✅ Content-Based Router (Issue #64)
- **Cross-cutting** — ✅ Health checks (Issue #35), ✅ Projections/read models (Issue #36), ✅ Snapshotting (Issue #52), ✅ Event versioning (Issue #37), ✅ Distributed Lock (Issue #55)
- **Modular Monolith** — ✅ `IModule` interface, ✅ Module registry & lifecycle hooks, ✅ Module-scoped behaviors (Issue #58)
- **Serverless** — ✅ Azure Functions (Issue #59), ✅ AWS Lambda (Issue #60), ✅ Durable Functions (Issue #61)

→ [View Phase 2 Issues](https://github.com/dlrivada/Encina/milestone/2)

### Phase 3: Testing & Quality
*Achieve reliability through comprehensive testing*

Focus: Increase coverage to ≥85%, complete Testcontainers fixtures, eliminate flaky tests.

→ [View Phase 3 Issues](https://github.com/dlrivada/Encina/milestone/3)

### Phase 4: Code Quality
*Static analysis and maintainability*

Focus: SonarCloud integration, eliminate code duplication, address complexity hotspots.

→ [View Phase 4 Issues](https://github.com/dlrivada/Encina/milestone/4)

### Phase 5: Documentation
*User-facing content and examples*

Key deliverables:
- Documentation site (DocFX + GitHub Pages)
- Quickstart guide (5 minutes to first request)
- Provider guides (all 39 packages documented)
- MediatR migration guide
- Runnable example projects

→ [View Phase 5 Issues](https://github.com/dlrivada/Encina/milestone/5)

### Phase 6: Release Preparation
*Security, publishing, and branding for 1.0*

Key deliverables:
- SLSA Level 2 compliance
- NuGet package publishing workflow
- Visual identity (logo, icons)
- Final documentation review

→ [View Phase 6 Issues](https://github.com/dlrivada/Encina/milestone/6)

---

## Not Implementing

| Feature | Reason |
|---------|--------|
| Generic Variance | Conflicts with "explicit over implicit" principle |
| EncinaResult wrapper | `Either<L,R>` from LanguageExt is sufficient |
| API Versioning Helpers (Issue #54) | `Asp.Versioning` already provides complete HTTP-level versioning; handler-level versioning is redundant |

## Deferred to Post-1.0

| Feature | Reason |
|---------|--------|
| ODBC Provider (Issue #56) | Valuable for legacy databases but not critical for core release; evaluate based on community demand |

### Deprecated Packages

The following packages were deprecated and removed:

| Package | Reason |
|---------|--------|
| Encina.Wolverine | Overlapping concerns with Encina's messaging |
| Encina.NServiceBus | Enterprise licensing conflicts |
| Encina.MassTransit | Overlapping concerns with Encina's messaging |
| Encina.Dapr | Infrastructure concerns belong at platform level |
| Encina.EventStoreDB | Marten provides better .NET integration |

> Deprecated code preserved in `.backup/deprecated-packages/`

---

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

**Pre-1.0 Policy**: Any feature can be added, modified, or removed without backward compatibility concerns.

**Post-1.0 Policy**: Breaking changes only in major versions following [Semantic Versioning](https://semver.org/).

---

## References

### Inspiration
- [LanguageExt](https://github.com/louthy/language-ext) — Functional programming for C#
- [MediatR](https://github.com/jbogard/MediatR) — Simple mediator implementation
- [Wolverine](https://wolverine.netlify.app/) — Next-gen .NET messaging

### Concepts
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) — Scott Wlaschin
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html) — Martin Fowler

---

## Links

- **Issues**: [github.com/dlrivada/Encina/issues](https://github.com/dlrivada/Encina/issues)
- **Milestones**: [github.com/dlrivada/Encina/milestones](https://github.com/dlrivada/Encina/milestones)
- **Changelog**: [CHANGELOG.md](CHANGELOG.md)
- **History**: [docs/history/](docs/history/)

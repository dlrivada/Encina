# Contributing to Encina

Thank you for your interest in contributing to Encina. This is a public repository — anyone can open issues, submit pull requests (via fork), and participate in discussions. This guide covers all ways to get involved.

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you agree to uphold its standards. Please report unacceptable behavior through the channels described there.

## How to Contribute

### Reporting Bugs

Before filing a bug report, search [existing issues](https://github.com/dlrivada/Encina/issues?q=is%3Aissue+label%3Abug) to avoid duplicates.

If you find a new bug, [open an issue](https://github.com/dlrivada/Encina/issues/new?template=bug_report.md) using the **Bug Report** template. A good bug report includes:

- A clear, descriptive title with the `[BUG]` prefix
- Steps to reproduce the behavior
- Expected vs. actual behavior
- .NET version, OS, and relevant package versions
- A minimal reproduction project or code snippet (when possible)

### Reporting Security Vulnerabilities

**Do not report security vulnerabilities through public GitHub issues.**

Use [GitHub Private Vulnerability Reporting](https://github.com/dlrivada/Encina/security/advisories/new) instead. See [SECURITY.md](SECURITY.md) for full details on our responsible disclosure process and what to include.

### Requesting Features

For new features or enhancements, [open an issue](https://github.com/dlrivada/Encina/issues/new?template=feature_request.md) using the **Feature Request** template. The template includes sections for:

- Motivation and use cases
- Proposed API design
- Provider implementation matrix (for database/caching/transport features)
- Cross-cutting integration evaluation (12 transversal functions)

**For substantial features**, consider opening a discussion or a `[SPIKE]` issue first to align on the design before investing in implementation.

### Other Issue Types

We have specialized templates for different types of contributions:

| Template | Prefix | When to Use |
|----------|--------|-------------|
| [Bug Report](https://github.com/dlrivada/Encina/issues/new?template=bug_report.md) | `[BUG]` | Something is broken or behaves unexpectedly |
| [Feature Request](https://github.com/dlrivada/Encina/issues/new?template=feature_request.md) | `[FEATURE]` | New functionality or enhancement |
| [Technical Debt](https://github.com/dlrivada/Encina/issues/new?template=technical_debt.md) | `[DEBT]` | Code quality, optimization, missing implementations |
| [Test Implementation](https://github.com/dlrivada/Encina/issues/new?template=test_implementation.md) | `[TEST]` | New tests, coverage gaps, benchmarks |
| [Architecture Spike](https://github.com/dlrivada/Encina/issues/new?template=architecture_spike.md) | `[SPIKE]` | Technical investigations and evaluations |
| [Refactoring](https://github.com/dlrivada/Encina/issues/new?template=refactoring.md) | `[REFACTOR]` | Code restructuring without behavior change |
| [Infrastructure](https://github.com/dlrivada/Encina/issues/new?template=infrastructure.md) | `[INFRA]` | CI/CD, Docker, build system, tooling |

### Questions and Discussions

For questions, ideas, or general discussion, use [GitHub Discussions](https://github.com/dlrivada/Encina/discussions). Issues should be reserved for actionable work items.

Good discussion topics:

- Design questions before starting a feature
- Architecture feedback or alternative approaches
- Integration patterns and best practices
- Sharing how you use Encina in your projects

### Finding Something to Work On

Look for issues labeled [`good first issue`](https://github.com/dlrivada/Encina/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22) or [`help wanted`](https://github.com/dlrivada/Encina/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22) if you're looking for a place to start.

## Submitting Pull Requests

### Before You Start

1. **Check for existing work** — search open PRs and issues to avoid duplication.
2. **Comment on the issue** — if you plan to work on an existing issue, leave a comment so others know.
3. **Design first for large changes** — open a `[SPIKE]` issue or Discussion before investing significant effort in new features or architectural changes.

### Prerequisites

Before opening a PR, make sure:

- [ ] Code formatting passes: `dotnet format Encina.slnx --verify-no-changes`
- [ ] All tests pass: `dotnet test Encina.slnx --configuration Release`
- [ ] Zero build warnings (`TreatWarningsAsErrors` is active)
- [ ] New/changed behavior has tests

### PR Guidelines

- Follow [Conventional Commits](https://www.conventionalcommits.org/) in the PR title: `type(scope?): subject`
- Keep the Railway Oriented Programming contract: errors travel as `Either<EncinaError, T>`, not exceptions
- If adding pipeline behaviors, return `ValueTask<Either<EncinaError, T>>` — avoid `throw` except for cancellations
- **Cross-cutting integration**: if you add a new feature, evaluate it against the 12 transversal functions (Caching, OpenTelemetry, Logging, Health Checks, Validation, Resilience, Distributed Locks, Transactions, Idempotency, Multi-Tenancy, Module Isolation, Audit Trail)
- Update documentation and badges when your changes affect capabilities, coverage, or quality

### PR Checklist

- [ ] Formatting and analyzers pass locally
- [ ] Tests pass in Release configuration
- [ ] Coverage does not drop below the CI threshold (90% lines)
- [ ] Pipeline behaviors follow the functional rail — no operational exceptions
- [ ] Cross-cutting integration evaluated (see above)
- [ ] Documentation updated if applicable

### CI Pipeline

Your PR will be validated by:

| Workflow | What It Checks |
|----------|----------------|
| `ci.yml` | Formatting, analyzers (`-warnaserror`), tests with coverage, 90% gate |
| `conventional-commits.yml` | PR title follows Conventional Commits |
| `codeql.yml` | Security analysis |
| `sbom.yml` | Supply chain verification |
| `benchmarks.yml` | Performance regression detection |

All checks must pass before merge.

## Style Guide

- Use `ValueTask` in the critical path to minimize allocations
- Prefer `EncinaError` / `Either` over exceptions for expected flows
- Document error codes and maintain consistency with `EncinaErrorCodes`
- Use file-scoped namespaces and guard clauses
- Add XML doc comments on all public APIs
- When adding tests, follow the conventions in the existing test projects (AAA pattern, descriptive names, one assertion per test)

## Recognition

We value every contribution. Contributors who have had work merged into Encina are eligible to be featured in the [**From Our Contributors**](README.md#from-our-contributors) section of the project README, where we showcase up to 2 of their independent projects. It's our way of giving back visibility to the people who help make Encina better.

If you'd like to be featured after your contribution is merged, just let us know which projects you'd like to highlight.

## License

By contributing to Encina, you agree that your contributions will be licensed under the [MIT License](LICENSE).

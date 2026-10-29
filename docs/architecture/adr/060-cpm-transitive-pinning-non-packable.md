# ADR-060: Central Package Management Transitive Pinning Only in Non-Packable Projects (Accepted)

## Status

**Accepted** - 2026-10-28, by the maintainer (decision 1051-D7 = C', [comment on #1051](https://github.com/dlrivada/Encina/issues/1051#issuecomment-6470576758)). Spike: [#1051](https://github.com/dlrivada/Encina/issues/1051). Implementation: [#2976](https://github.com/dlrivada/Encina/issues/2976), open when this ADR was written; the mechanism below is the decided one, not yet merged.

[AGENTS.md](../../../AGENTS.md) section 8 requires an ADR for architectural decisions; this repository-wide packaging policy is one.

## Context

Encina uses Central Package Management: `ManagePackageVersionsCentrally` is `true` in `Directory.Packages.props` and every version is declared there once. CPM governs direct references only. A transitive package takes the version its parent asks for, so the central file cannot raise a transitive package that has a security advisory, and the audit gate fails (SPEC-000 REQ-005). The property `CentralPackageTransitivePinningEnabled` makes the central version win for transitive packages too; its failure mode is NU1109, raised when a transitive package needs a version higher than the central pin.

Today the repository fixes advisories by hand: a central `PackageVersion` plus a direct `PackageReference` in every project that pulls the package in (`System.Security.Cryptography.Pkcs`, [#2299](https://github.com/dlrivada/Encina/issues/2299); `MessagePack`, [#2338](https://github.com/dlrivada/Encina/issues/2338)).

The [spike report](../../infrastructure/cpm-transitive-pinning.md) measured that enabling the flag globally leaks the pinned transitive versions into the nuspec of packable projects: the `Encina.Testing.WireMock` nuspec grows from 8 to 23 dependencies. Consumers would inherit versions imposed by Encina, against pay-for-what-you-use (AGENTS.md section 1). Most conflicts and pins sit in test and benchmark projects, which are not packed.

## Decision

`CentralPackageTransitivePinningEnabled` is `true` exactly in projects whose final `IsPackable` is `false`.

- The property is set in a new root `Directory.Build.targets` under `Condition="'$(IsPackable)' == 'false'"`. A `.targets` file is evaluated after the project file, so the condition sees the final value, including a `.csproj` override. `Directory.Build.props` derives the defaults from the path (`tests/` false at lines 48-50, `src/` true at lines 58-60) and lets a project override them, so a condition in the props file could not see an override. An unset `IsPackable` leaves the flag off, which fails safe.
- MSBuild uses only the nearest `Directory.Build.targets`, and `tests/Directory.Build.targets` already exists, so it imports the root file; there is one definition.
- Packable projects keep the flag off, so no pinned transitive version reaches the dependencies of a published package. A packable project that needs a transitive version raised keeps the direct-pin pattern.
- Conflicts are fixed by raising the central pin in `Directory.Packages.props`, never by keeping a lower version (AGENTS.md section 1) and never with `NoWarn` (AGENTS.md section 8). A central entry nothing references is deleted (AGENTS.md section 3). Audit findings are never suppressed.

[#2976](https://github.com/dlrivada/Encina/issues/2976) applies the decision: the conditional flag, the version bumps the spike measured, the removal of the unreferenced `Grpc.Tools` pin and of the `PrivateAssets="all"` OTLP reference in `Encina.Testing.WireMock`, and a nuspec comparison of the packable projects.

## Alternatives considered

- **A, flag on globally**: rejected for the nuspec leak above.
- **B / D, flag off, direct-pin pattern as the practice**: rejected because every advisory then needs hand-made references in several projects.
- **NuGet lock files**: do not pin versions centrally and retire no pin, so they do not answer the question.

## Consequences

- One place pins transitive versions for test and benchmark projects; the matrix of the spike is a lower bound, so #2976 may find more conflicts to bump.
- Advisories reaching packable `src/` projects still need the direct-pin pattern.
- The Verify bump spans three major versions, so its snapshots are reviewed in #2976.
- Whether restore honours the property per project is confirmed by #2976; if it does not, the work stops and is reported, with no fallback to a global flag.

## Related

- [CPM transitive pinning spike report](../../infrastructure/cpm-transitive-pinning.md)
- [SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) (REQ-005)
- [ADR-056](056-mutation-testing-scope.md), precedent of a policy ADR
- Issues: [#1051](https://github.com/dlrivada/Encina/issues/1051), [#2976](https://github.com/dlrivada/Encina/issues/2976), [#2299](https://github.com/dlrivada/Encina/issues/2299), [#2338](https://github.com/dlrivada/Encina/issues/2338), [#1043](https://github.com/dlrivada/Encina/issues/1043) (owns the `Encina.OpenTelemetry` OTLP reference)

## Date

2026-10-28

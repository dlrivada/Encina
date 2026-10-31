# CPM transitive pinning: measured state and options (spike #1051)

This page is for the maintainer who has to choose how Encina pins transitive NuGet packages, and for any contributor who needs to know why the repository has hand-made pin references. It records what was measured on `origin/main` on 2026-10-28, corrects the claims of issue #1051 that no longer hold, and records the one decision with its options and the maintainer's choice (C', 2026-10-28). The spike changes no build file; #2976 applies the decision. The decision is recorded in [ADR-060](../architecture/adr/060-cpm-transitive-pinning-non-packable.md).

## The problem from the basics

Encina uses Central Package Management (CPM). Every package version lives once in `Directory.Packages.props`, and `ManagePackageVersionsCentrally` is `true` (line 3). Projects write `<PackageReference Include="X" />` without a version.

CPM governs direct references only. A package that arrives transitively (project A references B, B depends on C) takes the version B asks for, whatever `Directory.Packages.props` says about C. That is a problem when C has a security advisory: the central file cannot raise C, and the solution fails the audit gate (SPEC-000 [REQ-005](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md): zero audit findings of moderate or higher severity, promoted to an error by `TreatWarningsAsErrors`, `Directory.Build.props:24-25`).

The property `CentralPackageTransitivePinningEnabled` makes the central version win for transitive packages too. Its failure mode is NU1109: if a transitive package needs a version higher than the central one, restore fails because pinning would downgrade it.

A concrete story, from the repository itself. Advisories on `System.Security.Cryptography.Pkcs` (#2299) and `MessagePack` (#2338) each blocked every pull request. Without the flag, the fix was a central `PackageVersion` plus a direct `PackageReference` in each project that pulls the package in (`Directory.Packages.props:181-182` and `:183-184`, each a comment naming the issue followed by the `PackageVersion` line). With the flag, the same fix would be the one `PackageVersion` line.

Today the repository has two pin patterns, not one:

| Pattern | Where | Count |
| --- | --- | --- |
| Direct reference with `PrivateAssets="all"`, only to fix a version | `OpenTelemetry.Exporter.OpenTelemetryProtocol` in `src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj:24` and `src/Encina.Testing.WireMock/Encina.Testing.WireMock.csproj:30` | 2 references |
| Central `PackageVersion` plus a direct `PackageReference` without `PrivateAssets` | `System.Security.Cryptography.Pkcs` (6 `src/` projects), `MessagePack` (`src/Encina.Aspire.Testing`, `tests/Encina.NBomber`), `Humanizer` (4 test projects) | 12 projects (inventory of 2026-10-28, from the decision register of #1051) |

The other `PrivateAssets="all"` references in `src/` are not pins: the public API analyzer package, `JetBrains.Annotations` in `Encina.Security.ABAC`, and the Roslyn references of `Encina.Security.ABAC.Analyzers`.

PR [#2302](https://github.com/dlrivada/Encina/pull/2302) recorded on 2026-10-15 that turning the flag on broke restore with NU1107, NU1109 and NU1608 conflicts (also in `docs/knowledge/issues/2299.md` and `docs/knowledge/issues/2338.md`). That was an observation on that day, not a prohibition. This spike repeats it and measures the size of the problem.

## What was measured

Method: from the worktree root, `dotnet restore Encina.slnx --force-evaluate -p:CentralPackageTransitivePinningEnabled=true`. Exit code 1. The property is not set anywhere in the tree, so the command line was the only switch. Only NU1109 appeared (126 lines); there was no NU1605, NU1608 or NU1107. The raw log (`restore-pinned.log`) and the nuspec files below exist only in the spike's working folder, which is git-ignored; this page carries the results.

### Conflict matrix

Every row is "central pin lower than a transitive requirement". Line numbers refer to `Directory.Packages.props`. The effort column is the spike's estimate, not a measurement: every fix is a one-line version bump, so the effort is the verification it needs (XS: restore and build; S: also check the dependent package's behaviour; M: also review changed test output).

| Package | Central version (line) | Required | Chain | Effort (estimate) |
| --- | --- | --- | --- | --- |
| Grpc.AspNetCore | 2.76.0 (111) | 2.80.0 | Aspire.Hosting.Testing 13.5.4 -> Grpc.AspNetCore | S, with the other two Grpc rows: check `Encina.gRPC` against 2.80.0 |
| Grpc.Net.Client | 2.76.0 (110) | 2.80.0 | Aspire.Hosting.Testing -> Grpc.Net.ClientFactory 2.80.0 -> Grpc.Net.Client | S (same check) |
| Grpc.Tools | 2.71.0 (113) | 2.80.0 via Aspire, 2.76.0 via Grpc.AspNetCore 2.76.0 | Aspire.Hosting.Testing -> Grpc.Tools | XS (bump or delete, see below) |
| Microsoft.Extensions.Http.Resilience | 10.5.0 (78) | 10.8.0 | Aspire.Hosting.Testing -> Http.Resilience | S: check the `Encina.Extensions.Resilience` tests |
| HotChocolate | 15.1.11 (115) | 15.1.14 | HotChocolate.AspNetCore 15.1.14 (line 116) -> HotChocolate | XS (patch release) |
| TngTech.ArchUnitNET | 0.13.2 (166) | 0.13.3 | TngTech.ArchUnitNET.xUnitV3 0.13.3 (line 167) -> ArchUnitNET | XS (patch release) |
| Verify | 28.16.0 (159) | 31.15.0 | Verify.XunitV3 31.15.0 (line 164) -> Verify | M: three major versions, snapshot review |
| Spectre.Console | 0.55.0 (155) | 0.57.2 | NBomber 6.6.0 -> Serilog.Sinks.SpectreConsole 0.4.0 -> Spectre.Console | S: check `src/` uses of Spectre.Console |
| System.CommandLine | 2.0.6 (154) | 2.0.7 | NBomber 6.6.0 -> System.CommandLine | S: check `src/` uses of System.CommandLine |

Projects reporting each conflict:

| Package | Reported by |
| --- | --- |
| Grpc.AspNetCore, Grpc.Net.Client, Microsoft.Extensions.Http.Resilience | Aspire.Testing, AspNetCore.Benchmarks, GuardTests, PropertyTests, UnitTests |
| Grpc.Tools | The five above plus gRPC |
| HotChocolate | GraphQL, AspNetCore.Benchmarks, GuardTests, UnitTests |
| TngTech.ArchUnitNET | Testing, Testing.Architecture, Testing.Examples, TestInfrastructure, Benchmarks, AspNetCore.Benchmarks, ContractTests, GuardTests, IntegrationTests, LoadTests, NBomber, PropertyTests, UnitTests |
| Verify | AspNetCore.Benchmarks, UnitTests |
| Spectre.Console, System.CommandLine | NBomber |

Two limits apply to this matrix:

- It is a lower bound. A project that fails restore stops the graph evaluation, so fixing these nine may reveal more conflicts. No rebuild after any bump was run.
- Most reporting projects are tests or benchmarks. Of the `src/` projects, only the gRPC and GraphQL packages (and the Testing family for ArchUnitNET) appear.

The usual fix for each row is NuGet's own hint: raise the central pin to the required version. Grpc.AspNetCore, Grpc.Net.Client and Grpc.Tools would move to 2.80.0 together; Aspire already resolves 2.80.0 at runtime, but compatibility of Grpc.AspNetCore 2.80.0 with `Encina.gRPC` was not checked. The Verify bump is three major versions (28 to 31), so snapshot tests may change. Whether `src/` code uses a changed `System.CommandLine` API was not checked.

Roslyn is not involved: no conflict involves an analyzer, so the Roslyn 4.14 concern of the issue did not arise.

### Grpc.Tools is unreferenced

`Grpc.Tools` appears in exactly one place, `Directory.Packages.props:113`. No `.csproj`, `.props` or `.targets` file references it, and the repository has no `.proto` file or `Protobuf` item, so nothing generates code with it. With the flag off the central entry has no effect at all; with the flag on it would pin the version that Aspire and Grpc.AspNetCore bring in transitively.

### Pinned transitives leak into the nuspec

`Encina.Testing.WireMock` was packed in Release with the flag off and on (both exit code 0, and pack restore did not fail with NU1109). The nuspec lists 8 dependencies with the flag off and 23 with it on. The 15 added dependencies, all with `exclude="Build,Analyzers"`, are:

- Humanizer 3.0.10
- Microsoft.CodeAnalysis.CSharp 5.0.0
- Microsoft.Extensions.Configuration, .Configuration.Abstractions, .Configuration.Binder, .DependencyInjection.Abstractions, .Diagnostics, .Hosting.Abstractions, .Http, .Logging, .Logging.Abstractions and .Options, all 10.0.12
- Newtonsoft.Json 13.0.4
- OpenTelemetry.Api 1.15.3
- Testcontainers 4.15.0

So with the flag on globally, every consumer of a packable Encina package would inherit the central versions as direct dependency floors.

## Claims of issue #1051 that no longer hold

| Claim in the issue | State on 2026-10-28 |
| --- | --- |
| `Microsoft.CodeAnalysis.Common` is pinned at 4.14.0 and conflicts with Marten/JasperFx 5.0.0 (NU1605) | There is no `Microsoft.CodeAnalysis.Common` pin. The Roslyn pins are `CSharp.Scripting`, `CSharp` and `CSharp.Workspaces` at 5.0.0 (lines 194 and 196-197), Marten is 9.38.0 (line 53). No NU1605 appeared. |
| Aspire 13.2.2 pulls Grpc.Tools 2.78.0 | Aspire is 13.5.4 (lines 151-152) and requires Grpc.Tools 2.80.0 or higher. |
| `Encina.Testing.WireMock`'s reference is the only workaround next to `Encina.OpenTelemetry`'s | A second, older pattern exists (central `PackageVersion` plus direct reference, table above). |
| Deliverable: update the "Build Environment Known Issues" section of `CLAUDE.md` | The section does not exist. It is obsolete: [AGENTS.md](../../AGENTS.md) section 11 forbids tracking known problems in `AGENTS.md` and `CLAUDE.md`. |

## Points settled by rule

These are not open; each follows from a written rule.

| Point | Settled as | Rule |
| --- | --- | --- |
| Conflicting central pins | Bump the pin to the required version; never keep a lower one for compatibility | AGENTS.md section 1: pre-1.0, best solution first |
| The unreferenced `Grpc.Tools` pin (`Directory.Packages.props:113`) | Remove it. With the flag off it has no effect; with the flag on, a package with no central `PackageVersion` is not pinned at all, so deleting the entry also removes its conflict | AGENTS.md section 3: every line serves a current purpose |
| `NoWarn` for NU codes | Not used; conflicts are fixed by a pin or bump. No `NoWarn` with an NU code exists in the tree, and NU1109 is an error that `WarningsAsErrors` already promotes | AGENTS.md section 8: zero warnings |
| Audit findings | Never suppressed or lowered | SPEC-000 REQ-005 and DEC-004 |
| `Encina.OpenTelemetry`'s OTLP reference | Stays as is; the edit belongs to #1043, which is open | [#1043 plan](../plans/otlp-exporter-implementation-plan-1043.md) |
| `Encina.Testing.WireMock`'s OTLP reference | Keeps `PrivateAssets="all"` until #2976 removes it, as the decision below directs | #1043 plan; [decision comment](https://github.com/dlrivada/Encina/issues/1051#issuecomment-6470576758) |
| CLAUDE.md deliverable | Dropped | AGENTS.md section 11 |
| Scope of the spike | It measures and recommends; applying the chosen option is a separate `[INFRA]` issue (#2976) | Issue #1051, Out of Scope |

## The decision

Question: which way should Encina pin transitive packages?

**Decided by the maintainer on 2026-10-28: C'** ([decision comment on #1051](https://github.com/dlrivada/Encina/issues/1051#issuecomment-6470576758)). The flag is on only in non-packable projects (`IsPackable` false: test and tooling projects), so pinned transitive versions never reach the dependencies of published Encina packages (AGENTS.md section 1, pay-for-what-you-use). The options as they were presented follow.

The labels follow the issue's options. A is the issue's Option A unchanged. C' narrows the issue's Option C, which turned the flag on for the two projects with OTLP references: C' turns it on for every project with `IsPackable` false, a rule chosen because of the nuspec leak measured above. B / D is the issue's Option B, keeping the flag off, extended to cover the second pin pattern (D) that the issue did not know about.

| Option | What it is | Wins when |
| --- | --- | --- |
| A | Flag on globally in `Directory.Packages.props` | The conflict list is short and bump-only, and a single place to patch advisories matters more than what consumers receive. Loses on the nuspec leak: consumers get the pinned transitives as direct dependencies. |
| C' | Flag on only where `IsPackable` is false (tests, benchmarks) | Most conflicts and pins are in non-packable projects, as measured, and Encina's consumers must get no imposed versions. Loses where a packable `src/` project still needs a pin: it keeps the direct-reference pattern. |
| B / D | Flag off; keep the central `PackageVersion` plus direct `PackageReference` pattern of #2299 and #2338 as the documented practice (the two OTLP references remain the `PrivateAssets="all"` exception) | The cost of making any flagged build clean exceeds the cost of the next incidents, or the maintainer wants no new global switch. Loses on recurrence: every advisory needs hand-made references in several projects. |

Also considered: NuGet lock files give reproducible resolution but do not pin versions centrally and retire no pin, so they do not answer the question.

The spike's recommendation, which the maintainer followed, was C'. The conflicts and the existing pins sit mostly in test projects, and C' avoids the nuspec leak, so Encina's consumers get no imposed versions. Caveats to weigh: the matrix is a lower bound, the Verify bump is three majors and may change snapshots, and the issue calls per-project use of the property "poorly documented"; whether restore honours a conditional value per project must be confirmed by the implementing issue (it was not tested here).

## Follow-up work

- [#2976](https://github.com/dlrivada/Encina/issues/2976) `[INFRA]` applies C': it confirms that restore honours the conditional flag, makes the bumps above, deletes the `Grpc.Tools` pin, removes the `PrivateAssets="all"` OTLP reference of `Encina.Testing.WireMock`, rebuilds to find the conflicts hidden by failed restores, and reviews the snapshots after the Verify bump.
- The OTLP reference of `Encina.OpenTelemetry` stays with #1043.

## Related issues

- [#1051](https://github.com/dlrivada/Encina/issues/1051): this spike.
- [#2976](https://github.com/dlrivada/Encina/issues/2976): applies the decision.
- [PR #1041](https://github.com/dlrivada/Encina/pull/1041): related merged pull request, read for this spike.
- [#1043](https://github.com/dlrivada/Encina/issues/1043): OTLP exporter; owns the `Encina.OpenTelemetry` reference.
- [#2299](https://github.com/dlrivada/Encina/issues/2299), [#2338](https://github.com/dlrivada/Encina/issues/2338): the later advisory incidents and the direct-pin pattern; PR [#2302](https://github.com/dlrivada/Encina/pull/2302).
- [#888](https://github.com/dlrivada/Encina/issues/888): related open epic.

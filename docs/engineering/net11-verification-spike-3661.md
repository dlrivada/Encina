---
nav_exclude: true
---

# Verifying Encina on .NET 11 and C# 15: go/no-go report (spike #3661)

This page is the go/no-go report of [#3661](https://github.com/dlrivada/Encina/issues/3661) for the maintainer and for the authors of the platform-move issues. It records what was measured when Encina was built and tested on the .NET 11 SDK, what breaks, what it costs to fix, and the conditions under which the move to `net11.0` and C# 15 can proceed. The platform was decided by the maintainer on 2026-11-11 (option B of the issue); this report is the verification that decision was made subject to. Nothing in `src/` was changed.

**Evidence date:** 2026-11-11, SDK 11.0.100, against `origin/main`. The raw logs and working notes were kept locally and are not part of the repository. Pre-1.0, backward compatibility is not a criterion ([AGENTS.md](../../AGENTS.md) section 1, [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md)).

## 1. Verdict: GO, with conditions

| Item | Condition |
| --- | --- |
| a | Pin the SDK now with a `global.json` (SDK 10.0.x, `rollForward` `latestFeature`) on `main`, independent of the move. SDK 11 alone already breaks the unmodified `net10.0` build (section 2), and CI will break the same way once runner images ship SDK 11, because without a `global.json` the newest installed SDK is picked even though the workflows request `10.0.x`. When the move lands, the pin becomes 11.0.x. |
| b | The platform move fixes the 244 diagnostics and the 4 unit-test failures listed in sections 3 and 4. None is a design obstacle. |
| c | The Stryker C# 15 parse check is still open (question 3). It must be re-run in the first platform PR, once the strict net11 build is clean. |
| d | MySQL EF Core stays skipped as today (question 4). Oracle's `MySql.EntityFrameworkCore` 10.0.12 supports EF Core 10 and is the candidate provider. |
| e | EF Core stays on 10 until Npgsql EF 11 is GA. |
| f | The root cause of the DataProtection empty-payload unit failure is unconfirmed and must be confirmed during the move. |

## 2. Step 1: SDK 11 on the unmodified net10.0 base

Command, on the unmodified base with only the SDK changed:

```powershell
dotnet build Encina.slnx -c Release
```

The build fails. `AnalysisLevel` is `latest`, so the SDK 11 analyzer rules are enabled, and `TreatWarningsAsErrors` turns them into errors. For the inventory the build was repeated with `-p:TreatWarningsAsErrors=false -p:WarningsAsErrors=`. There are 20 diagnostics:

| Where | Id | File and line |
| --- | --- | --- |
| src (error) | CA1877 | `src/Encina.Cli/Services/CodeGenerator.cs:245` |
| src (error) | CA2026 | `src/Encina.Cdc.Debezium/DebeziumHttpListener.cs:177` |
| src (error) | CA1830 | `src/Encina.Security.PII/Strategies/DateOfBirthMaskingStrategy.cs:83` |
| tests, hidden behind the src failures | CA2026 x12, CA2027 x5 | `Encina.UnitTests`, `Encina.IntegrationTests` |

Mitigation that does not need the move: a `global.json` pinning SDK 10.0.x with `rollForward` `latestFeature`, or `AnalysisLevel` 10.0. The pin is recommended (condition a) because it also protects CI.

## 3. The eight questions of the issue

### Q1. Build with zero warnings on net11.0 and LangVersion 15

With `net11.0` and `LangVersion` 15.0 there are 244 unique diagnostics. 30 `csproj` files hard-coded `net10.0` and had to be changed besides `Directory.Build.props` (11 of them also set `LangVersion` to `latest`).

| Id | Count | Where | Cause and fix |
| --- | --- | --- | --- |
| NU1510 | 207 (84 projects) | `PackageReference` to `Microsoft.Extensions.Logging.Abstractions` (74), `DependencyInjection.Abstractions` (56), `Options` (48), `Hosting.Abstractions` (27), `Caching.Abstractions` (1), `Configuration.Abstractions` (1) | These packages are now in the shared framework; remove the reference. A strict build fails at restore. |
| SYSLIB0066 | 10 | tests only | `IStartupValidator` is obsolete; there is no use in `src/`. |
| CA1307 | 6 | `src/Encina.ADO.PostgreSQL/BulkOperations/BulkOperationsPostgreSQL.cs:642-643`; `src/Encina.MongoDB/Sharding/ShardedMongoCollectionFactory.cs:211,218` and `src/Encina.MongoDB/Sharding/ShardedReadWriteMongoCollectionFactory.cs:285,292` | Pass an explicit `StringComparison`. |
| CS0419 | 1 | `src/Encina.Security.AntiTampering/HMAC/HMACSigner.cs:22` | A new `FixedTimeEquals` overload makes the `cref` ambiguous; qualify the `cref`. |
| CA2026 | 13 | 1 in src, 12 in tests | New SDK 11 rule. |
| CA2027 | 5 | tests | New SDK 11 rule. |
| CA1877 | 1 | src | New SDK 11 rule. |
| CA1830 | 1 | src | New SDK 11 rule. |

The 30 hard-coded `net10.0` project files must be touched by the move. Recommendation: remove the per-project `TargetFramework` so `Directory.Build.props` governs the whole solution.

Analyzers on C# 15 syntax: Meziantou, SonarAnalyzer.CSharp and Roslynator produced zero diagnostics on a union, a closed record and a `with(...)` collection expression, both in a scratch project (`artifacts/spike3661/probe`) and on internal union and closed types inside `src/Encina` (strict build, NU1510 suppressed). One language note found on the way: `closed` together with `abstract` is CS9384, because a `closed` class is implicitly abstract.

### Q2. Tests on net11.0

| Project | Result |
| --- | --- |
| Unit | 27293 passed, 4 failed, 1 skipped |
| Guard | 9105 passed |
| Contract | 2366 passed, 1 skipped |
| Property | 1448 passed, 1 skipped |
| Integration, EF Core SQL Server | 209 passed |
| Integration, EF Core PostgreSQL | 209 passed |
| Integration, EF Core MySQL | 30 passed, 140 skipped (pre-existing, see Q4) |
| Integration, other EF Core | 98 passed |
| Integration, Marten | 167 passed |
| Integration, MongoDB | 474 passed |
| Integration, ADO.NET and Dapper on the three databases | 99 passed |

The integration runs used real databases through Docker, with 0 failures. The four unit failures (diagnosed by `ci-diagnoser`):

| Test | Cause | Fix | Effort |
| --- | --- | --- | --- |
| `RecoverabilityOptionsCompositionTests` (2 tests) | DI 11 detects a circular dependency for `IErrorClassifier` (a `Configure` action injecting the classifier). The test assumed the older behaviour. | Change the test composition. | S |
| `DataProtectionMessageEncryptionProviderTests.EncryptThenDecrypt_EmptyPayload` | Decrypt returns `Left`. Cause unconfirmed: the diagnostic probe was blocked by the `guard-orchestrator-writes` hook for `ci-diagnoser`. | Confirm the cause first, then fix. | S once confirmed |
| `AddEncinaServiceGraphTests` (`IAsyncStartupValidator`) | The framework now registers `IAsyncStartupValidator`. | Add it to `NamedServiceTypes` (line 33). | S |

### Q3. Stryker 5.x on the new target

Stryker 5.0.0 (the tool manifest version; 5.1.0 is the latest) was run from `Encina.GuardTests` with a probe-only `mutate` set. It reached the build step, but the strict net11 build fails (CA1307 and the other diagnostics of Q1), and Stryker needs a zero-warning build first. **The C# 15 parse check is therefore still open.** Re-run it in the first platform PR after the build is clean (condition c).

### Q4. EF Core 10 providers on net11.0

| Package | Version | Target framework | EF Core dependency |
| --- | --- | --- | --- |
| Npgsql EF Core | 10.0.3 | net10.0 | [10.0.4, 11.0.0) |
| Npgsql EF Core | 11.0.0-rc.3 | net11.0 | EF Core 11 (release candidate, not GA) |
| Pomelo.EntityFrameworkCore.MySql | 9.0.0 (last) | net8.0 | [9.0.0, 9.0.999], no EF Core 10 or 11 build |
| Oracle MySql.EntityFrameworkCore | 10.0.12 | net8.0, net9.0, net10.0 | EF Core 10.0.12 |
| Oracle MySql.EntityFrameworkCore | 11.0.0-preview | net8.0 to net11.0 | preview |
| Oracle.EntityFrameworkCore | 10.23.26400 | net10.0 | [10, 11) |
| EF Core | 11.0.0 | net11.0 only | |

The 140 skipped MySQL EF Core tests are the existing Pomelo skip tracked in [#2086](https://github.com/dlrivada/Encina/issues/2086), not a new regression. The 10-provider matrix ([AGENTS.md](../../AGENTS.md) section 5) therefore stays as it is today for MySQL EF Core. Npgsql 10.0.x is capped below EF Core 11, so EF Core 11 waits for Npgsql EF 11 GA (conditions d and e).

Other packages, by target framework: Marten 9.59.0 supports net9.0 and net10.0 (the repository uses 9.38.0), and 10.0.0-alpha.1 supports net10.0 and net11.0. LanguageExt.Core 4.4.9 is netstandard2.0; 5.0.0-beta-78 is net10.0.

### Q5. The netstandard2.0 project

`Encina.Security.ABAC.Analyzers` (Roslyn 5.0.0, `LangVersion` 14 stays) builds and packs on SDK 11.

### Q6. CI images and workflows

- `setup-dotnet` with `10.0.x` appears in 19 workflow files (`ci.yml` 13 setup-dotnet steps, `ci-full.yml` 9, the `encina-full-ci` template 7, `load-tests.yml` 6, and others). There is a `DOTNET_SDK_VERSION` environment variable in `ci.yml`, `ci-full.yml`, `mutation-tests.yml`, `load-tests.yml` and the default inputs of the templates.
- `net10.0` is hard-coded in the artifact globs `bin/Release/net10.0` at `ci.yml:145-148` and `ci-full.yml:55-58`, and in `.github/scripts/generate-public-api.cs:9`.
- `src/Encina.Cli/Services/ProjectScaffolder.cs` (and two tests) emit `net10.0` into scaffolded projects.
- Runners: `ubuntu-latest` (64 uses) and `windows-latest` (2). `setup-dotnet` is v5 in the templates and v6 elsewhere.
- There is no `global.json`, so the SDK used is the newest one installed on the runner (condition a).

### Q7. Support dates

Confirmed on the [official policy page](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core): .NET 11 (STS) reached GA on 2026-11-10 and ends on 2028-11-14; .NET 10 (LTS) also ends on 2028-11-14.

### Q8. Breaking changes against Encina code

From the [.NET 11 compatibility list](https://learn.microsoft.com/en-us/dotnet/core/compatibility/11):

| Breaking change | Observed |
| --- | --- |
| DI cycle detection | Seen: two `RecoverabilityOptionsCompositionTests` failures (Q2). |
| `IStartupValidator` obsolete | Seen: SYSLIB0066 in tests only; no use in `src/`. |
| Microsoft.Extensions packages in the shared framework | Seen: NU1510, 207 times in 84 projects (Q1). |
| `BackgroundService` failure behaviour of `IHost.RunAsync` and `StopAsync` | Not observed by the tests; needs a code review. |
| DI scope disposal aggregates exceptions | Not observed by the tests; needs a code review. |
| Empty string in chained configuration overrides earlier providers | Not observed by the tests; needs a code review. |
| Options validation generator SYSLIB1205 | Not observed by the tests; needs a code review. |
| Minimum hardware x86-64-v2 | Not observed; affects deployment targets, not the build. |

Four of the five "not observed" items go to the follow-up code review (section 5); the hardware minimum is a deployment matter and is noted for the release documentation.

## 4. Fix list

| Item | Effort |
| --- | --- |
| `global.json` pin to SDK 10.0.x now, to 11.0.x on the move | S |
| `TargetFramework` and `LangVersion` in `Directory.Build.props`; remove the per-project setting from the 30 `csproj` files (11 also set `LangVersion`) | M |
| Remove 207 redundant `Microsoft.Extensions.*` `PackageReference` entries in 84 projects | M |
| Fix 3 src errors from SDK 11 rules (CA1877, CA2026, CA1830) | S |
| Fix 6 CA1307 and the CS0419 `cref` in src | S |
| Fix CA2026 (12) and CA2027 (5) and replace `IStartupValidator` (10 SYSLIB0066) in tests | M |
| Fix the 4 unit failures (confirm the DataProtection cause first) | S |
| CI: 19 workflow files, `DOTNET_SDK_VERSION`, artifact globs, `generate-public-api.cs`, `ProjectScaffolder.cs` and its tests | M |
| Re-run the Stryker parse check on the clean build | S |
| Amend `AGENTS.md` section 1 and SPEC-000; ADR for the platform move | S |

The whole move is larger than the sum of its parts because of review and CI time, so it is decomposed into several PRs (next section), each within the pull-request size limits of `AGENTS.md` section 11.

## 5. Follow-up issues

The issue files are drafted separately and opened by the orchestrator.

1. Platform-move PR set, decomposed (SDK pin on move, props and `csproj`, NU1510 package references, src diagnostics, test fixes, CI, `AGENTS.md` and SPEC-000 amendment, ADR).
2. Closed hierarchies adoption: `ReplicationRecord`, `KeyDestructionReference`, `RequestIdentity`, `AuditResolution`.
3. AES Key Wrap for #1250 (column encryption) and #1239 (`IKeyWrapper`).
4. `IAsyncStartupValidator` adoption.
5. `BackgroundService` failure behaviour of `IHost.RunAsync` and `StopAsync`, with the review of the other unobserved breaking changes of Q8.
6. `global.json` SDK pin on `main`.

## 6. Open points and gaps

- No ADR yet records the move to .NET 11; the issue asks for one on go (section 4). The ADR and the ROADMAP update listed in the issue deliverables are deferred to the platform-move issue (#3667), and the raw build and test logs were not committed.
- Stryker parse check (condition c) and the DataProtection cause (condition f) are open.
- The unions question for the ROP core is a separate spike, blocked by #3661 and not covered here.

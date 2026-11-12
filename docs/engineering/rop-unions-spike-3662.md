---
nav_exclude: true
---

# C# 15 unions for the ROP core spike (#3662)

This page is for the maintainer and for contributors who have to decide what type carries every Encina result: it records what was measured about C# 15 unions against `Either<EncinaError, T>` from LanguageExt, what each option would look like at real call sites, and what migrating would cost. It is an explanation of a decision under evaluation; the decision itself (D1) belongs to the maintainer.

**Status:** measured spike, 2026-11-11. **Issue:** [#3662](https://github.com/dlrivada/Encina/issues/3662). **Related:** [ADR-001](../architecture/adr/001-railway-oriented-programming.md), [ADR-004](../architecture/adr/004-reject-mediator-result.md), [ADR-051](../architecture/adr/051-languageext-version-for-1-0.md), [LanguageExt v5 spike (#1899)](languageext-v5-spike-1899.md).

## 1. Context

[ADR-001](../architecture/adr/001-railway-oriented-programming.md) makes `Either<EncinaError, T>` the return type of every Encina operation. C# 15 (shipping with .NET 11) adds unions, so the question is whether Encina should own its result type instead of depending on LanguageExt for it. [ADR-004](../architecture/adr/004-reject-mediator-result.md) (Rejected) declined an `EncinaResult<T>` wrapper over `Either`, an abstraction on top of LanguageExt; options (b), (c) and (d) are different, because they replace the carrier instead of wrapping it. Choosing one of them would still need a new ADR that supersedes or amends ADR-004. No ADR records the carrier choice yet; it is Dilemma D1.

| Option | What it is |
| --- | --- |
| (a) Keep `Either` | `Either<EncinaError, T>` from LanguageExt 4.4.9, as today |
| (b) Standard union | `public union StdResult<T>(EncinaError, T);`, compiler-generated; the case value is boxed |
| (c) Custom non-boxing union | `[Union] readonly struct Result<T> : IUnion` with typed fields, `HasValue` and two `TryGetValue` overloads; Encina writes the combinators |
| (d) Closed hierarchy | `abstract record HResult<T>` with sealed `Ok` and `Fail` cases and a private constructor |
| (c') Custom union with a reference error | Option (c) holding `T` inline and the error by reference. Designed, **not measured** |

## 2. Method and caveat

A prototype under [`artifacts/spikes/3662/`](../../artifacts/spikes/3662/) (`Types.cs`, `SendPathBenchmarks.cs`, `Probe.cs`) mirrors the 40-byte `EncinaError` layout and implements (b), (c) and (d). A non-inlined handler returns a success or an error; the caller pattern-matches (Either uses `Match`). Scenarios: success with `int`, success with a reference type (a sealed record), failure, Bind-Map-Bind success and short-circuit failure, and an `async ValueTask` wrapper over a synchronously completed handler.

Toolchain: .NET SDK 11.0.100, `net11.0`, `LangVersion` 15, LanguageExt.Core 4.4.9, BenchmarkDotNet 0.15.8.

**Caveat.** BenchmarkDotNet 0.15.8 throws `NotImplementedException: GetRuntimeVersion not implemented for NotRecognized` on `net11.0`, so the run uses `InProcessEmitToolchain` with `Job.ShortRun` (3 warmups, 3 iterations, 1 launch). Times are indicative, with wide error intervals on some rows; allocated bytes are exact. These are measured spike figures, not coverage figures.

Reproduce from `artifacts/spikes/3662`:

```powershell
dotnet run -c Release -- --filter "*SendPathBenchmarks*"
dotnet run -c Release -- --probe
```

## 3. Benchmark results

Mean time in ns and allocated bytes per operation. Full output: [`RopUnions.Spike.SendPathBenchmarks-report-github.md`](../../artifacts/performance/spike-3662/results/RopUnions.Spike.SendPathBenchmarks-report-github.md).

| Scenario | (a) Either | (b) std union | (c) custom union | (d) hierarchy |
| --- | --- | --- | --- | --- |
| Success, int | 6.48 ns / 0 B | 3.86 ns / 24 B | 7.19 ns / 0 B | 2.95 ns / 24 B |
| Success, reference T | 7.26 ns / 0 B | 0.73 ns / 0 B | 6.10 ns / 0 B | 4.57 ns / 24 B |
| Failure | 9.49 ns / 0 B | 7.11 ns / 56 B | 6.63 ns / 0 B | 6.06 ns / 56 B |
| Bind-Map-Bind, success | 32.05 ns / 24 B | 12.77 ns / 96 B | 29.55 ns / 0 B | 12.47 ns / 96 B |
| Bind-Map-Bind, failure at step 1 | 45.89 ns / 24 B | 44.79 ns / 224 B | 48.55 ns / 0 B | 21.57 ns / 112 B |
| async ValueTask, success | 62.20 ns / 0 B | 13.15 ns / 24 B | 61.11 ns / 0 B | 12.67 ns / 24 B |
| async ValueTask, failure | 66.03 ns / 0 B | 19.41 ns / 56 B | 64.18 ns / 0 B | 16.69 ns / 56 B |

Reading (an interpretation; the short-run error intervals are wide): speed seems to follow struct size, allocation follows boxing. Either and the custom union are 48 to 56 byte structs copied through every return, Bind step and async state machine, which is why their async rows cost about 61 to 66 ns against 13 to 19 ns for the 8-byte options. The standard union and the hierarchy are 8-byte references, faster per call, but they allocate on every value-type success and every error. Gen0 collections stayed between 0.0013 and 0.0119 per 1000 operations. Either's 24 B in the chain rows comes from LanguageExt's Bind/Map path (closure or delegate), not from the struct.

## 4. Probe facts

| Fact | Result |
| --- | --- |
| `EncinaError` size | 40 B (a string and two `Option<Exception>`) |
| `Either<EncinaError, int>` / `<string>` | 48 B / 56 B |
| `StdResult<T>` | 8 B (one object reference) |
| Custom `Result<int>` / `<string>` | 48 B / 56 B |
| `HResult<T>` | 8 B (reference) |
| Allocation of one `StdResult<int>` | success 24 B (boxed int), failure 56 B (boxed `EncinaError`) |
| `StdResult<string>` success | 0 B |
| Custom union and Either | 0 B on both paths |
| `HResult<T>` | 24 B |
| `switch` with type patterns on the custom union | 0 B: the compiler calls `TryGetValue`, never the boxing `Value` property |
| Same `switch` on a failed `StdResult<int>` | 56 B |

Defaults and nullability:

- `default(StdResult<T>)` and `default(Result<T>)` have no case; a `switch` over them throws `SwitchExpressionException`. The compiler warns CS8655 on switches over the generic `StdResult<T>` with an unconstrained `T`, so code needs a `null` or `_` arm. The custom union can define `default` as an explicit state, for example an "uninitialized" `EncinaError`, which fails closed. Either's `default` is the Bottom state and `Match` throws `BottomException`, a comparable trap.
- With an unconstrained `T`, the standard union cannot tell `T = EncinaError` from the error case; the custom union has the same ambiguity in `TryGetValue` for that instantiation, which is never used in practice.

## 5. System.Text.Json behaviour

| Type | Serialized form |
| --- | --- |
| `Either<EncinaError, int>` | `[{}]` for both Right and Left (it is seen as an `IEnumerable`) |
| Standard union and custom `[Union]` struct | the case value, untagged: success `42`, failure `{"Message":"not found","Exception":[]}` |
| Closed hierarchy without attributes | `{}` |
| Closed hierarchy with `[JsonPolymorphic]` / `[JsonDerivedType]` | tagged with a `$type` discriminator, the best wire shape of the four |

There is no Either or Option `JsonConverter` in `src/` (0 occurrences), so Encina does not serialize `Either` directly today. The union round trip works for a primitive success; the `EncinaError` case deserializes with an empty `Message` (get-only properties, private constructor) and `Option<Exception>` serializes as `[]`. Whichever option is chosen, a wire contract needs a tagged representation: an explicit converter or a DTO such as ProblemDetails.

## 6. API shape per option

| Option | Shape |
| --- | --- |
| (a) Either | `r.Match(Right: v => ..., Left: e => ...)`, `Bind`, `Map`, `MapLeft`, and `BindAsync`/`MapAsync`/`MatchAsync` from LanguageExt. Constructed with `Right<EncinaError,T>(x)` / `Left<EncinaError,T>(e)` or implicit conversions. No compiler exhaustiveness. |
| (b) Standard union | `public union Result<T>(EncinaError, T);` The body is compiler-generated, so combinators live in C# 14 extension members (`extension<T>(Result<T> r) { ... }`). Call sites read `r switch { T v => ..., EncinaError e => ... }`, checked for exhaustiveness. |
| (c) Custom union | `[Union] public readonly struct Result<T> : IUnion` with `HasValue`, `TryGetValue(out T)`, `TryGetValue(out EncinaError)`, implicit conversions, instance `Match/Map/Bind/MapError` and async extensions over `ValueTask<Result<T>>`. Same `switch` syntax and exhaustiveness as (b), without boxing. Encina writes and maintains about 15 to 25 combinators. |
| (d) Hierarchy | `r switch { Result<T>.Ok(var v) => ..., Result<T>.Fail(var e) => ... }`. Exhaustiveness needs the closed-hierarchy feature, outside this spike; without it a discard arm is required. |

Three real call sites rewritten with (c); (b) reads the same.

**1. `src/Encina/Core/Encina.cs` (`ExtractOutcome`)**

```csharp
// today
private static (bool IsSuccess, EncinaError? Error) ExtractOutcome<TResponse>(Either<EncinaError, TResponse> outcome)
    => outcome.Match(
        Left: err => (IsSuccess: false, Error: (EncinaError?)err),
        Right: _ => (IsSuccess: true, Error: null));

// union
private static (bool IsSuccess, EncinaError? Error) ExtractOutcome<TResponse>(Result<TResponse> outcome)
    => outcome switch
    {
        EncinaError err => (false, err),
        _ => (true, null),
    };
```

**2. `src/Encina/Sharding/EntityShardRouter.cs`, lines 80 to 87 (`GetShardId`)**: `CompoundShardKeyExtractor.Extract(entity).Bind(key => ...)` keeps the same shape with an owned `Bind`; only the return type changes to `Result<string>`. Sites 1 and 3 are at `src/Encina/Core/Encina.cs` lines 146 to 149 and `ParallelWhenAllDispatchStrategy.cs` lines 67 to 80.

**3. `src/Encina/Dispatchers/Strategies/ParallelWhenAllDispatchStrategy.cs`**

```csharp
// today
if (result.IsLeft)
{
    var error = result.Match(Left: err => err, Right: _ => EncinaErrors.Unknown);
    errors.Add((handler, error));
}
...
return Right<EncinaError, Unit>(Unit.Default);

// union
if (result is EncinaError error)
{
    errors.Add((handler, error));
}
...
return Result.Success; // a non-generic Result, or Result<Unit> with an Encina-owned Unit
```

## 7. Migration counts

Measured on `origin/main` at `68d08ddae`, `src/` only (3041 `.cs` files), by regular-expression counts over the file text. Occurrences are counted per match, not per line, so a line-based `Select-String` count differs slightly (it gives 5065 lines for `Either<EncinaError`); the file counts reproduce. `.Match(` and `.Map(` include `Option` and some non-LanguageExt calls, so they are upper bounds. Tests and samples are not counted and add to the total.

| Pattern | Occurrences | Files |
| --- | --- | --- |
| `Either<EncinaError` | 5071 | 813 |
| Public member lines with `Either<EncinaError` | 2568 | 577 |
| `.Match(` | 627 | 313 |
| `.Map(` | 272 | 101 |
| `.Bind(` | 40 | 23 |
| `.MapLeft(` | 14 | 12 |
| Async combinators (`BindAsync`, `MapAsync`, `MatchAsync`, `MapLeftAsync`) | 310 | 53 |
| `.IsLeft` / `.IsRight` | 535 | 201 |
| `Left(` / `Right(` constructors | 2583 | 304 |
| `Option<` | 557 | 138 |
| `Some(` / `None` | 575 | 228 |
| `Unit` | 2770 | 438 |
| `RepositoryError` | 217 | 10 |
| `using LanguageExt;` | 856 | 853 |
| `using static LanguageExt.Prelude` | 315 | 312 |
| Other LanguageExt types (Seq, Arr, Lst, HashMap, Atom, Validation, Fin, Try, Aff, Eff, EitherAsync, TryAsync, OptionAsync) | 17 | 13 |
| `JsonConverter` for Either or Option | 0 | 0 |
| `.csproj` files referencing LanguageExt | 30 of 107 | |

Dropping LanguageExt entirely, beyond the result type, requires replacing:

- `Option<T>` (557 uses). `EncinaError` itself holds two `Option<Exception>` fields and `GetCode()` returns `Option<string>`; the replacement is a nullable `T?` or an Encina `Option`.
- `Unit` (2770 uses): an Encina `Unit` struct or a non-generic `Result`.
- The Prelude helpers (`Right`, `Left`, `Some`, `None`, `Optional`, `unit`).
- The 17 uses of other LanguageExt types and the async combinators.
- The LanguageExt-based assertions in the `Encina.Testing.*` packages.

If (b) or (c) is chosen, a Roslyn-based codemod (a C# file-based app) can do the type renames, `Right<EncinaError,T>(x)` to `x`, `Left<...>(e)` to `e`, `.IsLeft` to `is EncinaError`, and `Match(Right:, Left:)` to an owned `Match(success, failure)` with the arguments swapped; `Option` and `Unit` are a second pass. Package by package from `src/Encina`, within the [AGENTS.md](../../AGENTS.md) worker limits (15 files, 1500 lines), this is well over 50 pull requests, and the `PublicAPI` files change in every package.

## 8. Interaction with #3419 and #3661

| Issue | Interaction |
| --- | --- |
| [#3419](https://github.com/dlrivada/Encina/issues/3419) (PR [#3599](https://github.com/dlrivada/Encina/pull/3599), open) | The PR adds `ErrorCategory`, `EncinaErrorMetadataKeys`, `LegacyErrorCategoryInference` and categorized `EncinaErrors.Create` overloads. The category lives in the error's metadata, not in a new field, so the 40-byte layout and every figure above are unchanged. It is orthogonal to the carrier: an owned `Result` can expose `result.Category` or a category-preserving `MapError` and make the category part of a tagged JSON shape; with Either this needs extension methods only. Land #3599 first: a carrier migration touches every call site and would otherwise conflict with the category migration. |
| [#3661](https://github.com/dlrivada/Encina/issues/3661) (verify Encina on .NET 11 / C# 15) | Options (b) and (c) need the platform move first, because union syntax, `IUnion` and `UnionAttribute` ship only in the .NET 11 runtime; (a) and (d) do not. Finding for #3661: BenchmarkDotNet 0.15.8 does not run out of process on `net11.0`, so `Encina.BenchmarkTests` needs a BenchmarkDotNet release that knows `net11.0`, or the in-process toolchain. |

## 9. Where each option wins

| Option | Wins when | Costs |
| --- | --- | --- |
| (a) Keep Either | No change is wanted now. Mature combinators; 0 B on single operations, but 24 B on both Bind-Map-Bind rows (section 3). | The slowest in async and chains (48 to 56 byte struct), no compiler exhaustiveness, 2568 public signatures stay on a library whose v5 is beta (see [ADR-051](../architecture/adr/051-languageext-version-for-1-0.md)), `[{}]` JSON. |
| (b) Standard union | Results are mostly reference types and errors are rare: fastest success for a reference `T` (0.73 ns, 0 B) and 3 to 5 times faster async. | Every value-type success allocates 24 B, every error 56 B, chains 96 to 224 B. Needs .NET 11. |
| (c) Custom union | Zero allocation on every path is the rule (hot path, error-heavy flows such as validation): 0 B everywhere, fastest failure among the structs, compiler exhaustiveness through `TryGetValue`. | Speed equals Either today because of the struct size. Encina owns the combinators. Needs .NET 11. |
| (d) Hierarchy | The wire shape matters (tagged JSON with `[JsonDerivedType]`) and chains short-circuit. | Always allocates 24 to 112 B; no language exhaustiveness until closed hierarchies ship. |
| (c') Reference-error custom union | Likely the best of both: a small struct (8 B plus `T` plus a tag), 0 B on success, one allocation on error. | Unmeasured; needs one more benchmark run before any decision. |

## 10. Recommendation (recommended, pending the maintainer)

Option (c), a custom non-boxing `[Union]` struct, preceded by a follow-up benchmark of the (c') layout before the ADR is written. The reason is the hot-path rule of Encina: (c) is the only measured option with 0 B on every path, it gives compiler exhaustiveness, and it removes LanguageExt from the public surface.

Option (c) is not faster than Either in the measured scenarios: success 7.19 ns versus 6.48 ns, chain 29.6 ns versus 32.0 ns, async 61 ns versus 62 ns (section 3). It saves 24 B only in chains. The case for (c) rests on exhaustiveness and on dropping the LanguageExt dependency, not on speed.

Option (d), closed hierarchies, was measured for completeness although the issue scoped it out.

**D1 is the maintainer's decision.** The migration is large (2568 public signatures, about 5000 type uses, plus the `Option` and `Unit` replacements) and, if D1 goes this way, must be decomposed into per-package issues after the decision. No ADR records D1 yet; the decision would be recorded as one.

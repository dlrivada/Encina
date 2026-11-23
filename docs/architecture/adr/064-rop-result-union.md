---
title: "ADR-064: An Encina-Owned Result Union Replaces LanguageExt Either as the ROP Carrier"
layout: default
parent: ADRs
grand_parent: Architecture
---

# ADR-064: An Encina-Owned Result Union Replaces LanguageExt Either as the ROP Carrier (Accepted)

This record is for contributors who convert a package to the new result type, and for anyone who needs to know why the type changed and what is still open. It records which type carries every Encina result, how that type is laid out, and what the migration has to settle.

## Status

**Accepted** - 2026-11-20. Decided by the maintainer in two steps on [#3662](https://github.com/dlrivada/Encina/issues/3662):

- **D1, 2026-11-13**: an Encina-owned, non-boxing result union replaces `Either<EncinaError, T>`, and LanguageExt leaves Encina entirely ([decision comment](https://github.com/dlrivada/Encina/issues/3662#issuecomment-6855194552)).
- **D1b, 2026-11-20**: the union's internal layout is option (B2), `RefResult<T>`, measured by spike [#3672](https://github.com/dlrivada/Encina/issues/3672) (PR [#4070](https://github.com/dlrivada/Encina/pull/4070)).

This ADR amends [ADR-001](001-railway-oriented-programming.md) (the ROP principle stands; its carrier changes) and [ADR-004](004-reject-mediator-result.md) (its decision to keep using LanguageExt `Either` directly no longer holds; its rejection of a wrapper over `Either` does). Encina is pre-1.0: the type described here is the target of the migration, not a stable API.

[AGENTS.md](../../../AGENTS.md) section 8 requires an ADR for architectural decisions, and the type every Encina operation returns is one.

## Context

[ADR-001](001-railway-oriented-programming.md) makes every Encina operation return `Either<EncinaError, T>` from LanguageExt. ADR-001 chose it for its mature, well-tested combinators, at a time when C# had no union types; the maintainer recorded with D1 that the missing language support was the reason a library was needed at all. C# 15, which ships with .NET 11, adds unions. The maintainer fixed three facts before deciding: the whole repository moves to `net11.0` and C# 15 ([#3667](https://github.com/dlrivada/Encina/issues/3667)); Encina stops depending on LanguageExt; and the best option is taken even when it is breaking or large, because Encina is pre-1.0 ([AGENTS.md](../../../AGENTS.md) section 1).

The spike [About C# 15 unions for the ROP core](../../engineering/rop-unions-spike-3662.md) measured four carriers on `net11.0` (§1 to §10, issue #3662). It then measured three layouts that hold the error differently (§11, issue #3672). The spike's figures are indicative short-run timings with exact allocated bytes. Its report and CSV are linked from §11, and this ADR cites them by section rather than restating them.

Two properties drove the decision:

- **Size of the struct.** A struct is copied whole on every return, every `Bind` step and every async state machine. `Either` and a union that holds the error inline are large structs, and §3 and §11 show them as the slowest options in the async and chain-success scenarios.
- **Allocation.** A union that boxes its case allocates on every value-type success and on every error (§3). Encina's hot path should not allocate on success.

## Decision

### D1 (2026-11-13): the carrier

- Encina's ROP carrier is an Encina-owned result union: a `[Union]` struct implementing `IUnion`, with typed fields for the value and the error, `HasValue` and two `TryGetValue` overloads (§1 and §6 of the spike, option (c)). It replaces `Either<EncinaError, T>` everywhere in `src/` and `tests/`.
- It does not box: a `switch` with type patterns calls `TryGetValue`, never a boxing `Value` property (spike §4).
- Handling both cases is checked by the compiler: a `switch` over the union is exhaustive in the same way as for a standard C# 15 union (spike §6).
- `default` is a defined state, an "uninitialized" error, so an unset result fails closed instead of reading as a success (spike §4).
- Encina writes and owns the combinators (`Match`, `Map`, `Bind`, `MapError` and their async forms) instead of taking them from LanguageExt.
- LanguageExt leaves Encina entirely. Each package's migration also replaces everything else it uses from LanguageExt (`Option`, `Unit`, the `Prelude` helpers and the LanguageExt-based assertions of the `Encina.Testing.*` packages). Any LanguageExt surface Encina does not use is removed in [#3847](https://github.com/dlrivada/Encina/issues/3847).

### D1b (2026-11-20): the internal layout

The union is option (B2) of spike §11, the `RefResult<T>` prototype in [`Types.cs`](../../../artifacts/spikes/3662/Types.cs):

- `EncinaError` becomes a **sealed, immutable class**. Today it is a `readonly record struct` (`src/Encina/Errors/EncinaError.cs`).
- The union holds `T` inline plus a nullable reference to the error, so it stays a small struct whatever the size of the error.
- The prototype differs from the target in two ways that the conversion must implement. Its `default` is a state with no case (§11), and the target makes that state the fail-closed uninitialized error of D1, which is a preallocated instance. Its `EncinaErrorRef` still holds the two `Option<Exception>` fields that D1 removes.
- **Preallocation rule.** Frequent errors that carry no variable data, such as the uninitialized state and cancellation, are preallocated static instances, so returning them allocates nothing. A freshly created error, for example one whose message or details carry data from the request, costs one allocation, as option (B1) costs on every failure.

The maintainer's reasons, with the evidence in spike §11 ("Results" and "Reading per criterion"):

- Among the layouts with zero allocation on success, (B2) and (B1) are the fastest on success, in chains and in async. That is the speed of the small reference carriers, without their allocation on success.
- Among those same layouts, (B2) is the fastest on the failure rows and on the chain rows when the error instance is reused.
- §11 rates (B2) costly because it changes `EncinaError` from struct to class. D1 already changes `EncinaError`'s shape, because removing LanguageExt removes its two `Option<Exception>` fields. The extra cost of making it a class is small.

## Alternatives considered

### D1: the carrier

These are options (a), (b) and (d) of spike §1, compared in §3, §6 and §9.

- **(a) Keep LanguageExt `Either`.** Rejected. It is among the slowest options in the async and chain-success scenarios because it is a large struct (§3). The compiler does not check that both cases are handled. Every public signature counted in §7 would stay on an external library that the language no longer needs to supply, and whose next major version is still in beta ([ADR-051](051-languageext-version-for-1-0.md)). System.Text.Json serializes it as `[{}]` (§5).
- **(b) Standard C# 15 `union`.** Rejected because it boxes: every value-type success and every error allocates, and chains allocate more still (§3, §4). Its body is compiler-generated, so the combinators could only be extension members (§6).
- **(d) Closed record hierarchy.** Rejected because every result allocates, on success as well as on failure (§3). Language-checked exhaustiveness waits for closed hierarchies (§6).

### D1b: the layout

These are the other layouts of spike §11, plus the inline union of §1.

- **(c) Custom union with `EncinaError` inline.** It allocates nothing on any path, but it is as large a struct as `Either` (§4, §11). §11 shows it slower than (B1), (B2) and (C) in chains and in async. It loses on speed.
- **(B1) `BoxResult<T>`, the error in a box allocated on failure.** It keeps `EncinaError` a struct and is as fast as (B2) on success. It allocates on every failure, which is the wrong cost for error-heavy flows such as validation (§11, "Reading per criterion"). It also needs a factory instead of a public constructor: a public one-argument constructor over the box would become a union case and trigger CS8509 (§11, "Complexity"). Its main advantage, not changing `EncinaError`, mostly disappears once D1 removes the `Option<Exception>` fields.
- **(C) `SlimResult<T>`, a shrunk error kept inline.** It allocates nothing even when an error is created fresh, but §11 shows its success, chain and async rows well behind (B1) and (B2). Its shrink also had to fit the exception and the [ADR-062](062-error-category-contract.md) metadata into one reference, which §11 left open. It loses on speed on the success path, which is the common one.

## Consequences

### Positive

- Results allocate nothing on success, and the union is a small struct on every path (spike §11).
- Failures with a preallocated error allocate nothing. Only a freshly created error allocates, once, and that allocation is not repeated as the result crosses `Bind`, `Map` or async steps (§11).
- The compiler checks that both cases are handled, and an unset result fails closed.
- Encina's public surface no longer exposes LanguageExt types. Its combinators are owned by Encina and can follow Encina's needs, for example carrying the error category.

### Negative

- **`EncinaError` changes from struct to class.** Code written against a struct has to be checked during the conversion:
  - Pattern matching: a type pattern `is EncinaError e` keeps its syntax. A variable of type `EncinaError` can now be `null`, so nullable annotations apply to it.
  - Equality: `EncinaError` loses the value equality a `record struct` gives. What equality the class defines is an open point (below).
  - `default`: `default(EncinaError)` is now `null`, not an empty error. The "no result yet" case is handled by the union's fail-closed uninitialized state, not by a default `EncinaError`.
- **Freshly created errors allocate.** The preallocation rule limits this to errors that carry data. A validation-heavy request that creates many fresh errors pays one allocation per error. The spike did not measure that profile: see the first open point.
- **Encina maintains the combinators** and their async forms (spike §6 estimates how many).
- **The migration is large.** It touches every `Either<EncinaError, T>` signature in `src/` and every `PublicAPI` file (spike §7), and it can start only after the platform move.

### Interaction with the error category (ADR-062)

[ADR-062](062-error-category-contract.md) stores an error's category on the error's internal carrier exception (today the `MetadataException` property, an `Option<Exception>`) and reads it with `GetCategory(this EncinaError)`. Its R1 choice rule has a pending amendment in PR [#4105](https://github.com/dlrivada/Encina/pull/4105). The category contract itself does not depend on the carrier: producers still set a category where the error is created, and hosts still map categories, never codes. Choosing (B2) removes the constraint §11 found for option (C), which had to fit the metadata into a single reference. What changes is where the category is stored: D1 removes the `Option<Exception>` fields, and the carrier exception lives in one of them. Where the category lives inside the class is an open point for [#3419](https://github.com/dlrivada/Encina/issues/3419) and ADR-062.

### Interaction with System.Text.Json

Spike §5 found that System.Text.Json writes a `[Union]` struct as its case value, untagged, and that `EncinaError` does not round-trip, because its properties are get-only and its constructor is private. No converter for `Either` or `Option` exists in `src/` today (§5), so no current wire contract depends on the carrier. Any wire contract that carries a result needs a tagged representation: an explicit converter, or a DTO such as ProblemDetails. The spike did not design that shape, and this ADR does not either (see the open points).

### Migration consequences

This ADR contains no migration guide; the conversion recipe is part of the #3667 decomposition. The order of the work:

1. **Platform move** to `net11.0` and C# 15, [#3667](https://github.com/dlrivada/Encina/issues/3667). It comes first because union syntax, `IUnion` and `UnionAttribute` exist only on .NET 11 (spike §8).
2. **The platform move's own .NET 11 follow-ups**, which do not depend on the carrier:
   - adopting C# 15 closed hierarchies in four types ([#3668](https://github.com/dlrivada/Encina/issues/3668));
   - replacing `IStartupValidator` with `IAsyncStartupValidator` ([#3670](https://github.com/dlrivada/Encina/issues/3670));
   - reviewing the hosted services against .NET 11's `BackgroundService`, DI disposal and configuration breaking changes ([#3671](https://github.com/dlrivada/Encina/issues/3671)).
3. **Carrier migration**, one package per pull request in the #3667 decomposition, on an integration branch.
4. **Removal of the unused LanguageExt surface**, [#3847](https://github.com/dlrivada/Encina/issues/3847). [ADR-051](051-languageext-version-for-1-0.md) stops applying when the last LanguageExt reference is gone.
5. **Merge to `main`**: the integration branch reaches `main` as one squash pull request labelled `size-exception-approved`.

## Open points

Each point is settled in the issue named, not in this ADR.

| Open point | Settled in |
| --- | --- |
| Measure a realistic validation-failure profile (many freshly created errors per request) with the (B2) layout. A surprising result reopens D1b with that data. | Acceptance item of the conversion-recipe sub-issue (R01) in the #3667 decomposition |
| Public names: the union type, its factories, the non-generic or `Unit` result for operations with no value, and the replacements for `Option<T>`, `Unit` and the `Prelude` helpers | The conversion-recipe sub-issue of the #3667 decomposition |
| The exact combinator set and its async forms over `ValueTask` | The conversion-recipe and carrier-types sub-issues of the #3667 decomposition |
| Which errors are preallocated, beyond the uninitialized state and cancellation | The conversion-recipe sub-issue of the #3667 decomposition |
| Equality semantics of the `EncinaError` class (reference or value equality) | The carrier-types sub-issue of the #3667 decomposition |
| Where the error category and the other metadata live inside the `EncinaError` class, once the `Option<Exception>` fields are gone | [#3419](https://github.com/dlrivada/Encina/issues/3419) and [ADR-062](062-error-category-contract.md) |
| The System.Text.Json shape of a result and of `EncinaError` (tagged converter or DTO) | The carrier-types sub-issue of the #3667 decomposition, with #3419 for the category's place in that shape |

## Related

- [ADR-001](001-railway-oriented-programming.md): Railway Oriented Programming (amended by this ADR)
- [ADR-004](004-reject-mediator-result.md): Decision to not implement `EncinaResult<T>` (amended by this ADR)
- [ADR-006](006-pure-rop-exception-handling.md): Pure ROP exception handling
- [ADR-051](051-languageext-version-for-1-0.md): Which LanguageExt version Encina 1.0 ships on
- [ADR-062](062-error-category-contract.md): Error category contract on `EncinaError`
- [About C# 15 unions for the ROP core](../../engineering/rop-unions-spike-3662.md) (spike #3662 and #3672)
- Issues: [#3662](https://github.com/dlrivada/Encina/issues/3662), [#3672](https://github.com/dlrivada/Encina/issues/3672), [#3667](https://github.com/dlrivada/Encina/issues/3667), [#3847](https://github.com/dlrivada/Encina/issues/3847), [#3419](https://github.com/dlrivada/Encina/issues/3419); pull requests [#4070](https://github.com/dlrivada/Encina/pull/4070), [#4105](https://github.com/dlrivada/Encina/pull/4105)

## Date

2026-11-20 (decision dates: D1 2026-11-13, D1b 2026-11-20)

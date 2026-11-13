---
title: "ADR-062: Error Category Contract on EncinaError"
layout: default
parent: ADRs
grand_parent: Architecture
---

# ADR-062: Error Category Contract on EncinaError (Accepted)

## Status

**Accepted** - 2026-11-11, by the maintainer's decision of 2026-11-11 on [#3419](https://github.com/dlrivada/Encina/issues/3419) (option a, a protocol-neutral category set where the error is created). Implementation of the contract: [#3545](https://github.com/dlrivada/Encina/issues/3545) (sub-issue S1 of #3419). The contract lives in core `Encina` ([#2982](https://github.com/dlrivada/Encina/issues/2982)).

[AGENTS.md](../../../AGENTS.md) section 8 requires an ADR for architectural decisions; the shape of the error contract that every transport reads is one.

## Context

The hosts derive the HTTP status of an error from the text of its code. `MapErrorCodeToStatusCode` is copied in `Encina.AspNetCore`, `Encina.AzureFunctions` and `Encina.AwsLambda` and matches prefixes and suffixes (`.not_found`, `.conflict`, `validation.`). Most code families match no rule, so they answer 500: repository not-found, concurrency conflict and tenant-required errors, `idempotency.in_progress`, the outbound codes and others (the evidence is in #3419). Some internal configuration lookups match `.not_found` and answer 404 by accident.

gRPC and GraphQL are 1.0 transports and need the same contract. A central code-to-HTTP table in core would answer HTTP only and would need a second mapping for gRPC and a third for GraphQL, all keyed by code text.

The maintainer decided on 2026-11-11 for option (a): the producer sets a protocol-neutral category where the error is created, and each protocol maps the category to its own status.

## Decision

### The contract

- The category is set at the call site through the categorized overloads `EncinaErrors.Create(string code, ErrorCategory category, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? details = null)` and `EncinaErrors.FromException(string code, ErrorCategory category, Exception exception, string? message = null, IReadOnlyDictionary<string, object?>? details = null)`.
- It is stored on the internal carrier exception of the `EncinaError` and read with the extension `GetCategory(this EncinaError)`.
- `GetCategory` rules: an explicit category is returned as is; an error with no code (`EncinaError.New`) is `Internal`; a code with no category returns `LegacyErrorCategoryInference` during the migration only.
- A host never parses the error code to choose a status.

### The enum

`ErrorCategory` (`src/Encina/Errors/ErrorCategory.cs`) has explicit, stable values and is additive only: a value is never renumbered or removed. `Internal = 0`, so a default or unset value fails toward 500. Adding a category means adding a row to every protocol table, to the property tests (`tests/Encina.PropertyTests/Core/ErrorCategoryProperties.cs`) and to the value test in `ErrorCategoryTests`.

### The table

| Category | HTTP | gRPC | GraphQL `extensions.code` |
| --- | --- | --- | --- |
| `Internal` | 500 | `INTERNAL` | `INTERNAL_SERVER_ERROR` |
| `Validation` | 400 | `INVALID_ARGUMENT` | `BAD_USER_INPUT` |
| `InvalidRequest` | 400 | `INVALID_ARGUMENT` | `BAD_REQUEST` |
| `Unauthenticated` | 401 + `WWW-Authenticate` | `UNAUTHENTICATED` | `UNAUTHENTICATED` |
| `Forbidden` | 403 | `PERMISSION_DENIED` | `FORBIDDEN` |
| `NotFound` | 404 | `NOT_FOUND` | `NOT_FOUND` |
| `Conflict` | 409 | `ABORTED` | `CONFLICT` |
| `AlreadyExists` | 409 | `ALREADY_EXISTS` | `CONFLICT` |
| `FailedPrecondition` | 400 (provisional, row owned by #3420) | `FAILED_PRECONDITION` | `FAILED_PRECONDITION` |
| `RateLimited` | 429 + `Retry-After` if known | `RESOURCE_EXHAUSTED` | `RATE_LIMITED` |
| `Unavailable` | 503 + `Retry-After` if known | `UNAVAILABLE` | `UNAVAILABLE` |
| `Timeout` | 504 | `DEADLINE_EXCEEDED` | `TIMEOUT` |
| `UpstreamFailure` | 502 | `UNAVAILABLE` | `BAD_GATEWAY` |
| `Cancelled` | 499 | `CANCELLED` | `CANCELLED` |

Each protocol owns its column. HTTP is written once in core as data and consumed by the three hosts (sub-issue S2, then H1-H3). gRPC lives in `Encina.gRPC` and GraphQL in `Encina.GraphQL` ([#3429](https://github.com/dlrivada/Encina/issues/3429)). Each table is an array or a lookup indexed by category, never a large `switch` (the CRAP gate, [AGENTS.md](../../../AGENTS.md) section 9). The table, the status-to-reason-phrase lookup for the problem `title` and the header rules below live in one file, `src/Encina/Errors/ErrorCategoryHttp.cs`.

### Header rules

- `Retry-After` is written only with 429 or 503 and only when the details key `EncinaErrorMetadataKeys.RetryAfter` (a `TimeSpan`) is present. It is never guessed.
- Core already has a second retry-after key, `OutboundErrorMetadataKeys.HttpRetryAfter` (`"http.retry_after"`), which `OutboundRetryRules.GetRetryAfter` reads for outbound retries. Hosts read only `EncinaErrorMetadataKeys.RetryAfter`: the outbound producer slice (`Encina.Refit`, rule R7) writes `EncinaErrorMetadataKeys.RetryAfter` alongside the outbound key, and [#3424](https://github.com/dlrivada/Encina/issues/3424) decides whether the two keys merge.
- Every 401 carries `WWW-Authenticate` ([RFC 9110 section 15.5.2](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.2)): the value of `EncinaErrorMetadataKeys.AuthenticateChallenge` when present, otherwise the default `Bearer`.

### Choice rules for producers

- **R1.** `NotFound` only when the request names the missing thing. Server-side configuration or wiring lookups (profile, policy, template, key, handler) are `Internal`.
- **R2.** Duplicates and "exists" are `AlreadyExists`. A version mismatch, an operation in progress and an invalid transition are `Conflict`. Do not use `FailedPrecondition` before #3420.
- **R3.** Field rules are `Validation`. A missing header, key, id or tenant is `InvalidRequest` (a missing tenant answers 400, the `TenantResolutionMiddleware` precedent).
- **R4.** Store, serialization, crypto and provider failures, a null request and a missing handler are `Internal`, never 501.
- **R5.** `*.cancelled` is `Cancelled` (499). A cancellation is never counted as a server fault.
- **R6.** Denials are `Forbidden`; no identity is `Unauthenticated`. Identity conflicts (subject-claim, tenant or scope conflicts) are `Forbidden`.
- **R7.** Outbound codes: `client_error`, `server_error` and `unexpected_response` are `UpstreamFailure` (502); `rate_limited` and `network_error` are `Unavailable` (503); `timeout` is `Timeout` (504); `unexpected` is `UpstreamFailure`, or `Internal` if the reviewer finds it local. The upstream status is never passed through.

Also decided: `idempotency.in_progress` is `Conflict` (409), and the two `encina.authorization.*_tenant_required` codes are `InvalidRequest` (400).

### Staged migration

- **S1** ([#3545](https://github.com/dlrivada/Encina/issues/3545), this ADR) adds the contract, the transitional inference and the completeness scan.
- **Producer slices**, one package each: `P-<package>`, core `C1`-`C5`, Messaging `M1`-`M2`, EntityFrameworkCore `EF1`-`EF2`, hosts `H1`-`H3`. Each adds the category to its calls and deletes its line in `tests/Encina.UnitTests/Core/ErrorCategoryPending.txt`. Slices that only add a category argument may be bundled by family with the `size-exception-approved` label.
- **Completeness scan** (`ErrorCategoryCompletenessStaticScanTests`) fails on an uncategorized `EncinaErrors.Create` or `FromException` call in `src/` outside the pending list, on a stale pending entry, and on one code created with two categories. A category passed through a variable must use the named `category:` argument to be recognised.
- **`LegacyErrorCategoryInference`** (`src/Encina/Errors/LegacyErrorCategoryInference.cs`) holds the hosts' former suffix rules verbatim and returns a category instead of a status: `.already_exists` and `.duplicate` give `AlreadyExists`, `.conflict` gives `Conflict`, both 409 as before, so unmigrated producers keep their status. It is a staged migration step approved by the maintainer on 2026-11-11, not a compatibility layer.
- **F2 end state**: the inference is deleted, the category-less overloads become internal, a category-less call means `Internal`, the pending file is deleted and the scan stays as a permanent guard.
- **Known limit**: code-less producers (`EncinaError.New`) are invisible to the scan and stay `Internal`.

### Ownership of related decisions

- [#3420](https://github.com/dlrivada/Encina/issues/3420): compliance and security denials. It owns the `FailedPrecondition` HTTP row and may add a category such as `LegalRestriction`, with one row in every table. Its codes stay uncategorized and are annotated in the pending file.
- [#3422](https://github.com/dlrivada/Encina/issues/3422): HMAC signature, nonce and timestamp failures (`Encina.Security.AntiTampering`).
- [#3424](https://github.com/dlrivada/Encina/issues/3424): circuit-breaker, timeout and bulkhead rejections (the `RateLimited`, `Unavailable` and `Timeout` rows, and setting `RetryAfter`).
- [#3426](https://github.com/dlrivada/Encina/issues/3426): which error codes a client may see.

## Alternatives considered

- **(b) One exact-code table in core**: rejected because it answers HTTP only, needs a second mapping for gRPC and a third for GraphQL, and moves the knowledge of each package's codes away from the package.
- **(c) A per-package `IErrorStatusMapper`**: rejected because every package would register a mapper per protocol, and a package that forgets one answers 500 silently.
- **(d) Keep the suffix rules and rename the codes**: rejected because the status stays implicit in the spelling of a string, tests reference the code constants throughout the suite, and the rules are still HTTP-only.
- **Typed code structs** (a code type that carries its category): rejected for the same reference count, and because they would not remove the need for a protocol table.

## Consequences

- Positive: one place per protocol maps categories, so the three hosts, gRPC and GraphQL stay consistent; a new error code needs no host change; the producer, which knows the meaning of the failure, chooses the category; the scan makes a forgotten category a test failure.
- Positive: the enum is small, additive and stable, so a category added later is one row per table.
- Negative: every producer under `src/` must be touched once, in the slices above, and the pending file and `LegacyErrorCategoryInference` exist until F2.
- Negative: a category is coarser than a code; two codes with the same category answer the same status.
- Negative: the choice rules R1-R7 are judgement calls that reviewers apply by hand; the scan checks that a category is present and consistent, not that it is right.
- The `FailedPrecondition` HTTP row is provisional until #3420.

## Related

- [ADR-001](001-railway-oriented-programming.md) and [ADR-006](006-pure-rop-exception-handling.md), the `Either<EncinaError, T>` error model
- Issues: [#3419](https://github.com/dlrivada/Encina/issues/3419), [#3545](https://github.com/dlrivada/Encina/issues/3545), [#2982](https://github.com/dlrivada/Encina/issues/2982), [#3429](https://github.com/dlrivada/Encina/issues/3429)

## Date

2026-11-11

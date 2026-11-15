# Implementation Plan: Schema and Contract Validation with OpenAPI (`Encina.Validation.OpenApi`)

> **Issue**: [#231](https://github.com/dlrivada/Encina/issues/231)
> **Type**: Feature
> **Related**: #227 (owns the generator project and the rule model S6 reuses), #230 (owns `IRequestValidator<T>`), #446 (OpenAPI and Swagger integration, planned apart), #259 (schema registry, unrelated transport), #234 and #236 (planned apart), #726 (central validation instrumentation), #3415 (a code-less validation `Left` never becomes a 400), #3426 (which error text a client may see), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: High (a new ASP.NET Core package, two new third-party dependencies, an OpenAPI 3.0 and 3.1 schema engine and a generator extension)
> **Estimated Scope**: ~1,800 lines of production code + ~3,000 lines of tests + ~400 lines of documentation, in seven pull requests of at most 15 files and 1,000 lines each

---

## Summary

### What exists today (read on 2026-11-15, main `db1e7f688`)

- No OpenAPI code exists in `src`. `Directory.Packages.props` has no `Microsoft.OpenApi`, no `Microsoft.AspNetCore.OpenApi` and no JSON Schema package (grep of the file). `Encina.AspNetCore` references the shared framework (`FrameworkReference Microsoft.AspNetCore.App`) and has one middleware, `EncinaContextMiddleware`, added by `UseEncinaContext` (`ApplicationBuilderExtensions.cs:58`), and `ProblemDetailsExtensions` (`ToProblemDetails`, `ToActionResult`) which maps validation codes to 400 by prefix (`validation.*`, `ProblemDetailsExtensions.cs:211-212`) until ADR-062 moves the mapping to the category.
- Validation today works on the already bound .NET object (`ValidationPipelineBehavior<TRequest, TResponse>` and the three providers), after model binding; nothing validates the raw HTTP body, the query string or the route values against a contract, and nothing reads a specification.
- The orchestrator's invalid-request `Left` is code-less (`ValidationOrchestrator.cs:68-72`); a code-less error never becomes a 400 (#3415).
- Not in `src`: `AddEncinaOpenApiValidation`, `OpenApiValidationOptions`, `[GeneratedValidator]` (the issue's name; #227 names its attribute `[GenerateValidator]`), any schema validator, any package named `Encina.Validation.OpenApi`. #446 (open, v0.18.0) plans `Encina.OpenApi` to produce documents; this plan consumes a specification file and does not depend on it.

### What this plan delivers

A new opt-in package `Encina.Validation.OpenApi` with a runtime request validator and, in its last implementation pull request, a compile-time generator. At start-up it loads one specification file (OpenAPI 3.0 or 3.1, local `$ref` only) and fails the application when the file is missing or invalid. A middleware, added by `UseEncinaOpenApiValidation`, finds the operation of each request (method and route template), validates the path and query parameters and the JSON body against the operation's schemas with a JSON Schema 2020-12 engine (3.0 constructs are normalized first), and answers an RFC 9457 `application/problem+json` 400 listing violations by JSON pointer and keyword, never the values. Limits (body size, depth, error count, regular expression time) are bounded and fail closed. The generator (gated on #227) reads the same specification as an `AdditionalFile` and emits `IGeneratedRequestValidator<T>` classes for the matching DTOs, so the three validation providers can validate the bound object by the same contract. Nothing changes for an application that does not call the registration or the middleware.

- Affected packages: `Encina.Validation.OpenApi` (new), `Encina.Validation.Generators` (S6), `Encina` (EventIds only).
- Provider category: Validation (3) for the generated validators (S6, through the contract of #230 and #227, the same result under each provider); the runtime middleware sits before the pipeline and is provider-independent.
- Dependencies added: a specification parser (`Microsoft.OpenApi`) and a JSON Schema engine, pinned in `Directory.Packages.props` when S1 opens (the version is read from NuGet then, not fixed here).

---

## Design Choices

<details>
<summary><strong>1. Where the runtime validation runs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A middleware that matches the operation, validates parameters and the buffered body and short-circuits with a problem details 400** | One component for MVC and minimal APIs; runs before model binding, so the raw JSON (unknown properties, wrong types) is seen; no change in handlers | Buffers the request body (bounded by a limit); needs `UseRouting` information to match templates |
| **B) An `IEndpointFilter` for minimal APIs and an MVC action filter** | Endpoint-scoped; the bound route data is available | Two implementations; the body is already bound, so type mismatches become model binding errors first |
| **C) An `IValidationProvider` that serializes the bound object and validates it** | Sits in the Encina pipeline | The bound object has lost unknown properties and coerced types, so the contract check is weaker; re-serialization costs |

### Chosen Option: **A — Middleware** (recommended, pending the maintainer)

### Rationale

- The issue's goal is "validate incoming requests match the documented schema" and "catch contract violations early in the pipeline"; only A sees the request as the client sent it. The compile-time path (S6) covers the bound object for the three providers. B wins when the application wants per-endpoint opt-in; C wins when the specification is only available as .NET types.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Schema engine</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Microsoft.OpenApi` to parse and a JSON Schema 2020-12 library to evaluate, after a normalization step that rewrites OpenAPI 3.0 constructs** | Complete keyword coverage (`allOf`, `oneOf`, `anyOf`, `not`, `format`, `pattern`, `$ref`); maintained by others; the same engine for 3.0 and 3.1 | Two new dependencies; their AOT and trimming support must be checked when S1 opens |
| **B) A minimal evaluator written over the `Microsoft.OpenApi` object model** | One new dependency; full control of limits and messages | Re-implements keywords; every missed keyword is a silent accept |

### Chosen Option: **A — Parser plus a JSON Schema engine** (recommended, pending the maintainer)

### Rationale

- A missed keyword in B is a fail-open defect in a validation gate; A moves that risk to a maintained library, with the limits of register rows C3 and C5 applied around it. B wins when the dependency footprint or AOT support of the candidate libraries is unacceptable.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. A request that matches no operation, or an unsupported construct</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `StrictMode = true` (default) rejects a request with no matching operation and an unknown query parameter; `StrictMode = false` lets them through and logs EventId 583 once per template** | Fail closed by default; the opt-out is explicit and logged | A route the specification forgot answers 400 until documented |
| **B) Always let an unmatched request through** | Never blocks an undocumented endpoint | The contract is not enforced where it is incomplete |

### Chosen Option: **A — Strict by default with a logged opt-out** (recommended, pending the maintainer)

### Rationale

- AGENTS.md section 3: gates deny on missing context and the only opt-out is explicit and logged. The issue's example sets `StrictMode = true`. B wins when the specification covers only a subset of the application on purpose.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Is the generator in this issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Yes, as the last implementation pull request (S6), opened only after #227 S2 to S4 are on `main`: the specification is an `AdditionalFile`; the operation's request schema becomes a rule model for the DTO named by `operationId`, emitted by the emitters of #227** | Meets the acceptance criteria "source generator"; one rule model for all generators | Waits for #227; a mapping between schema properties and DTO members is needed |
| **B) A follow-up issue after the runtime path ships** | The runtime path closes the issue sooner | The acceptance criterion stays unmet; the maintainer must accept the change |

### Chosen Option: **A — Include the generator, gated on #227** (recommended, pending the maintainer)

### Rationale

- The acceptance criteria and the issue's task list name the generator. S1 to S5 and S7 do not depend on it. B wins when #227 slips.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Specification source and parsing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A file path (`SpecificationPath`), read once at start-up, JSON or YAML as the parser supports, local `$ref` only** | The issue's configuration; no network access in the package; a missing file fails start-up | A changed file needs a restart |
| **B) Also a stream factory or the document of #446** | Works with generated documents | Not in the issue; waits for #446 |

### Chosen Option: **A — File path, loaded once**

### Rationale

- The issue's configuration example sets `SpecificationPath`. Remote `$ref` has no resolver in the package, so it cannot be fetched (register row C4).

**Class:** Trivial
**Settled by:** issue #231, "Configuration" (`options.SpecificationPath = "openapi.json"`) and its task "Implement OpenAPI spec parser"

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | A specification that is missing, unreadable, invalid or uses a remote `$ref` fails start-up (`ValidateOnStart`); the middleware is never active without a loaded document | Settled | AGENTS.md section 3 (security and compliance gates fail closed) |
| C2 | A violation lists the JSON pointer and the schema keyword (`/items/0/name`, `required`), never the offending value or the keyword's argument text from the input | Settled | AGENTS.md section 3 (`EncinaError.Message` and request values never reach logs or responses); #3426 |
| C3 | Defaults `MaxRequestBodyBytes = 1 MiB`, `MaxErrors = 20`, `MaxSchemaDepth = 64`, `PatternTimeout = 100 ms`; a body over the limit, a deeper document and a pattern that times out are violations, never accepted unchecked | Dilemma (recommended, pending the maintainer) | numeric defaults; fail closed fixes the direction |
| C4 | The package contains no code that fetches a URL; `$ref` resolves inside the loaded document only | Settled | AGENTS.md section 3 (fail closed); the SSRF classification of outbound endpoints in `src/Encina/Validation/EndpointValidator.cs` (#852) shows the repository's default of denying remote targets |
| C5 | `pattern` is evaluated with a match timeout (register row C3) so a hostile input cannot hold a thread | Settled | AGENTS.md section 3 (a gate must not be a denial-of-service vector) |
| C6 | OpenAPI 3.0 constructs are normalized to 3.1 before evaluation: `nullable: true` becomes a `"null"` type, boolean `exclusiveMinimum` and `exclusiveMaximum` become numeric bounds, `example` and `x-` keywords are ignored | Settled | OpenAPI Specification 3.1 (full alignment with JSON Schema 2020-12 is what lets one engine serve both) |
| C7 | Error codes and categories (ADR-062), created with the category overload: `validation.openapi.schema_violation` and `validation.openapi.parameter_invalid` (`ErrorCategory.Validation`), `validation.openapi.operation_not_found`, `validation.openapi.unsupported_media_type` and `validation.openapi.body_too_large` (`ErrorCategory.InvalidRequest`), `validation.openapi.specification_unavailable` and `validation.openapi.evaluation_failed` (`ErrorCategory.Internal`, R1 and R4) | Dilemma (recommended, pending the maintainer) | public codes; `operation_not_found` is `InvalidRequest`, not `NotFound`, because the router, not the specification, decides what exists (ADR-062 R1) |
| C8 | The response is `application/problem+json` (RFC 9457) with status 400 and an `errors` extension keyed by JSON pointer; the status comes from the category (ADR-062), written by the middleware because `ToProblemDetails` carries no field list | Dilemma (recommended, pending the maintainer) | public response shape |
| C9 | Header and cookie parameters are not validated | Settled | Issue #231 (the options list body, query and path only) |
| C10 | Telemetry: `ActivitySource` and `Meter` named `Encina.Validation.OpenApi`; one span per validated request, counter `encina.validation.openapi.requests` (tags `result`, `operation` as the template, never the raw path), histogram `encina.validation.openapi.duration` in seconds; the tenant tag only when the request context carries a tenant (ADR-054 D4) | Settled | ADR-054 D3 and D4 (instrument naming and tenant rule) |
| C11 | EventIds 580-585 packed from the start of the family block: 580 specification loaded, 581 load failed, 582 request rejected, 583 no operation matched (non-strict, once per template), 584 body over the limit, 585 evaluation failed | Trivial | AGENTS.md section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C12 | The middleware requires `UseEncinaContext` before it to read the tenant and the correlation id, and `UseEncinaOpenApiValidation` throws when `UseEncinaContext` has not been called on the same `IApplicationBuilder` (a marker in `IApplicationBuilder.Properties`), so the tenant tag is never lost silently | Dilemma (recommended, pending the maintainer) | ordering contract |

---

## Implementation Phases

### Phase 1: Package Skeleton, Options and Document Loader

> **Goal**: The package, the pinned dependencies and a loader that returns a validated document (pull request S1).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Validation.OpenApi/Encina.Validation.OpenApi.csproj` (`FrameworkReference Microsoft.AspNetCore.App`; references `Encina`; `Microsoft.OpenApi` and the schema engine; `PublicApiAnalyzers`), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`; **modify** `Directory.Packages.props` (the two `PackageVersion` lines, versions read from NuGet at that time) and add the project to `Encina.slnx`.
2. **Create** `OpenApiValidationOptions.cs` (`SpecificationPath`, `ValidateRequestBody`, `ValidateQueryParameters`, `ValidatePathParameters`, `StrictMode`, the limits of register row C3) and `OpenApiDocumentLoader.cs` (reads the file, parses 3.0 and 3.1, rejects remote `$ref` and returns an `Either<EncinaError, OpenApiDocument>`; `validation.openapi.specification_unavailable`).
3. **Modify** `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj` (one `ProjectReference` each); **create** `.github/coverage-manifest/Encina.Validation.OpenApi.json` (through `mechanical-fixer`).
4. Tests (S1): loader unit tests (a 3.0 file, a 3.1 file, a missing file, invalid content, a remote `$ref`), options unit tests, guard tests; fixtures `TestData/spec-3.0.json` and `TestData/spec-3.1.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Issue #231 adds request validation against an OpenAPI
specification. Read docs/plans/openapi-request-validation-implementation-plan-231.md, Design Choices 2, 3 and 5 and
register rows C1-C4.

TASK:
1. Create the Encina.Validation.OpenApi project, pin the two new packages in Directory.Packages.props, add it to Encina.slnx.
2. Write OpenApiValidationOptions and OpenApiDocumentLoader (Either<EncinaError, OpenApiDocument>).

KEY RULES:
- Fail closed: a missing, invalid or remote-referencing specification is an error, never an empty document.
- No code in the package fetches a URL. XML docs; PublicAPI, slnx and manifest through mechanical-fixer.

REFERENCE FILES:
- src/Encina.AspNetCore/Encina.AspNetCore.csproj, src/Encina.AspNetCore/ApplicationBuilderExtensions.cs
- src/Encina/Results/EncinaErrors.cs (category overload)
```

</details>

---

### Phase 2: Schema Engine Adapter, Normalizer and Limits

> **Goal**: Evaluate a JSON value against an OpenAPI schema with bounded cost (pull request S2).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `OpenApi30SchemaNormalizer.cs` (register row C6), `OpenApiSchemaValidator.cs` (`IReadOnlyList<SchemaViolation> Validate(JsonNode instance, OpenApiSchema schema)`; applies the depth, error count and pattern timeout limits of register row C3; an engine exception becomes `validation.openapi.evaluation_failed`), `SchemaViolation.cs` (JSON pointer and keyword only) and `OpenApiValidationErrorCodes.cs` (the codes of register row C7).
2. **Modify** `PublicAPI.Unshipped.txt`, `tests/Encina.PropertyTests/Encina.PropertyTests.csproj` (one `ProjectReference`) and the manifest.
3. Tests (S2): normalizer unit tests per construct, validator unit tests per keyword and per limit, a property test (an instance built from a generated schema validates; one mutation of it does not), a guard test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 2 of 7. Depends on S1.

TASK:
1. Write the OpenAPI 3.0 to 3.1 normalizer and the bounded schema validator with the limits of register row C3.
2. Violations carry the JSON pointer and the keyword only (register row C2).

KEY RULES:
- Every limit failure is a violation; an engine exception is validation.openapi.evaluation_failed (Internal).
- Tests execute the real engine on real fixtures; no reflection-only tests.

REFERENCE FILES:
- src/Encina.Validation.OpenApi/ (phase 1)
```

</details>

---

### Phase 3: Operation Matching and Parameter Validation

> **Goal**: Find the operation and validate path and query parameters (pull request S3).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `OpenApiOperationMatcher.cs` (HTTP method plus route template to an operation; templates compared on segments, no regular expression over the raw path) and `OpenApiParameterValidator.cs` (path and query parameters: required, type and format coercion from strings, `style` and `explode` for arrays; an unknown query parameter is a violation in strict mode).
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest (through `mechanical-fixer`).
3. Tests (S3): matcher and parameter unit tests, a guard test, a property test (a template with `n` segments matches exactly the paths with `n` segments of the right literals).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 3 of 7. Depends on S2.

TASK:
1. Write the operation matcher (segment-wise) and the parameter validator for path and query parameters.
2. Strict mode rejects an unmatched operation and an unknown query parameter; non-strict lets them through.

KEY RULES:
- Never log or return the raw path or a parameter value; operation labels use the route template.

REFERENCE FILES:
- src/Encina.Validation.OpenApi/ (phases 1 and 2)
```

</details>

---

### Phase 4: Middleware, Problem Details and Registration

> **Goal**: The component an application adds, with registration completeness proven (pull request S4).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `OpenApiValidationMiddleware.cs` (matches, validates parameters, buffers the body up to `MaxRequestBodyBytes`, validates JSON content, rewinds the stream, and on a violation writes the response through `OpenApiProblemDetailsWriter.cs`), `OpenApiValidationOptionsValidator.cs`, `OpenApiValidationServiceCollectionExtensions.cs` (`AddEncinaOpenApiValidation(Action<OpenApiValidationOptions>? configure)`: options with `ValidateOnStart`, the loaded document as a singleton) and `OpenApiValidationApplicationBuilderExtensions.cs` (`UseEncinaOpenApiValidation`, with the ordering check of register row C12).
2. **Modify** `PublicAPI.Unshipped.txt`, the manifest and `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` (one `ProjectReference`).
3. Tests (S4): middleware unit tests, a DI test with `ValidateOnBuild` and `ValidateScopes`, guard tests for both extensions, and an integration test over a `TestServer` host (`Microsoft.AspNetCore.Mvc.Testing`) in `tests/Encina.IntegrationTests/Validation/OpenApi/` covering a valid request, a body violation, a parameter violation, an unmatched route in both modes and a body over the limit. These are not database tests: no container.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 4 of 7. Depends on S1-S3.
AGENTS.md section 3: registration completeness (every option type and dependency registered, proven by a DI test).

TASK:
1. Write the middleware, the problem details writer, the options validator and the two extension methods.
2. Prove registration with a ValidateOnBuild and ValidateScopes test; write the TestServer integration test.

KEY RULES:
- The body is buffered within a bound and rewound; a violation never echoes input; the response is RFC 9457.
- The status is derived from the ADR-062 category; never parse a code string to choose it.

REFERENCE FILES:
- src/Encina.AspNetCore/ApplicationBuilderExtensions.cs, ProblemDetailsExtensions.cs, EncinaContextMiddleware
- tests/Encina.IntegrationTests/Validation/ (folder layout)
```

</details>

---

### Phase 5: Observability and Logging

> **Goal**: Span, metrics, log messages and the registered EventId range (pull request S5).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationOpenApi = (580, 589)`; **create** `Diagnostics/OpenApiValidationDiagnostics.cs` (source, meter, counter and histogram of register row C10) and `Diagnostics/OpenApiValidationLog.cs` (`[LoggerMessage]` 580-585 of register row C11; template and operation id only).
2. **Modify** the middleware to use them, `EncinaEventIdAllocationTests.cs`, the `PublicAPI.Unshipped.txt` of `Encina` and both manifests (through `mechanical-fixer`).
3. Tests (S5): a `MeterListener` and `ActivityListener` test (tags, tenant tag only when known, the raw path absent), a log test (ids inside the range, the input absent).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 5 of 7. ADR-021 and ADR-054.

TASK:
1. Register ValidationOpenApi (580, 589) in EventIdRanges.cs first; write the diagnostics and the log class (580-585).
2. Instrument the middleware; add the tests.

KEY RULES:
- Pack ids sequentially; the tenant tag only when the tenant is known; never the raw path, a value or EncinaError.Message.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Security.Sanitization/Diagnostics/SanitizationDiagnostics.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 6: Generator from the Specification

> **Goal**: Compile-time validators for the DTOs of a specification (pull request S6; opens only after #227 S2 to S4 are on `main`).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina.Validation.Generators/`: `OpenApiValidatorGenerator.cs` (reads `AdditionalFiles` named `openapi.json` or by a `build_metadata`, parses the schema with a netstandard2.0 reader of the subset it supports) and `Emit/OpenApiRuleModelBuilder.cs` (schema keywords to the rule model of #227; `operationId` plus the `[GeneratedValidator("CreateUser")]` name in the issue map to a DTO; an unmappable schema is a compile error, never a silent skip, as in #227 register row C2).
2. **Modify** the manifest of `Encina.Validation.Generators` (through `mechanical-fixer`).
3. Tests (S6): two snapshots, a unit test of the diagnostics, an equivalence test (the generated validator and the runtime engine of S2 agree on a corpus of valid and invalid bodies for the same schema).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 6 of 7. Requires the generator
project and emitters of issue #227 (S2 to S4) on main; see docs/plans/validation-source-generators-implementation-plan-227.md.

TASK:
1. Add an OpenAPI-driven generator to Encina.Validation.Generators that feeds the rule model of #227.
2. Snapshots, diagnostics test and the equivalence test against the runtime engine.

KEY RULES:
- Reflection-free output; an unsupported schema construct is a compile error; no value in any message.

REFERENCE FILES:
- src/Encina.Validation.Generators/ (issue #227), src/Encina.Validation.OpenApi/OpenApiSchemaValidator.cs
```

</details>

---

### Phase 7: Benchmark, Documentation and Finalization

> **Goal**: Performance evidence, usage guide, ADR and changelog (pull request S7).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.BenchmarkTests/Encina.Benchmarks/OpenApiValidationBenchmarks.cs` (the middleware path on a small and a large body, `BenchmarkSwitcher` per `AGENTS.md` section 9) and a load-test justification `.md` (the document is immutable after start-up and the middleware holds no shared state).
2. **Create** through `docs-writer`: `src/Encina.Validation.OpenApi/README.md`, `docs/features/openapi-request-validation.md`, an ADR for the integration point and the engine (number assigned when the ADR pull request opens) and its index line; **modify** `docs/INVENTORY.md`.
3. **Create** `changelog.d/231-openapi-request-validation.added.md` (through `mechanical-fixer`).
4. Build verification: `dotnet build --configuration Release` with zero warnings; per-flag coverage of the touched files against the manifest targets below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 7 of 7.

TASK:
1. Write the benchmark and the load-test justification; document the package, the options, the limits and the codes.
2. Add the ADR, the changelog fragment and run the build and the per-flag coverage measurement.

KEY RULES:
- Never type coverage or benchmark figures by hand; cite cov: markers (SPEC-001). Pages through docs-writer;
  fragments through mechanical-fixer. Sibling plans are referenced by issue number only.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| OpenAPI Specification 3.0 and 3.1 | Schema Object; 3.1 aligns with JSON Schema 2020-12 | Register row C6 |
| JSON Schema 2020-12 (Core and Validation) | Keywords, `$ref`, `format` as annotation | Engine behavior |
| RFC 9457 | Problem Details for HTTP APIs, extension members | Register row C8 |
| RFC 6901 | JSON Pointer | Violation locations |
| OpenAPI Specification and JSON Schema Validation (both linked in the issue) | The contract and the validation vocabulary | Source of the rules the middleware enforces |
| ADR-054 D3, D4 | One source and meter per package; tenant tag when known | Register row C10 |
| ADR-062 | Error category contract | Register row C7 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `UseEncinaContext`, `EncinaContextMiddleware` | `src/Encina.AspNetCore/ApplicationBuilderExtensions.cs` | Correlation and tenant for tags and logs |
| `ProblemDetailsExtensions` | `src/Encina.AspNetCore/ProblemDetailsExtensions.cs` | Status mapping conventions |
| `EncinaErrors.Create` with a category | `src/Encina/Results/EncinaErrors.cs` | Typed errors |
| Rule model and emitters | issue #227 (`src/Encina.Validation.Generators/`) | Phase 6 |
| `IGeneratedRequestValidator<T>` | issue #227 and #230 | Generated output |
| `Microsoft.AspNetCore.Mvc.Testing` | central package version | `TestServer` integration test |
| `SanitizationDiagnostics` | `src/Encina.Security.Sanitization/Diagnostics/` | Meter and activity pattern |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Validation.OpenApi` (`ValidationOpenApi`, new, inside the block 550-599 assigned to the validation-1 family) | 580-589 | 580-585 used (six messages); 586-589 free; #230 owns 550-559, #227 560-569, #232 590-599 |

### Estimated File Count

Every count includes tests, fixtures, coverage manifest edits, PublicAPI files, test project references, the README and the changelog fragment. No pull request uses a type created by a later one.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Package, options, loader | 5 (csproj, PublicAPI Shipped and Unshipped, options, loader) | 1 (`Directory.Packages.props`) | 5 (loader unit, options unit, guard, 2 spec fixtures) | 2 (unit and guard test csproj) | 1 | 1 (`Encina.slnx`) | 15 | ~700 |
| S2 | Normalizer, schema validator, limits | 4 | 1 (PublicAPI) | 4 (normalizer, validator, property, guard) | 1 (`Encina.PropertyTests.csproj`) | 1 | 0 | 11 | ~900 |
| S3 | Matcher, parameter validation | 2 | 1 (PublicAPI) | 4 (matcher unit, parameter unit, property, guard) | 0 | 1 | 0 | 8 | ~650 |
| S4 | Middleware, writer, DI | 5 | 1 (PublicAPI) | 5 (middleware, DI, 2 guard, integration) | 1 (`Encina.IntegrationTests.csproj`) | 1 | 0 | 13 | ~950 |
| S5 | Observability, logging, EventIds | 2 | 3 (`EventIdRanges.cs`, middleware, PublicAPI of `Encina`) | 2 (metrics and activity, log) | 1 (`EncinaEventIdAllocationTests`) | 2 | 0 | 10 | ~550 |
| S6 | Generator from the specification (needs #227 S2 to S4) | 2 | 0 | 4 (2 snapshots, diagnostics unit, equivalence) | 0 | 1 | 0 | 7 | ~700 |
| S7 | Benchmark, docs, ADR, changelog | 0 | 0 | 2 (benchmark, load `.md`) | 0 | 0 | 6 (package README, feature page, `docs/INVENTORY.md`, ADR, ADR index, changelog fragment) | 8 | ~450 |
| **Total** | | **20** | **7** | **26** | **5** | **7** | **7** | **72** | **~4,900** |

The columns sum to the total (20 + 7 + 26 + 5 + 7 + 7 = 72) and every statement of the total in this plan is 72 files; the rows sum to the same (15 + 11 + 8 + 13 + 10 + 7 + 8). The largest pull request is 15 files; none exceeds 1,000 lines. S6 waits for #227; S1 to S5 and S7 do not.

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifest by the pull request that adds the file:

| File | Unit | Guard | Property | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `OpenApiDocumentLoader.cs` | 90 | 85 | 0 | 0 | Every input state (3.0, 3.1, missing, invalid, remote `$ref`) is a unit case; path and options arguments are guard-tested |
| `OpenApi30SchemaNormalizer.cs`, `OpenApiSchemaValidator.cs` | 90 | 85 | 60 | 0 | Each keyword and limit has a unit case; the generated-schema property covers the valid and mutated instances |
| `OpenApiOperationMatcher.cs`, `OpenApiParameterValidator.cs` | 90 | 85 | 60 | 0 | Template and parameter styles are unit cases; the segment property pins the matching rule |
| `OpenApiValidationMiddleware.cs`, `OpenApiProblemDetailsWriter.cs` | 85 | 85 | 0 | 70 | Unit tests drive each branch with a fake `HttpContext`; the `TestServer` test runs the real pipeline |
| `OpenApiValidationServiceCollectionExtensions.cs`, `...ApplicationBuilderExtensions.cs`, `...OptionsValidator.cs` | 90 | 90 | 0 | 0 | Registration, the ordering check and option rules are tested by the DI test; null checks are guard-tested |
| `OpenApiValidationOptions.cs`, `SchemaViolation.cs`, `OpenApiValidationErrorCodes.cs` | 95 | 0 | 0 | 0 | Plain data and constants with no argument validation |
| `Diagnostics/*.cs` | 90 | 0 | 0 | 0 | Listener and log capture tests cover them; internal static types have no guard surface |
| `OpenApiValidatorGenerator.cs`, `Emit/OpenApiRuleModelBuilder.cs` | 85 | 0 | 0 | 0 | Driven through the generator harness of #227; internal types and Roslyn structs have no guard surface |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #231 - validation of requests against an OpenAPI specification.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation gates fail closed; errors never echo input; every new error code carries an ErrorCategory (ADR-062).
- The maintainer's answers to Design Choices 1-4 of docs/plans/openapi-request-validation-implementation-plan-231.md
  decide the integration point, the engine, strict mode and the generator scope.

IMPLEMENTATION OVERVIEW:
1. Encina.Validation.OpenApi: options, document loader (3.0 and 3.1, local $ref only, fail start-up when invalid).
2. Schema engine adapter with normalizer and bounded limits; operation matcher; parameter validator.
3. Middleware, RFC 9457 writer, AddEncinaOpenApiValidation and UseEncinaOpenApiValidation, DI test.
4. Span, counter, histogram, EventIds 580-585 in ValidationOpenApi (580-589), registered first.
5. Generator from the specification (after #227); benchmark, docs, ADR and changelog.
Seven pull requests (S1 to S7), see the Estimated File Count table.

KEY PATTERNS:
- JSON pointer and keyword only in violations; bounded body, depth, errors and pattern time.
- DI test with ValidateOnBuild and ValidateScopes; TestServer integration test; zero warnings.

REFERENCE FILES:
- src/Encina.AspNetCore/ApplicationBuilderExtensions.cs, ProblemDetailsExtensions.cs
- docs/plans/openapi-request-validation-implementation-plan-231.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | The document is loaded once and immutable; validation is per request |
| 2 | OpenTelemetry | ✅ Phase 5 | Span, counter and histogram on `Encina.Validation.OpenApi`; tenant tag only when known (ADR-054 D4, register row C10); the pipeline-level duration of validation is #726 |
| 3 | Structured Logging | ✅ Phase 5 | `OpenApiValidationLog` EventIds 580-585 in the new range 580-589; templates and operation ids only |
| 4 | Health Checks | ❌ N/A | The specification is loaded at start-up and fails the application when invalid; there is no runtime dependency to check |
| 5 | Validation | ✅ All phases | This is contract validation; the three providers of `AGENTS.md` section 5 receive the generated validators (Phase 6), the middleware is provider-independent |
| 6 | Resilience | ❌ N/A | In-process; the limits of register row C3 bound the cost; no external call |
| 7 | Distributed Locks | ❌ N/A | No shared mutable state |
| 8 | Transactions | ❌ N/A | Validation precedes the handler |
| 9 | Idempotency | ❌ N/A | Stateless and repeatable |
| 10 | Multi-Tenancy | ⏭️ | The tenant is a tag and a log field when known; tenant-specific specifications are a follow-up shared with the family (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`) |
| 11 | Module Isolation | ⏭️ | Module-scoped specifications: the same follow-up issue |
| 12 | Audit Trail | ⏭️ | Auditing of contract violations: a follow-up (draft: `artifacts/issues/validation-failure-audit.md`) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-062 error category contract | ✅ Merged | Codes of register row C7 |
| `Encina.AspNetCore` context middleware | ✅ On `main` | Register row C12 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Generator project and emitters | #227 | Blocking for S6 only |
| `IRequestValidator<T>` and the runner | #230 | Needed by S6 through #227 |
| OpenAPI and Swagger integration | #446 | May later offer a document provider; not required |
| Code-less validation `Left` never becomes a 400 | #3415 | The pipeline-level `Left` is #3415's; the middleware writes its own response |

---

## Next Steps

1. Link this plan from #231 and ask the maintainer to answer Design Choices 1 to 4 and the Dilemma rows C3, C7, C8 and C12 of the register when work starts.
2. Open the follow-up issues when the first pull request opens (shared with the family): tenant and module scoped specifications (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`) and auditing of validation failures (draft: `artifacts/issues/validation-failure-audit.md`).
3. Implement S1 to S7 as the pull requests of the Estimated File Count split, `Fixes #231` on S7.

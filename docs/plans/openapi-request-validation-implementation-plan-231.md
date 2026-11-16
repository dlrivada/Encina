# Implementation Plan: Schema and Contract Validation with OpenAPI (`Encina.Validation.OpenApi`)

> **Issue**: [#231](https://github.com/dlrivada/Encina/issues/231)
> **Type**: Feature
> **Related**: #227 (owns the generator project and the rule model S6 reuses), #230 (owns `IRequestValidator<T>`), #446 (OpenAPI and Swagger integration, planned apart), #259 (schema registry, unrelated transport), #234 and #236 (planned apart), #726 (central validation instrumentation), #3415 (a code-less validation `Left` never becomes a 400), #3426 (which error text a client may see), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: High (a new ASP.NET Core package, two or three new third-party dependencies, an OpenAPI 3.0 and 3.1 schema engine, a marker change in `Encina.AspNetCore` and a generator extension)
> **Estimated Scope**: ~1,900 lines of production code + ~3,000 lines of tests + ~400 lines of documentation, PublicAPI files and manifests (~5,300 changed lines in total), in eight pull requests (S0 to S7) of at most 15 files and 1,000 lines each

---

## Summary

### What exists today (read on 2026-11-16, main `8e01577ef`)

- No OpenAPI code exists in `src`. `Directory.Packages.props` has no `Microsoft.OpenApi`, no `Microsoft.AspNetCore.OpenApi` and no JSON Schema package (grep of the file); it does have `YamlDotNet`, `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.AspNetCore.TestHost` (10.0.12). `Encina.AspNetCore` references the shared framework (`FrameworkReference Microsoft.AspNetCore.App`) and has one middleware, `EncinaContextMiddleware`, added by `UseEncinaContext` (`ApplicationBuilderExtensions.cs:56`, which is only `app.UseMiddleware<EncinaContextMiddleware>()`: it sets nothing in `IApplicationBuilder.Properties`, verified by grep), and `ProblemDetailsExtensions` (`ToProblemDetails`, `ToActionResult`) which maps validation codes to 400 by prefix (`validation.*`) until ADR-062 moves the mapping to the category.
- Validation today works on the already bound .NET object (`ValidationPipelineBehavior<TRequest, TResponse>` and the three providers), after model binding; nothing validates the raw HTTP body, the query string or the route values against a contract, and nothing reads a specification.
- The orchestrator's invalid-request `Left` is code-less (`ValidationOrchestrator.cs:68-72`); a code-less error never becomes a 400 (#3415).
- Not in `src`: `AddEncinaOpenApiValidation`, `OpenApiValidationOptions`, `[GeneratedValidator]` (the issue's name; #227 names its attribute `[GenerateValidator]`), any schema validator, any package named `Encina.Validation.OpenApi`, any marker that tells a later middleware that `UseEncinaContext` ran. #446 (open, v0.18.0) plans `Encina.OpenApi` to produce documents; this plan consumes a specification file and does not depend on it.

### What this plan delivers

A new opt-in package `Encina.Validation.OpenApi` with a runtime request validator and, in its last implementation pull request, a compile-time generator. At start-up it loads one specification file (OpenAPI 3.0 or 3.1, local `$ref` only) and fails the application when the file is missing, invalid or uses a construct the package does not support. A middleware, added by `UseEncinaOpenApiValidation`, finds the operation of each request (method and route template), validates the path and query parameters and the JSON body against the operation's schemas with a JSON Schema 2020-12 engine, and answers an RFC 9457 `application/problem+json` response listing violations by JSON pointer and keyword, never the values. Limits (body size, depth, error count, regular expression time) are bounded and fail closed. The generator (gated on #227) reads the same specification as an `AdditionalFile` and emits `IGeneratedRequestValidator<T>` classes for the matching DTOs, so the three validation providers can validate the bound object by the same contract. Nothing changes for an application that does not call the registration or the middleware.

- Affected packages: `Encina.Validation.OpenApi` (new), `Encina.AspNetCore` (S0: a public check that `UseEncinaContext` ran, which sets a marker in `IApplicationBuilder.Properties`), `Encina.Validation.Generators` (S6), `Encina` (EventIds only).
- Provider category: Validation (3) for the generated validators (S6, through the contract of #230 and #227, the same result under each provider); the runtime middleware sits before the pipeline and is provider-independent.
- Dependencies added: see Research, "New dependencies": the package names, licences and target frameworks verified on 2026-11-16 against the NuGet nuspec files, and what stays to verify in the S1 spike (AOT and trimming, the JSON Schema engine).

---

## Design Choices

<details>
<summary><strong>1. Where the runtime validation runs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A middleware that matches the operation, validates parameters and the buffered body and short-circuits with a problem details response** | One component for MVC and minimal APIs; runs before model binding, so the raw JSON (unknown properties, wrong types) is seen; no change in handlers | Buffers the request body (bounded by a limit); needs `UseRouting` information to match templates |
| **B) An `IEndpointFilter` for minimal APIs and an MVC action filter** | Endpoint-scoped; the bound route data is available | Two implementations; the body is already bound, so type mismatches become model binding errors first |
| **C) An `IValidationProvider` that serializes the bound object and validates it** | Sits in the Encina pipeline | The bound object has lost unknown properties and coerced types, so the contract check is weaker; re-serialization costs |

### Chosen Option: **A — Middleware** (recommended, pending the maintainer)

### Rationale

- The issue's goal is "validate incoming requests match the documented schema" and "catch contract violations early in the pipeline"; only A sees the request as the client sent it. The compile-time path (S6) covers the bound object for the three providers. B wins when the application wants per-endpoint opt-in; C wins when the specification is only available as .NET types.
- The acceptance criterion "integration with ASP.NET Core model binding" is met by A as validation before binding: the middleware leaves the request stream rewound so model binding sees the same body. The criterion would read as "validate the bound model" under C.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Schema engine</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Microsoft.OpenApi` to parse and a JSON Schema 2020-12 library to evaluate, after the normalization of register row C6 if the parser still needs it** | Complete keyword coverage (`allOf`, `oneOf`, `anyOf`, `not`, `format`, `pattern`, `$ref`); maintained by others; the same engine for 3.0 and 3.1 | Two or three new dependencies; the candidate engine `JsonSchema.Net` 9.7.0 ships under `OSMFEULA.txt`, not an SPDX licence (see Research); AOT and trimming support unverified |
| **B) A minimal evaluator written over the `Microsoft.OpenApi` object model** | One new dependency (MIT); full control of limits and messages | Re-implements keywords; every missed keyword is a silent accept |

### Chosen Option: **A — Parser plus a JSON Schema engine** (recommended, pending the maintainer)

### Rationale

- A missed keyword in B is a fail-open defect in a validation gate; A moves that risk to a maintained library, with the limits of register rows C3 and C5 applied around it. B wins when the licence, the dependency footprint or the AOT support of the candidate engines is unacceptable; the licence reading of `OSMFEULA.txt` is the maintainer's, and the engine candidates are compared in the S1 spike before the dependency is pinned.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. A request that matches no operation, or an unsupported construct</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `StrictMode = true` (default) rejects a request with no matching operation and an unknown query parameter; `StrictMode = false` lets them through and logs EventId 583 (register row C22)** | Fail closed by default; the opt-out is explicit and logged | A route the specification forgot answers an error until documented |
| **B) Always let an unmatched request through** | Never blocks an undocumented endpoint | The contract is not enforced where it is incomplete |

### Chosen Option: **A — Strict by default with a logged opt-out** (recommended, pending the maintainer)

### Rationale

- AGENTS.md section 3: gates deny on missing context and the only opt-out is explicit and logged. The issue's example sets `StrictMode = true`. B wins when the specification covers only a subset of the application on purpose. Rejecting an unknown query parameter is a data-compatibility choice: a client that sends an extra parameter fails in strict mode.

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

- The acceptance criteria and the issue's task list name the generator. S0 to S5 and S7 do not depend on it. B wins when #227 slips. The issue's example is an `AbstractValidator<CreateUserRequest>` with `[GeneratedValidator("CreateUser")]`; this plan emits `IGeneratedRequestValidator<T>` (the contract of #227 and #230) instead of a FluentValidation class and keeps the attribute name only as the mapping key, so the acceptance text is reinterpreted, which the maintainer confirms by answering this Dilemma.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Specification source and parsing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A file path (`SpecificationPath`), read once at start-up, local `$ref` only** | The issue's configuration; no network access in the package; a missing file fails start-up | A changed file needs a restart |
| **B) Also a stream factory or the document of #446** | Works with generated documents | Not in the issue; waits for #446 |

### Chosen Option: **A — File path, loaded once**

### Rationale

- The issue's configuration example sets `SpecificationPath`. Remote `$ref` has no resolver in the package, so it cannot be fetched (register row C4). Which file formats are read is register row C13.

**Class:** Trivial
**Settled by:** issue #231, "Configuration" (`options.SpecificationPath = "openapi.json"`) and its task "Implement OpenAPI spec parser"

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | A specification that is missing, unreadable, invalid or uses a remote `$ref` fails start-up (`ValidateOnStart`); the middleware is never active without a loaded document | Settled | AGENTS.md section 3 (security and compliance gates fail closed) |
| C2 | A violation lists the JSON pointer and the schema keyword (`/items/0/name`, `required`), never the offending value or the keyword's argument text from the input | Settled | AGENTS.md section 3 (`EncinaError.Message` and request values never reach logs or responses); #3426 |
| C3 | Defaults `MaxRequestBodyBytes = 1 MiB`, `MaxErrors = 20`, `MaxSchemaDepth = 64`, `PatternTimeout = 100 ms`; a body over the limit, a deeper document and a pattern that times out are violations, never accepted unchecked. Strict mode also rejects an unknown query parameter (Design Choice 3) | Dilemma (recommended, pending the maintainer) | numeric defaults; fail closed fixes the direction |
| C4 | The package contains no code that fetches a URL; `$ref` resolves inside the loaded document only | Settled | AGENTS.md section 3 (fail closed); the SSRF classification of outbound endpoints in `src/Encina/Validation/EndpointValidator.cs` (#852) shows the repository's default of denying remote targets |
| C5 | `pattern` is evaluated with a match timeout (register row C3) so a hostile input cannot hold a thread | Settled | AGENTS.md section 3 (a gate must not be a denial-of-service vector) |
| C6 | OpenAPI 3.0 constructs are normalized to 3.1 semantics before evaluation: `nullable: true` becomes a null type, boolean `exclusiveMinimum` and `exclusiveMaximum` become numeric bounds, `example` and `x-` keywords are ignored. Whether the parser already does this is unverified: current `Microsoft.OpenApi` (3.x) models types as flags (`JsonSchemaType.Null`) and AGENTS.md section 1 records that OpenAPI 3.1 removed `OpenApiSchema.Nullable`, so the normalizer may reduce to a check; the S1 spike settles it. Option B: support only 3.1 semantics and reject a 3.0 document | Dilemma (recommended, pending the maintainer): normalize 3.0 | both options keep one engine; B wins when the parser's own 3.0 handling is complete or 3.0 is out of scope |
| C7 | Error codes and ADR-062 categories (category overload of `EncinaErrors.Create`): `validation.openapi.schema_violation` and `validation.openapi.parameter_invalid` (`ErrorCategory.Validation`, R3), `validation.openapi.unsupported_media_type` and `validation.openapi.body_too_large` (`ErrorCategory.InvalidRequest`), `validation.openapi.specification_unavailable` and `validation.openapi.evaluation_failed` (`ErrorCategory.Internal`, R4); `validation.openapi.operation_not_found` is decided with the status in row C14 | Dilemma (recommended, pending the maintainer) | public codes; categories follow the producer rules R3 and R4 of ADR-062 |
| C8 | The response is `application/problem+json` (RFC 9457) with an `errors` extension keyed by JSON pointer, written by the middleware because `ToProblemDetails` carries no field list; its status is row C14 | Dilemma (recommended, pending the maintainer) | public response shape |
| C9 | Header and cookie parameters are not validated | Settled | Issue #231 (the options list body, query and path only) |
| C10 | Telemetry: `ActivitySource` and `Meter` named `Encina.Validation.OpenApi`; one span per validated request, counter `encina.validation.openapi.requests` (tags `result`, `operation` as the template, never the raw path), histogram `encina.validation.openapi.duration` in seconds; the tenant tag only when the request context carries a tenant (ADR-054 D4); the plan builds no tenant-qualified id, so the reserved `default` segment does not apply | Settled | ADR-054 D3 and D4 (instrument naming and tenant rule) |
| C11 | EventIds 580-585 packed from the start of the family block: 580 specification loaded (lists the disabled checks of row C21), 581 load failed, 582 request rejected, 583 no operation matched (non-strict, bounded, row C22), 584 body over the limit, 585 evaluation failed | Trivial | AGENTS.md section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C12 | The middleware requires `UseEncinaContext` before it to read the tenant and the correlation id; `UseEncinaOpenApiValidation` throws when `app.IsEncinaContextRegistered()` is false. S0 adds that public method and the marker `UseEncinaContext` sets in `IApplicationBuilder.Properties` to `Encina.AspNetCore` (nothing exists today); the tenant tag is never lost silently. Option B: no ordering check, the tenant tag is simply absent | Dilemma (recommended, pending the maintainer) | ordering contract; B wins when the middleware should work without `Encina.AspNetCore` context |
| C13 | Specification formats: JSON only. Option B: JSON and YAML through the separate `Microsoft.OpenApi.YamlReader` package (one more dependency, not part of `Microsoft.OpenApi` 3.x) | Dilemma (recommended, pending the maintainer): JSON only | pay-for-what-you-use; B wins when the applications' specifications are YAML files |
| C14 | The response status per case: schema and parameter violations 400; `body_too_large` 413 and `unsupported_media_type` 415 (RFC 9110); an unmatched operation in strict mode 404, with `operation_not_found` as `NotFound` because the request names the route (ADR-062 R1). ADR-062 has no 413 or 415 category, so the middleware owns those statuses and ADR-062 gains no row. Option B: 400 for every case (the table of ADR-062, `InvalidRequest` for the three codes). Option C: 400 for violations, 413 and 415, and 400 for an unmatched route | Dilemma (recommended, pending the maintainer): option A | the statuses are public behavior; B wins when clients treat every contract failure alike, C when the route table must not leak |
| C15 | `format` (`email`, `uuid`, `date-time`, `date`, `uri`, `ipv4`, `ipv6`) is asserted, not annotated, for the formats the engine implements; a `format` the engine does not know fails start-up in strict mode. Option B: annotation only (the JSON Schema 2020-12 default), which accepts any value of a formatted field | Dilemma (recommended, pending the maintainer): assert | the issue wants format validation and B fails open |
| C16 | Unknown body properties follow the schema: `additionalProperties` absent means allowed (JSON Schema and OpenAPI default); `additionalProperties: false` rejects. Option B: strict mode treats an absent `additionalProperties` as `false` | Dilemma (recommended, pending the maintainer): follow the schema | B wins when the contract is meant to be closed; it rejects clients that send extra fields |
| C17 | Body rules: a `requestBody` declared `required` with an empty body is a violation; a body sent to an operation that declares no `requestBody` is a violation in strict mode; a `Content-Type` that is not `application/json` or a `+json` type declared for the operation is `unsupported_media_type`; an unparsable JSON body is a violation `type` at the root. Option B: a body is ignored when the operation declares none | Dilemma (recommended, pending the maintainer) | public behavior; B wins for clients that always send a body |
| C18 | Base path: the operation is matched against `Request.Path` relative to `PathBase`; the specification's `servers[].url` base paths are ignored. Option B: the first `servers[].url` base path is stripped before matching | Dilemma (recommended, pending the maintainer): ignore `servers` | B wins when the application is mounted under the path the specification declares and has no `PathBase` |
| C19 | The body is read only when the operation declares a `requestBody` and `ValidateRequestBody` is true, up to `MaxRequestBodyBytes + 1` bytes whether or not `Content-Length` is present (chunked bodies included); one byte over is `body_too_large`. Option B: reject a request with no `Content-Length` | Dilemma (recommended, pending the maintainer): bounded read | both keep the gate from buffering without bound (AGENTS.md section 3); B is simpler and breaks chunked clients |
| C20 | A construct the package does not support in the loaded document (a parameter `style` and `explode` pair other than the supported `simple` for path parameters and `form` for query parameters, a parameter that uses `content`, a `discriminator` mapping) fails start-up with `specification_unavailable` and names the construct. Option B: reject, at request time, the requests that use the construct. Option C: ignore it, which fails open and does not comply with row C1 | Dilemma (recommended, pending the maintainer): fail start-up | fail closed is fixed; the moment (start-up or request) is the choice |
| C21 | `ValidateRequestBody`, `ValidateQueryParameters` and `ValidatePathParameters` set to false skip that check for every matched operation; the start-up message (EventId 580) lists the disabled checks. Option B: no per-part switches | Dilemma (recommended, pending the maintainer) | the opt-outs are explicit and logged (AGENTS.md section 3); B removes the switches the issue lists |
| C22 | EventId 583 in non-strict mode is logged once per route pattern of the matched endpoint, from a thread-safe set bounded by `MaxTrackedTemplates` (default 1000, after which it stops logging and the counter keeps counting); an unmatched request has no operation template, so the key is the endpoint route pattern, or the method with the path template when no endpoint matched. Option B: log every unmatched request at Debug | Dilemma (recommended, pending the maintainer): bounded set | log volume must not follow the client; B holds no state |
| C23 | Query and path parameter values are coerced from strings with the invariant culture according to the schema type; a value that does not coerce is `parameter_invalid` | Settled | OpenAPI Specification 3.1, Parameter Object (`style` and `explode` defaults and the schema type of the parameter) |

---

## Implementation Phases

### Phase 0 (S0): Context Marker in `Encina.AspNetCore`

> **Goal**: Let a later middleware check that `UseEncinaContext` ran (register row C12); a pull request of its own because it changes another package.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.AspNetCore/ApplicationBuilderExtensions.cs`: `UseEncinaContext` sets `app.Properties["Encina.AspNetCore.ContextMiddleware"] = true`; add `public static bool IsEncinaContextRegistered(this IApplicationBuilder app)`; **modify** `src/Encina.AspNetCore/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.AspNetCore.json` (existing targets stay; through `mechanical-fixer`).
2. Tests (S0): **modify** `tests/Encina.UnitTests/AspNetCore/ApplicationBuilderExtensionsTests.cs` (the marker is set; the check is false before and true after); **create** a guard test for the new method (null `app`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 0</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 0 of 8 (S0).

TASK:
1. Make UseEncinaContext record that it ran and add IsEncinaContextRegistered(IApplicationBuilder).
2. Test both states and the null argument.

KEY RULES:
- No other behavior change; keep the manifest targets; PublicAPI through mechanical-fixer.

REFERENCE FILES:
- src/Encina.AspNetCore/ApplicationBuilderExtensions.cs, tests/Encina.UnitTests/AspNetCore/ApplicationBuilderExtensionsTests.cs
```

</details>

---

### Phase 1 (S1): Package Skeleton, Options and Document Loader

> **Goal**: The package, the pinned dependencies and a loader that returns a validated document.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Validation.OpenApi/Encina.Validation.OpenApi.csproj` (`FrameworkReference Microsoft.AspNetCore.App`; `ProjectReference` to `Encina` and to `Encina.AspNetCore` (the context middleware and `IsEncinaContextRegistered`); `Microsoft.OpenApi` and the schema engine of the S1 spike; `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.GuardTests` and `Encina.PropertyTests`; `PublicApiAnalyzers`), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`; **modify** `Directory.Packages.props` (the `PackageVersion` lines of the packages the spike confirms, versions read from NuGet at that time) and add the project to `Encina.slnx`.
2. **Create** `OpenApiValidationOptions.cs` (`SpecificationPath`, `ValidateRequestBody`, `ValidateQueryParameters`, `ValidatePathParameters`, `StrictMode`, `MaxTrackedTemplates`, the limits of register row C3) and `OpenApiDocumentLoader.cs` (internal; reads the file, parses 3.0 and 3.1, rejects remote `$ref` and unsupported constructs (row C20) and returns an `Either<EncinaError, OpenApiDocument>`; `validation.openapi.specification_unavailable`).
3. **Modify** `tests/Encina.UnitTests/Encina.UnitTests.csproj` (one `ProjectReference` and a `Content` glob for `Validation\OpenApi\TestData\*.json`) and `tests/Encina.GuardTests/Encina.GuardTests.csproj` (one `ProjectReference`); **create** `.github/coverage-manifest/Encina.Validation.OpenApi.json` (through `mechanical-fixer`).
4. Tests (S1): loader unit tests (a 3.0 file, a 3.1 file, a missing file, invalid content, a remote `$ref`, an unsupported construct), options unit tests, guard tests; fixtures `TestData/spec-3.0.json` and `TestData/spec-3.1.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Issue #231 adds request validation against an OpenAPI
specification. Read docs/plans/openapi-request-validation-implementation-plan-231.md, Design Choices 2, 3 and 5,
register rows C1-C6, C13 and C20, and the Research section "New dependencies".

TASK:
1. Create the Encina.Validation.OpenApi project, pin the packages the dependency spike confirmed in Directory.Packages.props, add it to Encina.slnx.
2. Write OpenApiValidationOptions and OpenApiDocumentLoader (Either<EncinaError, OpenApiDocument>).

KEY RULES:
- Fail closed: a missing, invalid, unsupported or remote-referencing specification is an error, never an empty document.
- No code in the package fetches a URL. XML docs; PublicAPI, slnx and manifest through mechanical-fixer.

REFERENCE FILES:
- src/Encina.AspNetCore/Encina.AspNetCore.csproj, src/Encina.AspNetCore/ApplicationBuilderExtensions.cs
- src/Encina/Results/EncinaErrors.cs (category overload)
```

</details>

---

### Phase 2 (S2): Schema Engine Adapter, Normalizer and Limits

> **Goal**: Evaluate a JSON value against an OpenAPI schema with bounded cost.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `OpenApi30SchemaNormalizer.cs` (register row C6), `OpenApiSchemaValidator.cs` (internal; `IReadOnlyList<SchemaViolation> Validate(JsonNode instance, OpenApiSchema schema)`; applies the depth, error count and pattern timeout limits of register row C3 and the `format` rule of row C15; an engine exception becomes `validation.openapi.evaluation_failed`), `SchemaViolation.cs` (JSON pointer and keyword only) and `OpenApiValidationErrorCodes.cs` (the codes of register row C7).
2. **Modify** `PublicAPI.Unshipped.txt`, `tests/Encina.PropertyTests/Encina.PropertyTests.csproj` (one `ProjectReference`) and the manifest.
3. Tests (S2): normalizer unit tests per construct, validator unit tests per keyword and per limit, a property test (an instance built from a generated schema validates; one mutation of it does not), a guard test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 2 of 8. Depends on S1.

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

### Phase 3 (S3): Operation Matching and Parameter Validation

> **Goal**: Find the operation and validate path and query parameters.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `OpenApiOperationMatcher.cs` (internal; HTTP method plus route template to an operation, relative to `PathBase` (row C18); templates compared on segments, no regular expression over the raw path) and `OpenApiParameterValidator.cs` (internal; path and query parameters: required, type and format coercion from strings (row C23), `style` and `explode` for arrays; an unknown query parameter is a violation in strict mode).
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest (through `mechanical-fixer`).
3. Tests (S3): matcher and parameter unit tests, a guard test, a property test (a template with `n` segments matches exactly the paths with `n` segments of the right literals).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 3 of 8. Depends on S2.

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

### Phase 4 (S4): Middleware, Problem Details and Registration

> **Goal**: The component an application adds, with registration completeness and the public contract proven.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `OpenApiValidationMiddleware.cs` (matches, validates parameters, reads the body within the bound of row C19, validates JSON content under the rules of row C17, rewinds the stream, and on a violation writes the response of rows C8 and C14 through `OpenApiProblemDetailsWriter.cs`), `OpenApiValidationOptionsValidator.cs`, `OpenApiValidationServiceCollectionExtensions.cs` (`AddEncinaOpenApiValidation(Action<OpenApiValidationOptions>? configure)`: options with `ValidateOnStart`, the loaded document as a singleton) and `OpenApiValidationApplicationBuilderExtensions.cs` (`UseEncinaOpenApiValidation`, with the ordering check of register row C12 through `IsEncinaContextRegistered`, delivered by S0).
2. **Modify** `PublicAPI.Unshipped.txt`, the manifest, `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` (one `ProjectReference`; the project already references `Microsoft.AspNetCore.TestHost`, `Mvc.Testing` is not needed) and `tests/Encina.ContractTests/Encina.ContractTests.csproj` (one `ProjectReference`).
3. Tests (S4): middleware unit tests, a DI test with `ValidateOnBuild` and `ValidateScopes`, guard tests for both extensions, `tests/Encina.ContractTests/Validation/OpenApiValidationContractTests.cs` (the error codes keep their ADR-062 categories, the options keep their defaults, the registration keeps its shape) and an integration test over a minimal-hosting `WebApplication` with `UseTestServer` (`Microsoft.AspNetCore.TestHost`; no `WebHostBuilder`) in `tests/Encina.IntegrationTests/Validation/OpenApi/` covering a valid request, a body violation, a parameter violation, an unmatched route in both modes, a body over the limit and a chunked body. These are not database tests: no container.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 4 of 8. Depends on S0-S3.
AGENTS.md section 3: registration completeness (every option type and dependency registered, proven by a DI test).

TASK:
1. Write the middleware, the problem details writer, the options validator and the two extension methods.
2. Prove registration with a ValidateOnBuild and ValidateScopes test; write the contract test and the TestServer integration test.

KEY RULES:
- The body is read within a bound and rewound; a violation never echoes input; the response is RFC 9457.
- The status follows register row C14; never parse a code string to choose a category.

REFERENCE FILES:
- src/Encina.AspNetCore/ApplicationBuilderExtensions.cs, ProblemDetailsExtensions.cs, EncinaContextMiddleware
- tests/Encina.IntegrationTests/Validation/ (folder layout)
```

</details>

---

### Phase 5 (S5): Observability and Logging

> **Goal**: Span, metrics, log messages and the registered EventId range.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationOpenApi = (580, 589)`; **create** `Diagnostics/OpenApiValidationDiagnostics.cs` (source, meter, counter and histogram of register row C10) and `Diagnostics/OpenApiValidationLog.cs` (`[LoggerMessage]` 580-585 of register row C11; template and operation id only).
2. **Modify** the middleware to use them, `EncinaEventIdAllocationTests.cs`, the `PublicAPI.Unshipped.txt` of `Encina` and both manifests (through `mechanical-fixer`).
3. Tests (S5): a `MeterListener` and `ActivityListener` test (tags, tenant tag only when known, the raw path absent), a log test (ids inside the range, the input absent, 583 bounded by `MaxTrackedTemplates`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 5 of 8. ADR-021 and ADR-054.

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

### Phase 6 (S6): Generator from the Specification

> **Goal**: Compile-time validators for the DTOs of a specification (opens only after #227 S2 to S4 are on `main`; the test layout below is the one #227's plan implies and is recounted when #227 lands).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina.Validation.Generators/`: `OpenApiValidatorGenerator.cs` (reads `AdditionalFiles` named `openapi.json` or by a `build_metadata`, parses the schema with a netstandard2.0 reader of the subset it supports) and `Emit/OpenApiRuleModelBuilder.cs` (schema keywords to the rule model of #227; `operationId` plus the `[GeneratedValidator("CreateUser")]` name in the issue map to a DTO; an unmappable schema is a compile error, never a silent skip, as in #227 register row C2). **Modify** `src/Encina.Validation.OpenApi/Encina.Validation.OpenApi.csproj` to pack the generator as an analyzer.
2. **Modify** the manifest of `Encina.Validation.Generators` and the test project reference that runs the generator (through `mechanical-fixer`).
3. Tests (S6): the snapshot test class, two verified snapshot files, two specification fixtures read as `AdditionalFiles`, a unit test of the diagnostics and an equivalence test (the generated validator and the runtime engine of S2 agree on a corpus of valid and invalid bodies for the same schema).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 6 of 8. Requires the generator
project and emitters of issue #227 (S2 to S4) on main.

TASK:
1. Add an OpenAPI-driven generator to Encina.Validation.Generators that feeds the rule model of #227; pack it as an analyzer in the OpenApi package.
2. Snapshots, diagnostics test and the equivalence test against the runtime engine.

KEY RULES:
- Reflection-free output; an unsupported schema construct is a compile error; no value in any message.

REFERENCE FILES:
- src/Encina.Validation.Generators/ (issue #227), src/Encina.Validation.OpenApi/OpenApiSchemaValidator.cs
```

</details>

---

### Phase 7 (S7): Benchmark, Documentation and Finalization

> **Goal**: Performance evidence, usage guide, ADR and changelog.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.BenchmarkTests/Encina.Benchmarks/OpenApiValidationBenchmarks.cs` (the middleware path on a small and a large body, `BenchmarkSwitcher` per `AGENTS.md` section 9), **modify** `tests/Encina.BenchmarkTests/Encina.Benchmarks/Encina.Benchmarks.csproj` (one `ProjectReference` per package, as the file does today) and **create** a load-test justification `.md` (the document is immutable after start-up; the only shared state is the bounded template set of row C22).
2. **Create** through `docs-writer`: `src/Encina.Validation.OpenApi/README.md`, `docs/features/openapi-request-validation.md`, an ADR for the integration point and the engine (number assigned when the ADR pull request opens) and its index line; **modify** `docs/INVENTORY.md`.
3. **Create** `changelog.d/231-openapi-request-validation.added.md` (through `mechanical-fixer`).
4. Build verification: `dotnet build --configuration Release` with zero warnings; per-flag coverage of the touched files against the manifest targets below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Issue #231, plan docs/plans/openapi-request-validation-implementation-plan-231.md, phase 7 of 8.

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
| OpenAPI Specification 3.0 and 3.1 | Schema Object; 3.1 aligns with JSON Schema 2020-12; Parameter Object (`style`, `explode`) | Register rows C6, C20, C23 |
| JSON Schema 2020-12 (Core and Validation) | Keywords, `$ref`, `format` as annotation by default | Register row C15 |
| RFC 9457 | Problem Details for HTTP APIs, extension members | Register row C8 |
| RFC 9110 | 413 Content Too Large, 415 Unsupported Media Type | Register row C14 |
| RFC 6901 | JSON Pointer | Violation locations |
| ADR-054 D3, D4 | One source and meter per package; tenant tag when known | Register row C10 |
| ADR-062 | Error category contract (no 413 or 415 category; R1, R3, R4) | Register rows C7 and C14 |

### New dependencies

Checked on 2026-11-16 against the nuspec files on api.nuget.org and `Directory.Packages.props` of main:

| Package | Verified | To verify in the S1 spike |
|---------|----------|---------------------------|
| `Microsoft.OpenApi` 3.11.0 (latest stable) | Not in `Directory.Packages.props`; licence MIT; targets `net8.0` and `netstandard2.0` (so .NET 10 consumes the `net8.0` asset) | AOT and trimming warnings; whether the parser already normalizes OpenAPI 3.0 `nullable` and boolean exclusive bounds (register row C6); the removal of `OpenApiSchema.Nullable` that AGENTS.md section 1 records |
| `Microsoft.OpenApi.YamlReader` 3.11.0 | MIT; a separate package, so YAML is not part of `Microsoft.OpenApi` (register row C13) | Only if the maintainer picks option B of row C13 |
| `JsonSchema.Net` 9.7.0 (candidate JSON Schema 2020-12 engine) | Targets `net10.0`, `net8.0`, `net9.0`, `netstandard2.0`; the nuspec names the licence file `OSMFEULA.txt`, not an SPDX identifier | Reading of that licence by the maintainer; AOT; keyword and `format` coverage; other candidate engines and their licences |
| `YamlDotNet` 18.1.0 | Already in `Directory.Packages.props` (not used by this package) | Not applicable |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `UseEncinaContext`, `EncinaContextMiddleware` | `src/Encina.AspNetCore/ApplicationBuilderExtensions.cs` | Correlation and tenant for tags and logs; S0 adds the marker |
| `ProblemDetailsExtensions` | `src/Encina.AspNetCore/ProblemDetailsExtensions.cs` | Status mapping conventions |
| `EncinaErrors.Create` with a category | `src/Encina/Results/EncinaErrors.cs` | Typed errors |
| Rule model and emitters | issue #227 (`src/Encina.Validation.Generators/`) | Phase 6 |
| `IGeneratedRequestValidator<T>` | issue #227 and #230 | Generated output |
| `Microsoft.AspNetCore.TestHost` | `Directory.Packages.props`, referenced by `Encina.IntegrationTests.csproj` | `TestServer` integration test |
| `SanitizationDiagnostics` | `src/Encina.Security.Sanitization/Diagnostics/` | Meter and activity pattern |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Validation.OpenApi` (`ValidationOpenApi`, new, inside the block 550-599 assigned to the validation-1 family) | 580-589 | 580-585 used (six messages); 586-589 free; #230 owns 550-559, #227 560-569, #232 590-599 |

### Estimated File Count

Every count includes tests, fixtures, coverage manifest edits, PublicAPI files, test project references (`ProjectReference` and `Content`), the README and the changelog fragment. No pull request uses a type created by a later one. Checked on `origin/main`: `Encina.Benchmarks.csproj` holds one `ProjectReference` per package; `Encina.ContractTests.csproj` references no validation package; `Encina.IntegrationTests.csproj` references `Microsoft.AspNetCore.TestHost`.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S0 | Context marker in `Encina.AspNetCore` | 0 | 2 (`ApplicationBuilderExtensions.cs`, PublicAPI) | 1 (guard) | 1 (`ApplicationBuilderExtensionsTests.cs`) | 1 (`Encina.AspNetCore.json`) | 0 | 5 | ~150 |
| S1 | Package, options, loader | 5 (csproj, PublicAPI Shipped and Unshipped, options, loader) | 1 (`Directory.Packages.props`) | 5 (loader unit, options unit, guard, 2 spec fixtures) | 2 (unit and guard test csproj) | 1 | 1 (`Encina.slnx`) | 15 | ~700 |
| S2 | Normalizer, schema validator, limits | 4 | 1 (PublicAPI) | 4 (normalizer, validator, property, guard) | 1 (`Encina.PropertyTests.csproj`) | 1 | 0 | 11 | ~900 |
| S3 | Matcher, parameter validation | 2 | 1 (PublicAPI) | 4 (matcher unit, parameter unit, property, guard) | 0 | 1 | 0 | 8 | ~650 |
| S4 | Middleware, writer, DI, contract | 5 | 1 (PublicAPI) | 6 (middleware, DI, 2 guard, contract, integration) | 2 (`Encina.IntegrationTests.csproj`, `Encina.ContractTests.csproj`) | 1 | 0 | 15 | ~1,000 |
| S5 | Observability, logging, EventIds | 2 | 3 (`EventIdRanges.cs`, middleware, PublicAPI of `Encina`) | 2 (metrics and activity, log) | 1 (`EncinaEventIdAllocationTests`) | 2 | 0 | 10 | ~550 |
| S6 | Generator from the specification (needs #227 S2 to S4) | 2 | 1 (`Encina.Validation.OpenApi.csproj`, analyzer packing) | 7 (snapshot class, 2 snapshots, 2 spec fixtures, diagnostics unit, equivalence) | 1 (test project reference) | 1 | 0 | 12 | ~900 |
| S7 | Benchmark, docs, ADR, changelog | 0 | 0 | 2 (benchmark, load `.md`) | 1 (`Encina.Benchmarks.csproj`) | 0 | 6 (package README, feature page, `docs/INVENTORY.md`, ADR, ADR index, changelog fragment) | 9 | ~450 |
| **Total** | | **20** | **10** | **31** | **9** | **8** | **7** | **85** | **~5,300** |

The rows sum to the total (5 + 15 + 11 + 8 + 15 + 10 + 12 + 9 = 85) and so do the columns (20 + 10 + 31 + 9 + 8 + 7 = 85); every statement of the total in this plan is 85 files and about 5,300 changed lines. The largest pull requests are 15 files (S1, S4); none exceeds 1,000 lines. S0 is independent and lands before S4; S6 waits for #227; S1 to S5 and S7 do not.

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifest by the pull request that adds the file:

| File | Unit | Guard | Property | Contract | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `ApplicationBuilderExtensions.cs` (S0) | existing, not lowered | 80 | 0 | 0 | 0 | The marker and the check are unit cases; the guard test covers the null `app`; existing targets stay |
| `OpenApiDocumentLoader.cs` | 90 | 85 | 0 | 0 | 0 | Every input state (3.0, 3.1, missing, invalid, remote `$ref`, unsupported construct) is a unit case; path and options arguments are guard-tested (InternalsVisibleTo for the guard project) |
| `OpenApi30SchemaNormalizer.cs`, `OpenApiSchemaValidator.cs` | 90 | 85 | 60 | 0 | 0 | Each keyword and limit has a unit case; the generated-schema property covers the valid and mutated instances |
| `OpenApiOperationMatcher.cs`, `OpenApiParameterValidator.cs` | 90 | 85 | 60 | 0 | 0 | Template and parameter styles are unit cases; the segment property pins the matching rule |
| `OpenApiValidationMiddleware.cs`, `OpenApiProblemDetailsWriter.cs` | 85 | 85 | 0 | 0 | 70 | Unit tests drive each branch with a fake `HttpContext`; the `TestServer` test runs the real pipeline including a chunked body |
| `OpenApiValidationServiceCollectionExtensions.cs`, `...ApplicationBuilderExtensions.cs`, `...OptionsValidator.cs` | 90 | 90 | 0 | 60 | 0 | Registration, the ordering check and option rules are tested by the DI test; null checks are guard-tested; the contract test pins the registration shape |
| `OpenApiValidationOptions.cs`, `SchemaViolation.cs`, `OpenApiValidationErrorCodes.cs` | 95 | 0 | 0 | 80 | 0 | Plain data and constants with no argument validation (guard 0); the contract test pins defaults and the code to category pairs |
| `Diagnostics/*.cs` | 90 | 0 | 0 | 0 | 0 | Listener and log capture tests cover them; internal static types have no guard surface |
| `OpenApiValidatorGenerator.cs`, `Emit/OpenApiRuleModelBuilder.cs` | 85 | 0 | 0 | 0 | 0 | Driven through the generator harness of #227; internal types and Roslyn structs have no guard surface |

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
  decide the integration point, the engine, strict mode and the generator scope; the Dilemma rows of the register decide
  the statuses, formats and body rules.

IMPLEMENTATION OVERVIEW:
0. Encina.AspNetCore: the context marker and IsEncinaContextRegistered.
1. Encina.Validation.OpenApi: options, document loader (3.0 and 3.1, local $ref only, fail start-up when invalid).
2. Schema engine adapter with normalizer and bounded limits; operation matcher; parameter validator.
3. Middleware, RFC 9457 writer, AddEncinaOpenApiValidation and UseEncinaOpenApiValidation, DI and contract tests.
4. Span, counter, histogram, EventIds 580-585 in ValidationOpenApi (580-589), registered first.
5. Generator from the specification (after #227); benchmark, docs, ADR and changelog.
Eight pull requests (S0 to S7), see the Estimated File Count table.

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
| 7 | Distributed Locks | ❌ N/A | The document is immutable; the only mutable state is the bounded, thread-safe template set of register row C22 |
| 8 | Transactions | ❌ N/A | Validation precedes the handler |
| 9 | Idempotency | ❌ N/A | Stateless and repeatable |
| 10 | Multi-Tenancy | ⏭️ | The tenant is a tag and a log field when known; tenant-specific specifications are a follow-up shared with the family: follow-up issue to be opened (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`) |
| 11 | Module Isolation | ⏭️ | Module-scoped specifications: the same follow-up issue to be opened |
| 12 | Audit Trail | ⏭️ | Auditing of contract violations: follow-up issue to be opened (draft: `artifacts/issues/validation-failure-audit.md`) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-062 error category contract | ✅ Merged | Codes of register row C7 |
| A marker that `UseEncinaContext` ran | Delivered by S0 of this plan | Register row C12; nothing on `main` today |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Generator project and emitters | #227 | Blocking for S6 only |
| `IRequestValidator<T>` and the runner | #230 | Needed by S6 through #227 |
| OpenAPI and Swagger integration | #446 | May later offer a document provider; not required |
| Code-less validation `Left` never becomes a 400 | #3415 | The pipeline-level `Left` is #3415's; the middleware writes its own response |

---

## Next Steps

1. Link this plan from #231 and ask the maintainer to answer Design Choices 1 to 4 and the Dilemma rows of the register (C3, C6 to C8, C12 to C22) when work starts; the S1 dependency spike runs first.
2. Open the follow-up issues when the first pull request opens (shared with the family): follow-up issue to be opened for tenant and module scoped specifications (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`) and follow-up issue to be opened for auditing of validation failures (draft: `artifacts/issues/validation-failure-audit.md`).
3. Implement S0 to S7 as the pull requests of the Estimated File Count split, `Fixes #231` on S7.

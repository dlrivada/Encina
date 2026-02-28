# Encina.Compliance.Anonymization — Pseudonymization and Anonymization Techniques

> **Issue**: [#407](https://github.com/dlrivada/Encina/issues/407)
> **Type**: Feature (new package)
> **Complexity**: High (9 phases, provider-independent core + 13 database providers for token store, ~100 files)
> **Estimated Scope**: ~4,000-5,500 lines of production code + ~3,000-4,000 lines of tests
> **GDPR Articles**: 4(5), 25, 32, 89
> **EDPB Guidelines**: 01/2025 on Pseudonymisation

---

## Summary

Implement `Encina.Compliance.Anonymization` — a new package providing pseudonymization (reversible, HMAC/AES-based) and anonymization (irreversible, generalization/suppression/perturbation) techniques for GDPR compliance. The module includes:

- **`IAnonymizer`** — irreversible anonymization with configurable techniques (generalization, suppression, perturbation, data masking, k-anonymity, l-diversity, t-closeness)
- **`IPseudonymizer`** — reversible pseudonymization with key management and rotation (AES-256-GCM + HMAC-SHA256)
- **`ITokenizer`** — format-preserving tokenization with token ↔ value mapping (requires persistent store for de-tokenization)
- **`[Anonymize]` / `[Pseudonymize]`** attributes for declarative field-level configuration
- **`AnonymizationPipelineBehavior<TRequest, TResponse>`** — auto-applies transformations on response fields
- **`ITokenMappingStore`** — persistent token storage across 13 database providers
- **Re-identification risk assessment** — statistical analysis of anonymization effectiveness

**Provider category**: Core package is provider-independent. `ITokenMappingStore` requires 13 database providers (ADO.NET ×4, Dapper ×4, EF Core ×4, MongoDB ×1) for persistent token de-tokenization.

**Affected packages**:
- `Encina.Compliance.Anonymization` (new — core package)
- `Encina.Compliance.Anonymization.ADO.Sqlite` (new)
- `Encina.Compliance.Anonymization.ADO.SqlServer` (new)
- `Encina.Compliance.Anonymization.ADO.PostgreSQL` (new)
- `Encina.Compliance.Anonymization.ADO.MySQL` (new)
- `Encina.Compliance.Anonymization.Dapper.Sqlite` (new)
- `Encina.Compliance.Anonymization.Dapper.SqlServer` (new)
- `Encina.Compliance.Anonymization.Dapper.PostgreSQL` (new)
- `Encina.Compliance.Anonymization.Dapper.MySQL` (new)
- `Encina.Compliance.Anonymization.EntityFrameworkCore.Sqlite` (new)
- `Encina.Compliance.Anonymization.EntityFrameworkCore.SqlServer` (new)
- `Encina.Compliance.Anonymization.EntityFrameworkCore.PostgreSQL` (new)
- `Encina.Compliance.Anonymization.EntityFrameworkCore.MySQL` (new)
- `Encina.Compliance.Anonymization.MongoDB` (new)

---

## Design Choices

<details>
<summary><strong>1. Package placement — single core package + 13 satellite provider packages</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single `Encina.Compliance.Anonymization` package (all-in-one)** | + Simple packaging, + One NuGet reference | - Forces DB dependencies on all users, - Violates pay-for-what-you-use |
| **B) Core + satellite provider packages (per Encina convention)** | + Follows established pattern, + Pay-for-what-you-use, + TryAdd override pattern | - More NuGet packages to maintain |
| **C) Extension of `Encina.Compliance.GDPR`** | + Fewer packages | - GDPR is Article 6/30 focused, - Violates single responsibility |

### Chosen Option: **B — Core + satellite provider packages**

### Rationale

- Follows the identical pattern used by Consent, DataSubjectRights, and GDPR modules
- Core package provides `IAnonymizer`, `IPseudonymizer`, `ITokenizer` + in-memory defaults
- Satellite packages register `ITokenMappingStore` implementations for each database provider
- Users who only need anonymization (no tokenization) never take a DB dependency
- `TryAdd` pattern allows satellite packages to override in-memory defaults

</details>

<details>
<summary><strong>2. Pseudonymization strategy — AES-256-GCM with HMAC-SHA256 deterministic mode</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) AES-256-GCM (randomized) + separate HMAC for lookup** | + Strong security (IND-CCA2), + Industry standard, + .NET native | - Requires storing nonce per operation, - Non-deterministic by default |
| **B) Format-Preserving Encryption (FF1/FF3-1)** | + Preserves format (credit card stays 16 digits), + Database-transparent | - Limited .NET libraries, - NIST SP 800-38G concerns, - Complex implementation |
| **C) HMAC-SHA256 only (one-way hash with key)** | + Simple, + Deterministic, + No nonce storage | - One-way only — cannot depseudonymize, - Limited to pseudonymization |
| **D) Hybrid: AES-256-GCM (reversible) + HMAC-SHA256 (deterministic lookup)** | + Best of both: reversible + searchable, + Industry best practice | - Slightly more complex key management |

### Chosen Option: **D — Hybrid AES-256-GCM + HMAC-SHA256**

### Rationale

- AES-256-GCM provides authenticated encryption for reversible pseudonymization (depseudonymize operation)
- HMAC-SHA256 provides deterministic pseudonym generation for cases where searchability is needed
- Both are natively supported in .NET 10 (`System.Security.Cryptography`)
- Key rotation is straightforward: generate new key, re-encrypt existing data with new key
- Follows EDPB Guidelines 01/2025 recommendation for strong cryptographic pseudonymization
- `IKeyProvider` abstraction allows plugging Azure Key Vault, AWS KMS, etc.

</details>

<details>
<summary><strong>3. Tokenizer architecture — token mapping store with `ITokenMappingStore`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Stateless tokenization (hash-based, no store)** | + No persistence needed, + Fast | - Cannot detokenize, - Limited to one-way |
| **B) Stateful tokenization with `ITokenMappingStore`** | + Full reversibility, + Lookup by token or value, + Audit trail | - Requires persistent store, - Additional DB table |
| **C) Vault-based tokenization (external service)** | + Centralized, + HSM backing | - External dependency, - Latency |

### Chosen Option: **B — Stateful tokenization with `ITokenMappingStore`**

### Rationale

- Tokenization's primary value is reversible mapping (token ↔ original value)
- Follows the same store abstraction pattern as `IConsentStore`, `IDSRRequestStore`
- In-memory default for testing/simple cases; database-backed for production
- 13 database providers required per Encina's multi-provider mandate
- Token format is configurable: UUID-based, prefix-based, or format-preserving

</details>

<details>
<summary><strong>4. Pipeline behavior design — response-side transformation with attribute detection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Request-side transformation (anonymize input before handler)** | + Data never reaches handler in raw form | - Breaks handler logic that needs real data, - Can't selectively anonymize |
| **B) Response-side transformation (anonymize output after handler)** | + Handler works with real data, + Selective per-field, + Matches GDPR intent | - Sensitive data briefly exists in memory |
| **C) Both request and response transformation** | + Maximum coverage | - Complex, - Double processing overhead |

### Chosen Option: **B — Response-side transformation**

### Rationale

- GDPR anonymization/pseudonymization is about protecting data _as it leaves_ the system (responses, exports, analytics)
- Handlers need real data for business logic (e.g., email validation needs actual email)
- `[Anonymize]` on response properties triggers generalization/masking/suppression _after_ the handler returns
- `[Pseudonymize]` on response properties triggers encryption _after_ the handler returns
- Static per-generic-type attribute caching ensures zero reflection overhead on subsequent calls
- Enforcement modes (Block/Warn/Disabled) follow established compliance module pattern

</details>

<details>
<summary><strong>5. Key management — `IKeyProvider` abstraction with built-in DPAPI and in-memory providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Hardcoded keys in configuration** | + Simple | - Security risk, - No rotation |
| **B) `IKeyProvider` interface with pluggable implementations** | + Extensible, + Supports Azure KV / AWS KMS / HSM, + Key rotation built-in | - Requires abstraction design |
| **C) .NET Data Protection API only** | + Built into .NET, + Key rotation included | - Not designed for field-level pseudonymization, - Opaque key management |

### Chosen Option: **B — `IKeyProvider` abstraction**

### Rationale

- `IKeyProvider` provides `GetKeyAsync(keyId)`, `RotateKeyAsync(keyId)`, `GetActiveKeyIdAsync()`
- Built-in `InMemoryKeyProvider` for testing and `DataProtectionKeyProvider` for simple production use
- Users can implement `IKeyProvider` for Azure Key Vault, AWS KMS, HashiCorp Vault, etc.
- Key rotation is a first-class operation: rotate key → re-encrypt all pseudonymized data
- Aligns with EDPB Guidelines 01/2025 Section 4.3 on key management

</details>

<details>
<summary><strong>6. Anonymization technique extensibility — strategy pattern with `IAnonymizationTechnique`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Switch/case on `AnonymizationTechnique` enum** | + Simple, + No DI needed | - Closed for extension, - Monolithic |
| **B) Strategy pattern with `IAnonymizationTechnique` per technique** | + Open/closed principle, + Users add custom techniques, + Testable | - More types |
| **C) Delegate-based configuration** | + Flexible, + No interfaces | - Hard to discover, - No DI lifetime control |

### Chosen Option: **B — Strategy pattern with `IAnonymizationTechnique`**

### Rationale

- Each technique (generalization, suppression, perturbation, masking, k-anonymity, etc.) implements `IAnonymizationTechnique`
- Registered via DI with `TryAdd`, users can replace or add custom techniques
- `CompositeAnonymizer` delegates to the appropriate technique based on the `[Anonymize]` attribute's `Technique` property
- Follows the same extensibility pattern as `IDataErasureStrategy` in DSR module
- k-Anonymity and l-Diversity require statistical analysis — separate strategy classes isolate that complexity

</details>

<details>
<summary><strong>7. Re-identification risk assessment — built-in statistical analyzer</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No built-in risk assessment** | + Simpler scope | - Users can't verify anonymization quality |
| **B) Built-in `IRiskAssessor` with k-anonymity, l-diversity, t-closeness checks** | + GDPR compliance verification, + Actionable metrics | - Additional complexity |
| **C) External tool integration only** | + Leverage specialized tools | - External dependency, - Integration cost |

### Chosen Option: **B — Built-in `IRiskAssessor`**

### Rationale

- GDPR Article 89 requires "appropriate safeguards" — risk assessment is how you demonstrate them
- `IRiskAssessor` provides `AssessAsync(dataset, quasiIdentifiers)` returning risk metrics
- Reports: k-value achieved, l-diversity score, t-closeness distance, re-identification probability
- Helps users iteratively improve anonymization profiles until risk is acceptable
- Scoped as a utility — not a pipeline behavior (used on-demand, not per-request)

</details>

---

## Implementation Phases

### Phase 1: Core Models & Enums

> **Goal**: Define all domain types for anonymization, pseudonymization, and tokenization.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create project** `src/Encina.Compliance.Anonymization/`
   - `Encina.Compliance.Anonymization.csproj` targeting `net10.0`
   - Reference `Encina` (core) for `EncinaError`, `Either`, `IPipelineBehavior`
   - Enable nullable, implicit usings, `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`

2. **`AnonymizationTechnique`** `Model/AnonymizationTechnique.cs`
   - Enum with values: `Generalization`, `Suppression`, `Perturbation`, `Swapping`, `DataMasking`, `KAnonymity`, `LDiversity`, `TCloseness`
   - XML documentation for each value referencing GDPR context

3. **`AnonymizationProfile`** `Model/AnonymizationProfile.cs`
   - Sealed record: `Id` (string), `Name` (string), `Description` (string?), `FieldRules` (`IReadOnlyList<FieldAnonymizationRule>`), `CreatedAtUtc` (DateTimeOffset)
   - Factory method `Create()`

4. **`FieldAnonymizationRule`** `Model/FieldAnonymizationRule.cs`
   - Sealed record: `FieldName` (string), `Technique` (AnonymizationTechnique), `Parameters` (`IReadOnlyDictionary<string, object>?`)
   - Parameters examples: `{"Granularity": 10}` for generalization, `{"Pattern": "***"}` for masking

5. **`PseudonymizationAlgorithm`** `Model/PseudonymizationAlgorithm.cs`
   - Enum: `Aes256Gcm`, `HmacSha256`

6. **`TokenizationOptions`** `Model/TokenizationOptions.cs`
   - Sealed record: `Format` (TokenFormat), `Prefix` (string?), `PreserveLength` (bool)

7. **`TokenFormat`** `Model/TokenFormat.cs`
   - Enum: `Uuid`, `Prefixed`, `FormatPreserving`

8. **`TokenMapping`** `Model/TokenMapping.cs`
   - Sealed record: `Id` (string), `Token` (string), `OriginalValueHash` (string), `EncryptedOriginalValue` (byte[]), `KeyId` (string), `CreatedAtUtc` (DateTimeOffset), `ExpiresAtUtc` (DateTimeOffset?)

9. **`RiskAssessmentResult`** `Model/RiskAssessmentResult.cs`
   - Sealed record: `KAnonymityValue` (int), `LDiversityValue` (int), `TClosenessDistance` (double), `ReIdentificationProbability` (double), `IsAcceptable` (bool), `AssessedAtUtc` (DateTimeOffset), `Recommendations` (`IReadOnlyList<string>`)

10. **`AnonymizationEnforcementMode`** `AnonymizationEnforcementMode.cs`
    - Enum: `Block`, `Warn`, `Disabled`

11. **`AnonymizationResult`** `Model/AnonymizationResult.cs`
    - Sealed record: `OriginalFieldCount` (int), `AnonymizedFieldCount` (int), `SkippedFieldCount` (int), `TechniqueApplied` (`IReadOnlyDictionary<string, AnonymizationTechnique>`)

12. **`KeyInfo`** `Model/KeyInfo.cs`
    - Sealed record: `KeyId` (string), `Algorithm` (PseudonymizationAlgorithm), `CreatedAtUtc` (DateTimeOffset), `ExpiresAtUtc` (DateTimeOffset?), `IsActive` (bool)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina.Compliance.Anonymization (Issue #407).

CONTEXT:
- New package: src/Encina.Compliance.Anonymization/
- Follows the same patterns as Encina.Compliance.DataSubjectRights and Encina.Compliance.Consent
- .NET 10, C# 14, nullable enabled
- All domain types are sealed records or enums
- ROP pattern: Either<EncinaError, T> for all operations

TASK:
- Create the project file (Encina.Compliance.Anonymization.csproj) referencing Encina core
- Create 12 model/enum files in Model/ and root namespace
- All types must have full XML documentation with <summary>, <remarks> where needed
- Create PublicAPI.Shipped.txt (empty) and PublicAPI.Unshipped.txt

KEY RULES:
- Namespace: Encina.Compliance.Anonymization (root) and Encina.Compliance.Anonymization.Model
- All records are sealed
- Use DateTimeOffset for timestamps with AtUtc suffix
- Factory methods preferred over constructors for complex types
- No [Obsolete] attributes, no backward compatibility
- Include XML doc <example> blocks where useful

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Model/DSRRequest.cs (sealed record pattern)
- src/Encina.Compliance.DataSubjectRights/Model/DataSubjectRight.cs (enum pattern)
- src/Encina.Compliance.DataSubjectRights/Encina.Compliance.DataSubjectRights.csproj (project file pattern)
```

</details>

---

### Phase 2: Core Interfaces & Abstractions

> **Goal**: Define all service interfaces following ROP conventions.

<details>
<summary><strong>Tasks</strong></summary>

1. **`IAnonymizer`** `Abstractions/IAnonymizer.cs`
   - `ValueTask<Either<EncinaError, T>> AnonymizeAsync<T>(T data, AnonymizationProfile profile, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, AnonymizationResult>> AnonymizeFieldsAsync<T>(T data, AnonymizationProfile profile, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, bool>> IsAnonymizedAsync<T>(T data, CancellationToken ct)`

2. **`IPseudonymizer`** `Abstractions/IPseudonymizer.cs`
   - `ValueTask<Either<EncinaError, T>> PseudonymizeAsync<T>(T data, string keyId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, T>> DepseudonymizeAsync<T>(T data, string keyId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, string>> PseudonymizeValueAsync(string value, string keyId, PseudonymizationAlgorithm algorithm, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, string>> DepseudonymizeValueAsync(string pseudonym, string keyId, CancellationToken ct)`

3. **`ITokenizer`** `Abstractions/ITokenizer.cs`
   - `ValueTask<Either<EncinaError, string>> TokenizeAsync(string value, TokenizationOptions options, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, string>> DetokenizeAsync(string token, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, bool>> IsTokenAsync(string value, CancellationToken ct)`

4. **`IKeyProvider`** `Abstractions/IKeyProvider.cs`
   - `ValueTask<Either<EncinaError, byte[]>> GetKeyAsync(string keyId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, KeyInfo>> RotateKeyAsync(string keyId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, string>> GetActiveKeyIdAsync(CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<KeyInfo>>> ListKeysAsync(CancellationToken ct)`

5. **`ITokenMappingStore`** `Abstractions/ITokenMappingStore.cs`
   - `ValueTask<Either<EncinaError, Unit>> StoreAsync(TokenMapping mapping, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Option<TokenMapping>>> GetByTokenAsync(string token, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Option<TokenMapping>>> GetByOriginalValueHashAsync(string hash, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> DeleteByKeyIdAsync(string keyId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<TokenMapping>>> GetAllAsync(CancellationToken ct)`

6. **`IAnonymizationTechnique`** `Abstractions/IAnonymizationTechnique.cs`
   - `AnonymizationTechnique Technique { get; }`
   - `ValueTask<Either<EncinaError, object?>> ApplyAsync(object? value, Type valueType, IReadOnlyDictionary<string, object>? parameters, CancellationToken ct)`
   - `bool CanApply(Type valueType)`

7. **`IRiskAssessor`** `Abstractions/IRiskAssessor.cs`
   - `ValueTask<Either<EncinaError, RiskAssessmentResult>> AssessAsync<T>(IReadOnlyList<T> dataset, IReadOnlyList<string> quasiIdentifiers, CancellationToken ct)`

8. **`IAnonymizationAuditStore`** `Abstractions/IAnonymizationAuditStore.cs`
   - `ValueTask<Either<EncinaError, Unit>> AddEntryAsync(AnonymizationAuditEntry entry, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<AnonymizationAuditEntry>>> GetBySubjectIdAsync(string subjectId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<AnonymizationAuditEntry>>> GetAllAsync(CancellationToken ct)`

9. **`AnonymizationAuditEntry`** `Model/AnonymizationAuditEntry.cs`
   - Sealed record: `Id` (string), `SubjectId` (string?), `Operation` (AnonymizationOperation), `Technique` (AnonymizationTechnique?), `FieldName` (string?), `KeyId` (string?), `PerformedAtUtc` (DateTimeOffset), `PerformedByUserId` (string?)

10. **`AnonymizationOperation`** `Model/AnonymizationOperation.cs`
    - Enum: `Anonymized`, `Pseudonymized`, `Depseudonymized`, `Tokenized`, `Detokenized`, `KeyRotated`, `RiskAssessed`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Encina.Compliance.Anonymization (Issue #407).

CONTEXT:
- Phase 1 complete: all enums, records, value objects exist in Model/
- Now creating core service interfaces in Abstractions/
- All methods return ValueTask<Either<EncinaError, T>> (ROP pattern)
- Interfaces must be overridable via TryAdd in DI

TASK:
- Create 8 interface files in Abstractions/
- Create AnonymizationAuditEntry record and AnonymizationOperation enum in Model/
- All interfaces must have full XML documentation
- Follow the exact naming and parameter conventions of existing compliance modules

KEY RULES:
- All async methods return ValueTask<Either<EncinaError, T>>
- Optional results use Option<T> (from LanguageExt)
- Unit type for void operations (from LanguageExt)
- CancellationToken is always the last parameter
- No exceptions for business logic — use EncinaError
- Interface prefix: I{Name}

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Abstractions/IDSRRequestStore.cs (store interface pattern)
- src/Encina.Compliance.DataSubjectRights/Abstractions/IDataErasureStrategy.cs (strategy interface pattern)
- src/Encina.Compliance.DataSubjectRights/Abstractions/IDataSubjectIdExtractor.cs (extractor pattern)
```

</details>

---

### Phase 3: Default Implementations

> **Goal**: Provide in-memory and default implementations for all interfaces.

<details>
<summary><strong>Tasks</strong></summary>

1. **`InMemoryKeyProvider`** `InMemory/InMemoryKeyProvider.cs`
   - Thread-safe key storage using `ConcurrentDictionary<string, byte[]>`
   - Generates 256-bit keys via `RandomNumberGenerator`
   - Supports key rotation: generates new key, marks old as inactive

2. **`InMemoryTokenMappingStore`** `InMemory/InMemoryTokenMappingStore.cs`
   - `ConcurrentDictionary<string, TokenMapping>` indexed by token
   - Secondary index by `OriginalValueHash` for lookup

3. **`InMemoryAnonymizationAuditStore`** `InMemory/InMemoryAnonymizationAuditStore.cs`
   - Thread-safe list, queries by SubjectId

4. **`DefaultAnonymizer`** `DefaultAnonymizer.cs`
   - Implements `IAnonymizer`
   - Uses reflection to find fields with `[Anonymize]` attribute
   - Delegates to registered `IAnonymizationTechnique` implementations
   - Caches field metadata per type (static `ConcurrentDictionary`)

5. **`DefaultPseudonymizer`** `DefaultPseudonymizer.cs`
   - Implements `IPseudonymizer`
   - Uses `IKeyProvider` for key retrieval
   - AES-256-GCM for reversible pseudonymization
   - HMAC-SHA256 for deterministic pseudonymization
   - Reflection-based field discovery with `[Pseudonymize]` attribute

6. **`DefaultTokenizer`** `DefaultTokenizer.cs`
   - Implements `ITokenizer`
   - Generates tokens based on `TokenFormat` (UUID, prefixed, format-preserving)
   - Stores mapping via `ITokenMappingStore`
   - Encrypts original value before storage (using `IKeyProvider`)
   - HMAC-SHA256 for deduplication (same value → same token)

7. **Anonymization technique strategies** `Techniques/`
   - `GeneralizationTechnique.cs` — numeric ranges, date truncation
   - `SuppressionTechnique.cs` — replace with null/default
   - `PerturbationTechnique.cs` — add noise within configurable range
   - `DataMaskingTechnique.cs` — pattern-based masking (`***@email.com`)
   - `SwappingTechnique.cs` — record-level value swapping

8. **`DefaultRiskAssessor`** `DefaultRiskAssessor.cs`
   - Implements `IRiskAssessor`
   - Calculates k-anonymity: groups records by quasi-identifiers, reports minimum group size
   - Calculates l-diversity: distinct sensitive values per equivalence class
   - Calculates t-closeness: distribution distance (Earth Mover's Distance)
   - Returns `RiskAssessmentResult` with recommendations

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Encina.Compliance.Anonymization (Issue #407).

CONTEXT:
- Phases 1-2 complete: models and interfaces exist
- Now creating default implementations that work without external dependencies
- In-memory stores for testing/simple deployments
- Cryptographic implementations using System.Security.Cryptography (.NET 10)

TASK:
- Create 3 in-memory stores in InMemory/
- Create DefaultAnonymizer, DefaultPseudonymizer, DefaultTokenizer
- Create 5 anonymization technique strategies in Techniques/
- Create DefaultRiskAssessor with statistical analysis
- All implementations must be thread-safe

KEY RULES:
- Thread safety: ConcurrentDictionary for in-memory stores
- AES-256-GCM: use System.Security.Cryptography.AesGcm (.NET 10 API)
- HMAC-SHA256: use System.Security.Cryptography.HMACSHA256
- RandomNumberGenerator for key/nonce generation (no Random/new Random())
- Cache reflection metadata per type using static ConcurrentDictionary
- All operations return Either<EncinaError, T> — catch exceptions and wrap in AnonymizationErrors
- XML documentation on all public APIs

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/InMemory/InMemoryDSRRequestStore.cs (in-memory store pattern)
- src/Encina.Compliance.DataSubjectRights/Erasure/HardDeleteErasureStrategy.cs (strategy pattern)
- src/Encina.Compliance.DataSubjectRights/DefaultDataSubjectRightsHandler.cs (handler pattern)
```

</details>

---

### Phase 4: Attributes & Pipeline Behavior

> **Goal**: Enable declarative field-level anonymization/pseudonymization with auto-detection in the pipeline.

<details>
<summary><strong>Tasks</strong></summary>

1. **`[Anonymize]`** `Attributes/AnonymizeAttribute.cs`
   - `AttributeTargets.Property`
   - Properties: `Technique` (AnonymizationTechnique), `Granularity` (int?), `Pattern` (string?), `NoiseRange` (double?)
   - XML docs with usage examples

2. **`[Pseudonymize]`** `Attributes/PseudonymizeAttribute.cs`
   - `AttributeTargets.Property`
   - Properties: `KeyId` (string), `Algorithm` (PseudonymizationAlgorithm, default `Aes256Gcm`)
   - XML docs with usage examples

3. **`[Tokenize]`** `Attributes/TokenizeAttribute.cs`
   - `AttributeTargets.Property`
   - Properties: `Format` (TokenFormat, default `Uuid`), `Prefix` (string?)
   - XML docs with usage examples

4. **`AnonymizationPipelineBehavior<TRequest, TResponse>`** `AnonymizationPipelineBehavior.cs`
   - Implements `IPipelineBehavior<TRequest, TResponse>`
   - Static per-generic-type attribute caching (`static readonly` field)
   - On `TResponse`: scans for `[Anonymize]`, `[Pseudonymize]`, `[Tokenize]` attributes
   - Delegates to `IAnonymizer`, `IPseudonymizer`, `ITokenizer` respectively
   - Enforcement modes: Block (error if transformation fails), Warn (log + continue), Disabled
   - Activity + metric recording per operation

5. **`AnonymizationAttributeInfo`** (private nested record in behavior)
   - Cached metadata: `PropertyInfo`, attribute type, parameters
   - Resolved once per closed generic type

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina.Compliance.Anonymization (Issue #407).

CONTEXT:
- Phases 1-3 complete: models, interfaces, default implementations exist
- Now creating attributes for declarative field-level configuration
- Pipeline behavior auto-applies transformations on response types
- Must follow the exact pattern of ProcessingRestrictionPipelineBehavior in DSR module

TASK:
- Create 3 attribute classes in Attributes/
- Create AnonymizationPipelineBehavior with static per-type attribute caching
- Behavior scans TResponse properties for [Anonymize], [Pseudonymize], [Tokenize]
- Delegates to the appropriate service for each decorated property
- Enforcement modes: Block/Warn/Disabled

KEY RULES:
- Static readonly field for cached attribute info per closed generic type
- Use ResolveAttributeInfo() static method called in field initializer
- Enforcement modes: Block returns Either.Left, Warn logs warning + continues, Disabled skips
- Log with [LoggerMessage] source generator (EventIds from Phase 7)
- Record metrics via Diagnostics (Phase 7)
- ArgumentNullException.ThrowIfNull on all constructor parameters
- ConfigureAwait(false) on all awaits

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs (EXACT pattern to follow)
- src/Encina.Compliance.DataSubjectRights/Attributes/RestrictProcessingAttribute.cs (attribute pattern)
- src/Encina.Compliance.DataSubjectRights/Attributes/PersonalDataAttribute.cs (attribute pattern)
```

</details>

---

### Phase 5: Configuration, DI & Options Validation

> **Goal**: Wire up all services with the standard Encina DI pattern.

<details>
<summary><strong>Tasks</strong></summary>

1. **`AnonymizationOptions`** `AnonymizationOptions.cs`
   - Sealed class with properties:
     - `DefaultTechnique` (AnonymizationTechnique, default `DataMasking`)
     - `EnforcementMode` (AnonymizationEnforcementMode, default `Block`)
     - `AutoRotateKeys` (bool, default `false`)
     - `RotationIntervalDays` (int, default `90`)
     - `AddHealthCheck` (bool, default `false`)
     - `AutoRegisterFromAttributes` (bool, default `true`)
     - `AssembliesToScan` (`List<Assembly>`)
     - `PublishNotifications` (bool, default `true`)
     - `TrackAuditTrail` (bool, default `true`)
     - `KAnonymity` (KAnonymitySettings nested class: `K` int default 5, `QuasiIdentifiers` `List<string>`)
   - Fluent methods: `ScanAssembly()`, `ScanAssemblyContaining<T>()`

2. **`AnonymizationOptionsValidator`** `AnonymizationOptionsValidator.cs`
   - Implements `IValidateOptions<AnonymizationOptions>`
   - Validates: `RotationIntervalDays > 0`, `KAnonymity.K >= 2`

3. **`ServiceCollectionExtensions`** `ServiceCollectionExtensions.cs`
   - `AddEncinaAnonymization(Action<AnonymizationOptions>? configure = null)`
   - Registers (all TryAdd):
     - `AnonymizationOptions` via `Configure<T>`
     - `IValidateOptions<AnonymizationOptions>` → validator
     - `TimeProvider.System`
     - `IAnonymizer` → `DefaultAnonymizer` (Scoped)
     - `IPseudonymizer` → `DefaultPseudonymizer` (Scoped)
     - `ITokenizer` → `DefaultTokenizer` (Scoped)
     - `IKeyProvider` → `InMemoryKeyProvider` (Singleton)
     - `ITokenMappingStore` → `InMemoryTokenMappingStore` (Singleton)
     - `IAnonymizationAuditStore` → `InMemoryAnonymizationAuditStore` (Singleton)
     - `IRiskAssessor` → `DefaultRiskAssessor` (Scoped)
     - All `IAnonymizationTechnique` implementations (Singleton)
     - `AnonymizationPipelineBehavior<,>` (Transient)
   - Conditional: health check, auto-registration hosted service

4. **`AnonymizationAutoRegistrationDescriptor`** `AnonymizationAutoRegistrationDescriptor.cs`
   - Holds list of assemblies to scan

5. **`AnonymizationAutoRegistrationHostedService`** `AnonymizationAutoRegistrationHostedService.cs`
   - `IHostedService` — scans assemblies for `[Anonymize]`, `[Pseudonymize]`, `[Tokenize]` attributes
   - Logs discovered fields at startup

6. **Health check** `Health/AnonymizationHealthCheck.cs`
   - `DefaultName = "anonymization"`
   - `Tags = ["compliance", "anonymization", "gdpr"]`
   - Checks: key provider accessible, token store available

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina.Compliance.Anonymization (Issue #407).

CONTEXT:
- Phases 1-4 complete: models, interfaces, implementations, attributes, pipeline behavior exist
- Now wiring everything with DI using the standard Encina pattern
- All registrations use TryAdd for override by satellite provider packages
- Options validated via IValidateOptions<T>

TASK:
- Create AnonymizationOptions (sealed class with fluent methods)
- Create AnonymizationOptionsValidator
- Create ServiceCollectionExtensions.AddEncinaAnonymization()
- Create auto-registration descriptor + hosted service
- Create health check

KEY RULES:
- All services registered with TryAdd* (TryAddSingleton, TryAddScoped, TryAddTransient)
- Options: services.Configure<T>(configure) then TryAddSingleton<IValidateOptions<T>, Validator>
- TimeProvider.System registered with TryAddSingleton
- Pipeline behavior: TryAddTransient(typeof(IPipelineBehavior<,>), typeof(AnonymizationPipelineBehavior<,>))
- Health check conditional on AddHealthCheck option
- Auto-registration conditional on AutoRegisterFromAttributes option
- XML documentation on AddEncinaAnonymization with <remarks>, <example>, <list>

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs (EXACT pattern)
- src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs (options pattern)
- src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptionsValidator.cs (validator pattern)
- src/Encina.Compliance.DataSubjectRights/DSRAutoRegistrationDescriptor.cs (descriptor)
- src/Encina.Compliance.DataSubjectRights/DSRAutoRegistrationHostedService.cs (hosted service)
- src/Encina.Compliance.DataSubjectRights/Health/DataSubjectRightsHealthCheck.cs (health check)
```

</details>

---

### Phase 6: Error Factory

> **Goal**: Centralized error creation with structured metadata.

<details>
<summary><strong>Tasks</strong></summary>

1. **`AnonymizationErrors`** `AnonymizationErrors.cs`
   - Static class with factory methods:
     - `AnonymizationFailed(string fieldName, AnonymizationTechnique technique, string message)`
     - `PseudonymizationFailed(string fieldName, string keyId, string message)`
     - `DepseudonymizationFailed(string fieldName, string keyId, string message)`
     - `TokenizationFailed(string value, string message)`
     - `DetokenizationFailed(string token, string message)`
     - `KeyNotFound(string keyId)`
     - `KeyRotationFailed(string keyId, string message)`
     - `TokenNotFound(string token)`
     - `RiskAssessmentFailed(string message)`
     - `TechniqueNotSupported(AnonymizationTechnique technique, Type valueType)`
     - `InvalidProfile(string profileId, string message)`
     - `StoreError(string operation, string message)`
     - `ReIdentificationRiskTooHigh(double probability, double threshold)`
   - Error codes follow `anonymization.{category}` convention
   - Each method includes structured metadata dictionary

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina.Compliance.Anonymization (Issue #407).

CONTEXT:
- Phases 1-5 complete
- Need centralized error factory following established compliance module pattern

TASK:
- Create AnonymizationErrors static class with ~13 factory methods
- Each method returns EncinaError with error code, message, and metadata dictionary
- Error codes use "anonymization.{category}" convention

KEY RULES:
- Error codes: "anonymization.failed", "anonymization.key_not_found", "anonymization.token_not_found", etc.
- Metadata dictionary uses string keys matching parameter names
- Use EncinaErrors.Create() or direct EncinaError construction per project convention
- Full XML documentation on all methods

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/DSRErrors.cs (EXACT pattern)
```

</details>

---

### Phase 7: Observability (Diagnostics)

> **Goal**: Full OpenTelemetry instrumentation with activities, meters, and structured logging.

<details>
<summary><strong>Tasks</strong></summary>

1. **`AnonymizationDiagnostics`** `Diagnostics/AnonymizationDiagnostics.cs`
   - `ActivitySource`: `"Encina.Compliance.Anonymization"` version `"1.0"`
   - `Meter`: `"Encina.Compliance.Anonymization"` version `"1.0"`
   - Tag constants: `anon.technique`, `anon.outcome`, `anon.field_name`, `anon.key_id`, `anon.algorithm`, `anon.enforcement_mode`
   - Counters:
     - `anon.operations.total` (tags: technique, outcome)
     - `anon.pseudonymizations.total` (tags: algorithm, outcome)
     - `anon.tokenizations.total` (tags: outcome)
     - `anon.key_rotations.total` (tags: outcome)
     - `anon.pipeline.transformations.total` (tags: technique, outcome)
     - `anon.risk_assessments.total` (tags: outcome)
   - Histograms:
     - `anon.operation.duration` (ms)
     - `anon.pseudonymization.duration` (ms)
     - `anon.risk_assessment.duration` (ms)
   - Activity starters: `StartAnonymization()`, `StartPseudonymization()`, `StartTokenization()`, `StartKeyRotation()`, `StartRiskAssessment()`, `StartPipelineTransformation()`
   - Recording helpers: `RecordCompleted()`, `RecordFailed()`, `RecordSkipped()`, `RecordBlocked()`, `RecordWarned()`

2. **`AnonymizationLogMessages`** `Diagnostics/AnonymizationLogMessages.cs`
   - Event ID range: **8400-8499**
   - `internal static partial class` with `[LoggerMessage]` source generator
   - Subcategories:
     - 8400-8409: Auto-registration
     - 8410-8419: Health check
     - 8420-8429: Anonymization operations
     - 8430-8439: Pseudonymization operations
     - 8440-8449: Tokenization operations
     - 8450-8459: Key management
     - 8460-8469: Pipeline behavior
     - 8470-8479: Risk assessment
     - 8480-8489: Audit trail
     - 8490-8499: Reserved

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Encina.Compliance.Anonymization (Issue #407).

CONTEXT:
- Phases 1-6 complete
- Need full OpenTelemetry instrumentation following established compliance module pattern
- Event IDs allocated: 8400-8499 (non-colliding with GDPR 8100-8220, Consent 8200-8299, DSR 8300-8399)

TASK:
- Create AnonymizationDiagnostics with ActivitySource + Meter + tag constants + counters + histograms + activity helpers
- Create AnonymizationLogMessages with [LoggerMessage] source generator events in range 8400-8499
- Organize events into subcategories with clear allocation blocks

KEY RULES:
- ActivitySource name: "Encina.Compliance.Anonymization"
- Meter name: "Encina.Compliance.Anonymization"
- Version: "1.0"
- Tag prefix: "anon." (e.g., "anon.technique", "anon.outcome")
- Counter format: Meter.CreateCounter<long>("anon.{name}.total")
- Histogram format: Meter.CreateHistogram<double>("anon.{name}.duration", unit: "ms")
- Activity starters check HasListeners() first
- [LoggerMessage] is internal static partial class
- Event IDs: 8400-8499 range, allocated in blocks of 10

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs (EXACT pattern)
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DSRLogMessages.cs (EXACT pattern)
```

</details>

---

### Phase 8: Multi-Provider `ITokenMappingStore` Implementations

> **Goal**: Implement `ITokenMappingStore` for all 13 database providers.

<details>
<summary><strong>Tasks</strong></summary>

#### 8a. Persistence Entity & Mapper (in core package)

1. **`TokenMappingEntity`** `TokenMappingEntity.cs`
   - Properties matching database columns: `Id`, `Token`, `OriginalValueHash`, `EncryptedOriginalValue` (byte[]), `KeyId`, `CreatedAtUtc`, `ExpiresAtUtc`

2. **`TokenMappingMapper`** `TokenMappingMapper.cs`
   - `ToEntity(TokenMapping)` and `ToDomain(TokenMappingEntity)` static methods
   - Handle DateTimeOffset ↔ string conversions for SQLite

#### 8b. ADO.NET Providers (×4)

For each ADO provider (`Encina.Compliance.Anonymization.ADO.Sqlite`, `.ADO.SqlServer`, `.ADO.PostgreSQL`, `.ADO.MySQL`):

1. **`TokenMappingStoreADO{Provider}.cs`** — implements `ITokenMappingStore`
   - Constructor: `DbConnection`, `TimeProvider`
   - SQL for: `INSERT`, `SELECT BY token`, `SELECT BY hash`, `DELETE BY keyId`, `SELECT ALL`
   - Provider-specific SQL (see table below)

2. **`ServiceCollectionExtensions.cs`** — `AddEncinaAnonymization{Provider}()`
   - Registers `ITokenMappingStore` → `TokenMappingStoreADO{Provider}` before core registration

| Provider | Table Name | Parameter | LIMIT | DateTime |
|----------|-----------|-----------|-------|----------|
| SQLite | `anonymization_token_mappings` | `@param` | `LIMIT` | ISO 8601 string |
| SQL Server | `AnonymizationTokenMappings` | `@param` | `TOP` | `datetimeoffset` |
| PostgreSQL | `anonymization_token_mappings` | `@param` | `LIMIT` | `timestamptz` |
| MySQL | `` `anonymization_token_mappings` `` | `@param` | `LIMIT` | `DATETIME(6)` |

#### 8c. Dapper Providers (×4)

For each Dapper provider (`Encina.Compliance.Anonymization.Dapper.Sqlite`, `.Dapper.SqlServer`, `.Dapper.PostgreSQL`, `.Dapper.MySQL`):

1. **`TokenMappingStoreDapper{Provider}.cs`** — implements `ITokenMappingStore`
   - Uses `Dapper.QueryAsync<T>`, `ExecuteAsync`
   - Same SQL variations as ADO.NET

2. **`ServiceCollectionExtensions.cs`** — `AddEncinaAnonymizationDapper{Provider}()`

#### 8d. EF Core Providers (×4)

For each EF Core provider:

1. **`TokenMappingStoreEF.cs`** (shared implementation)
   - Uses `DbContext` with `DbSet<TokenMappingEntity>`
   - LINQ queries mapped to SQL

2. **`AnonymizationDbContext.cs`** or integration via `IEntityTypeConfiguration<TokenMappingEntity>`

3. **`ServiceCollectionExtensions.cs`** — `AddEncinaAnonymizationEntityFrameworkCore{Provider}()`

#### 8e. MongoDB Provider

1. **`TokenMappingStoreMongoDB.cs`** — implements `ITokenMappingStore`
   - Uses `IMongoCollection<TokenMappingEntity>`
   - BSON serialization, indexes on `Token` and `OriginalValueHash`

2. **`ServiceCollectionExtensions.cs`** — `AddEncinaAnonymizationMongoDB()`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Encina.Compliance.Anonymization (Issue #407).

CONTEXT:
- Phases 1-7 complete: core package with models, interfaces, implementations, diagnostics
- Now implementing ITokenMappingStore for all 13 database providers
- Each provider gets its own satellite NuGet package
- Follow the exact same multi-provider pattern used by Consent and DSR modules

TASK:
- Create TokenMappingEntity + TokenMappingMapper in core package
- Create 13 satellite packages (4 ADO, 4 Dapper, 4 EF Core, 1 MongoDB)
- Each satellite has: store implementation + ServiceCollectionExtensions
- Each ServiceCollectionExtensions registers ITokenMappingStore BEFORE core

KEY RULES:
- Store naming: TokenMappingStore{Provider} (e.g., TokenMappingStoreADOSqlite)
- DI method naming: AddEncinaAnonymization{Category}{Provider}() (e.g., AddEncinaAnonymizationADOSqlite)
- SQLite: ISO 8601 "O" format for DateTimeOffset, NEVER use datetime('now'), @param parameters
- SQL Server: TOP (@n), @param, datetimeoffset native
- PostgreSQL: LIMIT @n, @param, timestamptz, case-sensitive identifiers
- MySQL: LIMIT @n, @param, backtick identifiers, DATETIME(6)
- All stores: return Either<EncinaError, T>, catch exceptions → AnonymizationErrors.StoreError()
- Token column: UNIQUE index for fast lookups
- OriginalValueHash column: INDEX for deduplication lookups
- EncryptedOriginalValue: VARBINARY/BYTEA for encrypted data

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/DSRRequestEntity.cs (entity pattern)
- src/Encina.Compliance.DataSubjectRights/DSRRequestMapper.cs (mapper pattern)
- Existing ADO/Dapper/EFCore store implementations in Encina.Compliance.* satellite packages
```

</details>

---

### Phase 9: Testing & Documentation

> **Goal**: Comprehensive test coverage and complete documentation.

<details>
<summary><strong>Tasks</strong></summary>

#### 9a. Unit Tests (`tests/Encina.UnitTests/Compliance/Anonymization/`)

1. **AnonymizationTechnique tests** — each technique (generalization, suppression, perturbation, masking, swapping) tested with various data types
2. **DefaultAnonymizer tests** — attribute detection, field transformation, profile application
3. **DefaultPseudonymizer tests** — AES-256-GCM encrypt/decrypt round-trip, HMAC determinism, key-not-found error
4. **DefaultTokenizer tests** — tokenize/detokenize round-trip, deduplication (same value → same token), format variants
5. **InMemoryKeyProvider tests** — key generation, rotation, active key tracking
6. **InMemoryTokenMappingStore tests** — CRUD operations, lookup by hash, delete by keyId
7. **InMemoryAnonymizationAuditStore tests** — add/query operations
8. **AnonymizationPipelineBehavior tests** — attribute detection, enforcement modes, transformation delegation
9. **DefaultRiskAssessor tests** — k-anonymity calculation, l-diversity, t-closeness
10. **AnonymizationOptions tests** — fluent API, validation (invalid rotation days, invalid K)
11. **ServiceCollectionExtensions tests** — all services registered, TryAdd override works

#### 9b. Guard Tests (`tests/Encina.GuardTests/Compliance/Anonymization/`)

1. All public constructors and methods: `ArgumentNullException` for null parameters
2. Cover: `DefaultAnonymizer`, `DefaultPseudonymizer`, `DefaultTokenizer`, `InMemoryKeyProvider`, `InMemoryTokenMappingStore`, `AnonymizationPipelineBehavior`, `ServiceCollectionExtensions`

#### 9c. Property Tests (`tests/Encina.PropertyTests/Compliance/Anonymization/`)

1. **Round-trip invariant**: `Pseudonymize(x) → Depseudonymize → x` for any string
2. **Determinism invariant**: `HMAC(x, key)` always produces same output for same input
3. **Tokenize round-trip**: `Tokenize(x) → Detokenize → x` for any string
4. **K-anonymity invariant**: k-value never decreases when adding more generalization

#### 9d. Contract Tests (`tests/Encina.ContractTests/Compliance/Anonymization/`)

1. **ITokenMappingStore contract**: all 13 provider implementations follow same behavior
   - Store then retrieve by token
   - Store then retrieve by hash
   - Delete by keyId removes all matching
   - GetAll returns all stored mappings

#### 9e. Integration Tests (`tests/Encina.IntegrationTests/Compliance/Anonymization/`)

1. Per-provider integration tests using `[Collection("Provider-Database")]` shared fixtures
2. Tests: CRUD operations against real databases, concurrent writes, Unicode values

#### 9f. Load & Benchmark Justifications

1. **Load tests**: `tests/Encina.LoadTests/Compliance/Anonymization/Anonymization.md` — justification (not concurrent, single-request transformations)
2. **Benchmarks**: `tests/Encina.BenchmarkTests/.../Anonymization.md` — justification (crypto operations dominated by .NET BCL, not our code)

#### 9g. Documentation

1. **XML documentation** — all public APIs with `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`
2. **CHANGELOG.md** — add entry under `### Added` in Unreleased section
3. **Package README.md** — `src/Encina.Compliance.Anonymization/README.md`
4. **docs/features/anonymization.md** — usage guide with configuration examples
5. **docs/INVENTORY.md** — update with new files and packages
6. **PublicAPI.Unshipped.txt** — all new public symbols tracked
7. **Build verification**: `dotnet build --configuration Release` → 0 errors, 0 warnings
8. **Test verification**: `dotnet test` → all pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of Encina.Compliance.Anonymization (Issue #407).

CONTEXT:
- Phases 1-8 complete: full core package + 13 satellite provider packages
- Now creating comprehensive tests and documentation

TASK:
- Create unit tests for all core components (~30-40 test classes)
- Create guard tests for all public APIs
- Create property tests for cryptographic invariants
- Create contract tests for ITokenMappingStore across all providers
- Create integration tests using [Collection] shared fixtures
- Create load/benchmark justification .md files
- Update CHANGELOG.md, docs/INVENTORY.md, create README.md and feature docs

KEY RULES:
- Unit tests: AAA pattern, ONE assert per test, descriptive names
- Guard tests: test ArgumentNullException for every public parameter
- Property tests: FsCheck generators for arbitrary strings/bytes
- Contract tests: parameterized across all 13 providers
- Integration tests: [Collection("Provider-Database")] fixtures, ClearAllDataAsync in InitializeAsync
- SQLite integration: NEVER dispose shared connection, use [Collection("ADO-Sqlite")]
- Test output: artifacts/ directory only
- Coverage target: ≥85% line coverage

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/ (existing test organization)
- tests/Encina.IntegrationTests/ (existing integration patterns)
- tests/Encina.PropertyTests/ (property test patterns)
- tests/Encina.ContractTests/ (contract test patterns)
- tests/Encina.GuardTests/ (guard test patterns)
```

</details>

---

## Research

### GDPR Articles Covered

| Article | Topic | Relevance to This Module |
|---------|-------|--------------------------|
| Art. 4(5) | Definition of pseudonymization | Core definition — "processing in such a manner that data can no longer be attributed to a specific data subject without additional information" |
| Art. 25 | Data protection by design | Pseudonymization as a technical measure for DPIA |
| Art. 32 | Security of processing | Pseudonymization and encryption as security measures |
| Art. 89 | Safeguards for research/statistics | Anonymization required for research use outside GDPR scope |
| Art. 11 | Processing not requiring identification | Controller may demonstrate inability to identify data subject |

### EDPB Guidelines 01/2025 — Key Requirements

| Section | Requirement | Implementation |
|---------|-------------|----------------|
| 4.1 | Pseudonymization must use strong cryptographic algorithms | AES-256-GCM + HMAC-SHA256 |
| 4.2 | Additional information must be kept separately | `IKeyProvider` abstraction — keys separate from data |
| 4.3 | Key management and rotation | `RotateKeyAsync()`, configurable rotation interval |
| 4.4 | Risk of re-identification must be assessed | `IRiskAssessor` with k-anonymity, l-diversity, t-closeness |
| 5.1 | Anonymization must be irreversible | Generalization, suppression, perturbation — no reverse operation |
| 5.2 | Anonymized data falls outside GDPR | `IsAnonymizedAsync()` verification |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `EncinaError` | `Encina/` | All error returns |
| `Either<L, R>` | LanguageExt | ROP pattern on all operations |
| `IPipelineBehavior<TRequest, TResponse>` | `Encina/` | Pipeline behavior base |
| `IRequestContext` | `Encina/` | Context in pipeline |
| `IValidateOptions<T>` | Microsoft.Extensions | Options validation |
| Compliance module patterns | `Encina.Compliance.*` | DI, diagnostics, attributes, auto-registration |
| `TimeProvider` | .NET 10 BCL | Testable time operations |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Security | 8000-8099 | Security module |
| Encina.Compliance.GDPR | 8100-8220 | GDPR + Lawful Basis |
| Encina.Compliance.Consent | 8200-8299 | Consent management |
| Encina.Compliance.DataSubjectRights | 8300-8399 | DSR module |
| **Encina.Compliance.Anonymization** | **8400-8499** | **This module (new)** |

### Estimated File Count

| Category | Count | Notes |
|----------|-------|-------|
| Models & Enums | 14 | Core domain types |
| Abstractions (interfaces) | 8 | Service contracts |
| Default implementations | 10 | In-memory stores, services, techniques |
| Attributes | 3 | Declarative annotations |
| Pipeline behavior | 1 | Auto-transformation |
| Configuration & DI | 5 | Options, validator, extensions, descriptor, hosted service |
| Error factory | 1 | Centralized error creation |
| Diagnostics | 2 | ActivitySource + LoggerMessage |
| Entity & mapper | 2 | Persistence mapping |
| Health check | 1 | Health verification |
| **Core subtotal** | **~47** | |
| Satellite packages (×13) | ~39 | Store + DI per provider (3 files × 13) |
| **Total production** | **~86** | |
| Unit tests | ~35 | |
| Guard tests | ~10 | |
| Property tests | ~5 | |
| Contract tests | ~3 | |
| Integration tests | ~13 | Per provider |
| Justification docs | ~2 | Load + benchmark |
| Documentation | ~5 | README, feature doc, CHANGELOG, INVENTORY |
| **Total tests + docs** | **~73** | |
| **Grand total** | **~159** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete prompt for implementing all phases</strong></summary>

```
You are implementing Encina.Compliance.Anonymization (Issue #407) — a GDPR-compliant pseudonymization
and anonymization module for the Encina framework.

PROJECT CONTEXT:
- .NET 10, C# 14, nullable enabled
- ROP pattern: Either<EncinaError, T> for all operations
- Pre-1.0: best solution, no backward compatibility
- Follows existing compliance module patterns (Consent, DataSubjectRights, GDPR)
- New core package: Encina.Compliance.Anonymization
- 13 satellite packages for ITokenMappingStore database implementations

IMPLEMENTATION OVERVIEW:

Phase 1: Core Models & Enums (Model/ folder)
- AnonymizationTechnique enum (8 values)
- AnonymizationProfile, FieldAnonymizationRule, TokenMapping, RiskAssessmentResult sealed records
- PseudonymizationAlgorithm, TokenFormat, AnonymizationEnforcementMode, AnonymizationOperation enums
- KeyInfo, AnonymizationResult, TokenizationOptions sealed records

Phase 2: Core Interfaces (Abstractions/ folder)
- IAnonymizer, IPseudonymizer, ITokenizer, IKeyProvider, ITokenMappingStore
- IAnonymizationTechnique, IRiskAssessor, IAnonymizationAuditStore
- AnonymizationAuditEntry record

Phase 3: Default Implementations
- InMemoryKeyProvider, InMemoryTokenMappingStore, InMemoryAnonymizationAuditStore
- DefaultAnonymizer (reflection + attribute-based field scanning)
- DefaultPseudonymizer (AES-256-GCM + HMAC-SHA256)
- DefaultTokenizer (UUID/prefix/format-preserving + ITokenMappingStore)
- 5 technique strategies: Generalization, Suppression, Perturbation, DataMasking, Swapping
- DefaultRiskAssessor (k-anonymity, l-diversity, t-closeness)

Phase 4: Attributes & Pipeline Behavior
- [Anonymize], [Pseudonymize], [Tokenize] property attributes
- AnonymizationPipelineBehavior with static per-type caching + enforcement modes

Phase 5: Configuration, DI & Options
- AnonymizationOptions (sealed class, fluent API)
- AnonymizationOptionsValidator (IValidateOptions<T>)
- ServiceCollectionExtensions.AddEncinaAnonymization() with TryAdd pattern
- Auto-registration descriptor + hosted service
- Health check

Phase 6: Error Factory
- AnonymizationErrors static class (~13 factory methods)

Phase 7: Observability
- AnonymizationDiagnostics (ActivitySource + Meter + counters + histograms)
- AnonymizationLogMessages ([LoggerMessage] source generator, EventIds 8400-8499)

Phase 8: 13 Database Providers for ITokenMappingStore
- TokenMappingEntity + TokenMappingMapper in core
- ADO.NET: Sqlite, SqlServer, PostgreSQL, MySQL (4 packages)
- Dapper: Sqlite, SqlServer, PostgreSQL, MySQL (4 packages)
- EF Core: Sqlite, SqlServer, PostgreSQL, MySQL (4 packages)
- MongoDB (1 package)

Phase 9: Testing & Documentation
- Unit tests, guard tests, property tests, contract tests, integration tests
- CHANGELOG.md, README.md, feature docs, INVENTORY.md, PublicAPI.Unshipped.txt

KEY PATTERNS TO FOLLOW:
- Pipeline behavior: static readonly per-generic-type attribute caching
- DI: TryAdd* for all registrations, configure/validate options
- Stores: Either<EncinaError, T>, Option<T>, ValueTask
- Diagnostics: ActivitySource("Encina.Compliance.Anonymization"), Meter, [LoggerMessage]
- Health: DefaultName const, Tags static array, scoped IServiceProvider
- Auto-registration: IHostedService scanning assemblies for attributes
- Integration tests: [Collection("Provider-Database")] shared fixtures

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/ — primary reference for all patterns
- src/Encina.Compliance.Consent/ — simpler reference
- src/Encina.Compliance.GDPR/ — GDPR integration reference
- tests/Encina.UnitTests/Compliance/ — test organization
- tests/Encina.IntegrationTests/ — integration test patterns

CRITICAL RULES:
- SQLite: ISO 8601 "O" format, never datetime('now'), never dispose shared connection
- All public APIs must have XML documentation
- Event IDs: 8400-8499 (non-colliding)
- Coverage target: ≥85%
- Zero build warnings
- No [Obsolete], no backward compatibility
```

</details>

---

## Next Steps

1. **Review** this plan and validate design choices
2. **Publish** as comment on [Issue #407](https://github.com/dlrivada/Encina/issues/407)
3. **Implement** phase by phase (Phases 1-7 in core package, Phase 8 for providers, Phase 9 for tests)
4. **Final commit** with `Fixes #407` in message

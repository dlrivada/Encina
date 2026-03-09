# Implementation Plan: `XacmlXmlPolicySerializer` — XACML 3.0 XML Policy Import/Export

> **Issue**: [#692](https://github.com/dlrivada/Encina/issues/692)
> **Type**: Feature
> **Complexity**: Medium (4 phases, single package, ~15 files)
> **Estimated Scope**: ~1,800-2,200 lines of production code + ~2,000-2,500 lines of tests
> **Prerequisite**: #691 (Persistent PAP) — ✅ Completed

---

## Summary

Implement `XacmlXmlPolicySerializer` as an alternative `IPolicySerializer` that serializes/deserializes Encina ABAC policies to/from XACML 3.0 standard XML format (OASIS specification). This enables interoperability with external PAP/PDP systems (Axiomatics, AuthzForce, WSO2 Balana), compliance with government/industry standards (NIST SP 800-162, NATO STANAG, PCI-DSS 4.0), and positions Encina as the only .NET ABAC library with native XACML XML support.

The feature is **provider-independent** — the serializer sits above the storage layer. `IPolicyStore` stores whatever string `IPolicySerializer.Serialize()` returns. No database or provider-specific code is needed.

**Affected package**: `Encina.Security.ABAC` only.

---

## Design Choices

<details>
<summary><strong>1. Package Placement — Extend existing <code>Encina.Security.ABAC</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Security.ABAC.Xacml` package** | Clean separation, independent versioning | Unnecessary package for a single serializer class; XACML is already the core standard of the ABAC module |
| **B) Extend existing `Encina.Security.ABAC`** | Same package as `DefaultPolicySerializer`, no new NuGet dependency, consistent `IPolicySerializer` contract | Slightly larger package |
| **C) New `Encina.Interop.Xacml` package** | General interop package | Overkill; XACML serialization is tightly coupled to the ABAC domain model |

### Chosen Option: **B — Extend existing `Encina.Security.ABAC`**

### Rationale

- `XacmlXmlPolicySerializer` implements the same `IPolicySerializer` interface already defined in `Encina.Security.ABAC.Persistence`
- All XACML constants (`XACMLFunctionIds`, `XACMLDataTypes`, `XACMLStatusCodes`) are already in this package
- The serializer has no external dependencies beyond `System.Xml.Linq` (already in the BCL)
- Follows the established pattern: `DefaultPolicySerializer` lives in the same package
- New files go under `Persistence/Xacml/` subfolder for logical separation

</details>

<details>
<summary><strong>2. XML API Choice — <code>System.Xml.Linq</code> (LINQ to XML)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `System.Xml.Linq` (XDocument/XElement)** | Clean fluent API, immutable elements, excellent for tree construction/querying, no schema compilation needed | Slightly higher memory than streaming |
| **B) `XmlReader`/`XmlWriter`** | Streaming, lowest memory | Extremely verbose for nested structures, painful for recursive expression trees, hard to maintain |
| **C) `XmlSerializer`** | Automatic mapping | Requires DTOs mirroring XACML schema, poor control over namespaces, can't handle polymorphic `IExpression` |
| **D) `XmlDocument` (DOM)** | Familiar DOM API | Mutable, legacy API, less idiomatic in modern C# |

### Chosen Option: **A — `System.Xml.Linq` (LINQ to XML)**

### Rationale

- XACML policies are inherently tree-structured (PolicySet → Policy → Rule → Target → AnyOf → AllOf → Match)
- `XElement` construction maps naturally to the recursive domain model
- `XNamespace` handling is clean and type-safe (critical for XACML + Encina extension namespaces)
- Deserialization with LINQ queries is readable and maintainable
- Policy documents are small (KB range, not MB) — streaming overhead isn't warranted
- Same approach used by AuthzForce and other XACML implementations

</details>

<details>
<summary><strong>3. Function ID Mapping Strategy — Static bidirectional dictionary with <code>XACMLFunctionIds</code> integration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Static `FrozenDictionary<string, string>` with reflection from `XACMLFunctionIds`** | Single source of truth, auto-syncs with existing constants, `.NET 10 FrozenDictionary` for perf | Reflection at static init, URN pattern must be consistent |
| **B) Manual dictionary with all URN mappings** | Explicit, no reflection | Duplicates `XACMLFunctionIds` constants, maintenance burden, drift risk |
| **C) Convention-based URN construction** | Minimal code, auto-maps any function ID | Not all XACML functions follow the same URN pattern (different versions: 1.0, 2.0, 3.0) |
| **D) Attribute-based mapping on `XACMLFunctionIds` fields** | Declarative, clean | Requires modifying existing stable code, attribute overhead |

### Chosen Option: **A — Static `FrozenDictionary` with explicit mapping**

### Rationale

- XACML function URNs do NOT follow a single convention — they span versions 1.0, 2.0, and 3.0 of the spec
  - e.g., `urn:oasis:names:tc:xacml:1.0:function:string-equal` (v1.0)
  - e.g., `urn:oasis:names:tc:xacml:3.0:function:string-starts-with` (v3.0)
- Convention-based construction would require version metadata per function
- A static `FrozenDictionary<string, string>` (short ID → URN) with a reverse lookup is the safest approach
- The registry class `XacmlFunctionRegistry` provides `ToUrn(shortId)` and `ToShortId(urn)` methods
- All functions from `XACMLFunctionIds` are mapped; unknown URNs during deserialization are passed through with a warning log

</details>

<details>
<summary><strong>4. Encina Extension Properties — Custom XML namespace (<code>urn:encina:xacml:extensions:1.0</code>)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Custom XML namespace for Encina-specific attributes** | XSD-valid, external tools ignore gracefully, round-trip preserves data | Requires namespace declaration on root element |
| **B) XACML `<Description>` element abuse** | No custom namespace needed | Semantic misuse, fragile parsing, can't represent booleans/integers cleanly |
| **C) Drop Encina extensions on XML export** | Simplest, pure XACML | Lossy serialization, can't round-trip, `IsEnabled`/`Priority` lost |
| **D) XACML obligation with Encina metadata** | Technically valid XACML | Semantic hack, obligations have evaluation-time semantics |

### Chosen Option: **A — Custom XML namespace**

### Rationale

- `IsEnabled` and `Priority` are Encina-specific properties not present in XACML 3.0
- The XACML 3.0 XSD explicitly allows additional attributes from other namespaces via `xs:anyAttribute`
- External XACML tools (AuthzForce, WSO2 Balana) gracefully ignore unknown namespaces
- Round-trip is lossless: `PolicySet → XML → PolicySet` preserves all data
- On deserialization of external XACML (without `encina:` namespace), defaults are applied: `IsEnabled = true`, `Priority = 0`
- Namespace URI: `urn:encina:xacml:extensions:1.0`
- Prefix: `encina`

</details>

<details>
<summary><strong>5. DI Registration Strategy — <code>UseXacmlXmlSerializer()</code> fluent method on <code>ABACOptions</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fluent method `UseXacmlXmlSerializer()` on `ABACOptions`** | Consistent with other `Use*` options, discoverable, clean | Adds method to options class |
| **B) Separate `AddEncinaXacmlXml()` extension** | Independent registration | Confusing relationship with `AddEncinaABAC`, overkill for a serializer swap |
| **C) Manual `services.AddSingleton<IPolicySerializer, XacmlXmlPolicySerializer>()` before `AddEncinaABAC`** | Already works (TryAdd pattern) | Not discoverable, relies on ordering |

### Chosen Option: **A — Fluent method on `ABACOptions`**

### Rationale

- The existing registration uses `TryAddSingleton<IPolicySerializer, DefaultPolicySerializer>()`, meaning any prior registration wins
- `UseXacmlXmlSerializer()` sets an internal flag on `ABACOptions` that causes the DI registration to use `XacmlXmlPolicySerializer` instead
- Additionally, `RegisterXacmlXmlSerializer()` registers the XML serializer as a **named/keyed** service alongside the default JSON serializer (for import/export scenarios where both are needed)
- Both methods are chainable and documented with XML docs

</details>

<details>
<summary><strong>6. AttributeValue Serialization — XACML data type URIs with value text content</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Standard XACML `<AttributeValue DataType="...">text</AttributeValue>`** | XACML-compliant, interoperable | Must handle CLR-to-XML type conversion carefully |
| **B) JSON value embedded in XML CDATA** | Preserves CLR type fidelity | Non-standard, external tools can't parse |
| **C) XSD-typed XML elements** | Strong typing | Overly complex, not how XACML works |

### Chosen Option: **A — Standard XACML AttributeValue format**

### Rationale

- XACML 3.0 §7.3.1 defines `<AttributeValue>` with a `DataType` attribute (full URI) and text content
- `XACMLDataTypes` constants already provide the full URIs (`http://www.w3.org/2001/XMLSchema#string`, etc.)
- CLR values are converted to/from XML text using standard formatting:
  - `string` → text as-is
  - `bool` → `"true"`/`"false"` (XSD boolean)
  - `int`/`long` → numeric text
  - `double` → numeric text with InvariantCulture
  - `DateTime` → ISO 8601 (`"O"` format)
- On deserialization, the `DataType` URI determines how to parse the text content back to a CLR object

</details>

---

## Implementation Phases

### Phase 1: Core Mapping Infrastructure

<details>
<summary><strong>Tasks</strong></summary>

**Files to create (4):**

1. **`src/Encina.Security.ABAC/Persistence/Xacml/XacmlNamespaces.cs`**
   - `namespace Encina.Security.ABAC.Persistence.Xacml`
   - `internal static class XacmlNamespaces`
   - Constants:
     - `XacmlCore` = `XNamespace.Get("urn:oasis:names:tc:xacml:3.0:core:schema:wd-17")`
     - `Encina` = `XNamespace.Get("urn:encina:xacml:extensions:1.0")`
     - `EncinaPrefix` = `"encina"`
   - Element name constants (e.g., `PolicySet`, `Policy`, `Rule`, `Target`, `AnyOf`, `AllOf`, `Match`, etc.)
   - Attribute name constants (e.g., `PolicySetId`, `PolicyId`, `RuleId`, `RuleCombiningAlgId`, etc.)

2. **`src/Encina.Security.ABAC/Persistence/Xacml/XacmlFunctionRegistry.cs`**
   - `namespace Encina.Security.ABAC.Persistence.Xacml`
   - `internal static class XacmlFunctionRegistry`
   - `FrozenDictionary<string, string> ShortIdToUrn` — maps all `XACMLFunctionIds` constants to full XACML URNs
   - `FrozenDictionary<string, string> UrnToShortId` — reverse lookup
   - `static string ToUrn(string shortId)` — returns URN or passthrough if already a URN
   - `static string ToShortId(string urn)` — returns short ID or passthrough if unknown
   - `static bool IsKnownUrn(string urn)` — check if URN is in the registry

3. **`src/Encina.Security.ABAC/Persistence/Xacml/XacmlMappingExtensions.cs`**
   - `namespace Encina.Security.ABAC.Persistence.Xacml`
   - `internal static class XacmlMappingExtensions`
   - `static string ToXacmlUrn(this AttributeCategory category)` — enum to URN string
   - `static AttributeCategory ToAttributeCategory(string urn)` — URN to enum (with fallback error)
   - `static string ToXacmlUrn(this CombiningAlgorithmId algorithm, bool isRuleCombining)` — enum to URN (rule vs policy prefix)
   - `static CombiningAlgorithmId ToCombiningAlgorithmId(string urn)` — URN to enum
   - `static string ToXacmlString(this Effect effect)` — `Permit`/`Deny` (XACML uses PascalCase)
   - `static Effect ToEffect(string value)` — string to Effect enum
   - `static string ToXacmlString(this FulfillOn fulfillOn)` — enum to XACML string
   - `static FulfillOn ToFulfillOn(string value)` — string to FulfillOn enum
   - `static string FormatXacmlValue(object? value, string dataType)` — CLR value to XML text
   - `static object? ParseXacmlValue(string? text, string dataType)` — XML text to CLR value

4. **`src/Encina.Security.ABAC/Persistence/Xacml/XacmlDataTypeMap.cs`**
   - `namespace Encina.Security.ABAC.Persistence.Xacml`
   - `internal static class XacmlDataTypeMap`
   - `static string InferDataType(object? value)` — CLR type to XACML DataType URI (for export when DataType is missing)
   - `static FrozenDictionary<string, Func<string, object?>> Parsers` — DataType URI → parser function

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
You are working on the Encina .NET 10 library (C# 14). The project follows Railway Oriented Programming
(Either<EncinaError, T>) and has an ABAC module implementing XACML 3.0.

The existing code includes:
- XACMLFunctionIds (src/Encina.Security.ABAC/XACMLFunctionIds.cs) — short function IDs like "string-equal"
- XACMLDataTypes (src/Encina.Security.ABAC/XACMLDataTypes.cs) — full URI data types like "http://www.w3.org/2001/XMLSchema#string"
- AttributeCategory enum (Subject, Resource, Environment, Action)
- CombiningAlgorithmId enum (DenyOverrides, PermitOverrides, FirstApplicable, etc.)
- Effect enum (Permit, Deny, NotApplicable, Indeterminate)
- FulfillOn enum (Permit, Deny)

TASK:
Create the XACML XML mapping infrastructure under src/Encina.Security.ABAC/Persistence/Xacml/:

1. XacmlNamespaces.cs — Static class with XNamespace constants:
   - XacmlCore = "urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"
   - Encina = "urn:encina:xacml:extensions:1.0"
   - Element/attribute name constants as static readonly XName fields (e.g., PolicySet, Policy, Rule, etc.)

2. XacmlFunctionRegistry.cs — Bidirectional mapping between XACMLFunctionIds short IDs and full URNs:
   - Use FrozenDictionary<string, string> for both directions
   - URN patterns (version varies by function):
     - Equality/comparison/string/arithmetic: urn:oasis:names:tc:xacml:1.0:function:{short-id}
     - Bag functions: urn:oasis:names:tc:xacml:1.0:function:{short-id}
     - Set functions: urn:oasis:names:tc:xacml:1.0:function:{short-id}
     - Higher-order: urn:oasis:names:tc:xacml:3.0:function:{short-id}
     - String starts-with/ends-with/contains/substring: urn:oasis:names:tc:xacml:3.0:function:{short-id}
     - Logical (and/or/not/n-of): urn:oasis:names:tc:xacml:1.0:function:{short-id}
     - Type conversion: urn:oasis:names:tc:xacml:3.0:function:{short-id}
     - Regex: urn:oasis:names:tc:xacml:1.0:function:{short-id}
   - Methods: ToUrn(shortId), ToShortId(urn), IsKnownUrn(urn)
   - If a shortId is not found, check if it's already a full URN (passthrough)

3. XacmlMappingExtensions.cs — Extension methods for enum-to-URN mapping:
   - AttributeCategory → URN:
     - Subject → urn:oasis:names:tc:xacml:1.0:subject-category:access-subject
     - Resource → urn:oasis:names:tc:xacml:3.0:attribute-category:resource
     - Action → urn:oasis:names:tc:xacml:3.0:attribute-category:action
     - Environment → urn:oasis:names:tc:xacml:3.0:attribute-category:environment
   - CombiningAlgorithmId → URN (two variants: rule-combining-algorithm and policy-combining-algorithm):
     - DenyOverrides → urn:oasis:names:tc:xacml:3.0:{type}:deny-overrides
     - PermitOverrides → urn:oasis:names:tc:xacml:3.0:{type}:permit-overrides
     - FirstApplicable → urn:oasis:names:tc:xacml:1.0:{type}:first-applicable
     - OnlyOneApplicable → urn:oasis:names:tc:xacml:1.0:{type}:only-one-applicable
     - DenyUnlessPermit → urn:oasis:names:tc:xacml:3.0:{type}:deny-unless-permit
     - PermitUnlessDeny → urn:oasis:names:tc:xacml:3.0:{type}:permit-unless-deny
     - OrderedDenyOverrides → urn:oasis:names:tc:xacml:3.0:{type}:ordered-deny-overrides
     - OrderedPermitOverrides → urn:oasis:names:tc:xacml:3.0:{type}:ordered-permit-overrides
   - Value formatting: CLR object to XACML text (using InvariantCulture, ISO 8601 for dates)
   - Value parsing: XACML text to CLR object (based on DataType URI)

4. XacmlDataTypeMap.cs — CLR type to DataType inference and parsing:
   - string → XACMLDataTypes.String
   - bool → XACMLDataTypes.Boolean
   - int/long → XACMLDataTypes.Integer
   - double/float → XACMLDataTypes.Double
   - DateTime/DateTimeOffset → XACMLDataTypes.DateTime
   - Uri → XACMLDataTypes.AnyURI

KEY RULES:
- .NET 10 / C# 14
- All classes internal (not public API)
- XML doc comments on all members
- Use System.Collections.Frozen.FrozenDictionary for immutable lookups
- No exceptions for business logic — return defaults or use Either where appropriate
- Null-safe with nullable reference types

REFERENCE FILES:
- src/Encina.Security.ABAC/XACMLFunctionIds.cs (all short function IDs)
- src/Encina.Security.ABAC/XACMLDataTypes.cs (data type URIs)
- src/Encina.Security.ABAC/Model/AttributeCategory.cs
- src/Encina.Security.ABAC/Model/CombiningAlgorithmId.cs
- src/Encina.Security.ABAC/Model/Effect.cs
- src/Encina.Security.ABAC/Model/FulfillOn.cs
```

</details>

---

### Phase 2: XacmlXmlPolicySerializer Implementation

<details>
<summary><strong>Tasks</strong></summary>

**Files to create (1):**

1. **`src/Encina.Security.ABAC/Persistence/Xacml/XacmlXmlPolicySerializer.cs`**
   - `namespace Encina.Security.ABAC.Persistence.Xacml`
   - `public sealed class XacmlXmlPolicySerializer : IPolicySerializer`
   - Constructor: `XacmlXmlPolicySerializer(ILogger<XacmlXmlPolicySerializer> logger)`
   - **Serialization methods** (Encina model → XACML 3.0 XML):
     - `string Serialize(PolicySet policySet)` — builds `XDocument` with XACML root element
     - `string Serialize(Policy policy)` — builds `XDocument` with `<Policy>` root
     - `private XElement SerializePolicySetElement(PolicySet policySet)` — recursive
     - `private XElement SerializePolicyElement(Policy policy)`
     - `private XElement SerializeRuleElement(Rule rule)`
     - `private XElement SerializeTargetElement(Target target)`
     - `private XElement SerializeAnyOfElement(AnyOf anyOf)`
     - `private XElement SerializeAllOfElement(AllOf allOf)`
     - `private XElement SerializeMatchElement(Match match)`
     - `private XElement SerializeConditionElement(Apply condition)` — wraps in `<Condition><Apply ...>`
     - `private XElement SerializeApplyElement(Apply apply)` — recursive expression tree
     - `private XElement SerializeExpressionElement(IExpression expression)` — dispatch by type
     - `private XElement SerializeAttributeDesignatorElement(AttributeDesignator designator)`
     - `private XElement SerializeAttributeValueElement(AttributeValue value)`
     - `private XElement SerializeVariableReferenceElement(VariableReference reference)`
     - `private XElement SerializeVariableDefinitionElement(VariableDefinition definition)`
     - `private XElement SerializeObligationExpressionsElement(IReadOnlyList<Obligation> obligations)`
     - `private XElement SerializeAdviceExpressionsElement(IReadOnlyList<AdviceExpression> advice)`
     - `private XElement SerializeAttributeAssignmentElement(AttributeAssignment assignment)`
   - **Deserialization methods** (XACML 3.0 XML → Encina model):
     - `Either<EncinaError, PolicySet> DeserializePolicySet(string data)`
     - `Either<EncinaError, Policy> DeserializePolicy(string data)`
     - `private PolicySet ParsePolicySetElement(XElement element)`
     - `private Policy ParsePolicyElement(XElement element)`
     - `private Rule ParseRuleElement(XElement element)`
     - `private Target ParseTargetElement(XElement element)`
     - `private AnyOf ParseAnyOfElement(XElement element)`
     - `private AllOf ParseAllOfElement(XElement element)`
     - `private Match ParseMatchElement(XElement element)`
     - `private Apply? ParseConditionElement(XElement? element)` — unwraps `<Condition><Apply ...>`
     - `private Apply ParseApplyElement(XElement element)`
     - `private IExpression ParseExpressionElement(XElement element)` — dispatch by element name
     - `private AttributeDesignator ParseAttributeDesignatorElement(XElement element)`
     - `private AttributeValue ParseAttributeValueElement(XElement element)`
     - `private VariableReference ParseVariableReferenceElement(XElement element)`
     - `private VariableDefinition ParseVariableDefinitionElement(XElement element)`
     - `private IReadOnlyList<Obligation> ParseObligationExpressions(XElement? element)`
     - `private IReadOnlyList<AdviceExpression> ParseAdviceExpressions(XElement? element)`
     - `private AttributeAssignment ParseAttributeAssignmentElement(XElement element)`
   - **Encina extension handling**:
     - On serialize: add `encina:IsEnabled` and `encina:Priority` attributes on `<PolicySet>` and `<Policy>` elements
     - On deserialize: read `encina:IsEnabled` (default `true`) and `encina:Priority` (default `0`) if present
   - **Error handling**:
     - Wrap all deserialization in try-catch, return `Either.Left(ABACErrors.DeserializationFailed(...))`
     - Log warnings for unknown elements/attributes (graceful skip)
   - **XML declaration**: `<?xml version="1.0" encoding="utf-8"?>` with indented formatting

**Files to modify (2):**

2. **`src/Encina.Security.ABAC/ABACOptions.cs`** — Add fluent methods:
   - `internal bool UseXacmlXml { get; set; }` — flag for DI registration
   - `internal bool RegisterXacmlXmlAsKeyed { get; set; }` — flag for keyed registration
   - `public ABACOptions UseXacmlXmlSerializer()` — sets `UseXacmlXml = true`, returns `this`
   - `public ABACOptions RegisterXacmlXmlSerializer()` — sets `RegisterXacmlXmlAsKeyed = true`, returns `this`

3. **`src/Encina.Security.ABAC/ServiceCollectionExtensions.cs`** — Update DI registration:
   - When `optionsInstance.UseXacmlXml`:
     - Register `IPolicySerializer` → `XacmlXmlPolicySerializer` (instead of `DefaultPolicySerializer`)
   - When `optionsInstance.RegisterXacmlXmlAsKeyed`:
     - Register `XacmlXmlPolicySerializer` as a keyed singleton (`"xacml-xml"`)
     - Keep `DefaultPolicySerializer` as the primary `IPolicySerializer`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

````
CONTEXT:
You are working on the Encina .NET 10 library (C# 14). The ABAC module has an existing
IPolicySerializer interface and DefaultPolicySerializer (JSON). You are implementing an
alternative XacmlXmlPolicySerializer that converts between Encina domain models and XACML 3.0 XML.

Phase 1 is complete — the following infrastructure exists:
- XacmlNamespaces.cs — XNamespace constants for XACML 3.0 and Encina extensions
- XacmlFunctionRegistry.cs — Bidirectional short-ID ↔ URN mapping
- XacmlMappingExtensions.cs — Enum ↔ URN conversion
- XacmlDataTypeMap.cs — CLR type ↔ DataType URI mapping

TASK:
1. Create XacmlXmlPolicySerializer.cs implementing IPolicySerializer:

   SERIALIZATION (Encina → XML):
   - Build XDocument with proper namespace declarations
   - Root element: PolicySet or Policy with xmlns and xmlns:encina
   - Recursive traversal: PolicySet → Policy → Rule → Target/Condition/Obligations/Advice
   - Target: Target → AnyOf → AllOf → Match
   - Condition: Condition wrapping an Apply element
   - Expressions: Apply (FunctionId="urn:..."), AttributeDesignator, AttributeValue, VariableReference
   - Obligations: ObligationExpressions → ObligationExpression
   - Advice: AdviceExpressions → AdviceExpression
   - VariableDefinitions: VariableDefinition (VariableId="...") → expression
   - Encina extensions: encina:IsEnabled="true", encina:Priority="10" as attributes
   - Output: indented XML string with XML declaration

   DESERIALIZATION (XML → Encina):
   - Parse XDocument from string
   - Detect root element: PolicySet or Policy
   - Reverse mapping for all elements
   - Handle missing Encina extensions gracefully (defaults: IsEnabled=true, Priority=0)
   - Return Either of EncinaError or PolicySet/Policy
   - On XML parse error: return Left(ABACErrors.DeserializationFailed(...))
   - On unknown elements: log warning, skip

   XACML 3.0 ELEMENT STRUCTURE (reference):

   PolicySet (root, attrs: PolicySetId, PolicyCombiningAlgId, xmlns, xmlns:encina,
              encina:IsEnabled, encina:Priority)
   ├── Description (text)
   ├── Target
   │   └── AnyOf
   │       └── AllOf
   │           └── Match (attr: MatchId → function URN)
   │               ├── AttributeValue (attr: DataType → URI, text content → value)
   │               └── AttributeDesignator (attrs: Category, AttributeId, DataType, MustBePresent)
   ├── Policy (attrs: PolicyId, RuleCombiningAlgId)
   │   ├── VariableDefinition (attr: VariableId)
   │   │   └── Apply (attr: FunctionId → URN) or other expression
   │   └── Rule (attrs: RuleId, Effect → "Permit"/"Deny")
   │       ├── Target (same structure)
   │       ├── Condition
   │       │   └── Apply (attr: FunctionId → URN)
   │       │       ├── AttributeDesignator / AttributeValue / VariableReference / nested Apply
   │       │       └── ...
   │       ├── ObligationExpressions
   │       │   └── ObligationExpression (attrs: ObligationId, FulfillOn)
   │       │       └── AttributeAssignmentExpression (attrs: AttributeId, Category?)
   │       │           └── AttributeValue (attr: DataType, text content)
   │       └── AdviceExpressions
   │           └── AdviceExpression (attrs: AdviceId, AppliesTo)
   │               └── AttributeAssignmentExpression (attrs: AttributeId)
   │                   └── AttributeValue (attr: DataType, text content)
   └── PolicySet (nested, same structure — recursive)

2. Modify ABACOptions.cs — Add:
   - internal bool UseXacmlXml { get; set; }
   - internal bool RegisterXacmlXmlAsKeyed { get; set; }
   - public ABACOptions UseXacmlXmlSerializer() { UseXacmlXml = true; return this; }
   - public ABACOptions RegisterXacmlXmlSerializer() { RegisterXacmlXmlAsKeyed = true; return this; }
   - XML doc comments on both methods

3. Modify ServiceCollectionExtensions.cs — In the persistent PAP section:
   - If optionsInstance.UseXacmlXml: register IPolicySerializer → XacmlXmlPolicySerializer
   - Else: keep DefaultPolicySerializer (existing behavior)
   - If optionsInstance.RegisterXacmlXmlAsKeyed: additionally register XacmlXmlPolicySerializer
     as a keyed service with key "xacml-xml"

KEY RULES:
- .NET 10 / C# 14
- ROP: Either of EncinaError, T for deserialization
- Use ABACErrors.SerializationFailed() and ABACErrors.DeserializationFailed() for errors
- Constructor takes ILogger of XacmlXmlPolicySerializer
- Use System.Xml.Linq (XDocument, XElement, XAttribute, XNamespace)
- Indented XML output with SaveOptions.None (default formatting)
- Handle null/empty values gracefully
- ArgumentNullException.ThrowIfNull for public method parameters

REFERENCE FILES:
- src/Encina.Security.ABAC/Persistence/IPolicySerializer.cs
- src/Encina.Security.ABAC/Persistence/DefaultPolicySerializer.cs
- src/Encina.Security.ABAC/Model/ (all domain model records)
- src/Encina.Security.ABAC/ABACErrors.cs
- src/Encina.Security.ABAC/ABACOptions.cs
- src/Encina.Security.ABAC/ServiceCollectionExtensions.cs
- src/Encina.Security.ABAC/Persistence/Xacml/XacmlNamespaces.cs (Phase 1)
- src/Encina.Security.ABAC/Persistence/Xacml/XacmlFunctionRegistry.cs (Phase 1)
- src/Encina.Security.ABAC/Persistence/Xacml/XacmlMappingExtensions.cs (Phase 1)
- src/Encina.Security.ABAC/Persistence/Xacml/XacmlDataTypeMap.cs (Phase 1)
````

</details>

---

### Phase 3: Observability & Diagnostics

<details>
<summary><strong>Tasks</strong></summary>

**Files to modify (2):**

1. **`src/Encina.Security.ABAC/Diagnostics/ABACDiagnostics.cs`** — Add XACML XML metrics:
   - `Counter<long> XacmlXmlSerializeTotal` — tagged counter with `abac.format:xacml_xml`
   - `Counter<long> XacmlXmlDeserializeTotal` — tagged counter
   - `Counter<long> XacmlXmlErrorTotal` — serialization/deserialization failures
   - `Histogram<long> XacmlXmlSizeBytes` — XML document size in bytes
   - Activity helpers:
     - `StartXacmlXmlSerialize(string entityType)` → Activity (span name: `ABAC.Serialize.XacmlXml`)
     - `StartXacmlXmlDeserialize(string entityType)` → Activity (span name: `ABAC.Deserialize.XacmlXml`)

2. **`src/Encina.Security.ABAC/Diagnostics/ABACLogMessages.cs`** — Add XACML XML log messages:
   - **EventId range: 9050-9059** (XACML XML serialization)
   - `9050` — `XacmlXmlSerializationCompleted` (Debug): "Serialized {EntityType} '{EntityId}' to XACML XML ({XmlSize} bytes, {DurationMs:F2}ms)"
   - `9051` — `XacmlXmlDeserializationCompleted` (Debug): "Deserialized {EntityType} from XACML XML ({XmlSize} bytes, {DurationMs:F2}ms)"
   - `9052` — `XacmlXmlDeserializationFailed` (Warning): "Failed to deserialize XACML XML to {EntityType}: {ErrorMessage}"
   - `9053` — `XacmlXmlExtensionsMissing` (Information): "Deserialized XACML XML for {EntityType} '{EntityId}' without Encina extensions (defaults applied)"
   - `9054` — `XacmlXmlUnknownFunction` (Warning): "Encountered unknown XACML function URN '{FunctionUrn}' during deserialization (passed through as-is)"
   - `9055` — `XacmlXmlUnknownElement` (Debug): "Skipping unknown XACML element '{ElementName}' during deserialization"

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
You are working on the Encina .NET 10 library. The ABAC module has existing observability in:
- ABACDiagnostics.cs — ActivitySource + Meter with counters and histograms
- ABACLogMessages.cs — LoggerMessage source-generated methods (EventIds 9000-9040)

TASK:
Extend the existing diagnostics files to add XACML XML serialization observability.

1. In ABACDiagnostics.cs, add:
   - Counter<long> XacmlXmlSerializeTotal (name: "abac.pap.serialize.xacml_xml.total")
   - Counter<long> XacmlXmlDeserializeTotal (name: "abac.pap.deserialize.xacml_xml.total")
   - Counter<long> XacmlXmlErrorTotal (name: "abac.pap.xacml_xml.errors")
   - Histogram<long> XacmlXmlSizeBytes (name: "abac.pap.xacml_xml.size_bytes", unit: "By")
   - Activity helpers StartXacmlXmlSerialize/StartXacmlXmlDeserialize following the existing
     StartPapSerialize pattern (check HasListeners, set tags for operation and entity type)

2. In ABACLogMessages.cs, add EventIds 9050-9055:
   - 9050: XacmlXmlSerializationCompleted (Debug)
   - 9051: XacmlXmlDeserializationCompleted (Debug)
   - 9052: XacmlXmlDeserializationFailed (Warning)
   - 9053: XacmlXmlExtensionsMissing (Information)
   - 9054: XacmlXmlUnknownFunction (Warning)
   - 9055: XacmlXmlUnknownElement (Debug)

   Follow the exact pattern of existing messages (LoggerMessage attribute, internal static partial,
   parameter naming conventions).

3. Update XacmlXmlPolicySerializer to use these diagnostics:
   - Call ABACDiagnostics.StartXacmlXmlSerialize/Deserialize around operations
   - Record metrics (counters, histogram for XML size)
   - Call log messages at appropriate points
   - Record success/failure on Activity

KEY RULES:
- EventIds 9050-9059 reserved for XACML XML serialization (non-colliding with 9000-9040)
- Follow exact existing patterns for ActivitySource helpers, Meter counters, LoggerMessage methods
- Use the established tag names (abac.operation, abac.entity_type, abac.status)

REFERENCE FILES:
- src/Encina.Security.ABAC/Diagnostics/ABACDiagnostics.cs
- src/Encina.Security.ABAC/Diagnostics/ABACLogMessages.cs
- src/Encina.Security.ABAC/Persistence/Xacml/XacmlXmlPolicySerializer.cs (Phase 2)
```

</details>

---

### Phase 4: Testing & Documentation

<details>
<summary><strong>Tasks</strong></summary>

**Test files to create (6):**

1. **`tests/Encina.UnitTests/Security/ABAC/Persistence/Xacml/XacmlXmlPolicySerializerTests.cs`**
   - Namespace: `Encina.UnitTests.Security.ABAC.Persistence.Xacml`
   - **Sections** (using `#region`):
     - PolicySet Round-Trip (serialize then deserialize, verify all properties preserved)
     - Policy Round-Trip (same pattern)
     - Nested PolicySet Round-Trip (PolicySet with child PolicySets and Policies)
     - Complex Expressions Round-Trip (Apply with nested Apply, VariableReference, etc.)
     - Enum Serialization (all AttributeCategory, CombiningAlgorithmId, Effect, FulfillOn values)
     - XML Format Verification (check output contains correct XACML namespace, element names, attributes)
     - Encina Extensions (IsEnabled, Priority preserved; missing extensions use defaults)
     - Deserialization Error Handling (invalid XML, wrong root element, missing required attributes)
     - External XACML Compatibility (parse XACML XML without Encina extensions)
     - Obligation and Advice Round-Trip
     - VariableDefinition Round-Trip
   - Use helpers: `CreateMinimalPolicySet()`, `CreatePolicyWithRules()`, `CreateNestedPolicySet()`, `CreateComplexCondition()`
   - Assertions: FluentAssertions, `Should().Be()`, `result.IsRight.Should().BeTrue()`

2. **`tests/Encina.UnitTests/Security/ABAC/Persistence/Xacml/XacmlFunctionRegistryTests.cs`**
   - Test all functions in `XACMLFunctionIds` have a URN mapping
   - Test reverse lookup (URN → short ID)
   - Test unknown URN passthrough
   - Test unknown short ID passthrough
   - Test `IsKnownUrn` for known and unknown URNs

3. **`tests/Encina.UnitTests/Security/ABAC/Persistence/Xacml/XacmlMappingExtensionsTests.cs`**
   - Test all `AttributeCategory` → URN mappings and reverse
   - Test all `CombiningAlgorithmId` → URN mappings (both rule and policy variants)
   - Test all `Effect` → string mappings
   - Test all `FulfillOn` → string mappings
   - Test value formatting for all data types
   - Test value parsing for all data types

4. **`tests/Encina.GuardTests/Security/ABAC/Persistence/Xacml/XacmlXmlPolicySerializerGuardTests.cs`**
   - `Serialize_NullPolicySet_ThrowsArgumentNullException`
   - `Serialize_NullPolicy_ThrowsArgumentNullException`
   - `DeserializePolicySet_NullData_ThrowsArgumentNullException`
   - `DeserializePolicySet_EmptyData_ThrowsArgumentNullException`
   - `DeserializePolicy_NullData_ThrowsArgumentNullException`
   - `DeserializePolicy_EmptyData_ThrowsArgumentNullException`

5. **`tests/Encina.ContractTests/Security/ABAC/Persistence/Xacml/PolicySerializerContractTests.cs`**
   - Shared contract: both `DefaultPolicySerializer` and `XacmlXmlPolicySerializer` must pass
   - Use `[Theory]` with `MemberData` providing both serializer instances
   - Contract tests:
     - `Serialize_MinimalPolicySet_DeserializesBack` (round-trip)
     - `Serialize_PolicyWithAllRuleEffects_DeserializesBack`
     - `Serialize_PolicyWithConditions_DeserializesBack`
     - `Serialize_PolicyWithObligationsAndAdvice_DeserializesBack`
     - `Serialize_NestedPolicySets_DeserializesBack`
     - `Deserialize_InvalidData_ReturnsLeft` (both JSON and XML should fail gracefully)

6. **`tests/Encina.PropertyTests/Security/ABAC/Persistence/Xacml/XacmlXmlRoundTripPropertyTests.cs`**
   - FsCheck property-based tests with arbitrary generators for:
     - `PolicySet` with random policies, rules, targets
     - `Policy` with random rules, conditions
     - `Rule` with random effects, targets, conditions
   - Property: `SerializeThenDeserialize_PreservesAllData`
   - `MaxTest = 50` for reasonable execution time

**Test justification files to create (2):**

7. **`tests/Encina.IntegrationTests/Security/ABAC/Persistence/Xacml/XacmlXmlPolicySerializer.md`**
   - Justification: serializer is a pure in-memory transformation with no I/O
   - Adequate coverage from unit, contract, and property tests

8. **`tests/Encina.LoadTests/Security/ABAC/Persistence/Xacml/XacmlXmlPolicySerializer.md`**
   - Justification: administrative operation, not a hot path
   - Serialization happens on policy save/load, not per-request

**Benchmark file (optional, useful for docs):**

9. **`tests/Encina.BenchmarkTests/Encina.Benchmarks/Security/ABAC/PolicySerializerBenchmarks.cs`** (optional)
   - Compare JSON vs XACML XML serialization performance
   - `[Benchmark] SerializePolicySetJson()`, `[Benchmark] SerializePolicySetXacmlXml()`
   - `[Benchmark] DeserializePolicySetJson()`, `[Benchmark] DeserializePolicySetXacmlXml()`

**Documentation files to create/update (6):**

10. **`docs/features/abac/xacml/xacml-xml-serialization.md`** — New feature documentation:
    - Overview & motivation
    - Configuration (`UseXacmlXmlSerializer()`, `RegisterXacmlXmlSerializer()`)
    - XML format reference (example XACML XML output)
    - Encina extensions namespace
    - Import from external XACML systems
    - Export for compliance auditing
    - Round-trip guarantees

11. **`docs/features/abac/reference/configuration.md`** — Update:
    - Add `UseXacmlXmlSerializer()` option
    - Add `RegisterXacmlXmlSerializer()` option

12. **`CHANGELOG.md`** — Add entry under Unreleased:
    - `### Added` section for XACML XML serializer

13. **`src/Encina.Security.ABAC/PublicAPI.Unshipped.txt`** — Update:
    - Add `XacmlXmlPolicySerializer` class
    - Add `ABACOptions.UseXacmlXmlSerializer()` method
    - Add `ABACOptions.RegisterXacmlXmlSerializer()` method

14. **`docs/INVENTORY.md`** — Update:
    - Add new files under `Encina.Security.ABAC`

15. **XML doc comments** — Ensure all new public APIs have complete XML docs:
    - `summary`, `remarks`, `param`, `returns`, `example` tags where appropriate

**Build verification:**

16. `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings
17. `dotnet test` → all tests pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
You are working on the Encina .NET 10 library. The ABAC module now has:
- XacmlXmlPolicySerializer (Phase 2) — XACML 3.0 XML serializer implementing IPolicySerializer
- XacmlFunctionRegistry, XacmlMappingExtensions, XacmlNamespaces, XacmlDataTypeMap (Phase 1)
- Observability integrated (Phase 3)

Existing test patterns are in:
- tests/Encina.UnitTests/Security/ABAC/Persistence/DefaultPolicySerializerTests.cs (618 lines, 8 sections)
- tests/Encina.ContractTests/Security/ABAC/PersistentPAPContractTests.cs (reflection-based contract tests)
- tests/Encina.GuardTests/Security/ABAC/PersistentPAPGuardTests.cs (null/param validation)
- tests/Encina.PropertyTests/Security/ABAC/PersistentPAPPropertyTests.cs (FsCheck round-trip)

TASK:
Create comprehensive tests and documentation:

1. UNIT TESTS (tests/Encina.UnitTests/Security/ABAC/Persistence/Xacml/):
   a) XacmlXmlPolicySerializerTests.cs — ~500-600 lines
      - Use FluentAssertions, #region sections, AAA pattern
      - Helper methods: CreateMinimalPolicySet(), CreatePolicyWithRules(), etc.
      - Sections: PolicySet Round-Trip, Policy Round-Trip, Nested PolicySet, Complex Expressions,
        Enum Serialization, XML Format, Encina Extensions, Error Handling, External XACML,
        Obligations/Advice, VariableDefinitions
      - Test round-trip: serialize → deserialize → compare all properties
      - Test XML format: verify namespace declarations, element names, attribute values
      - Test graceful defaults for missing Encina extensions

   b) XacmlFunctionRegistryTests.cs — ~100-150 lines
      - All XACMLFunctionIds constants have URN mapping
      - Reverse lookup works
      - Unknown URN/ID passthrough

   c) XacmlMappingExtensionsTests.cs — ~200-250 lines
      - All enum-to-URN mappings verified
      - Value formatting/parsing for all data types

2. GUARD TESTS (tests/Encina.GuardTests/Security/ABAC/Persistence/Xacml/):
   XacmlXmlPolicySerializerGuardTests.cs — null/empty parameter validation

3. CONTRACT TESTS (tests/Encina.ContractTests/Security/ABAC/Persistence/Xacml/):
   PolicySerializerContractTests.cs — shared contract between DefaultPolicySerializer and XacmlXmlPolicySerializer
   Use [Theory] with MemberData providing both serializer instances
   Both must pass identical round-trip and error handling contracts

4. PROPERTY TESTS (tests/Encina.PropertyTests/Security/ABAC/Persistence/Xacml/):
   XacmlXmlRoundTripPropertyTests.cs — FsCheck generators for arbitrary PolicySet/Policy
   Property: serialize → deserialize = original (structural equality via record comparison)

5. JUSTIFICATION FILES:
   - tests/Encina.IntegrationTests/Security/ABAC/Persistence/Xacml/XacmlXmlPolicySerializer.md
   - tests/Encina.LoadTests/Security/ABAC/Persistence/Xacml/XacmlXmlPolicySerializer.md

6. DOCUMENTATION:
   - docs/features/abac/xacml/xacml-xml-serialization.md (feature guide)
   - Update docs/features/abac/reference/configuration.md (add serializer options)
   - Update CHANGELOG.md (### Added under Unreleased)
   - Update PublicAPI.Unshipped.txt
   - Update docs/INVENTORY.md

7. BUILD & TEST VERIFICATION:
   - dotnet build Encina.slnx --configuration Release → 0 errors, 0 warnings
   - dotnet test → all pass

KEY RULES:
- Follow existing test patterns exactly (DefaultPolicySerializerTests for unit tests)
- FluentAssertions for assertions
- FsCheck for property tests with MaxTest = 50
- #region sections for organization
- NSubstitute for mocks (ILogger)
- Trait("Category", "Unit/Contract/Guard/Property") on all test classes
- No Thread.Sleep, no flaky tests
- CHANGELOG format matches existing entries (see latest v0.13.0 entries)

REFERENCE FILES:
- tests/Encina.UnitTests/Security/ABAC/Persistence/DefaultPolicySerializerTests.cs
- tests/Encina.ContractTests/Security/ABAC/PersistentPAPContractTests.cs
- tests/Encina.GuardTests/Security/ABAC/PersistentPAPGuardTests.cs
- tests/Encina.PropertyTests/Security/ABAC/PersistentPAPPropertyTests.cs
- src/Encina.Security.ABAC/Persistence/Xacml/*.cs (all Phase 1-3 files)
```

</details>

---

## Research

### Standards & Specifications

| Standard | Reference | Relevance |
|----------|-----------|-----------|
| XACML 3.0 Core Specification | OASIS, 2013 | Policy XML format (§5-7), combining algorithms, expression model |
| XACML 3.0 XSD Schema | `xacml-core-v3-schema-wd-17.xsd` | Element/attribute definitions, namespace URI |
| XACML 3.0 Function Identifiers | Appendix A | Function URN conventions (version-specific) |
| XACML 3.0 Data Types | Appendix B | XML Schema data type URIs |
| XML Schema Datatypes | W3C, 2004 | Value formatting for string, boolean, integer, double, dateTime |
| NIST SP 800-162 | NIST, 2014 | Recommends XACML for ABAC in US government systems |
| PCI-DSS 4.0 | PCI SSC, 2022 | Auditable access control policies requirement |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `IPolicySerializer` | `Persistence/IPolicySerializer.cs` | Interface implemented by `XacmlXmlPolicySerializer` |
| `DefaultPolicySerializer` | `Persistence/DefaultPolicySerializer.cs` | Pattern reference, contract test partner |
| `ExpressionJsonConverter` | `Persistence/ExpressionJsonConverter.cs` | Pattern for polymorphic `IExpression` handling |
| `XACMLFunctionIds` | `XACMLFunctionIds.cs` | Source for short function IDs (all 90+ constants) |
| `XACMLDataTypes` | `XACMLDataTypes.cs` | Data type URI constants |
| `ABACErrors` | `ABACErrors.cs` | `SerializationFailed()`, `DeserializationFailed()` factory methods |
| `ABACDiagnostics` | `Diagnostics/ABACDiagnostics.cs` | ActivitySource, Meter (extend with XML-specific counters) |
| `ABACLogMessages` | `Diagnostics/ABACLogMessages.cs` | LoggerMessage methods (extend with XML-specific events) |
| `ABACOptions` | `ABACOptions.cs` | Extend with `UseXacmlXmlSerializer()` method |
| `ServiceCollectionExtensions` | `ServiceCollectionExtensions.cs` | Add conditional XML serializer registration |
| Domain model records | `Model/*.cs` | `PolicySet`, `Policy`, `Rule`, `Target`, `Apply`, etc. |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| ABAC Pipeline | 9000-9009 | Evaluation, PDP decision, enforcement |
| ABAC Obligations | 9010-9019 | Obligation handler execution |
| ABAC Advice | 9020-9029 | Advice handler execution |
| ABAC PAP Store | 9030-9040 | Load, save, delete, serialize, connectivity |
| **ABAC XACML XML** | **9050-9059** | **Serialization, deserialization, extensions, unknown elements** |
| *(Reserved)* | 9060-9099 | Future ABAC features |

### Estimated File Count

| Category | Files | Lines (est.) |
|----------|------:|-------------:|
| Core mapping infrastructure | 4 | ~500-600 |
| Serializer implementation | 1 | ~800-1,000 |
| Options/DI modifications | 2 | ~40-60 |
| Diagnostics modifications | 2 | ~80-100 |
| Unit tests | 3 | ~900-1,000 |
| Guard tests | 1 | ~60-80 |
| Contract tests | 1 | ~150-200 |
| Property tests | 1 | ~150-200 |
| Test justifications | 2 | ~40-60 |
| Documentation | 3 | ~200-300 |
| **Total** | **~20** | **~3,000-3,600** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined Prompt for All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 (C# 14) library implementing XACML 3.0 ABAC with Railway Oriented Programming
(Either<EncinaError, T>). The ABAC module lives in src/Encina.Security.ABAC/ and has:
- IPolicySerializer interface (Serialize/Deserialize for PolicySet and Policy)
- DefaultPolicySerializer (JSON implementation using System.Text.Json)
- ExpressionJsonConverter (polymorphic IExpression serialization with $type discriminator)
- Full domain model: PolicySet, Policy, Rule, Target, AnyOf, AllOf, Match, Apply,
  AttributeDesignator, AttributeValue, VariableReference, VariableDefinition,
  Obligation, AdviceExpression, AttributeAssignment
- Enums: AttributeCategory, CombiningAlgorithmId, Effect, FulfillOn
- Constants: XACMLFunctionIds (90+ short function IDs), XACMLDataTypes (11 URI data types)
- ABACErrors factory class with SerializationFailed/DeserializationFailed methods
- ABACDiagnostics (ActivitySource + Meter) and ABACLogMessages (EventIds 9000-9040)
- ABACOptions with UsePersistentPAP, PolicyCaching, CustomFunctions, SeedPolicySets/Policies
- ServiceCollectionExtensions with TryAddSingleton<IPolicySerializer, DefaultPolicySerializer>

IMPLEMENTATION OVERVIEW:
Implement XacmlXmlPolicySerializer — an alternative IPolicySerializer that converts between
Encina ABAC domain models and XACML 3.0 standard XML format.

Phase 1: Core mapping infrastructure (4 files in Persistence/Xacml/):
- XacmlNamespaces — XNamespace constants
- XacmlFunctionRegistry — Short ID ↔ Full URN bidirectional FrozenDictionary
- XacmlMappingExtensions — Enum ↔ URN conversion, value formatting/parsing
- XacmlDataTypeMap — CLR type ↔ DataType URI inference

Phase 2: XacmlXmlPolicySerializer (1 file + 2 modifications):
- Implements IPolicySerializer using System.Xml.Linq
- Recursive serialization/deserialization of entire policy tree
- Encina extensions via custom namespace (urn:encina:xacml:extensions:1.0)
- DI registration via ABACOptions.UseXacmlXmlSerializer()

Phase 3: Observability (2 file modifications):
- ABACDiagnostics: XACML XML counters, histograms, activity helpers
- ABACLogMessages: EventIds 9050-9055 for XML serialization events

Phase 4: Testing & documentation (~10 files):
- Unit tests (3 files), guard tests, contract tests, property tests
- Test justifications for integration/load tests
- Feature documentation, CHANGELOG, PublicAPI.Unshipped.txt

KEY PATTERNS:
1. IPolicySerializer interface:
   - Serialize(PolicySet) → string
   - Serialize(Policy) → string
   - DeserializePolicySet(string) → Either<EncinaError, PolicySet>
   - DeserializePolicy(string) → Either<EncinaError, Policy>

2. XACML 3.0 XML namespace: urn:oasis:names:tc:xacml:3.0:core:schema:wd-17
3. Encina extensions namespace: urn:encina:xacml:extensions:1.0

4. AttributeCategory URN mapping:
   - Subject → urn:oasis:names:tc:xacml:1.0:subject-category:access-subject
   - Resource → urn:oasis:names:tc:xacml:3.0:attribute-category:resource
   - Action → urn:oasis:names:tc:xacml:3.0:attribute-category:action
   - Environment → urn:oasis:names:tc:xacml:3.0:attribute-category:environment

5. CombiningAlgorithmId URN pattern:
   urn:oasis:names:tc:xacml:{version}:{rule|policy}-combining-algorithm:{name}

6. Function URN pattern (version varies):
   urn:oasis:names:tc:xacml:{1.0|3.0}:function:{short-id}

7. Expression dispatch by IExpression type:
   Apply → <Apply FunctionId="urn:...">
   AttributeDesignator → <AttributeDesignator Category="urn:..." AttributeId="..." DataType="...">
   AttributeValue → <AttributeValue DataType="...">text</AttributeValue>
   VariableReference → <VariableReference VariableId="..."/>

8. Condition wrapping: Rule.Condition (Apply?) → <Condition><Apply ...>...</Apply></Condition>

9. Encina extensions as XML attributes:
   encina:IsEnabled="true" (default true if missing)
   encina:Priority="10" (default 0 if missing)

REFERENCE FILES:
- src/Encina.Security.ABAC/Persistence/IPolicySerializer.cs
- src/Encina.Security.ABAC/Persistence/DefaultPolicySerializer.cs
- src/Encina.Security.ABAC/Persistence/ExpressionJsonConverter.cs
- src/Encina.Security.ABAC/Model/*.cs (all domain model files)
- src/Encina.Security.ABAC/XACMLFunctionIds.cs
- src/Encina.Security.ABAC/XACMLDataTypes.cs
- src/Encina.Security.ABAC/ABACErrors.cs
- src/Encina.Security.ABAC/ABACOptions.cs
- src/Encina.Security.ABAC/ServiceCollectionExtensions.cs
- src/Encina.Security.ABAC/Diagnostics/ABACDiagnostics.cs
- src/Encina.Security.ABAC/Diagnostics/ABACLogMessages.cs
- tests/Encina.UnitTests/Security/ABAC/Persistence/DefaultPolicySerializerTests.cs
- tests/Encina.ContractTests/Security/ABAC/PersistentPAPContractTests.cs
- tests/Encina.GuardTests/Security/ABAC/PersistentPAPGuardTests.cs
- tests/Encina.PropertyTests/Security/ABAC/PersistentPAPPropertyTests.cs
```

</details>

---

## Next Steps

1. **Review** — Approve or request changes to this plan
2. **Publish** — Post as comment on issue #692 for reference
3. **Implement Phase 1** — Core mapping infrastructure (XacmlNamespaces, XacmlFunctionRegistry, XacmlMappingExtensions, XacmlDataTypeMap)
4. **Implement Phase 2** — XacmlXmlPolicySerializer + ABACOptions/DI changes
5. **Implement Phase 3** — Observability extensions (diagnostics, log messages)
6. **Implement Phase 4** — Tests (unit, guard, contract, property) + documentation
7. **Final commit** — `dotnet build` + `dotnet test` → commit with `Fixes #692`

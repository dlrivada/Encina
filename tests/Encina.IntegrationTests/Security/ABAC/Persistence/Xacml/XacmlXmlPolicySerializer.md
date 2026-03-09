# IntegrationTests - XACML XML Policy Serializer

## Status: Not Implemented

## Justification

The `XacmlXmlPolicySerializer` is a pure in-memory XML serialization/deserialization component with no external dependencies (database, file system, network). Integration tests are not applicable for the following reasons:

### 1. No External Dependencies

The serializer operates entirely in-memory using `System.Xml.Linq`. It does not:

- Connect to any database
- Read or write files
- Make network calls
- Interact with any external service

Integration tests are designed to verify the interaction between components and external systems. Since this component has no such interactions, integration tests would be functionally identical to the existing unit tests.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests** (`XacmlXmlPolicySerializerTests.cs`): Comprehensive round-trip serialization, XML format validation, error handling, data type support, XACML standard compliance
- **Unit Tests** (`XacmlFunctionRegistryTests.cs`): Function URN bidirectional mapping, registry symmetry
- **Unit Tests** (`XacmlMappingExtensionsTests.cs`): Enum-to-URN mapping, value formatting/parsing, data type inference
- **Guard Tests** (`XacmlXmlPolicySerializerGuardTests.cs`): Null/invalid argument validation
- **Property Tests** (`XacmlXmlRoundTripPropertyTests.cs`): FsCheck-generated random inputs verify invariants hold for any valid input
- **Contract Tests** (`PersistentPAPContractTests.cs`, `XacmlInfrastructureContractTests.cs`): Interface shape, type contracts, registry symmetry

### 3. Where Integration Tests Would Apply

If a future `XacmlFilePolicyStore` or similar component were added that reads/writes XACML XML files from disk or a remote source, integration tests would be appropriate for that component — not for the serializer itself.

## Related Files

- `src/Encina.Security.ABAC/Persistence/Xacml/XacmlXmlPolicySerializer.cs`
- `src/Encina.Security.ABAC/Persistence/Xacml/XacmlFunctionRegistry.cs`
- `src/Encina.Security.ABAC/Persistence/Xacml/XacmlMappingExtensions.cs`
- `src/Encina.Security.ABAC/Persistence/Xacml/XacmlDataTypeMap.cs`
- `src/Encina.Security.ABAC/Persistence/Xacml/XacmlNamespaces.cs`
- `tests/Encina.UnitTests/Security/ABAC/Persistence/Xacml/XacmlXmlPolicySerializerTests.cs`
- `tests/Encina.UnitTests/Security/ABAC/Persistence/Xacml/XacmlFunctionRegistryTests.cs`
- `tests/Encina.UnitTests/Security/ABAC/Persistence/Xacml/XacmlMappingExtensionsTests.cs`
- `tests/Encina.GuardTests/Security/ABAC/Persistence/Xacml/XacmlXmlPolicySerializerGuardTests.cs`
- `tests/Encina.PropertyTests/Security/ABAC/Persistence/Xacml/XacmlXmlRoundTripPropertyTests.cs`
- `tests/Encina.ContractTests/Security/ABAC/Persistence/Xacml/XacmlInfrastructureContractTests.cs`

## Date: 2026-03-09
## Issue: #692

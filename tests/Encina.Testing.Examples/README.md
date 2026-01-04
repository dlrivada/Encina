# Encina.Testing.Examples

**Living Documentation** - Reference implementations demonstrating Encina.Testing.* package usage patterns.

## Purpose

This project serves as:

1. **Executable documentation**: Every example is a runnable test that validates patterns work as documented
2. **Migration reference**: Developers can copy-paste patterns when migrating existing tests
3. **Pattern validation**: If a pattern doesn't work here, it won't work in real migrations
4. **Onboarding resource**: New contributors can learn Encina testing patterns from working examples

## Structure

```
Encina.Testing.Examples/
├── Domain/                           # Sample domain types for examples
│   ├── CreateOrderCommand.cs         # Sample command
│   ├── GetOrderQuery.cs              # Sample query with DTO
│   ├── OrderCreatedEvent.cs          # Sample notification
│   ├── CreateOrderHandler.cs         # Command handler with outbox
│   └── GetOrderHandler.cs            # Query handler with repository
├── Unit/                             # Unit test examples
│   ├── HandlerTestExamples.cs        # EncinaTestFixture patterns
│   ├── HandlerSpecificationExamples.cs # HandlerSpecification BDD pattern
│   └── EitherAssertionExamples.cs    # Shouldly Either extensions
├── Integration/                      # Integration test examples
│   └── ModuleIntegrationExamples.cs  # ModuleTestFixture patterns
├── Fixtures/                         # Fixture usage examples
│   └── WireMockFixtureExamples.cs    # EncinaWireMockFixture patterns
├── TestData/                         # Test data generation examples
│   ├── BogusExamples.cs              # EncinaFaker patterns
│   └── MessagingFakerExamples.cs     # FakeOutboxStore, OutboxTestHelper patterns
├── PropertyBased/                    # Property-based testing examples
│   └── PropertyTestExamples.cs       # FsCheck with EncinaProperty patterns
├── ContractTests/                    # Consumer-driven contract testing
│   └── PactConsumerExamples.cs       # EncinaPactConsumerBuilder patterns
└── Architecture/                     # Architecture test examples
    └── ArchitectureRulesExamples.cs  # EncinaArchitectureRulesBuilder
```

## Running Examples

```bash
# Run all examples
dotnet test tests/Encina.Testing.Examples

# Run specific category
dotnet test tests/Encina.Testing.Examples --filter "FullyQualifiedName~Unit"
dotnet test tests/Encina.Testing.Examples --filter "FullyQualifiedName~PropertyBased"
```

## Related Documentation

- [Testing Dogfooding Plan](../../docs/plans/testing-dogfooding-plan.md) - Comprehensive migration guide
- [Issue #498](https://github.com/dlrivada/Encina/issues/498) - Epic tracking all migration work

## Guidelines for Adding Examples

1. **Every example must be a `[Fact]`** - No dead code, everything runs
2. **Use XML documentation** - Explain what pattern the example demonstrates
3. **Keep examples focused** - One concept per test method
4. **Use descriptive names** - `Method_Scenario_ExpectedBehavior` pattern
5. **Reference plan sections** - Link to relevant documentation

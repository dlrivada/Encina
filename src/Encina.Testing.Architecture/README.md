# Encina.Testing.Architecture

Architecture testing rules for Encina using [ArchUnitNET](https://github.com/TNG/ArchUnitNET). Provides pre-built rules for enforcing clean architecture patterns, handler dependencies, request immutability, and layer separation.

## Installation

```bash
dotnet add package Encina.Testing.Architecture
```

## Quick Start

### Option 1: Using the Test Base Class

Inherit from `EncinaArchitectureTestBase` for automatic architecture tests:

```csharp
using Encina.Testing.Architecture;

public class ArchitectureTests : EncinaArchitectureTestBase
{
    protected override Assembly ApplicationAssembly => typeof(CreateOrderHandler).Assembly;
    protected override Assembly? DomainAssembly => typeof(Order).Assembly;
    protected override Assembly? InfrastructureAssembly => typeof(OrderRepository).Assembly;

    protected override string? DomainNamespace => "MyApp.Domain";
    protected override string? ApplicationNamespace => "MyApp.Application";
    protected override string? InfrastructureNamespace => "MyApp.Infrastructure";
}
```

This automatically runs these tests:
- Handlers should not depend on infrastructure
- Notifications should be sealed
- Handlers should be sealed
- Behaviors should be sealed
- Validators should follow naming convention
- Domain should not depend on messaging (if namespace configured)
- Layers should be properly separated (if namespaces configured)

### Option 2: Using Individual Rules

Use `EncinaArchitectureRules` for more control:

```csharp
using ArchUnitNET.Loader;
using Encina.Testing.Architecture;

public class CustomArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(typeof(MyHandler).Assembly)
        .Build();

    [Fact]
    public void Handlers_ShouldNotDependOnInfrastructure()
    {
        EncinaArchitectureRules
            .HandlersShouldNotDependOnInfrastructure()
            .Check(Architecture);
    }

    [Fact]
    public void Domain_ShouldNotDependOnMessaging()
    {
        EncinaArchitectureRules
            .DomainShouldNotDependOnMessaging("MyApp.Domain")
            .Check(Architecture);
    }
}
```

### Option 3: Using the Rules Builder

Use `EncinaArchitectureRulesBuilder` for fluent configuration:

```csharp
using Encina.Testing.Architecture;

public class BuilderArchitectureTests
{
    [Fact]
    public void Architecture_ShouldFollowEncinaPatterns()
    {
        new EncinaArchitectureRulesBuilder(typeof(MyHandler).Assembly)
            .EnforceHandlerAbstractions()
            .EnforceSealedNotifications()
            .EnforceSealedHandlers()
            .EnforceLayerSeparation(
                "MyApp.Domain",
                "MyApp.Application",
                "MyApp.Infrastructure")
            .Verify();
    }

    [Fact]
    public void Architecture_ShouldFollowAllStandardRules()
    {
        new EncinaArchitectureRulesBuilder(
                typeof(MyHandler).Assembly,
                typeof(Order).Assembly)
            .ApplyAllStandardRules()
            .Verify();
    }
}
```

## Available Rules

### Handler Rules

| Rule | Description |
|------|-------------|
| `HandlersShouldNotDependOnInfrastructure()` | Handlers should not depend on EF Core, Dapper, database drivers |
| `HandlersShouldBeSealed()` | Handlers should be sealed classes |

### Notification Rules

| Rule | Description |
|------|-------------|
| `NotificationsShouldBeSealed()` | Notifications and Events should be sealed |

### Behavior Rules

| Rule | Description |
|------|-------------|
| `BehaviorsShouldBeSealed()` | Pipeline behaviors should be sealed |

### Validator Rules

| Rule | Description |
|------|-------------|
| `ValidatorsShouldFollowNamingConvention()` | Validators should end with "Validator" |

### Layer Separation Rules

| Rule | Description |
|------|-------------|
| `DomainShouldNotDependOnMessaging(namespace)` | Domain layer should not depend on Encina.Messaging |
| `DomainShouldNotDependOnApplication(domain, app)` | Domain should not depend on Application layer |
| `ApplicationShouldNotDependOnInfrastructure(app, infra)` | Application should not depend on Infrastructure |
| `CleanArchitectureLayersShouldBeSeparated(...)` | Combined rule for all layer separations |

### Repository Rules

| Rule | Description |
|------|-------------|
| `RepositoryInterfacesShouldResideInDomain(namespace)` | Repository interfaces should be in Domain |
| `RepositoryImplementationsShouldResideInInfrastructure(namespace)` | Repository implementations should be in Infrastructure |

## Builder API

The `EncinaArchitectureRulesBuilder` provides a fluent API:

```csharp
var result = new EncinaArchitectureRulesBuilder(assembly1, assembly2)
    .EnforceHandlerAbstractions()      // Handler isolation
    .EnforceSealedNotifications()      // Sealed notifications
    .EnforceSealedHandlers()           // Sealed handlers
    .EnforceSealedBehaviors()          // Sealed behaviors
    .EnforceValidatorNaming()          // Naming conventions
    .EnforceDomainMessagingIsolation("MyApp.Domain")
    .EnforceLayerSeparation("Domain", "Application", "Infrastructure")
    .EnforceRepositoryInterfacesInDomain("MyApp.Domain")
    .EnforceRepositoryImplementationsInInfrastructure("MyApp.Infrastructure")
    .AddCustomRule(myCustomRule)       // Custom rules
    .ApplyAllStandardRules()           // All standard rules at once
    .VerifyWithResult();               // Returns result instead of throwing

if (result.IsFailure)
{
    foreach (var violation in result.Violations)
    {
        Console.WriteLine($"{violation.RuleName}: {violation.Message}");
    }
}
```

## Custom Rules

Create custom rules using ArchUnitNET's fluent API:

```csharp
using static ArchUnitNET.Fluent.ArchRuleDefinition;

var customRule = Types()
    .That()
    .HaveNameEndingWith("Service")
    .Should()
    .BePublic()
    .Because("Service classes should be public");

new EncinaArchitectureRulesBuilder(assembly)
    .AddCustomRule(customRule)
    .Verify();
```

## Dependencies

- [TngTech.ArchUnitNET.xUnit](https://github.com/TNG/ArchUnitNET) - Architecture testing framework
- Encina - Core library

## License

This project is licensed under the MIT License.

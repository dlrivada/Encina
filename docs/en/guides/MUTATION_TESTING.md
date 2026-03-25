# Encina Mutation Testing Guide

## Goals

- Establish Stryker.NET as the baseline mutation engine for the Encina library.
- Track actionable metrics (high 85, low 70, break 60) aligned with roadmap thresholds.

## Prerequisites

- .NET 10 SDK installed.
- Install Stryker CLI (`dotnet tool install --global dotnet-stryker`) if not already available.
- Restore dependencies with `dotnet restore Encina.slnx` prior to running Stryker.

## Running The Suite

- Execute `dotnet tool run dotnet-stryker --config-file stryker-config.json --solution Encina.slnx` from the repository root.
- Use the C# helper script for convenience: `dotnet run --file .github/scripts/run-stryker.cs`
- Prefer Release builds to mirror CI behavior (`--configuration Release`).
- The repository config pins `concurrency: 1` to avoid vstest runner hangs on Windows; adjust once the suite stabilizes.

## Reporting

- HTML report: `artifacts/mutation/<timestamp>/reports/mutation-report.html` (generated automatically).
- Raw console output remains the primary log; redirect the helper script if a persistent log file is required.
- Console summary highlights surviving mutants; treat anything above the break threshold as a failure condition.

### Current Baseline (2025-12-08)

- Mutation score: **93.74%** (449 killed, 2 survived, 0 timeout mutants) using `dotnet tool run dotnet-stryker` with the repo configuration.
- CI runs `.github/scripts/run-stryker.cs` and enforces the baseline; refresh the README badge via `.github/scripts/update-mutation-summary.cs` right after local runs.
- Survivors mainly sit in historical reports; the live suite is green. Keep future contributions paired with targeted tests so surviving mutants stay at zero.

### Paused Hardening Tasks

The dedicated mutation-hardening initiative is paused until new feature work settles. When ready to resume:

1. Re-run `dotnet stryker --project src/Encina/Encina.csproj --test-projects tests/Encina.UnitTests/Encina.UnitTests.csproj` to focus on the Encina core mutants (previously IDs 280–366).
2. Investigate any survivors and add unit or property tests around metrics pipeline integration and handler result validation.
3. Update the mutation badge and dashboard via `.github/scripts/update-mutation-summary.cs` once the new score is recorded.

## Test Quality Patterns

Encina provides helper attributes in the `Encina.Testing.Mutations` namespace to document and track mutation testing insights directly in your test code.

### NeedsMutationCoverageAttribute

Use this attribute to mark tests that have surviving mutants and need stronger assertions:

```csharp
using Encina.Testing.Mutations;

[Fact]
[NeedsMutationCoverage("Boundary condition not verified - survived arithmetic mutation on line 45")]
public void Calculate_BoundaryValue_ShouldReturnExpectedResult()
{
    // Arrange
    var calculator = new Calculator();

    // Act
    var result = calculator.Calculate(100);

    // Assert - TODO: Add boundary check for 100 vs 101
    result.ShouldBe(200);
}

// With optional metadata
[Fact]
[NeedsMutationCoverage("Missing null check verification", MutantId = "280", SourceFile = "src/Calculator.cs", Line = 45)]
public void Calculate_NullInput_ShouldThrow()
{
    // ...
}
```

**Workflow:**
1. Run Stryker mutation testing
2. Identify surviving mutants in the report
3. Add `[NeedsMutationCoverage]` to the corresponding test with a description
4. Strengthen the assertions to kill the mutant
5. Remove the attribute once the mutant is killed

### MutationKillerAttribute

Use this attribute to document tests explicitly written to kill specific mutation types:

```csharp
using Encina.Testing.Mutations;

[Fact]
[MutationKiller("EqualityMutation", Description = "Verifies >= is not mutated to >")]
public void IsAdult_ExactlyEighteen_ShouldReturnTrue()
{
    // Arrange
    var person = new Person { Age = 18 };

    // Act
    var result = person.IsAdult(); // Uses age >= 18

    // Assert - This test kills the >= to > mutation
    result.ShouldBeTrue();
}

// With location metadata
[Fact]
[MutationKiller("ArithmeticMutation", SourceFile = "src/Calculator.cs", TargetMethod = "Add", Line = 25)]
public void Add_NegativeNumbers_ShouldReturnCorrectSum()
{
    // ...
}
```

**Common mutation types:**
- `EqualityMutation`: `==`→`!=`, `<`→`<=`, `>`→`>=`
- `ArithmeticMutation`: `+`→`-`, `*`→`/`, `%`→`*`
- `BooleanMutation`: `true`→`false`, `&&`→`||`
- `UnaryMutation`: `-x`→`x`, `!x`→`x`, `++`→`--`
- `NullCheckMutation`: `x==null`→`x!=null`
- `StringMutation`: `""`→`"Stryker was here!"`
- `LinqMutation`: `First()`→`Last()`, `Any()`→`All()`
- `BlockRemoval`: Removing entire statements

### Best Practices

1. **One mutation killer test per mutation type** - Keep tests focused on specific mutations
2. **Use precise assertions** - Assertions should detect the exact code change
3. **Document mutation location** - Include source file and line when known
4. **Include boundary values** - Test exact boundary conditions for arithmetic/equality mutations
5. **Multiple attributes allowed** - Tests can kill multiple mutation types

## CLI Integration

Generate a Stryker configuration file using the Encina CLI:

```bash
# Generate basic configuration
encina generate stryker --project src/MyApp/MyApp.csproj

# Generate with custom thresholds
encina generate stryker --project src/MyApp/MyApp.csproj --threshold-high 85 --threshold-break 60

# Generate advanced configuration with baseline and incremental testing
encina generate stryker --project src/MyApp/MyApp.csproj --advanced

# Specify test projects explicitly
encina generate stryker --project src/MyApp/MyApp.csproj -t tests/MyApp.Tests/MyApp.Tests.csproj
```

## Recommended Thresholds

| Project Type | Break | Low | High | Rationale |
|--------------|-------|-----|------|-----------|
| Core Libraries | 60% | 70% | 85% | Critical code needs strong coverage |
| Application Code | 50% | 60% | 75% | Balance between coverage and velocity |
| New Projects | 40% | 50% | 70% | Lower initially, increase over time |
| Mature Projects | 70% | 80% | 90% | Established codebases should aim higher |

## Tips for Effective Mutation Testing

1. **Start with a subset** - Use `--since main` to only test changed code
2. **Use baseline mode** - Enable `baseline.enabled` to skip unchanged files
3. **Exclude trivial code** - Methods like `ToString`, `Dispose`, `GetHashCode` add noise
4. **Focus on killed vs survived** - Timeouts and no-coverage need attention too
5. **Iterate on survivors** - Each surviving mutant is a potential test gap

## Next Steps

- Keep the 93.74% baseline intact by refreshing the badge after every Stryker run.
- Resume the hardening plan when feature development stabilises, starting with the paused tasks above.
- Only introduce ignore patterns after confirming mutants are either equivalent or unreachable by design.

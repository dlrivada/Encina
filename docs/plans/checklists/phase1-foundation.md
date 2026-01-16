# Phase 1: Foundation Checklist

> **Historical Note**: This checklist was created before the January 2026 test consolidation. The individual test projects mentioned have been consolidated into 7 projects under `tests/`. See `docs/plans/test-consolidation-plan.md` for the current test structure.

**Goal**: Add Encina.Testing.* package references without breaking existing tests.

**Estimated Effort**: 1-2 hours per project

---

## Pre-Migration Checklist

- [ ] Run `dotnet test` to ensure all tests pass before migration
- [ ] Create a new branch: `feature/testing-dogfooding-phase1`
- [ ] Review existing test dependencies with `scripts/audit-test-dependencies.ps1`

## Core Package References

For each test project, add the following project references:

- [ ] **Encina.Testing.Shouldly** - Either assertion extensions
  ```xml
  <ProjectReference Include="..\..\src\Encina.Testing.Shouldly\Encina.Testing.Shouldly.csproj" />
  ```

- [ ] **Encina.Testing.Bogus** - Seeded test data generation
  ```xml
  <ProjectReference Include="..\..\src\Encina.Testing.Bogus\Encina.Testing.Bogus.csproj" />
  ```

- [ ] **Encina.Testing.Fakes** - Messaging pattern test doubles
  ```xml
  <ProjectReference Include="..\..\src\Encina.Testing.Fakes\Encina.Testing.Fakes.csproj" />
  ```

## Global Usings (Optional)

Add to test project `.csproj`:

```xml
<ItemGroup>
  <Using Include="Encina.Testing.Shouldly" />
  <Using Include="Encina.Testing.Bogus" />
</ItemGroup>
```

## Verification

- [ ] Run `dotnet build` - no errors
- [ ] Run `dotnet test` - all existing tests still pass
- [ ] No behavior changes expected at this phase

## Post-Migration

- [ ] Commit changes with message: `chore(tests): add Encina.Testing.* package references`
- [ ] Update tracking issue #498

---

## Projects to Update

### Tier 1 (Core)
- [ ] `tests/Encina.Tests/Encina.Tests.csproj`
- [ ] `tests/Encina.Messaging.Tests/Encina.Messaging.Tests.csproj`

### Tier 2 (Providers)
- [ ] `tests/Encina.EntityFrameworkCore.Tests/Encina.EntityFrameworkCore.Tests.csproj`
- [ ] `tests/Encina.Dapper.Tests/Encina.Dapper.Tests.csproj`

### Tier 3 (Features)
- [ ] `tests/Encina.Validation.*.Tests/*.csproj`
- [ ] `tests/Encina.Caching.*.Tests/*.csproj`

---

## Troubleshooting

### Build Error: Duplicate Type Definitions

If you see ambiguous type errors:
```csharp
// Use alias to resolve
using FakeOutbox = Encina.Testing.Fakes.Stores.FakeOutboxStore;
```

### Restore Failures

```bash
dotnet restore --force-evaluate
```

# Phase 2: Either Assertions Checklist

**Goal**: Replace custom Either assertion helpers with Encina.Testing.Shouldly extensions.

**Estimated Effort**: 2-4 hours per project

---

## Pre-Migration Checklist

- [ ] Phase 1 completed (packages added)
- [ ] Run `dotnet test` to establish baseline
- [ ] Create branch: `feature/testing-dogfooding-phase2`

## Find and Replace Patterns

### Pattern 1: Success Assertions

**Search for**:
```csharp
result.IsRight.ShouldBeTrue();
var value = result.Match(Right: v => v, Left: _ => throw new Exception());
```

**Replace with**:
```csharp
var value = result.ShouldBeSuccess();
```

### Pattern 2: Failure Assertions

**Search for**:
```csharp
result.IsLeft.ShouldBeTrue();
result.IfLeft(err => err.Code.ShouldBe("expected.code"));
```

**Replace with**:
```csharp
var error = result.ShouldBeError();
error.Code.ShouldBe("expected.code");
```

### Pattern 3: Validation Errors

**Search for**:
```csharp
result.IsLeft.ShouldBeTrue();
result.IfLeft(err => err.Code.ShouldStartWith("encina.validation"));
```

**Replace with**:
```csharp
result.ShouldBeValidationError();
```

### Pattern 4: Not Found Errors

**Search for**:
```csharp
result.IsLeft.ShouldBeTrue();
result.IfLeft(err => err.Code.ShouldStartWith("encina.notfound"));
```

**Replace with**:
```csharp
result.ShouldBeNotFoundError();
```

### Pattern 5: Custom Error Code Matching

**Search for**:
```csharp
result.IfLeft(err => err.Code.ShouldContain("specific.code"));
```

**Replace with**:
```csharp
result.ShouldBeErrorWithCode("specific.code");
```

### Pattern 6: Async Patterns

**Search for**:
```csharp
var result = await handler.Handle(request);
result.IsRight.ShouldBeTrue();
```

**Replace with**:
```csharp
var value = await handler.Handle(request).ShouldBeSuccessAsync();
```

## Files to Check

Use this grep pattern to find candidates:
```bash
grep -r "IsRight.ShouldBe" tests/
grep -r "IsLeft.ShouldBe" tests/
grep -r "\.Match(Right:" tests/
grep -r "\.IfLeft(" tests/
```

## Add Required Usings

For each modified file, ensure:
```csharp
using Encina.Testing.Shouldly;
```

## Verification

- [ ] Run `dotnet build` - no errors
- [ ] Run `dotnet test` - all tests still pass
- [ ] Behavior unchanged, only syntax improved

## Post-Migration

- [ ] Commit: `refactor(tests): migrate to Encina.Testing.Shouldly Either assertions`
- [ ] Update tracking issue #498

---

## File-by-File Checklist

### Encina.Tests
- [ ] `EncinaTests.cs`
- [ ] `PipelineBehaviorsTests.cs`
- [ ] `Health/OutboxHealthCheckTests.cs`

### Encina.Messaging.Tests
- [ ] `Outbox/*.cs`
- [ ] `Inbox/*.cs`
- [ ] `Saga/*.cs`

(Add more files as identified)

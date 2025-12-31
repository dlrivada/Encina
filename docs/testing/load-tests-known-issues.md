# Load Tests Known Issues

## .NET 10 JIT Bug with IAsyncEnumerable (NBomber)

**Status:** Active workaround required  
**Affected Projects:** All `*.LoadTests` projects using NBomber  
**Affected Runtime:** .NET 10.0.x (until a servicing patch is released)

### Problem

NBomber uses `IAsyncEnumerable` internally, which triggers a JIT bug in .NET 10 related to object stack allocation with conditional escape analysis. This causes crashes or unexpected behavior during load test execution.

### Workaround

Set the following environment variable before running load tests:

**Windows (PowerShell):**
```powershell
$env:DOTNET_JitObjectStackAllocationConditionalEscape = "0"
dotnet test --filter "FullyQualifiedName~LoadTests"
```

**Windows (CMD):**
```cmd
set DOTNET_JitObjectStackAllocationConditionalEscape=0
dotnet test --filter "FullyQualifiedName~LoadTests"
```

**Linux/macOS:**
```bash
export DOTNET_JitObjectStackAllocationConditionalEscape=0
dotnet test --filter "FullyQualifiedName~LoadTests"
```

### CI/CD Configuration

For GitHub Actions or Azure DevOps, add the environment variable to your workflow:

```yaml
env:
  DOTNET_JitObjectStackAllocationConditionalEscape: "0"
```

### Affected Load Test Projects

- `Encina.FluentValidation.LoadTests`
- `Encina.GuardClauses.LoadTests`
- Any other project using NBomber with .NET 10

### Resolution Timeline

This workaround should be re-evaluated and potentially removed when:
1. A .NET 10.0.x servicing patch addresses the JIT bug
2. NBomber releases an update that works around the issue internally
3. The project migrates to a newer .NET version where the bug is fixed

**TODO:** Monitor the [dotnet/runtime](https://github.com/dotnet/runtime) repository for related fixes. Specifically, watch for PRs or issues mentioning "JIT bug .NET 10" or "JIT emitter" that address compilation issues with NBomber. Remove this workaround once a fix is merged and released in a .NET 10.0.x servicing patch, or when upgrading to a .NET version where the issue is resolved.

<!--
title: [BUG] Encina.Messaging README.md documents non-existent fluent builder extension methods
labels: bug
milestone: v0.14.0 — Hardening
-->

## Description

The "Fluent Builder (Alternative Syntax)" section in the `Encina.Messaging` README documents a fluent API chain (`AddTransactions()`, `AddOutbox()`, `AddInbox()`, `AddSagas()`, `AddScheduling()`) that does not exist in the codebase. `AddEncinaEntityFrameworkCore<AppDbContext>()` returns a plain `IServiceCollection`, which does not possess these extension methods.

## Steps to Reproduce

1. Configure the `Encina.Messaging` package according to the "Fluent Builder (Alternative Syntax)" section in `src/Encina.Messaging/README.md:505-521`.
2. Call method `services.AddEncinaEntityFrameworkCore<AppDbContext>().AddTransactions().AddOutbox(...).AddInbox(...).AddSagas().AddScheduling();`.
3. Pass parameter `...` (as shown in the documentation).
4. See error: Compiler error indicating that `AddTransactions`, `AddOutbox`, `AddInbox`, `AddSagas`, and `AddScheduling` do not exist on type `IServiceCollection`.

## Expected Behavior

The code snippet in the README should compile and represent valid, available extension methods for the `Encina.Messaging` and related packages.

## Actual Behavior

The fluent builder chain described in the documentation is fictional. Grep searches for `public static.*AddTransactions\(|public static.*AddOutbox\(|public static.*AddInbox\(` and `AddSagas\(` across `src/` return no matches in the code. The parameterless overload of `AddEncinaEntityFrameworkCore<AppDbContext>()` (`ServiceCollectionExtensions.cs:429`) returns a plain `IServiceCollection`, which has no such fluent members to chain.

## Environment

- **Encina Version**: 0.14.0-dev
- **.NET Version**: .NET 10
- **OS**: Not applicable (found by static review of the code, not at runtime)
- **Package(s) Affected**: Encina.Messaging

## Code Sample

```csharp
// Minimal reproducible example based on the incorrect documentation in src/Encina.Messaging/README.md:505-521
// This code does not compile because the extension methods do not exist.
services.AddEncinaEntityFrameworkCore<AppDbContext>()
        .AddTransactions()
        .AddOutbox(...)
        .AddInbox(...)
        .AddSagas()
        .AddScheduling();
```

## Stack Trace

```
// Compilation error: The name 'AddTransactions' does not exist in the current context
// Compilation error: The name 'AddOutbox' does not exist in the current context
// Compilation error: The name 'AddInbox' does not exist in the current context
// Compilation error: The name 'AddSagas' does not exist in the current context
// Compilation error: The name 'AddScheduling' does not exist in the current context
```

## Additional Context

Related Issues: #1323 (Encina.ADO.PostgreSQL README Quick Start calls `AddEncinaADOPostgreSQL`, a method that does not exist), #1396 (EF Core and GraphQL package READMEs still show `error.Message` in log/error examples). This issue violates SKILL.md §3 house rule 3.
- #1323 - partially related (it covers only part of this finding)

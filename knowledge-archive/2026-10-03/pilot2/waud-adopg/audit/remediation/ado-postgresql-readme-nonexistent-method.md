<!-- issue
title: [BUG] Encina.ADO.PostgreSQL README Quick Start calls AddEncinaADOPostgreSQL, a method that does not exist
labels: bug, area-database, area-documentation
milestone: v0.14.0 — Hardening
-->

## Description

`src/Encina.ADO.PostgreSQL/README.md` line 37 shows a Quick Start sample calling
`AddEncinaADOPostgreSQL(...)`. That method does not exist anywhere in the package. The real
registration entry points are the `AddEncinaADO(...)` overloads in
`src/Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs:39,118,150`. A reader following the
README's own sample gets a compile error on the first line of setup code.

## Steps to Reproduce

1. Open `src/Encina.ADO.PostgreSQL/README.md:37`.
2. Copy the Quick Start sample into a new project referencing `Encina.ADO.PostgreSQL`.
3. Build.

## Expected Behavior

The README's Quick Start sample compiles against the package's actual public API.

## Actual Behavior

`CS0117`-class error: `AddEncinaADOPostgreSQL` is not a member of `IServiceCollection`
(extension method does not exist under that name).

## Environment

- **Encina Version**: pre-1.0, main as of 2026-09-24
- **.NET Version**: .NET 10.0
- **OS**: any
- **Package(s) Affected**: Encina.ADO.PostgreSQL (documentation only)

## Code Sample

```csharp
// README.md:37 — current (does not compile):
services.AddEncinaADOPostgreSQL(options => { ... });

// should be one of the real overloads, e.g.:
services.AddEncinaADO(connectionString, options => { ... });
```

## Stack Trace

N/A — documentation defect, not a runtime failure.

## Additional Context

Found during the SPEC-003 pilot-2 deep quality audit of `Encina.ADO.PostgreSQL` (AUD-11).
Route through `docs-writer` per the encina-docs skill (README ownership); the fix is a rename of
the sample's method call plus, ideally, a docs-reviewer check that every code sample in a
package README uses a name verified against `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt` or
the actual source, since this is exactly the kind of drift SPEC-003 AUD-11 was written to catch.

## Root Cause

The README was likely drafted from an earlier or planned naming convention
(`AddEncina<Provider>` per-database, mirroring how EF Core packages sometimes name their
registration method) that was never implemented for this package; the actual `AddEncinaADO`
naming is shared, generic across the three ADO providers, and the README was not updated to
match, nor caught by a docs review.

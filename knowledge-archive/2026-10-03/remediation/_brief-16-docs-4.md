Draft ONE remediation issue file for a finding from the SPEC-003 audit of closed GitHub issue #16 of
the Encina .NET library (docs stage, finding 4, severity Blocker). Use
ONLY the input finding text; never invent facts. Output EXACTLY the header comment block below followed by the
template body below it, keeping every '## ' header of the template body verbatim and in the same order, and
ticking a checkbox only from the options the template body itself lists:

<!--
title: [BUG] <specific title drawn from the finding>
labels: bug
milestone: v0.14.0 — Hardening
-->

## Description

A clear and concise description of the bug.

## Steps to Reproduce

1. Configure '...'
2. Call method '...'
3. Pass parameter '...'
4. See error

## Expected Behavior

What you expected to happen.

## Actual Behavior

What actually happened.

## Environment

- **Encina Version**: [e.g., 0.9.0]
- **.NET Version**: [e.g., .NET 10.0]
- **OS**: [e.g., Windows 11, Ubuntu 24.04]
- **Package(s) Affected**: [e.g., Encina.EntityFrameworkCore, Encina.Dapper.SqlServer]

## Code Sample

```csharp
// Minimal reproducible example
```

## Stack Trace

```
// If applicable, paste the full stack trace here
```

## Additional Context

Add any other context about the problem here (screenshots, logs, related issues).

Guidance:
- Put the finding's file:line evidence in the Location (or Steps to Reproduce) section.
- Related Issues: include #16 and any of these candidate open issues that are related but are NOT
  the same problem (a same-problem duplicate must never reach this step): #1372: [DEBT] Package and provider counts differ between the GitHub description, docs/index.md, README.md and the code; #1299: [DEBT] docs/INVENTORY.md is Spanish and full of hand-typed, unverified figures; #1396: [DEBT] EF Core and GraphQL package READMEs still show error.Message in log/error examples; #1323: [BUG] Encina.ADO.PostgreSQL README Quick Start calls AddEncinaADOPostgreSQL, a method that does not exist; #1202: [DEBT] Compliance READMEs and ADR-019 describe types and provider coverage that do not exist; state the PostgreSQL/Marten requirement; #1138: [INFRA] Full link scan fails on every push: devblogs.microsoft.com serves an expired TLS certificate; #1218: [DEBT] Article-coverage specification for Encina.Compliance.ProcessorAgreements, EU and national articles; #1215: [DEBT] Article-coverage specification for Encina.Compliance.LawfulBasis, EU and national articles
- Environment: this section will be overwritten deterministically after you reply, so match it rather than guessing -- Encina Version "0.14.0-dev", .NET Version ".NET 10", OS "Not applicable (found by static review of the code, not at runtime)", Package(s) Affected "Encina.Messaging".

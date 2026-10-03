Draft ONE remediation issue file for a finding from the SPEC-003 audit of closed GitHub issue #16 of
the Encina .NET library (code stage, finding 2, severity Blocker). Use
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
  the same problem (a same-problem duplicate must never reach this step): #708: [FEATURE] Add in-memory cache for gRPC reflection method lookups and generic method compilation; #709: [FEATURE] Add in-memory cache for message transport topology and routing key resolution; #707: [FEATURE] Add in-memory cache for Security context extraction and audit metadata resolution; #826: [FEATURE] Encina.Compliance.NIS2 — Management Accountability Lifecycle (Art. 20); #825: [FEATURE] Encina.Compliance.NIS2 — Coordinated Vulnerability Disclosure (Art. 12); #706: [FEATURE] Add in-memory cache for Specification expression compilation and composition; #598: [FEATURE] ITenantStore: Cloud-Native Implementations (Cosmos DB, Azure Table, DynamoDB); #824: [FEATURE] Encina.Compliance.NIS2 — Risk Analysis Lifecycle & Evidence Capture (Art. 21.2.a); #1435: [BUG] LawfulBasisValidationPipelineBehavior logs the raw data subject id and EncinaError.Message on every consent check; #1454: [BUG] EncinaError.Message still reaches OpenTelemetry activity tags/status in DataSubjectRights, DPIA and Retention
- Environment: this section will be overwritten deterministically after you reply, so match it rather than guessing -- Encina Version "0.14.0-dev", .NET Version ".NET 10", OS "Not applicable (found by static review of the code, not at runtime)", Package(s) Affected "Encina.OpenTelemetry".

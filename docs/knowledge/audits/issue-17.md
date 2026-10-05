# Audit: Issue #17 - Event Sourcing Strategy Pattern

## Scope

The issue touched 69 files in the following packages:
- `src/Encina.EventStoreDB/` - Deprecated, moved to `.backup/deprecated-packages/`
- `src/Encina.Wolverine/` - Deprecated, moved to `.backup/deprecated-packages/`
- `src/Encina.NServiceBus/` - Deprecated, moved to `.backup/deprecated-packages/`
- `src/Encina.MassTransit/` - Deprecated, moved to `.backup/deprecated-packages/`
- `src/Encina.Dapr/` - Deprecated, moved to `.backup/deprecated-packages/`
- Tests for the above packages

All files were deleted (diff-filter=D) on 2025-12-23 in commit 87d92a39 titled "refactor: deprecate EventStoreDB, Wolverine, NServiceBus, MassTransit, Dapr".

## Deletion Verification

Confirmed deleted paths (sample of 3):
- `src/Encina.EventStoreDB/Encina.EventStoreDB.csproj` - Deleted in 87d92a39 on 2025-12-23
- `src/Encina.Wolverine/Encina.Wolverine.csproj` - Deleted in 87d92a39 on 2025-12-23
- `src/Encina.MassTransit/Encina.MassTransit.csproj` - Deleted in 87d92a39 on 2025-12-23

## Decision Record

The removal was part of a deliberate architectural decision:
- **ADR-027**: "Marten is the event-sourcing provider; Encina.EventStoreDB deprecated and excluded from new features" (#17, #321)
- **Rationale**: Marten provides better .NET integration via PostgreSQL, which aligns with the existing infrastructure stack
- **Scope**: Only Marten continues for event sourcing; single-vendor simplicity preferred over premature abstraction

## Verdict

**code-removed** — All files touched by issue #17 have been intentionally removed from the active codebase and archived in `.backup/deprecated-packages/`. No audit items apply.

## Related Decision

See also:
- Commit 87d92a39 (2025-12-23): "refactor: deprecate EventStoreDB, Wolverine, NServiceBus, MassTransit, Dapr"
- PR #1113: "docs: promote nine rules into CLAUDE.md and record ADR-027..030"
- ADR-027: Marten as the sole event-sourcing provider

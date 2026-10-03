<!-- issue
title: [BUG] MongoDB InboxMessageFactory discards InboxMetadata
labels: bug, area-mongodb
milestone: v0.14.0 — Hardening
-->

## Description

Found by the SPEC-003 audit of #12 (adversarial-reviewer pass).

`src/Encina.MongoDB/Inbox/InboxMessageFactory.cs` accepts an `InboxMetadata? metadata` parameter
in its `Create(...)` method but never assigns it to the created `InboxMessage` — the parameter is
silently discarded. The other 8 provider factory implementations (ADO ×3, Dapper ×3,
EntityFrameworkCore) serialize and persist it (e.g.
`src/Encina.ADO.SqlServer/Inbox/InboxMessageFactory.cs:32`:
`Metadata = metadata != null ? JsonSerializer.Serialize(metadata, JsonOptions) : null`).

## Steps to Reproduce

1. `services.AddEncinaMongoDB(connectionString, config => config.UseInbox = true);`
2. Process an inbound message through the Inbox pipeline with non-null `InboxMetadata` supplied.
3. Read back the persisted `InboxMessage` document from MongoDB.
4. Observe the metadata field is empty/null, even though it was supplied.

## Expected Behavior

MongoDB's `InboxMessageFactory.Create(...)` serializes and persists the `InboxMetadata` parameter
the same way the other 8 provider factories do, so metadata round-trips consistently regardless
of provider (Provider Coherence principle, `CLAUDE.md`).

## Actual Behavior

The `metadata` parameter is accepted but discarded; the persisted `InboxMessage` never carries it,
for MongoDB only.

## Environment

- Encina main branch, as of the SPEC-003 audit of #12 (2026-09-25).
- Package: `Encina.MongoDB`.

## Code Sample

```csharp
// src/Encina.MongoDB/Inbox/InboxMessageFactory.cs (current, abbreviated)
public IInboxMessage Create(
    string messageId,
    string requestType,
    DateTime receivedAtUtc,
    DateTime expiresAtUtc,
    InboxMetadata? metadata) // <-- accepted but never used
{
    return new InboxMessage
    {
        MessageId = messageId,
        RequestType = requestType,
        ReceivedAtUtc = receivedAtUtc,
        ExpiresAtUtc = expiresAtUtc,
        // Metadata is missing here
    };
}
```

## Stack Trace

N/A — silent data loss, not an exception.

## Additional Context

Root cause: likely an oversight when the MongoDB factory was written against the same
`IInboxMessageFactory` interface as the other 8 providers during #12's centralization — the
signature was copied but the metadata-serialization line was not. Untested: no assertion in
MongoDB's `InboxMessageFactoryTests.cs` exercises the metadata parameter, so this was never
caught by CI. The fix should also add that missing test assertion (mirroring the ADO/Dapper/EF
Core factory tests) so the regression cannot reappear silently.

## Related Issues

#12 (SPEC-003 audit source)

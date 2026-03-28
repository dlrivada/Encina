CREATE TABLE IF NOT EXISTS ResidencyAuditEntries (
    Id           TEXT    NOT NULL PRIMARY KEY,
    EntityId     TEXT    NULL,
    DataCategory TEXT    NOT NULL,
    SourceRegion TEXT    NOT NULL,
    TargetRegion TEXT    NULL,
    ActionValue  INTEGER NOT NULL,
    OutcomeValue INTEGER NOT NULL,
    LegalBasis   TEXT    NULL,
    RequestType  TEXT    NULL,
    UserId       TEXT    NULL,
    TimestampUtc TEXT    NOT NULL,
    Details      TEXT    NULL
);

CREATE INDEX IF NOT EXISTS IX_ResidencyAuditEntries_EntityId ON ResidencyAuditEntries (EntityId);
CREATE INDEX IF NOT EXISTS IX_ResidencyAuditEntries_TimestampUtc ON ResidencyAuditEntries (TimestampUtc);
CREATE INDEX IF NOT EXISTS IX_ResidencyAuditEntries_OutcomeValue ON ResidencyAuditEntries (OutcomeValue);

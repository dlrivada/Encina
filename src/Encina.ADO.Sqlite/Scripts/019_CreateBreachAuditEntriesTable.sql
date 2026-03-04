CREATE TABLE IF NOT EXISTS BreachAuditEntries (
    Id                TEXT    NOT NULL PRIMARY KEY,
    BreachId          TEXT    NOT NULL,
    Action            TEXT    NOT NULL,
    Detail            TEXT    NULL,
    PerformedByUserId TEXT    NULL,
    OccurredAtUtc     TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_BreachAuditEntries_BreachId ON BreachAuditEntries (BreachId);

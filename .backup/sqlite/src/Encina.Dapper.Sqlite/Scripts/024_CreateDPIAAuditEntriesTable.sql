CREATE TABLE IF NOT EXISTS DPIAAuditEntries (
    Id            TEXT NOT NULL PRIMARY KEY,
    AssessmentId  TEXT NOT NULL,
    Action        TEXT NOT NULL,
    PerformedBy   TEXT NULL,
    OccurredAtUtc TEXT NOT NULL,
    Details       TEXT NULL,
    TenantId      TEXT NULL,
    ModuleId      TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_DPIAAuditEntries_AssessmentId
    ON DPIAAuditEntries (AssessmentId);

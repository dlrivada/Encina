CREATE TABLE IF NOT EXISTS ProcessorAgreementAuditEntries (
    Id                TEXT NOT NULL PRIMARY KEY,
    ProcessorId       TEXT NOT NULL,
    DPAId             TEXT NULL,
    Action            TEXT NOT NULL,
    Detail            TEXT NULL,
    PerformedByUserId TEXT NULL,
    OccurredAtUtc     TEXT NOT NULL,
    TenantId          TEXT NULL,
    ModuleId          TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_ProcessorAgreementAuditEntries_ProcessorId
    ON ProcessorAgreementAuditEntries (ProcessorId);

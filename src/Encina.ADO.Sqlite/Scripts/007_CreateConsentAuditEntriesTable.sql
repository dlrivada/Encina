-- =============================================
-- Create ConsentAuditEntries table for SQLite
-- For GDPR consent audit trail (Article 7(1))
-- =============================================

CREATE TABLE IF NOT EXISTS ConsentAuditEntries
(
    Id TEXT NOT NULL PRIMARY KEY,
    SubjectId TEXT NOT NULL,
    Purpose TEXT NOT NULL,
    Action INTEGER NOT NULL,
    OccurredAtUtc TEXT NOT NULL,
    PerformedBy TEXT NOT NULL,
    IpAddress TEXT NULL,
    Metadata TEXT NOT NULL DEFAULT '{}'
);

CREATE INDEX IF NOT EXISTS IX_ConsentAuditEntries_SubjectId ON ConsentAuditEntries (SubjectId);
CREATE INDEX IF NOT EXISTS IX_ConsentAuditEntries_SubjectId_Purpose ON ConsentAuditEntries (SubjectId, Purpose);
CREATE INDEX IF NOT EXISTS IX_ConsentAuditEntries_OccurredAtUtc ON ConsentAuditEntries (OccurredAtUtc);

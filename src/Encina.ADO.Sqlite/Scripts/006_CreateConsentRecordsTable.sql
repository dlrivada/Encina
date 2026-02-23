-- =============================================
-- Create ConsentRecords table for SQLite
-- For GDPR consent record lifecycle management
-- =============================================

CREATE TABLE IF NOT EXISTS ConsentRecords
(
    Id TEXT NOT NULL PRIMARY KEY,
    SubjectId TEXT NOT NULL,
    Purpose TEXT NOT NULL,
    Status INTEGER NOT NULL,
    ConsentVersionId TEXT NOT NULL,
    GivenAtUtc TEXT NOT NULL,
    WithdrawnAtUtc TEXT NULL,
    ExpiresAtUtc TEXT NULL,
    Source TEXT NOT NULL,
    IpAddress TEXT NULL,
    ProofOfConsent TEXT NULL,
    Metadata TEXT NOT NULL DEFAULT '{}'
);

CREATE INDEX IF NOT EXISTS IX_ConsentRecords_SubjectId ON ConsentRecords (SubjectId);
CREATE INDEX IF NOT EXISTS IX_ConsentRecords_SubjectId_Purpose ON ConsentRecords (SubjectId, Purpose);
CREATE INDEX IF NOT EXISTS IX_ConsentRecords_Status ON ConsentRecords (Status);

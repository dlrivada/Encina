-- =============================================
-- Create DSRRequests table for SQLite
-- For GDPR Data Subject Rights (Articles 15-22)
-- =============================================

CREATE TABLE IF NOT EXISTS DSRRequests
(
    Id                    TEXT NOT NULL PRIMARY KEY,
    SubjectId             TEXT NOT NULL,
    RightTypeValue        INTEGER NOT NULL,
    StatusValue           INTEGER NOT NULL,
    ReceivedAtUtc         TEXT NOT NULL,
    DeadlineAtUtc         TEXT NOT NULL,
    CompletedAtUtc        TEXT NULL,
    ExtensionReason       TEXT NULL,
    ExtendedDeadlineAtUtc TEXT NULL,
    RejectionReason       TEXT NULL,
    RequestDetails        TEXT NULL,
    VerifiedAtUtc         TEXT NULL,
    ProcessedByUserId     TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_DSRRequests_SubjectId ON DSRRequests (SubjectId);
CREATE INDEX IF NOT EXISTS IX_DSRRequests_StatusValue ON DSRRequests (StatusValue);
CREATE INDEX IF NOT EXISTS IX_DSRRequests_RightTypeValue ON DSRRequests (RightTypeValue);
CREATE INDEX IF NOT EXISTS IX_DSRRequests_DeadlineAtUtc ON DSRRequests (DeadlineAtUtc);

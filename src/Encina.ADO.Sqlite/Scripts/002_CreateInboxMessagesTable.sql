-- =============================================
-- Create InboxMessages table
-- For idempotent message processing (exactly-once semantics)
-- =============================================

CREATE TABLE IF NOT EXISTS InboxMessages
(
    MessageId TEXT NOT NULL PRIMARY KEY,
    RequestType TEXT NOT NULL,
    ReceivedAtUtc TEXT NOT NULL,
    ProcessedAtUtc TEXT NULL,
    ExpiresAtUtc TEXT NOT NULL,
    Response TEXT NULL,
    ErrorMessage TEXT NULL,
    RetryCount INTEGER NOT NULL DEFAULT 0,
    NextRetryAtUtc TEXT NULL,
    Metadata TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_InboxMessages_ExpiresAt
    ON InboxMessages (ExpiresAtUtc);

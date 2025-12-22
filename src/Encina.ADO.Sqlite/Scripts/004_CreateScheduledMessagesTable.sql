-- =============================================
-- Create ScheduledMessages table
-- For delayed and recurring command execution
-- =============================================

CREATE TABLE IF NOT EXISTS ScheduledMessages
(
    Id TEXT NOT NULL PRIMARY KEY,
    RequestType TEXT NOT NULL,
    Content TEXT NOT NULL,
    ScheduledAtUtc TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    ProcessedAtUtc TEXT NULL,
    LastExecutedAtUtc TEXT NULL,
    ErrorMessage TEXT NULL,
    RetryCount INTEGER NOT NULL DEFAULT 0,
    NextRetryAtUtc TEXT NULL,
    IsRecurring INTEGER NOT NULL DEFAULT 0,
    CronExpression TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_ScheduledMessages_ScheduledAt
    ON ScheduledMessages (ScheduledAtUtc, ProcessedAtUtc, RetryCount);

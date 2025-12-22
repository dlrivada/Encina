-- =============================================
-- Create ScheduledMessages table
-- For delayed and recurring command execution
-- =============================================

CREATE TABLE ScheduledMessages
(
    Id RAW(16) NOT NULL PRIMARY KEY,
    RequestType VARCHAR2(500) NOT NULL,
    Content CLOB NOT NULL,
    ScheduledAtUtc TIMESTAMP NOT NULL,
    CreatedAtUtc TIMESTAMP NOT NULL,
    ProcessedAtUtc TIMESTAMP NULL,
    LastExecutedAtUtc TIMESTAMP NULL,
    ErrorMessage CLOB NULL,
    RetryCount NUMBER(10) DEFAULT 0 NOT NULL,
    NextRetryAtUtc TIMESTAMP NULL,
    IsRecurring NUMBER(1) DEFAULT 0 NOT NULL,
    CronExpression VARCHAR2(100) NULL
);

CREATE INDEX IX_ScheduledMessages_ScheduledAt ON ScheduledMessages (ScheduledAtUtc, ProcessedAtUtc, RetryCount);

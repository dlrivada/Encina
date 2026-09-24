-- =============================================
-- Create ScheduledMessages table
-- For delayed and recurring command execution
-- =============================================

CREATE TABLE IF NOT EXISTS scheduledmessages
(
    id UUID NOT NULL PRIMARY KEY,
    requesttype VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    scheduledatutc TIMESTAMP NOT NULL,
    createdatutc TIMESTAMP NOT NULL,
    processedatutc TIMESTAMP NULL,
    lastexecutedatutc TIMESTAMP NULL,
    errormessage TEXT NULL,
    retrycount INTEGER NOT NULL DEFAULT 0,
    nextretryatutc TIMESTAMP NULL,
    correlationid VARCHAR(256) NULL,
    metadata TEXT NULL,
    isrecurring BOOLEAN NOT NULL DEFAULT FALSE,
    cronexpression VARCHAR(100) NULL
);

CREATE INDEX IF NOT EXISTS ix_scheduledmessages_scheduledat_processed
    ON scheduledmessages (scheduledatutc, processedatutc, retrycount)
    INCLUDE (nextretryatutc, isrecurring);

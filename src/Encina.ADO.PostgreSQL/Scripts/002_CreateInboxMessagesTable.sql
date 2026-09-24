-- =============================================
-- Create InboxMessages table
-- For idempotent message processing (exactly-once semantics)
-- =============================================

CREATE TABLE IF NOT EXISTS inboxmessages (
    messageid TEXT NOT NULL PRIMARY KEY,
    requesttype TEXT NOT NULL,
    receivedatutc TIMESTAMP NOT NULL,
    processedatutc TIMESTAMP NULL,
    expiresatutc TIMESTAMP NOT NULL,
    response TEXT NULL,
    errormessage TEXT NULL,
    retrycount INTEGER NOT NULL DEFAULT 0,
    nextretryatutc TIMESTAMP NULL,
    metadata TEXT NULL
);

CREATE INDEX IF NOT EXISTS ix_inboxmessages_expiresat ON inboxmessages (expiresatutc) WHERE processedatutc IS NOT NULL;

-- =============================================
-- Create dsrauditentries table
-- For GDPR Data Subject Rights audit trail
-- =============================================

CREATE TABLE dsrauditentries (
    id                VARCHAR(36)  NOT NULL PRIMARY KEY,
    dsrrequestid      VARCHAR(36)  NOT NULL,
    action            VARCHAR(256) NOT NULL,
    detail            TEXT         NULL,
    performedbyuserid VARCHAR(256) NULL,
    occurredatutc     TIMESTAMPTZ  NOT NULL
);

CREATE INDEX ix_dsrauditentries_dsrrequestid ON dsrauditentries (dsrrequestid);
CREATE INDEX ix_dsrauditentries_occurredatutc ON dsrauditentries (occurredatutc);

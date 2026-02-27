-- =============================================
-- Create dsrrequests table
-- For GDPR Data Subject Rights (Articles 15-22)
-- =============================================

CREATE TABLE dsrrequests (
    id                    VARCHAR(36)   NOT NULL PRIMARY KEY,
    subjectid             VARCHAR(256)  NOT NULL,
    righttypevalue         INT           NOT NULL,
    statusvalue           INT           NOT NULL,
    receivedatutc         TIMESTAMPTZ   NOT NULL,
    deadlineatutc         TIMESTAMPTZ   NOT NULL,
    completedatutc        TIMESTAMPTZ   NULL,
    extensionreason       VARCHAR(1024) NULL,
    extendeddeadlineatutc TIMESTAMPTZ   NULL,
    rejectionreason       VARCHAR(1024) NULL,
    requestdetails        TEXT          NULL,
    verifiedatutc         TIMESTAMPTZ   NULL,
    processedbyuserid     VARCHAR(256)  NULL
);

CREATE INDEX ix_dsrrequests_subjectid ON dsrrequests (subjectid);
CREATE INDEX ix_dsrrequests_statusvalue ON dsrrequests (statusvalue);
CREATE INDEX ix_dsrrequests_righttypevalue ON dsrrequests (righttypevalue);
CREATE INDEX ix_dsrrequests_deadlineatutc ON dsrrequests (deadlineatutc);

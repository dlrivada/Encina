CREATE TABLE dpiaassessments (
    id                  VARCHAR(36)   NOT NULL PRIMARY KEY,
    requesttypename     TEXT          NOT NULL,
    statusvalue         INT           NOT NULL,
    processingtype      VARCHAR(256)  NULL,
    reason              TEXT          NULL,
    resultjson          JSONB         NULL,
    dpoconsultationjson JSONB         NULL,
    createdatutc        TIMESTAMPTZ   NOT NULL,
    approvedatutc       TIMESTAMPTZ   NULL,
    nextreviewatutc     TIMESTAMPTZ   NULL,
    tenantid            VARCHAR(256)  NULL,
    moduleid            VARCHAR(256)  NULL
);

CREATE UNIQUE INDEX ux_dpiaassessments_requesttypename ON dpiaassessments (requesttypename);
CREATE INDEX ix_dpiaassessments_statusvalue ON dpiaassessments (statusvalue);
CREATE INDEX ix_dpiaassessments_nextreviewatutc ON dpiaassessments (nextreviewatutc);

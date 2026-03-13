CREATE TABLE IF NOT EXISTS processoragreementauditentries (
    id                  VARCHAR(256)  NOT NULL PRIMARY KEY,
    processorid         VARCHAR(256)  NOT NULL,
    dpaid               VARCHAR(256)  NULL,
    action              VARCHAR(256)  NOT NULL,
    detail              TEXT          NULL,
    performedbyuserid   VARCHAR(256)  NULL,
    occurredatutc       TIMESTAMPTZ   NOT NULL,
    tenantid            VARCHAR(256)  NULL,
    moduleid            VARCHAR(256)  NULL
);

CREATE INDEX IF NOT EXISTS ix_processoragreementauditentries_processorid ON processoragreementauditentries (processorid);

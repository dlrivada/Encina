CREATE TABLE IF NOT EXISTS readauditentries (
    id              UUID          NOT NULL PRIMARY KEY,
    entitytype      VARCHAR(256)  NOT NULL,
    entityid        VARCHAR(256)  NULL,
    userid          VARCHAR(256)  NULL,
    tenantid        VARCHAR(128)  NULL,
    accessedatutc   TIMESTAMPTZ   NOT NULL,
    correlationid   VARCHAR(256)  NULL,
    purpose         VARCHAR(1024) NULL,
    accessmethod    INTEGER       NOT NULL DEFAULT 0,
    entitycount     INTEGER       NOT NULL DEFAULT 0,
    metadata        TEXT          NULL
);

CREATE INDEX IF NOT EXISTS ix_readauditentries_entity ON readauditentries (entitytype, entityid);
CREATE INDEX IF NOT EXISTS ix_readauditentries_accessedat ON readauditentries (accessedatutc);
CREATE INDEX IF NOT EXISTS ix_readauditentries_userid ON readauditentries (userid) WHERE userid IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_readauditentries_tenantid ON readauditentries (tenantid) WHERE tenantid IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_readauditentries_correlationid ON readauditentries (correlationid) WHERE correlationid IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_readauditentries_accessmethod ON readauditentries (accessmethod);

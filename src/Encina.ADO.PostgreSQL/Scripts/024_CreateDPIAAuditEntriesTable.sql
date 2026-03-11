CREATE TABLE dpiaauditentries (
    id            VARCHAR(36)   NOT NULL PRIMARY KEY,
    assessmentid  VARCHAR(36)   NOT NULL,
    action        VARCHAR(256)  NOT NULL,
    performedby   VARCHAR(256)  NULL,
    occurredatutc TIMESTAMPTZ   NOT NULL,
    details       TEXT          NULL,
    tenantid      VARCHAR(256)  NULL,
    moduleid      VARCHAR(256)  NULL
);

CREATE INDEX ix_dpiaauditentries_assessmentid ON dpiaauditentries (assessmentid);

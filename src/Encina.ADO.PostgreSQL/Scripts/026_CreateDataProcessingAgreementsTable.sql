CREATE TABLE IF NOT EXISTS dataprocessingagreements (
    id                                VARCHAR(256)  NOT NULL PRIMARY KEY,
    processorid                       VARCHAR(256)  NOT NULL,
    statusvalue                       INTEGER       NOT NULL,
    signedatutc                       TIMESTAMPTZ   NOT NULL,
    expiresatutc                      TIMESTAMPTZ   NULL,
    hassccs                           BOOLEAN       NOT NULL,
    processingpurposesjson            TEXT          NOT NULL,
    processondocumentedinstructions   BOOLEAN       NOT NULL,
    confidentialityobligations        BOOLEAN       NOT NULL,
    securitymeasures                  BOOLEAN       NOT NULL,
    subprocessorrequirements          BOOLEAN       NOT NULL,
    datasubjectrightsassistance       BOOLEAN       NOT NULL,
    complianceassistance              BOOLEAN       NOT NULL,
    datadeletionorreturn              BOOLEAN       NOT NULL,
    auditrights                       BOOLEAN       NOT NULL,
    tenantid                          VARCHAR(256)  NULL,
    moduleid                          VARCHAR(256)  NULL,
    createdatutc                      TIMESTAMPTZ   NOT NULL,
    lastupdatedatutc                  TIMESTAMPTZ   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_dataprocessingagreements_processorid ON dataprocessingagreements (processorid);
CREATE INDEX IF NOT EXISTS ix_dataprocessingagreements_statusvalue ON dataprocessingagreements (statusvalue);
CREATE INDEX IF NOT EXISTS ix_dataprocessingagreements_expiresatutc ON dataprocessingagreements (expiresatutc);
CREATE INDEX IF NOT EXISTS ix_dataprocessingagreements_tenantid ON dataprocessingagreements (tenantid);

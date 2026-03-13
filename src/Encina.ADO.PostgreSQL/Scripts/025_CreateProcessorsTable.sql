CREATE TABLE IF NOT EXISTS processors (
    id                                    VARCHAR(256)  NOT NULL PRIMARY KEY,
    name                                  VARCHAR(450)  NOT NULL,
    country                               VARCHAR(100)  NOT NULL,
    contactemail                          VARCHAR(256)  NULL,
    parentprocessorid                     VARCHAR(256)  NULL,
    depth                                 INTEGER       NOT NULL,
    subprocessorauthorizationtypevalue    INTEGER       NOT NULL,
    tenantid                              VARCHAR(256)  NULL,
    moduleid                              VARCHAR(256)  NULL,
    createdatutc                          TIMESTAMPTZ   NOT NULL,
    lastupdatedatutc                      TIMESTAMPTZ   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_processors_parentprocessorid ON processors (parentprocessorid);
CREATE INDEX IF NOT EXISTS ix_processors_tenantid ON processors (tenantid);

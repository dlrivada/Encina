-- Creates the processingactivities table for GDPR Article 30 Records of Processing Activities (RoPA).
-- Each row represents a registered processing activity linked to an Encina request type.

CREATE TABLE processingactivities (
    id                             VARCHAR(36)   NOT NULL PRIMARY KEY,
    requesttypename                VARCHAR(1000) NOT NULL,
    name                           VARCHAR(500)  NOT NULL,
    purpose                        TEXT          NOT NULL,
    lawfulbasisvalue               INT           NOT NULL,
    categoriesofdatasubjectsjson   TEXT          NOT NULL,
    categoriesofpersonaldatajson   TEXT          NOT NULL,
    recipientsjson                 TEXT          NOT NULL,
    thirdcountrytransfers          TEXT          NULL,
    safeguards                     TEXT          NULL,
    retentionperiodticks           BIGINT        NOT NULL,
    securitymeasures               TEXT          NOT NULL,
    createdatutc                   TIMESTAMPTZ   NOT NULL,
    lastupdatedatutc               TIMESTAMPTZ   NOT NULL,
    CONSTRAINT uq_processingactivities_requesttypename UNIQUE (requesttypename)
);

CREATE INDEX ix_processingactivities_lawfulbasisvalue ON processingactivities (lawfulbasisvalue);
CREATE INDEX ix_processingactivities_createdatutc ON processingactivities (createdatutc);

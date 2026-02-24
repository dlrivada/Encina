-- Creates the liarecords table for Legitimate Interest Assessment records.
-- Each row represents a documented balancing test under GDPR Article 6(1)(f).

CREATE TABLE liarecords (
    id                         VARCHAR(256)  NOT NULL PRIMARY KEY,
    name                       VARCHAR(512)  NOT NULL,
    purpose                    VARCHAR(1024) NOT NULL,
    legitimateinterest         TEXT          NOT NULL,
    benefits                   TEXT          NOT NULL,
    consequencesifnotprocessed TEXT          NOT NULL,
    necessityjustification     TEXT          NOT NULL,
    alternativesconsideredjson TEXT          NOT NULL,
    dataminimisationnotes      TEXT          NOT NULL,
    natureofdata               TEXT          NOT NULL,
    reasonableexpectations     TEXT          NOT NULL,
    impactassessment           TEXT          NOT NULL,
    safeguardsjson             TEXT          NOT NULL,
    outcomevalue               INT           NOT NULL,
    conclusion                 TEXT          NOT NULL,
    conditions                 TEXT          NULL,
    assessedatutc              TIMESTAMPTZ   NOT NULL,
    assessedby                 VARCHAR(256)  NOT NULL,
    dpoinvolvement             BOOLEAN       NOT NULL DEFAULT FALSE,
    nextreviewatutc            TIMESTAMPTZ   NULL
);

CREATE INDEX ix_liarecords_outcomevalue ON liarecords(outcomevalue);

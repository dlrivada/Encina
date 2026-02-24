-- Creates the lawfulbasisregistrations table for GDPR Article 6 lawful basis tracking.
-- Each row represents a lawful basis declaration for a specific request type.

CREATE TABLE lawfulbasisregistrations (
    id                VARCHAR(36)   NOT NULL PRIMARY KEY,
    requesttypename   VARCHAR(512)  NOT NULL,
    basisvalue        INT           NOT NULL,
    purpose           VARCHAR(1024) NULL,
    liareference      VARCHAR(256)  NULL,
    legalreference    VARCHAR(256)  NULL,
    contractreference VARCHAR(256)  NULL,
    registeredatutc   TIMESTAMPTZ   NOT NULL,
    CONSTRAINT uq_lawfulbasisregistrations_requesttypename UNIQUE (requesttypename)
);

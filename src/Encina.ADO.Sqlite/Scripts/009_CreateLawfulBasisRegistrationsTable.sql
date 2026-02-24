-- Creates the LawfulBasisRegistrations table for GDPR Article 6 lawful basis tracking.
-- Each row represents a lawful basis declaration for a specific request type.

CREATE TABLE IF NOT EXISTS LawfulBasisRegistrations
(
    Id                TEXT NOT NULL PRIMARY KEY,
    RequestTypeName   TEXT NOT NULL,
    BasisValue        INTEGER NOT NULL,
    Purpose           TEXT NULL,
    LIAReference      TEXT NULL,
    LegalReference    TEXT NULL,
    ContractReference TEXT NULL,
    RegisteredAtUtc   TEXT NOT NULL,
    CONSTRAINT UQ_LawfulBasisRegistrations_RequestTypeName UNIQUE (RequestTypeName)
);

-- Creates the LawfulBasisRegistrations table for GDPR Article 6 lawful basis tracking.
-- Each row represents a lawful basis declaration for a specific request type.

CREATE TABLE `LawfulBasisRegistrations` (
    `Id`                VARCHAR(36)   NOT NULL PRIMARY KEY,
    `RequestTypeName`   VARCHAR(512)  NOT NULL,
    `BasisValue`        INT           NOT NULL,
    `Purpose`           VARCHAR(1024) NULL,
    `LIAReference`      VARCHAR(256)  NULL,
    `LegalReference`    VARCHAR(256)  NULL,
    `ContractReference` VARCHAR(256)  NULL,
    `RegisteredAtUtc`   DATETIME(6)   NOT NULL,
    UNIQUE KEY `UQ_LawfulBasisRegistrations_RequestTypeName` (`RequestTypeName`)
);

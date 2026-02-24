-- Creates the LawfulBasisRegistrations table for GDPR Article 6 lawful basis tracking.
-- Each row represents a lawful basis declaration for a specific request type.

CREATE TABLE [LawfulBasisRegistrations] (
    [Id]                NVARCHAR(36)   NOT NULL PRIMARY KEY,
    [RequestTypeName]   NVARCHAR(512)  NOT NULL,
    [BasisValue]        INT            NOT NULL,
    [Purpose]           NVARCHAR(1024) NULL,
    [LIAReference]      NVARCHAR(256)  NULL,
    [LegalReference]    NVARCHAR(256)  NULL,
    [ContractReference] NVARCHAR(256)  NULL,
    [RegisteredAtUtc]   DATETIME2(7)   NOT NULL,
    CONSTRAINT UQ_LawfulBasisRegistrations_RequestTypeName UNIQUE ([RequestTypeName])
);

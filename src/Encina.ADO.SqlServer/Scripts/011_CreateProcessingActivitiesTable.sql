-- Creates the ProcessingActivities table for GDPR Article 30 Records of Processing Activities (RoPA).
-- Each row represents a registered processing activity linked to an Encina request type.

CREATE TABLE [ProcessingActivities] (
    [Id]                             NVARCHAR(36)    NOT NULL PRIMARY KEY,
    [RequestTypeName]                NVARCHAR(1000)  NOT NULL,
    [Name]                           NVARCHAR(500)   NOT NULL,
    [Purpose]                        NVARCHAR(MAX)   NOT NULL,
    [LawfulBasisValue]               INT             NOT NULL,
    [CategoriesOfDataSubjectsJson]   NVARCHAR(MAX)   NOT NULL,
    [CategoriesOfPersonalDataJson]   NVARCHAR(MAX)   NOT NULL,
    [RecipientsJson]                 NVARCHAR(MAX)   NOT NULL,
    [ThirdCountryTransfers]          NVARCHAR(MAX)   NULL,
    [Safeguards]                     NVARCHAR(MAX)   NULL,
    [RetentionPeriodTicks]           BIGINT          NOT NULL,
    [SecurityMeasures]               NVARCHAR(MAX)   NOT NULL,
    [CreatedAtUtc]                   DATETIME2(7)    NOT NULL,
    [LastUpdatedAtUtc]               DATETIME2(7)    NOT NULL,
    CONSTRAINT UQ_ProcessingActivities_RequestTypeName UNIQUE ([RequestTypeName])
);

CREATE INDEX IX_ProcessingActivities_LawfulBasisValue ON [ProcessingActivities] ([LawfulBasisValue]);
CREATE INDEX IX_ProcessingActivities_CreatedAtUtc ON [ProcessingActivities] ([CreatedAtUtc]);

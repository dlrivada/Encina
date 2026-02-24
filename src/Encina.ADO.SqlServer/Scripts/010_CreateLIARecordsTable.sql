-- Creates the LIARecords table for Legitimate Interest Assessment records.
-- Each row represents a documented balancing test under GDPR Article 6(1)(f).

CREATE TABLE [LIARecords] (
    [Id]                         NVARCHAR(256)  NOT NULL PRIMARY KEY,
    [Name]                       NVARCHAR(512)  NOT NULL,
    [Purpose]                    NVARCHAR(1024) NOT NULL,
    [LegitimateInterest]         NVARCHAR(MAX)  NOT NULL,
    [Benefits]                   NVARCHAR(MAX)  NOT NULL,
    [ConsequencesIfNotProcessed] NVARCHAR(MAX)  NOT NULL,
    [NecessityJustification]     NVARCHAR(MAX)  NOT NULL,
    [AlternativesConsideredJson] NVARCHAR(MAX)  NOT NULL,
    [DataMinimisationNotes]      NVARCHAR(MAX)  NOT NULL,
    [NatureOfData]               NVARCHAR(MAX)  NOT NULL,
    [ReasonableExpectations]     NVARCHAR(MAX)  NOT NULL,
    [ImpactAssessment]           NVARCHAR(MAX)  NOT NULL,
    [SafeguardsJson]             NVARCHAR(MAX)  NOT NULL,
    [OutcomeValue]               INT            NOT NULL,
    [Conclusion]                 NVARCHAR(MAX)  NOT NULL,
    [Conditions]                 NVARCHAR(MAX)  NULL,
    [AssessedAtUtc]              DATETIME2(7)   NOT NULL,
    [AssessedBy]                 NVARCHAR(256)  NOT NULL,
    [DPOInvolvement]             BIT            NOT NULL DEFAULT 0,
    [NextReviewAtUtc]            DATETIME2(7)   NULL
);

CREATE INDEX IX_LIARecords_OutcomeValue ON [LIARecords]([OutcomeValue]);

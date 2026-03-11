CREATE TABLE [dbo].[DPIAAssessments] (
    [Id]                  NVARCHAR(36)   NOT NULL PRIMARY KEY,
    [RequestTypeName]     NVARCHAR(450)  NOT NULL,
    [StatusValue]         INT            NOT NULL,
    [ProcessingType]      NVARCHAR(256)  NULL,
    [Reason]              NVARCHAR(MAX)  NULL,
    [ResultJson]          NVARCHAR(MAX)  NULL,
    [DPOConsultationJson] NVARCHAR(MAX)  NULL,
    [CreatedAtUtc]        DATETIME2(7)   NOT NULL,
    [ApprovedAtUtc]       DATETIME2(7)   NULL,
    [NextReviewAtUtc]     DATETIME2(7)   NULL,
    [TenantId]            NVARCHAR(256)  NULL,
    [ModuleId]            NVARCHAR(256)  NULL,
    INDEX [IX_DPIAAssessments_StatusValue] ([StatusValue]),
    INDEX [IX_DPIAAssessments_NextReviewAtUtc] ([NextReviewAtUtc])
);
GO

CREATE UNIQUE INDEX [UX_DPIAAssessments_RequestTypeName]
    ON [dbo].[DPIAAssessments] ([RequestTypeName]);
GO

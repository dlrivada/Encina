-- =============================================
-- Create SagaStates table
-- For distributed transaction orchestration with compensation
-- =============================================

CREATE TABLE [dbo].[SagaStates]
(
    [SagaId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [SagaType] NVARCHAR(500) NOT NULL,
    [Data] NVARCHAR(MAX) NOT NULL,
    [Status] INT NOT NULL, -- 0=Running, 1=Completed, 2=Failed, 3=Compensating, 4=Compensated
    [StartedAtUtc] DATETIME2(7) NOT NULL,
    [LastUpdatedAtUtc] DATETIME2(7) NOT NULL,
    [CompletedAtUtc] DATETIME2(7) NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [CurrentStep] INT NOT NULL DEFAULT 0,

    INDEX [IX_SagaStates_Status_LastUpdated]
        ([Status], [LastUpdatedAtUtc])
);
GO

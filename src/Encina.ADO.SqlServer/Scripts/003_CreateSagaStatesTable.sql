-- =============================================
-- Create SagaStates table
-- For distributed transaction orchestration with compensation
-- =============================================

CREATE TABLE [dbo].[SagaStates]
(
    [SagaId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [SagaType] NVARCHAR(500) NOT NULL,
    [Data] NVARCHAR(MAX) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL, -- Running, Completed, Failed, Compensating, Compensated
    [StartedAtUtc] DATETIME2(7) NOT NULL,
    [LastUpdatedAtUtc] DATETIME2(7) NOT NULL,
    [CompletedAtUtc] DATETIME2(7) NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [CurrentStep] INT NOT NULL DEFAULT 0,
    [TimeoutAtUtc] DATETIME2(7) NULL,
    [CorrelationId] NVARCHAR(256) NULL,
    [Metadata] NVARCHAR(MAX) NULL,

    INDEX [IX_SagaStates_Status_LastUpdated]
        ([Status], [LastUpdatedAtUtc])
);
GO

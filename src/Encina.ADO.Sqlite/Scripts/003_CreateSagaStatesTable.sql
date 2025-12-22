-- =============================================
-- Create SagaStates table
-- For distributed transaction orchestration with compensation
-- Status values: 0=Running, 1=Completed, 2=Failed, 3=Compensating, 4=Compensated
-- =============================================

CREATE TABLE IF NOT EXISTS SagaStates
(
    SagaId TEXT NOT NULL PRIMARY KEY,
    SagaType TEXT NOT NULL,
    Data TEXT NOT NULL,
    Status INTEGER NOT NULL,
    StartedAtUtc TEXT NOT NULL,
    LastUpdatedAtUtc TEXT NOT NULL,
    CompletedAtUtc TEXT NULL,
    ErrorMessage TEXT NULL,
    CurrentStep INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS IX_SagaStates_Status
    ON SagaStates (Status, LastUpdatedAtUtc);

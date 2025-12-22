-- =============================================
-- Create SagaStates table
-- For distributed transaction orchestration with compensation
-- Status values: 0=Running, 1=Completed, 2=Failed, 3=Compensating, 4=Compensated
-- =============================================

CREATE TABLE SagaStates
(
    SagaId RAW(16) NOT NULL PRIMARY KEY,
    SagaType VARCHAR2(500) NOT NULL,
    Data CLOB NOT NULL,
    Status NUMBER(10) NOT NULL,
    StartedAtUtc TIMESTAMP NOT NULL,
    LastUpdatedAtUtc TIMESTAMP NOT NULL,
    CompletedAtUtc TIMESTAMP NULL,
    ErrorMessage CLOB NULL,
    CurrentStep NUMBER(10) DEFAULT 0 NOT NULL
);

CREATE INDEX IX_SagaStates_Status ON SagaStates (Status, LastUpdatedAtUtc);

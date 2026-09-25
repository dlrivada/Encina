-- =============================================
-- Create SagaStates table
-- For distributed transaction orchestration with compensation
-- =============================================

CREATE TABLE IF NOT EXISTS sagastates
(
    sagaid UUID NOT NULL PRIMARY KEY,
    sagatype TEXT NOT NULL,
    data TEXT NOT NULL,
    status VARCHAR(50) NOT NULL, -- Running, Completed, Failed, Compensating, Compensated
    startedatutc TIMESTAMP NOT NULL,
    lastupdatedatutc TIMESTAMP NOT NULL,
    completedatutc TIMESTAMP NULL,
    errormessage TEXT NULL,
    currentstep INTEGER NOT NULL DEFAULT 0,
    timeoutatutc TIMESTAMP NULL,
    correlationid VARCHAR(256) NULL,
    metadata TEXT NULL
);

CREATE INDEX IF NOT EXISTS ix_sagastates_status_lastupdated ON sagastates (status, lastupdatedatutc);

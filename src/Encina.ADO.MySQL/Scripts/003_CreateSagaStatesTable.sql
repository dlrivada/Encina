-- =============================================
-- Create SagaStates table
-- For distributed transaction orchestration with compensation
-- =============================================

CREATE TABLE IF NOT EXISTS `SagaStates`
(
    `SagaId` CHAR(36) NOT NULL,
    `SagaType` VARCHAR(500) NOT NULL,
    `Data` LONGTEXT NOT NULL,
    `Status` VARCHAR(50) NOT NULL, -- Running, Completed, Failed, Compensating, Compensated
    `StartedAtUtc` DATETIME(6) NOT NULL,
    `LastUpdatedAtUtc` DATETIME(6) NOT NULL,
    `CompletedAtUtc` DATETIME(6) NULL,
    `ErrorMessage` LONGTEXT NULL,
    `CurrentStep` INT NOT NULL DEFAULT 0,
    `TimeoutAtUtc` DATETIME(6) NULL,
    `CorrelationId` VARCHAR(256) NULL,
    `Metadata` TEXT NULL,

    PRIMARY KEY (`SagaId`),
    INDEX `IX_SagaStates_Status_LastUpdated` (`Status`, `LastUpdatedAtUtc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

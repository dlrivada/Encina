-- =============================================
-- Encina.Dapper - Complete Database Schema (MySQL/MariaDB)
-- Run this script to create all messaging pattern tables
-- =============================================

-- =============================================
-- Create OutboxMessages table
-- For reliable event publishing (at-least-once delivery)
-- =============================================

CREATE TABLE IF NOT EXISTS `OutboxMessages` (
    `Id` CHAR(36) NOT NULL,
    `NotificationType` VARCHAR(500) NOT NULL,
    `Content` LONGTEXT NOT NULL,
    `CreatedAtUtc` DATETIME(6) NOT NULL,
    `ProcessedAtUtc` DATETIME(6) NULL,
    `ErrorMessage` LONGTEXT NULL,
    `RetryCount` INT NOT NULL DEFAULT 0,
    `NextRetryAtUtc` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_OutboxMessages_ProcessedAt_RetryCount` (`ProcessedAtUtc`, `RetryCount`, `NextRetryAtUtc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =============================================
-- Create InboxMessages table
-- For idempotent message processing (exactly-once semantics)
-- =============================================
-- Note: MySQL has no partial/filtered index equivalent to SQL Server's
-- "WHERE ProcessedAtUtc IS NOT NULL"; ExpiresAtUtc is indexed unconditionally.

CREATE TABLE IF NOT EXISTS `InboxMessages`
(
    `MessageId` VARCHAR(255) NOT NULL,
    `RequestType` VARCHAR(500) NOT NULL,
    `ReceivedAtUtc` DATETIME(6) NOT NULL,
    `ProcessedAtUtc` DATETIME(6) NULL,
    `ExpiresAtUtc` DATETIME(6) NOT NULL,
    `Response` LONGTEXT NULL,
    `ErrorMessage` LONGTEXT NULL,
    `RetryCount` INT NOT NULL DEFAULT 0,
    `NextRetryAtUtc` DATETIME(6) NULL,
    `Metadata` LONGTEXT NULL,
    PRIMARY KEY (`MessageId`),
    INDEX `IX_InboxMessages_ExpiresAt` (`ExpiresAtUtc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

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

-- =============================================
-- Create ScheduledMessages table
-- For delayed and recurring command execution
-- =============================================

CREATE TABLE IF NOT EXISTS `ScheduledMessages`
(
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `RequestType` VARCHAR(500) NOT NULL,
    `Content` LONGTEXT NOT NULL,
    `ScheduledAtUtc` DATETIME(6) NOT NULL,
    `CreatedAtUtc` DATETIME(6) NOT NULL,
    `ProcessedAtUtc` DATETIME(6) NULL,
    `LastExecutedAtUtc` DATETIME(6) NULL,
    `ErrorMessage` LONGTEXT NULL,
    `RetryCount` INT NOT NULL DEFAULT 0,
    `NextRetryAtUtc` DATETIME(6) NULL,
    `CorrelationId` VARCHAR(256) NULL,
    `Metadata` TEXT NULL,
    `IsRecurring` TINYINT(1) NOT NULL DEFAULT 0,
    `CronExpression` VARCHAR(100) NULL,

    INDEX `IX_ScheduledMessages_ScheduledAt_Processed` (`ScheduledAtUtc`, `ProcessedAtUtc`, `RetryCount`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =============================================
-- Create AuditLogs table for MySQL/MariaDB
-- For audit trail tracking
-- =============================================

CREATE TABLE IF NOT EXISTS `AuditLogs`
(
    `Id` CHAR(36) NOT NULL,
    `EntityType` VARCHAR(256) NOT NULL,
    `EntityId` VARCHAR(256) NOT NULL,
    `Action` INT NOT NULL,
    `UserId` VARCHAR(256) NULL,
    `TimestampUtc` DATETIME(6) NOT NULL,
    `OldValues` LONGTEXT NULL,
    `NewValues` LONGTEXT NULL,
    `CorrelationId` VARCHAR(256) NULL,

    PRIMARY KEY (`Id`),

    INDEX `IX_AuditLogs_Entity` (`EntityType`, `EntityId`),
    INDEX `IX_AuditLogs_Timestamp` (`TimestampUtc`),
    INDEX `IX_AuditLogs_UserId` (`UserId`),
    INDEX `IX_AuditLogs_CorrelationId` (`CorrelationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
